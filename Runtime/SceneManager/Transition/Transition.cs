using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Onion.SceneManagement.Transition {
    public sealed class Transition {
        private const int initialCapacity = 16;

        private readonly List<SceneReference> _destinations = new(initialCapacity);
        private readonly List<TransitionAction> _actions = new(initialCapacity);
        private readonly Queue<Awaitable> _parallels = new(initialCapacity);

        private readonly List<SceneReference> _toLoad = new(initialCapacity);
        private readonly List<Scene> _toUnload = new(initialCapacity);
        private readonly Stack<TransitionAction> _actionStack = new(initialCapacity);

        private bool _isUsed = false;
        private TransitionMode _mode = TransitionMode.Default;

        internal Transition() { }

        public static Transition Create(IEnumerable<SceneReference> destinations) {
            return TransitionPool.Get()
                .To(destinations);
        }

        public static Transition Create(SceneReference destination) {
            return TransitionPool.Get()
                .To(destination);
        }

        public static Transition Create(IEnumerable<SceneReference> destinations, TransitionMode mode) {
            return TransitionPool.Get()
                .To(destinations)
                .Mode(mode);
        }

        public static Transition Create(SceneReference destination, TransitionMode mode) {
            return TransitionPool.Get()
                .To(destination)
                .Mode(mode);
        }

        internal Transition Add(TransitionAction action) => Add(action, false);

        internal Transition Add(TransitionAction action, bool parallel) {
            action.parallel = parallel;
            _actions.Add(action);

            return this;
        }

        internal Transition To(IEnumerable<SceneReference> references) {
            Debug.Log($"Adding destinations to transition: {string.Join(", ", references.Select(r => r.scene.name))}.");
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

        internal void Init() {
            _isUsed = false;
        }
        
        internal void Clear() {
            _destinations.Clear();
            _actions.Clear();
            _parallels.Clear();
        }

        public void Execute() {
            _ = ExecuteAsync();
        }

        public async Awaitable ExecuteAsync() {
            if (_isUsed) {
                Debug.LogWarning("This transition has already been used. Creating a new transition for reuse.");
                return;    
            }

            _isUsed = true;

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
                        
                        if      (action.type == TransitionActionType.Load)   CreateLoadBatch(batchActions);
                        else if (action.type == TransitionActionType.Unload) CreateUnloadBatch(batchActions);
                        else if (action.type == TransitionActionType.Enter)  CreateEnterBatch(batchActions);
                        else if (action.type == TransitionActionType.Exit)   CreateExitBatch(batchActions);
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
                Clear();
                TransitionPool.Release(this);
            }
        }

        private void Prepare() {
            _toLoad.Clear();
            _toUnload.Clear();

            if (_destinations.Count == 0) {
                return;
            }

            var loadedScenes = SceneManager.loadedScenes;

            if (_mode.unloadMode == UnloadMode.CleanUp) {
                _toUnload.AddRange(loadedScenes);
            }
            
            foreach (var destination in _destinations) {
                if (_mode.unloadMode == UnloadMode.CleanUp) {
                    _toUnload.Remove(destination.scene);
                }

                bool isLoaded = destination.scene.isLoaded;
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
                        if (isLoaded) {
                            _toUnload.Add(destination.scene);
                        }
                        _toLoad.Add(destination);

                        break;
                }
            }
        }

        private void CreateLoadBatch(in List<TransitionAction> actions) {
            foreach (var reference in _toLoad) {
                actions.Add(TransitionAction.Load(reference, parallel: true));
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

            foreach (var reference in _toLoad) {
                if (!reference.scene.isLoaded) {
                    Debug.LogWarning($"Scene {reference.scene.name} is not loaded. Skipping enter action for this scene.");
                    continue;
                }

                actions.Add(TransitionAction.Enter(reference.GetLoader().scene, parallel: true));
            }

            actions.Add(TransitionAction.WaitForParallels());

            // phase 2: enter async
            foreach (var scene in SceneManager.loadedScenes) {
                actions.Add(TransitionAction.GlobalEnterAsync(scene, parallel: true));
            }

            foreach (var reference in _toLoad) {
                if (!reference.scene.isLoaded) {
                    continue;
                }

                actions.Add(TransitionAction.EnterAsync(reference.GetLoader().scene, parallel: true));
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
                TransitionActionType.Load   => SceneManager.LoadSceneAsync_Internal(action.reference.CreateLoader()),
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
                TransitionActionType.NextFrame      => Awaitable.NextFrameAsync(),

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

        private Awaitable CallbackAsync_Internal(TransitionAction action) {
            if (action.asyncCallback == null) {
                return null;
            }

            return action.asyncCallback.Invoke();
        }

        private async Awaitable WaitWhile_Internal(TransitionAction action) {
            if (action.condition == null) {
                return;
            }

            while (!action.condition()) {
                await Awaitable.NextFrameAsync(cancellationToken: action.cancellationToken);
            }
        }

        private async Awaitable WaitUntil_Internal(TransitionAction action) {
            if (action.condition == null) {
                return;
            }

            while (action.condition()) {
                await Awaitable.NextFrameAsync(cancellationToken: action.cancellationToken);
            }
        }
    }
}