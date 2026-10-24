using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Onion.SceneManagement.Setting;

namespace Onion.SceneManagement.Transition {
    public sealed class Transition {
        // A typical transition has 4-6 actions; the collections grow when needed.
        private const int initialCapacity = 4;

        private readonly List<SceneReference> _destinations = new(initialCapacity);
        private readonly List<TransitionAction> _actions = new(initialCapacity);
        private readonly Queue<Awaitable> _parallels = new(initialCapacity);

        private readonly List<SceneReference> _toLoad = new(initialCapacity);
        private readonly List<Scene> _toUnload = new(initialCapacity);
        private readonly Stack<TransitionAction> _actionStack = new(initialCapacity);

        // The loaders Load created, so Activate and Enter reach the instance this transition loaded,
        // not the first loaded instance of the same scene (LoadMode.Additive).
        private readonly List<SceneLoader> _loaders = new(initialCapacity);

        // Set only by CreateUnload; most transitions leave it empty (no array allocated).
        private readonly List<Scene> _unloadTargets = new();

        private bool _isUsed = false;
        private TransitionMode _mode = TransitionMode.Default;

        // Transitions run one at a time, in the order they were started (see ExecuteAsync).
        private static int _running;
        private static int _awaitingUserCode;
        private static int _nextTicket;
        private static int _nowServing;

        // A Play session that ended mid-transition would otherwise leave the queue blocked when domain reload is off.
        // Internal so tests can also reset it between tests that run in one Play session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStatics() {
            _running = 0;
            _awaitingUserCode = 0;
            _nextTicket = 0;
            _nowServing = 0;
        }

        // Marks async user code (handlers, callbacks, conditions) a running transition is awaiting.
        internal static void BeginUserCode() => _awaitingUserCode++;
        internal static void EndUserCode() => _awaitingUserCode--;

        internal Transition() { }

        public static Transition Create(IEnumerable<SceneReference> destinations) {
            return new Transition()
                .To(destinations);
        }

        public static Transition Create(SceneReference destination) {
            return new Transition()
                .To(destination);
        }

        public static Transition Create(IEnumerable<SceneReference> destinations, TransitionMode mode) {
            return new Transition()
                .To(destinations)
                .Mode(mode);
        }

        public static Transition Create(SceneReference destination, TransitionMode mode) {
            return new Transition()
                .To(destination)
                .Mode(mode);
        }

        // Keeps action.parallel as the caller set it (e.g. Callback(..., parallel: true)).
        // Unloads exactly the targets that are still loaded when it runs, and loads nothing.
        internal static Transition CreateUnload(IEnumerable<Scene> targets) {
            var transition = new Transition();
            transition._unloadTargets.AddRange(targets);

            return transition;
        }

        internal Transition Add(TransitionAction action) {
            _actions.Add(action);

            return this;
        }

        internal Transition To(IEnumerable<SceneReference> references) {
            _destinations.AddRange(references);

            return this;
        }

        internal Transition To(SceneReference reference) {
            _destinations.Add(reference);

            return this;
        }

        internal Transition Mode(TransitionMode mode) {
            _mode = mode;

            return this;
        }

        internal void Clear() {
            _destinations.Clear();
            _actions.Clear();
            _parallels.Clear();
            _loaders.Clear();
            _unloadTargets.Clear();
        }

        public void Execute(bool queued = true) {
            _ = ExecuteAsync(queued);
        }

