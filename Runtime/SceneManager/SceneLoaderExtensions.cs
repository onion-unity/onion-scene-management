using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Onion.SceneManagement {
    internal static class SceneLoaderExtensions {
        private static readonly Dictionary<Scene, SceneLoader> _loaders = new();

        // Loaders of the previous Play session's scenes would otherwise survive when domain reload is off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            _loaders.Clear();
        }

        public static void Register(this Scene scene, SceneLoader loader) {
            if (!scene.IsValid()) {
                return;
            }

            _loaders[scene] = loader;
        }

        public static void Unregister(this Scene scene) {
            // Called after the unload; a scene still loaded means the unload failed (e.g. Addressables).
            // Keep its loader, or the scene could never be unloaded through the package again.
            if (scene.isLoaded) {
                return;
            }

            _loaders.Remove(scene);
        }

        public static bool HasLoader(this Scene scene) {
            return _loaders.ContainsKey(scene);
        }

        public static bool TryGetLoader(this Scene scene, out SceneLoader loader) {
            return _loaders.TryGetValue(scene, out loader);
        }

        public static SceneLoader GetLoader(this Scene scene) {
            if (TryGetLoader(scene, out var loader)) {
                return loader;
            }

            return null;
        }

        public static SceneLoader CreateLoader(this SceneReference reference) {
            return reference.type switch {
                SceneReferenceType.BuiltIn => new BuiltInSceneLoader(reference),
#if ONION_ADDRESSABLES
                SceneReferenceType.Addressable => new AddressableSceneLoader(reference),
#endif
                _ => null
            };
        }
    }
}