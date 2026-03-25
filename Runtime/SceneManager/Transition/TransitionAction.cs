using System;
using System.Threading;
using UnityEngine;

namespace Onion.SceneManagement.Transition {
    // fat struct for transition action
    public struct TransitionAction {
        internal TransitionActionType type;
        internal SceneReference reference;
        
        internal float waitSeconds;
        internal int waitFrames;
        internal bool ignoreTimeScale;

        internal Action callback;
        internal Func<Awaitable> asyncCallback;
        internal CancellationToken cancellationToken;

        internal bool parallel;
    }
}