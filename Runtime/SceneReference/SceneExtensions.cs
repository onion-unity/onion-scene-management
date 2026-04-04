using System.Collections.Generic;
using UnityEngine;
using UniSceneManagement = UnityEngine.SceneManagement;

namespace Onion.SceneManagement {
    public static class SceneExtensions {
        public static bool IsActive(this UniSceneManagement.Scene scene) {
            return scene == UniSceneManagement.SceneManager.GetActiveScene();
        }

        public static SceneReference Reference(this UniSceneManagement.Scene scene) {
            return SceneReference.FromScene(scene);
        }

        public static void Unload(this UniSceneManagement.Scene scene) {
            scene.Reference().Unload();
        }

        public static Awaitable UnloadAsync(this UniSceneManagement.Scene scene) {
            return scene.Reference().UnloadAsync();
        }

        public static void Pin(this UniSceneManagement.Scene scene) => SceneManager.Pin(scene);
        public static void Pin(this IEnumerable<UniSceneManagement.Scene> scenes) => SceneManager.Pin(scenes);
        public static void Unpin(this UniSceneManagement.Scene scene) => SceneManager.Unpin(scene);
        public static void Unpin(this IEnumerable<UniSceneManagement.Scene> scenes) => SceneManager.Unpin(scenes);
    }
}