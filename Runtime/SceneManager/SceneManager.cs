using System.Collections.Generic;
using Onion.SceneManagement.Handler;
using Onion.SceneManagement.Transition;
using UnityEngine;
using UnityEngine.Pool;
using UniSceneManagement = UnityEngine.SceneManagement;
using UniScene = UnityEngine.SceneManagement.Scene;
using Onion.SceneManagement.Utility;
using Onion.SceneManagement.Setting;
using System;

namespace Onion.SceneManagement {
    public static class SceneManager {
        private static readonly Dictionary<UniScene, SceneHandlerCollection> _handlersCache = new();
        private static readonly List<UniScene> _loadedScenes = new();
        private static readonly HashSet<UniScene> _pinnedScenes = new();
        private static UniScene _barrierScene;
        public static IReadOnlyList<UniScene> loadedScenes => _loadedScenes;

        public static event Action<UniScene> sceneLoaded;
        public static event Action<UniScene> sceneUnloaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize() {
            _loadedScenes.Clear();
            _handlersCache.Clear();

            for (int i = 0; i < UniSceneManagement.SceneManager.sceneCount; i++) {
                var scene = UniSceneManagement.SceneManager.GetSceneAt(i);
                if (SceneManagementSettings.useBootstrap && SceneManagementSettings.canUseBootstrapScene) {
                    var bootstrapScene = SceneManagementSettings.bootstrapScene.scene;

                    if (scene == bootstrapScene) {
                        Bootstrapper.isReady = true;

                        Debug.Log($"Bootstrap scene '{bootstrapScene.name}' is loaded.");
                    }
                }

                _loadedScenes.Add(scene);
                sceneLoaded?.Invoke(scene);

                var reference = SceneReference.FromScene(scene);
                var loader = reference.CreateLoader();
                loader.scene = scene;

                var handlers = new SceneHandlerCollection();
                handlers.Initialize(scene);

                _handlersCache[scene] = handlers;
            }

            foreach (var scene in _loadedScenes) {
                _ = NotifyEnter_Internal(scene);
                _ = NotifyEnter_Internal(scene, global: true);
            }

            foreach (var scene in _loadedScenes) {
                _ = NotifyAsyncEnter_Internal(scene);
                _ = NotifyAsyncEnter_Internal(scene, global: true);
            }
        }

        // --- Public API ---
        
        public static void LoadScene(SceneReference reference, TransitionMode mode = default) {
            _ = LoadSceneAsync(reference, mode);
        }

        public static void LoadScenes(IEnumerable<SceneReference> references, TransitionMode mode = default) {
            _ = LoadScenesAsync(references, mode);
        }

        public static Awaitable LoadSceneAsync(SceneReference reference, TransitionMode mode = default) {
            return SceneManagementSettings.overlapLoading
                ? Transition.Transition.Create(reference, mode).NotifyExit().Load().Unload().NotifyEnter().ExecuteAsync()
                : Transition.Transition.Create(reference, mode).NotifyExit().Unload().Load().NotifyEnter().ExecuteAsync();
        }

        public static Awaitable LoadScenesAsync(IEnumerable<SceneReference> references, TransitionMode mode = default) {
            if (references.IsNullOrEmpty()) {
                return null;
            }

            return SceneManagementSettings.overlapLoading
                ? Transition.Transition.Create(references, mode).NotifyExit().Load().Unload().NotifyEnter().ExecuteAsync()
                : Transition.Transition.Create(references, mode).NotifyExit().Unload().Load().NotifyEnter().ExecuteAsync();
        }

        public static void UnloadScene(SceneReference reference, UnloadMode mode = default) {
            _ = UnloadSceneAsync(reference, mode);
        }

        public static void UnloadScenes(IEnumerable<SceneReference> references, UnloadMode mode = default) {
            _ = UnloadScenesAsync(references, mode);
        }

        public static Awaitable UnloadSceneAsync(SceneReference reference, UnloadMode mode = default) {
            return Transition.Transition.Create(reference, new() { unloadMode = mode })
                .NotifyExit()
                .Unload()
                .ExecuteAsync();
        }

        public static Awaitable UnloadScenesAsync(IEnumerable<SceneReference> references, UnloadMode mode = default) {
            if (references.IsNullOrEmpty()) {
                return null;
            }

            return Transition.Transition.Create(references, new() { unloadMode = mode })
                .NotifyExit()
                .Unload()
                .ExecuteAsync();
        }

        public static void Pin(UniScene scene) {
            if (_pinnedScenes.Contains(scene)) {
                return;
            }

            if (scene.IsValid() && scene.isLoaded) {
                _pinnedScenes.Add(scene);
            }
        }

        public static void Pin(SceneReference reference)
            => Pin(reference.scene);

        public static void Pin(IEnumerable<UniScene> scenes) {
            foreach (var scene in scenes) {
                Pin(scene);
            }
        }

        public static void Pin(IEnumerable<SceneReference> references) {
            foreach (var reference in references) {
                Pin(reference);
            }
        }

        public static void Unpin(UniScene scene) {
            if (_pinnedScenes.Contains(scene)) {
                _pinnedScenes.Remove(scene);
            }
        }

        public static void Unpin(SceneReference reference)
            => Unpin(reference.scene);

        public static void Unpin(IEnumerable<UniScene> scenes) {
            foreach (var scene in scenes) {
                Unpin(scene);
            }
        }

        public static void Unpin(IEnumerable<SceneReference> references) {
            foreach (var reference in references) {
                Unpin(reference);
            }
        }

        public static bool IsPinned(UniScene scene) {
            return _pinnedScenes.Contains(scene);
        }

        public static bool IsPinned(SceneReference reference)
            => IsPinned(reference.scene);

        public static bool ArePinned(IEnumerable<UniScene> scenes) {
            foreach (var scene in scenes) {
                if (!IsPinned(scene)) {
                    return false;
                }
            }
            return true;
        }

        public static bool ArePinned(IEnumerable<SceneReference> references) {
            foreach (var reference in references) {
                if (!IsPinned(reference)) {
                    return false;
                }
            }
            return true;
        }
        

        // --- internal API ---

        internal static async Awaitable LoadSceneAsync_Internal(SceneLoader loader) {
            if (loader?.scene.isLoaded == true) {
                return;
            }

            await loader.LoadAsync();
            sceneLoaded?.Invoke(loader.scene);

            RemoveBarrierIfExists();

            _loadedScenes.Add(loader.scene);

            var handlers = new SceneHandlerCollection();
            handlers.Initialize(loader.scene);

            _handlersCache[loader.scene] = handlers;
        }

        internal static async Awaitable ActivateSceneAsync_Internal(SceneLoader loader) {
            await loader.ActivateAsync();
        }

        internal static async Awaitable UnloadSceneAsync_Internal(SceneLoader loader) {
            if (loader?.scene.isLoaded != true) {
                return;
            }

            CreateBarrierIfNeeded();

            await loader.UnloadAsync();
            sceneUnloaded?.Invoke(loader.scene);
            
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

        private static void CreateBarrierIfNeeded() {
            if (loadedScenes.Count > 1) {
                return;
            }

            _barrierScene = UniSceneManagement.SceneManager.CreateScene("OnionSceneManagement_Barrier");
        }

        private static void RemoveBarrierIfExists() {
            if (_barrierScene.isLoaded) {
                UniSceneManagement.SceneManager.UnloadSceneAsync(_barrierScene);
                _barrierScene = default;
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