using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Onion.SceneManagement.Transition {
    // fat struct for transition action
    public struct TransitionAction {
        internal TransitionActionType type;
        internal Scene scene;
        internal SceneReference reference;
        
        internal float waitSeconds;
        internal int waitFrames;
        internal bool ignoreTimeScale;

        internal Action callback;
        internal Func<Awaitable> asyncCallback;
        internal CancellationToken cancellationToken;

        internal bool parallel;

        // internal static TransitionAction Load(SceneReference reference) {
        //     return new TransitionAction {
        //         type = TransitionActionType.Load,
        //         reference = reference
        //     };
        // }

        // internal static TransitionAction Unload(Scene scene) {
        //     return new TransitionAction {
        //         type = TransitionActionType.Unload,
        //         scene = scene
        //      };
        // }

        // internal static TransitionAction Enter(Scene scene) {
        //     return new TransitionAction {
        //         type = TransitionActionType.Enter,
        //         scene = scene
        //      };
        // }

        // internal static TransitionAction GlobalEnter(Scene scene) {
        //     return new TransitionAction {
        //         type = TransitionActionType.GlobalEnter,
        //         scene = scene
        //      };
        // }

        // internal static TransitionAction EnterAsync(Scene scene) {
        //     return new TransitionAction {
        //         type = TransitionActionType.EnterAsync,
        //         scene = scene
        //      };
        // }


        // internal static TransitionAction GlobalEnterAsync(Scene scene) {
        //     return new TransitionAction {
        //         type = TransitionActionType.GlobalEnterAsync,
        //         scene = scene
        //     };
        // }

        // internal static TransitionAction Exit(Scene scene) {
        //     return new TransitionAction {
        //         type = TransitionActionType.Exit,
        //         scene = scene
        //     };
        // }

        // internal static TransitionAction GlobalExit(Scene scene) {
        //     return new TransitionAction {
        //         type = TransitionActionType.GlobalExit,
        //         scene = scene
        //     };
        // }

        // internal static TransitionAction ExitAsync(Scene scene) {
        //     return new TransitionAction {
        //         type = TransitionActionType.ExitAsync,
        //         scene = scene
        //     };
        // }

        // internal static TransitionAction GlobalExitAsync(Scene scene) {
        //     return new TransitionAction {
        //         type = TransitionActionType.GlobalExitAsync,
        //         scene = scene
        //     };
        // }
    }
}