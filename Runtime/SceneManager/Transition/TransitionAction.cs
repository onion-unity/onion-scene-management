using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Onion.SceneManagement.Transition {
    // fat struct for transition action
    public struct TransitionAction {
        internal TransitionActionType type;
        
        internal bool batch;
        internal Scene scene;
        internal SceneReference reference;
        
        internal float waitSeconds;
        internal int waitFrames;
        internal bool ignoreTimeScale;

        internal Action callback;
        internal Func<Awaitable> asyncCallback;

        internal Func<bool> condition;

        internal CancellationToken cancellationToken;

        internal bool parallel;

        internal static TransitionAction Load(SceneReference reference, bool parallel) {
            return new TransitionAction {
                type = TransitionActionType.Load,
                reference = reference,
                parallel = parallel
            };
        }

        internal static TransitionAction Unload(Scene scene, bool parallel) {
            return new TransitionAction {
                type = TransitionActionType.Unload,
                scene = scene,
                parallel = parallel
             };
        }

        internal static TransitionAction Enter(Scene scene, bool parallel) {
            return new TransitionAction {
                type = TransitionActionType.Enter,
                scene = scene,
                parallel = parallel
             };
        }

        internal static TransitionAction GlobalEnter(Scene scene, bool parallel) {
            return new TransitionAction {
                type = TransitionActionType.GlobalEnter,
                scene = scene,
                parallel = parallel
             };
        }

        internal static TransitionAction EnterAsync(Scene scene, bool parallel) {
            return new TransitionAction {
                type = TransitionActionType.EnterAsync,
                scene = scene,
                parallel = parallel
             };
        }


        internal static TransitionAction GlobalEnterAsync(Scene scene, bool parallel) {
            return new TransitionAction {
                type = TransitionActionType.GlobalEnterAsync,
                scene = scene,
                parallel = parallel
            };
        }

        internal static TransitionAction Exit(Scene scene, bool parallel) {
            return new TransitionAction {
                type = TransitionActionType.Exit,
                scene = scene,
                parallel = parallel
            };
        }

        internal static TransitionAction GlobalExit(Scene scene, bool parallel) {
            return new TransitionAction {
                type = TransitionActionType.GlobalExit,
                scene = scene,
                parallel = parallel
            };
        }

        internal static TransitionAction ExitAsync(Scene scene, bool parallel) {
            return new TransitionAction {
                type = TransitionActionType.ExitAsync,
                scene = scene,
                parallel = parallel
            };
        }

        internal static TransitionAction GlobalExitAsync(Scene scene, bool parallel) {
            return new TransitionAction {
                type = TransitionActionType.GlobalExitAsync,
                scene = scene,
                parallel = parallel
            };
        }

        internal static TransitionAction WaitWhile(Func<bool> condition) {
            return new TransitionAction {
                type = TransitionActionType.WaitWhile,
                condition = condition
            };
        }

        internal static TransitionAction WaitUntil(Func<bool> condition) {
            return new TransitionAction {
                type = TransitionActionType.WaitUntil,
                condition = condition
            };
        }

        internal static TransitionAction WaitForParallels() {
            return new TransitionAction {
                type = TransitionActionType.WaitForParallels
            };
        }
    }
}