        /// <summary>
        /// Runs the transition after the transitions started before it have finished.
        /// With <paramref name="queued"/> false, it starts right away instead, alongside any running transition
        /// (its steps still run as usual); use that only for a transition that can't conflict with them,
        /// e.g. one that doesn't unload. A transition started from a handler or callback the running
        /// transition awaits starts right away either way, so awaiting it doesn't deadlock.
        /// </summary>
        public async Awaitable ExecuteAsync(bool queued = true) {
            if (_isUsed) {
                Debug.LogWarning("This transition has already been used. Creating a new transition for reuse.");
                return;
            }

            _isUsed = true;

            // While a running transition awaits a handler or callback, that code may be awaiting this
            // transition, so waiting for the running one would deadlock: this one runs right away.
            queued = queued && (_running == 0 || _awaitingUserCode == 0);
            if (queued) {
                int ticket = _nextTicket++;
                while (ticket != _nowServing || _running > 0) {
                    await Awaitable.NextFrameAsync(Application.exitCancellationToken);
                }
            }

            _running++;
            try {
                Prepare();
                
                for (int i = _actions.Count - 1; i >= 0; i--) {
                    _actionStack.Push(_actions[i]);
                }
                
                while (_actionStack.Count > 0) {
                    var action = _actionStack.Pop();

                    if (!action.parallel) {
                        await WaitForParallels();
                    }

                    if (action.batch) { // batch is normally not parellel.
                        var batchActions = ListPool<TransitionAction>.Get();
                        
                        if      (action.type == TransitionActionType.Load)     CreateLoadBatch(batchActions, action.activateOnLoad);
                        else if (action.type == TransitionActionType.Activate) CreateActivateBatch(batchActions);
                        else if (action.type == TransitionActionType.Unload)   CreateUnloadBatch(batchActions);
                        else if (action.type == TransitionActionType.Enter)    CreateEnterBatch(batchActions);
                        else if (action.type == TransitionActionType.Exit)     CreateExitBatch(batchActions);
                        else {
                            Debug.LogWarning($"Batch action of type {action.type} is not supported. Skipping batch.");
                        }

                        for (int i = batchActions.Count - 1; i >= 0; i--) {
                            _actionStack.Push(batchActions[i]);
                        }
                        ListPool<TransitionAction>.Release(batchActions);

                        continue;    
                    }

                    var awaitable = Execute_Internal(action);
                    if (awaitable != null) {                   
                        if (action.parallel) {
                            _parallels.Enqueue(awaitable);
                        }
                        else {
                            await awaitable;
                        }
                    }
                }

                await WaitForParallels();
            }
            finally {
                _running--;
                if (queued) {
                    _nowServing++;
                }

                // Not pooled: a caller may still hold this instance, and a pooled one could later
                // pass the _isUsed check while running another caller's transition.
                Clear();
            }
        }

        private void Prepare() {
            _toLoad.Clear();
            _toUnload.Clear();

            // No destinations is a valid target state: with CleanUp, everything that isn't pinned is unloaded.
            var loadedScenes = SceneManager.loadedScenes;

            if (_unloadTargets.Count > 0) {
                // Resolved here, not when the unload was requested: a transition queued ahead of
                // this one may have loaded or unloaded scenes since.
                foreach (var scene in _unloadTargets) {
                    if (scene.isLoaded) {
                        _toUnload.Add(scene);
                    }
                }
            }
            else if (_mode.unloadMode == UnloadMode.CleanUp) {
                _toUnload.AddRange(loadedScenes);
            }
            
            foreach (var destination in _destinations) {
                // No loader can be created for it, so every batch skips it.
                if (destination.type == SceneReferenceType.None) {
                    Debug.LogError($"Scene (GUID: {destination.guid}) cannot be loaded. Assign a scene and add it to the Build Profile scene list or to Addressables.");
                    continue;
                }

                // Every loaded instance counts, not only the first one (LoadMode.Additive loads several).
                var path = destination.path;
                bool isLoaded = false;
                foreach (var scene in loadedScenes) {
                    if (scene.path != path) {
                        continue;
                    }

                    isLoaded = true;
                    if (_mode.unloadMode == UnloadMode.CleanUp) {
                        _toUnload.Remove(scene);
                    }
                }

                switch (_mode.loadMode) {
                    case LoadMode.None:
                        if (!isLoaded) {
                            _toLoad.Add(destination);
                        }

                        break;
                    case LoadMode.Additive:
                        _toLoad.Add(destination);

                        break;
                    case LoadMode.Refresh:
                        foreach (var scene in loadedScenes) {
                            if (scene.path == path) {
                                _toUnload.Add(scene);
                            }
                        }
                        _toLoad.Add(destination);

                        break;
                }
            }

            // The bootstrap scene is never unloaded, so it must not get exit notifications either.
            _toUnload.RemoveAll(scene => scene.IsPinned() || IsBootstrapScene(scene));
        }

