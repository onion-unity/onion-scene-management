// using UnityEngine;
// using UnityEngine.Events;

// #if UNITY_EDITOR
// using UnityEditor;
// #endif

using UnityEngine;

namespace Onion.SceneManagement {
    internal static class Bootstrapper {
        internal static bool isReady { get; set; }

        // Otherwise a Placeholder destroys itself in the next Play session when domain reload is off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            isReady = false;
        }
    }
}