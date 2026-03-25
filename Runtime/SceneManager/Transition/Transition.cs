using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Onion.SceneManagement.Transition {
    public class Transition {
        private const int initialCapacity = 8;

        private readonly List<TransitionAction> _actions = new(initialCapacity);
        private readonly Queue<Awaitable> _parallels = new(initialCapacity);
        private bool _isUsed = false;

        internal Transition() { }

        public static Transition Create() {
            return TransitionPool.Get();
        }

        internal Transition Add(TransitionAction action) => Add(action, false);

        internal Transition Add(TransitionAction action, bool parallel) {
            action.parallel = parallel;
            _actions.Add(action);

            return this;
        }
        
        internal void Clear() {
            _isUsed = false;

            _actions.Clear();
            _parallels.Clear();
        }

        public void Execute() {
            _ = ExecuteAsync();
        }

        public async Awaitable ExecuteAsync() {
            if (_isUsed) return;
            _isUsed = true;

            try {
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

        private async Awaitable WaitForParallels() {
            while (_parallels.Count > 0) {
                var parallel = _parallels.Dequeue();

                await parallel;    
            }
        }

        private Awaitable Execute_Internal(TransitionAction action) {
            return action.type switch {
                TransitionActionType.Load => null,
                TransitionActionType.Unload => null,
                TransitionActionType.Enter => null,
                TransitionActionType.Exit => null,
                TransitionActionType.EnterAsync => null,
                TransitionActionType.ExitAsync => null,

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

        private async Awaitable Callback_Internal(TransitionAction action) {
            if (action.callback == null) {
                return;
            }

            action.callback.Invoke();
        }

        private Awaitable CallbackAsync_Internal(TransitionAction action) {
            if (action.asyncCallback == null) {
                return null;
            }

            return action.asyncCallback.Invoke();
        }
    }
}