        private static bool IsBootstrapScene(Scene scene) {
            return SceneManagementSettings.canUseBootstrapScene
                && scene == SceneManagementSettings.bootstrapScene.scene;
        }

        private void CreateLoadBatch(in List<TransitionAction> actions, bool activateOnLoad) {
            foreach (var reference in _toLoad) {
                actions.Add(TransitionAction.Load(reference, activateOnLoad, parallel: true));
            }
        }

        private void CreateActivateBatch(in List<TransitionAction> actions) {
            foreach (var loader in _loaders) {
                actions.Add(TransitionAction.Activate(loader, parallel: true));
            }
        }

        private void CreateUnloadBatch(in List<TransitionAction> actions) {
            foreach (var scene in _toUnload) {
                actions.Add(TransitionAction.Unload(scene, parallel: true));
            }
        }

        private void CreateEnterBatch(in List<TransitionAction> actions) {
            // phase 1: enter
            foreach (var scene in SceneManager.loadedScenes) {
                actions.Add(TransitionAction.GlobalEnter(scene, parallel: true));
            }

            foreach (var loader in _loaders) {
                if (!loader.scene.isLoaded) {
                    Debug.LogWarning($"Scene {loader.scene.name} is not loaded. Skipping enter action for this scene.");
                    continue;
                }

                actions.Add(TransitionAction.Enter(loader.scene, parallel: true));
            }

            actions.Add(TransitionAction.WaitForParallels());

            // phase 2: enter async
            foreach (var scene in SceneManager.loadedScenes) {
                actions.Add(TransitionAction.GlobalEnterAsync(scene, parallel: true));
            }

            foreach (var loader in _loaders) {
                if (!loader.scene.isLoaded) {
                    continue;
                }

                actions.Add(TransitionAction.EnterAsync(loader.scene, parallel: true));
            }
        }

        private void CreateExitBatch(in List<TransitionAction> actions) {
            actions.Add(TransitionAction.WaitForParallels());

            // phase 1: exit async
            foreach (var scene in _toUnload) {
                if (!scene.isLoaded) {
                    continue;
                }

                actions.Add(TransitionAction.ExitAsync(scene, parallel: true));
            }

            foreach (var scene in SceneManager.loadedScenes) {
                actions.Add(TransitionAction.GlobalExitAsync(scene, parallel: true));
            }

            actions.Add(TransitionAction.WaitForParallels());

            // phase 2: exit
            foreach (var scene in _toUnload) {
                if (!scene.isLoaded) {
                    continue;
                }

                actions.Add(TransitionAction.Exit(scene, parallel: true));
            }

            foreach (var scene in SceneManager.loadedScenes) {
                actions.Add(TransitionAction.GlobalExit(scene, parallel: true));
            }
        }

        private async Awaitable WaitForParallels() {
            while (_parallels.Count > 0) {
                var parallel = _parallels.Dequeue();

                await parallel;    
            }
        }

