using System;
using System.Collections.Generic;
using Onion.SceneManagement.Handler;
using UnityEngine;

using UniScene = UnityEngine.SceneManagement.Scene;

namespace Onion.SceneManagement {
    public static class SceneManager {
        private static readonly Dictionary<UniScene, SceneHandlerCollection> _handlersCache = new();
        private static readonly List<UniScene> _activeScenes = new();

        internal static async Awaitable LoadSceneAsync_Internal(SceneLoader loader) {
            if (loader?.scene.isLoaded == true) {
                return;
            }

            await loader.LoadAsync();

            _activeScenes.Add(loader.scene);

            var handlers = new SceneHandlerCollection();
            handlers.Initialize(loader.scene);

            _handlersCache[loader.scene] = handlers;
        }

        internal static async Awaitable UnloadSceneAsync_Internal(SceneLoader loader) {
            if (loader?.scene.isLoaded != true) {
                return;
            }

            await loader.UnloadAsync();

            _activeScenes.Remove(loader.scene);

            if (_handlersCache.TryGetValue(loader.scene, out var handlers)) {
                handlers.Clear();
                _handlersCache.Remove(loader.scene);
            }
        }
    }
}