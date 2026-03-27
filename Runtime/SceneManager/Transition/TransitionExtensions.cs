using System;
using UnityEngine;

namespace Onion.SceneManagement.Transition {
    public static class TransitionExtensions {
        // --- Batch Actions ---
        public static Transition Load(this Transition transition)
            => transition.Add(new TransitionAction() {
                type = TransitionActionType.Load,
                batch = true, 
            });

        public static Transition Unload(this Transition transition)
            => transition.Add(new TransitionAction() {
                type = TransitionActionType.Unload,
                batch = true, 
            });

        public static Transition NotifyEnter(this Transition transition)
            => transition.Add(new TransitionAction() {
                type = TransitionActionType.Enter,
                batch = true, 
            });

        public static Transition NotifyExit(this Transition transition)
            => transition.Add(new TransitionAction() {
                type = TransitionActionType.Exit,
                batch = true, 
            });

        // --- Callback Actions ---
        public static Transition Callback(this Transition transition, Action callback, bool parallel = false)
            => transition.Add(new TransitionAction() {
                type = TransitionActionType.Callback,
                callback = callback,
                parallel = parallel,
            });

        public static Transition Callback(this Transition transition, Func<Awaitable> asyncCallback, bool parallel = false)
            => transition.Add(new TransitionAction() {
                type = TransitionActionType.Callback,
                asyncCallback = asyncCallback,
                parallel = parallel,
            });

        // --- Wait/Sync Actions ---
        public static Transition Delay(this Transition transition, float seconds, bool ignoreTimeScale = false, bool parallel = false)
            => transition.Add(new TransitionAction() {
                type = TransitionActionType.Delay,
                waitSeconds = seconds,
                ignoreTimeScale = ignoreTimeScale,
                parallel = parallel,
            });

        public static Transition DelayFrame(this Transition transition, int frameCount = 1, bool parallel = false)
            => transition.Add(new TransitionAction() {
                type = TransitionActionType.DelayFrame,
                waitFrames = frameCount,
                parallel = parallel,
            });

        public static Transition NextFrame(this Transition transition)
            => transition.Add(new TransitionAction() {
                type = TransitionActionType.NextFrame,
            });

        public static Transition WaitForEndOfFrame(this Transition transition)
            => transition.Add(new TransitionAction() {
                type = TransitionActionType.WaitForEndOfFrame,
            });

        public static Transition WaitForFixedUpdate(this Transition transition)
            => transition.Add(new TransitionAction() {
                type = TransitionActionType.WaitForFixedUpdate,
            });
    }
}