        private Awaitable Execute_Internal(TransitionAction action) {
            return action.type switch {
                TransitionActionType.Load   => Load_Internal(action),
                TransitionActionType.Activate => SceneManager.ActivateSceneAsync_Internal(action.loader),
                TransitionActionType.Unload => SceneManager.UnloadSceneAsync_Internal(action.scene.GetLoader()),

                TransitionActionType.Enter      => SceneManager.NotifyEnter_Internal(action.scene, global: false),
                TransitionActionType.Exit       => SceneManager.NotifyExit_Internal(action.scene, global: false),
                TransitionActionType.EnterAsync => SceneManager.NotifyAsyncEnter_Internal(action.scene, global: false),
                TransitionActionType.ExitAsync  => SceneManager.NotifyAsyncExit_Internal(action.scene, global: false),

                TransitionActionType.GlobalEnter      => SceneManager.NotifyEnter_Internal(action.scene, global: true),
                TransitionActionType.GlobalExit       => SceneManager.NotifyExit_Internal(action.scene, global: true),
                TransitionActionType.GlobalEnterAsync => SceneManager.NotifyAsyncEnter_Internal(action.scene, global: true),
                TransitionActionType.GlobalExitAsync  => SceneManager.NotifyAsyncExit_Internal(action.scene, global: true),

                TransitionActionType.Delay      => Delay_Internal(action),
                TransitionActionType.DelayFrame => DelayFrame_Internal(action),
                TransitionActionType.NextFrame  => Awaitable.NextFrameAsync(),

                TransitionActionType.WaitForEndOfFrame  => Awaitable.EndOfFrameAsync(),
                TransitionActionType.WaitForFixedUpdate => Awaitable.FixedUpdateAsync(),

                TransitionActionType.WaitWhile => WaitWhile_Internal(action),
                TransitionActionType.WaitUntil => WaitUntil_Internal(action),

                TransitionActionType.Callback      => Callback_Internal(action),
                TransitionActionType.CallbackAsync => CallbackAsync_Internal(action),

                TransitionActionType.WaitForParallels => WaitForParallels(),
                _ => null
            };
        }

        private Awaitable Load_Internal(TransitionAction action) {
            var loader = action.reference.CreateLoader();
            if (loader == null) {
                return null;
            }

            _loaders.Add(loader);
            return SceneManager.LoadSceneAsync_Internal(loader, action.activateOnLoad);
        }

        private async Awaitable Delay_Internal(TransitionAction action) {
            if (action.waitSeconds <= 0f) {
                return;
            } 

            if (action.ignoreTimeScale) {
                float elapsed = 0f;
                
                while (elapsed < action.waitSeconds) {
                    await Awaitable.NextFrameAsync(cancellationToken: action.cancellationToken);
                    elapsed += Time.unscaledDeltaTime;
                }
            }
            else {
                await Awaitable.WaitForSecondsAsync(action.waitSeconds, cancellationToken: action.cancellationToken);
            }
        }

        private async Awaitable DelayFrame_Internal(TransitionAction action) {
            for (int i = 0; i < action.waitFrames; i++) {
                await Awaitable.NextFrameAsync(cancellationToken: action.cancellationToken);
            }
        }

        private Awaitable Callback_Internal(TransitionAction action) {
            action.callback?.Invoke();
            
            return null;
        }

        private async Awaitable CallbackAsync_Internal(TransitionAction action) {
            if (action.asyncCallback == null) {
                return;
            }

            BeginUserCode();
            try {
                var awaitable = action.asyncCallback.Invoke();
                if (awaitable != null) {
                    await awaitable;
                }
            }
            finally {
                EndUserCode();
            }
        }

        // Conditions count as user code: one may wait for a transition started from a callback.
        private async Awaitable WaitWhile_Internal(TransitionAction action) {
            if (action.condition == null) {
                return;
            }

            BeginUserCode();
            try {
                while (action.condition()) {
                    await Awaitable.NextFrameAsync(cancellationToken: action.cancellationToken);
                }
            }
            finally {
                EndUserCode();
            }
        }

        private async Awaitable WaitUntil_Internal(TransitionAction action) {
            if (action.condition == null) {
                return;
            }

            BeginUserCode();
            try {
                while (!action.condition()) {
                    await Awaitable.NextFrameAsync(cancellationToken: action.cancellationToken);
                }
            }
            finally {
                EndUserCode();
            }
        }
    }
}