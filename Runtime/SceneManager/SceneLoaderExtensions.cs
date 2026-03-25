using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Onion.SceneManagement {
    internal static class SceneLoaderExtensions {
        private static readonly Dictionary<Scene, SceneLoader> _loaders = new();

        public static void Register(this Scene scene, SceneLoader loader) {
            if (!scene.IsValid()) {
                return;
            }

            _loaders[scene] = loader;
        }

        public static void Unregister(this Scene scene) {
            if (scene.IsValid()) {
                _loaders.Remove(scene);
            }
        }

        public static bool HasLoader(this Scene scene) {
            return _loaders.ContainsKey(scene);
        }

        public static bool HasLoader(this SceneReference reference)
            => HasLoader(reference.scene);

        public static bool TryGetLoader(this Scene scene, out SceneLoader loader) {
            return _loaders.TryGetValue(scene, out loader);
        }

        public static bool TryGetLoader(this SceneReference reference, out SceneLoader loader)
            => TryGetLoader(reference.scene, out loader);

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