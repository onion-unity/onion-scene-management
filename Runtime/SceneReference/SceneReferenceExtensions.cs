using System.Collections.Generic;
using UnityEngine;

namespace Onion.SceneManagement {
    public static class SceneReferenceExtensions {
        #region --- Load & Unload --- 
        public static void Load(this SceneReference reference, TransitionMode mode = default) {
            SceneManager.LoadScene(reference, mode);
        }

        public static Awaitable LoadAsync(this SceneReference reference, TransitionMode mode = default) {
            return SceneManager.LoadSceneAsync(reference, mode);
        }

        public static void Unload(this SceneReference reference) {
            SceneManager.UnloadScene(reference);
        }

        public static Awaitable UnloadAsync(this SceneReference reference) {
            return SceneManager.UnloadSceneAsync(reference);
        }

        public static void Load(this IEnumerable<SceneReference> references, TransitionMode mode = default) {
            SceneManager.LoadScenes(references, mode);
        }

        public static Awaitable LoadAsync(this IEnumerable<SceneReference> references, TransitionMode mode = default) {
            return SceneManager.LoadScenesAsync(references, mode);
        }

        public static void Unload(this IEnumerable<SceneReference> references) {
            SceneManager.UnloadScenes(references);
        }

        public static Awaitable UnloadAsync(this IEnumerable<SceneReference> references) {
            return SceneManager.UnloadScenesAsync(references);
        }
        #endregion
    }
}