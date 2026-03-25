using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Onion.SceneManagement.Transition {
    public sealed class Transition {
        private const int initialCapacity = 8;

        private readonly List<SceneReference> _destinations = new(initialCapacity);
        private readonly List<TransitionAction> _actions = new(initialCapacity);
        private readonly Queue<Awaitable> _parallels = new(initialCapacity);

        private readonly List<SceneReference> _toLoad = new(initialCapacity);
        private readonly List<Scene> _toUnload = new(initialCapacity);

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
            _isUsed = false;

            _destinations.Clear();

            _actions.Clear();
            _parallels.Clear();
        }

        public void Execute() {
            _ = ExecuteAsync();
        }

        public async Awaitable ExecuteAsync() {
            if (_isUsed) {
                Debug.LogWarning("This transition has already been used.");
                return;    
            }

            _isUsed = true;

            try {
                Prepare();

                foreach (var action in _actions) {
                    if (!action.parallel) {
                        await WaitForParallels();
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
            if (_destinations.Count == 0) {
                return;
            }

            _toLoad.AddRange(_destinations);
            _toUnload.AddRange(SceneManager.loadedScenes);

            switch (_mode.unloadMode) {
                case UnloadMode.None:

                    break;
                case UnloadMode.CleanUp:

                    break;
            }

            switch (_mode.loadMode) {
                case LoadMode.None:

                    break;
                case LoadMode.Additive:

                    break;
                case LoadMode.Refresh:

                    break;
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
                TransitionActionType.Load => SceneManager.LoadSceneAsync_Internal(action.reference.CreateLoader()),
                TransitionActionType.Unload => SceneManager.UnloadSceneAsync_Internal(action.scene.GetLoader()),

                TransitionActionType.Enter => SceneManager.NotifyEnter_Internal(action.scene, global: false),
                TransitionActionType.Exit => SceneManager.NotifyExit_Internal(action.scene, global: false),
                TransitionActionType.EnterAsync => SceneManager.NotifyAsyncEnter_Internal(action.scene, global: false),
                TransitionActionType.ExitAsync => SceneManager.NotifyAsyncExit_Internal(action.scene, global: false),

                TransitionActionType.GlobalEnter => SceneManager.NotifyEnter_Internal(action.scene, global: true),
                TransitionActionType.GlobalExit => SceneManager.NotifyExit_Internal(action.scene, global: true),
                TransitionActionType.GlobalEnterAsync => SceneManager.NotifyAsyncEnter_Internal(action.scene, global: true),
                TransitionActionType.GlobalExitAsync => SceneManager.NotifyAsyncExit_Internal(action.scene, global: true),

                TransitionActionType.Delay => Delay_Internal(action),
                TransitionActionType.DelayFrame => DelayFrame_Internal(action),
                TransitionActionType.Yield => Awaitable.NextFrameAsync(),
                TransitionActionType.WaitForEndOfFrame => Awaitable.EndOfFrameAsync(),
                TransitionActionType.WaitForFixedUpdate => Awaitable.FixedUpdateAsync(),

                TransitionActionType.Callback => Callback_Internal(action),
                TransitionActionType.CallbackAsync => CallbackAsync_Internal(action),
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
    }
}