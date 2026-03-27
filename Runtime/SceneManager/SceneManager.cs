using System;
using System.Collections.Generic;
using Onion.SceneManagement.Handler;
using Onion.SceneManagement.Transition;
using UnityEngine;
using UnityEngine.Pool;
using UniSceneManagement = UnityEngine.SceneManagement;
using UniScene = UnityEngine.SceneManagement.Scene;

namespace Onion.SceneManagement {
    public static class SceneManager {
        private static readonly Dictionary<UniScene, SceneHandlerCollection> _handlersCache = new();
        private static readonly List<UniScene> _loadedScenes = new();
        public static IReadOnlyList<UniScene> loadedScenes => _loadedScenes;

        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        // private static void Initialize() {
        //     _loadedScenes.Clear();
        //     _handlersCache.Clear();

        //     for (int i = 0; i < UniSceneManagement.SceneManager.sceneCount; i++) {
        //         var scene = UniSceneManagement.SceneManager.GetSceneAt(i);

        //         _loadedScenes.Add(scene);
                
        //         var reference = SceneReference.FromScene(scene);
        //         var loader = reference.CreateLoader();
        //         loader.scene = scene;

        //         var handlers = new SceneHandlerCollection();
        //         handlers.Initialize(scene);

        //         _handlersCache[scene] = handlers;
        //     }
        // }

        // --- Public API ---
        
        public static void LoadScene(SceneReference reference, TransitionMode mode = default) {
            _ = LoadSceneAsync(reference, mode);
        }

        public static void LoadScenes(IEnumerable<SceneReference> references, TransitionMode mode = default) {
            _ = LoadScenesAsync(references, mode);
        }

        public static Awaitable LoadSceneAsync(SceneReference reference, TransitionMode mode = default) {
            return Transition.Transition.Create(reference)
                .NotifyExit()
                .Unload()
                .Load()
                .NotifyEnter()
                .ExecuteAsync();
        }

        public static Awaitable LoadScenesAsync(IEnumerable<SceneReference> references, TransitionMode mode = default) {
            return Transition.Transition.Create(references)
                .NotifyExit()
                .Unload()
                .Load()
                .NotifyEnter()
                .ExecuteAsync();
        }

        public static void UnloadScene(SceneReference reference, TransitionMode mode = default) {
            _ = UnloadSceneAsync(reference, mode);
        }

        public static void UnloadScenes(IEnumerable<SceneReference> references, TransitionMode mode = default) {
            _ = UnloadScenesAsync(references, mode);
        }

        public static Awaitable UnloadSceneAsync(SceneReference reference, TransitionMode mode = default) {
            return Transition.Transition.Create(reference)
                .NotifyExit()
                .Unload()
                .ExecuteAsync();
        }

        public static Awaitable UnloadScenesAsync(IEnumerable<SceneReference> references, TransitionMode mode = default) {
            return Transition.Transition.Create(references)
                .NotifyExit()
                .Unload()
                .ExecuteAsync();
        }

        // --- internal API ---

        internal static async Awaitable LoadSceneAsync_Internal(SceneLoader loader) {
            if (loader?.scene.isLoaded == true) {
                return;
            }

            await loader.LoadAsync();

            _loadedScenes.Add(loader.scene);

            var handlers = new SceneHandlerCollection();
            handlers.Initialize(loader.scene);

            _handlersCache[loader.scene] = handlers;
        }

        internal static async Awaitable UnloadSceneAsync_Internal(SceneLoader loader) {
            if (loader?.scene.isLoaded != true) {
                return;
            }

            Debug.Log("Unloading scene: " + loader.scene.name);

            await loader.UnloadAsync();

            _loadedScenes.Remove(loader.scene);

            if (_handlersCache.TryGetValue(loader.scene, out var handlers)) {
                handlers.Clear();
                _handlersCache.Remove(loader.scene);
            }
        }

        internal static Awaitable NotifyEnter_Internal(UniScene scene, bool global = false) {
            if (_handlersCache.TryGetValue(scene, out var handlers)) {
                var list = global ? handlers.enter.always : handlers.enter.normal;
                foreach (var handler in list) {
                    if (IsHandlerDestroyed(handler)) continue;

                    handler.OnSceneEnter();
                }
            }

            return null;
        }

        internal static async Awaitable NotifyAsyncEnter_Internal(UniScene scene, bool global = false) {
            if (!_handlersCache.TryGetValue(scene, out var handlers)) {
                return;
            }

            using var pool = ListPool<Awaitable>.Get(out var awaitables);

            var list = global ? handlers.asyncEnter.always : handlers.asyncEnter.normal;
            foreach (var handler in list) {
                if (IsHandlerDestroyed(handler)) continue;

                awaitables.Add(handler.OnSceneEnterAsync());
            }

            foreach (var awaitable in awaitables) {
                await awaitable;
            }
        }

        internal static Awaitable NotifyExit_Internal(UniScene scene, bool global = false) {
            if (_handlersCache.TryGetValue(scene, out var handlers)) {
                var list = global ? handlers.exit.always : handlers.exit.normal;
                foreach (var handler in list) {
                    if (IsHandlerDestroyed(handler)) continue;

                    handler.OnSceneExit();
                }
            }
            
            return null;
        }

        internal static async Awaitable NotifyAsyncExit_Internal(UniScene scene, bool global = false) {
            if (!_handlersCache.TryGetValue(scene, out var handlers)) {
                return;
            }

            using var pool = ListPool<Awaitable>.Get(out var awaitables);

            var list = global ? handlers.asyncExit.always : handlers.asyncExit.normal;
            foreach (var handler in list) {
                if (IsHandlerDestroyed(handler)) continue;

                awaitables.Add(handler.OnSceneExitAsync());
            }

            foreach (var awaitable in awaitables) {
                await awaitable;
            }
        }

        private static bool IsHandlerDestroyed(ISceneHandler handler) {
            if (handler == null) return true;
            if (handler is not MonoBehaviour mb) {
                return false;
            }

            return mb == null;
        }
    }
}