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

        // Keeps the previous Play session's state out of the next one when domain reload is off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            _pinnedScenes.Clear();
            _barrierScene = default;
            sceneLoaded = null;
            sceneUnloaded = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize() {
            _loadedScenes.Clear();
            _handlersCache.Clear();

#if !UNITY_EDITOR
            // The build is checked too (BootstrapSceneBuildProcessor), but a misplaced Bootstrap scene fails silently otherwise.
            if (SceneManagementSettings.useBootstrap
                && (!SceneManagementSettings.canUseBootstrapScene || SceneManagementSettings.bootstrapScene.buildIndex != 0)) {
                Debug.LogError("Use Bootstrap is on, but the Bootstrap scene is not the first scene in the build.");
            }
#endif

            InitializeExistingScene();
            LoadInitialScenesIfNeeded();
        }

        private static void InitializeExistingScene() {
            for (int i = 0; i < UniSceneManagement.SceneManager.sceneCount; i++) {
                var scene = UniSceneManagement.SceneManager.GetSceneAt(i);
                if (SceneManagementSettings.useBootstrap && SceneManagementSettings.canUseBootstrapScene) {
                    var bootstrapScene = SceneManagementSettings.bootstrapScene.scene;

                    if (scene == bootstrapScene) {
                        Bootstrapper.isReady = true;

                        // Debug.Log($"Bootstrap scene '{bootstrapScene.name}' is loaded.");
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

        private static void LoadInitialScenesIfNeeded() {
            if (!SceneManagementSettings.canUseBootstrapScene) {
                return;
            }

#if UNITY_EDITOR
            if (!Bootstrapper.isReady || _handlersCache.Count > 1) {
                return;
            }
#endif
            var initialScenes = SceneManagementSettings.initialScenes;
            if (initialScenes.IsNullOrEmpty()) {
                return;
            }

            initialScenes.Load();
        }

        // --- Public API ---
        
        public static void LoadScene(SceneReference reference, TransitionMode mode = default) {
            _ = LoadSceneAsync(reference, mode);
        }

        public static void LoadScene(int buildIndex, TransitionMode mode = default) {
            _ = LoadSceneAsync(buildIndex, mode);
        }

        public static void LoadScene(string name, TransitionMode mode = default) {
            _ = LoadSceneAsync(name, mode);
        }

        public static void LoadScenes(IEnumerable<SceneReference> references, TransitionMode mode = default) {
            _ = LoadScenesAsync(references, mode);
        }

        public static void LoadScenes(IEnumerable<int> buildIndices, TransitionMode mode = default) {
            _ = LoadScenesAsync(buildIndices, mode);
        }

        public static void LoadScenes(IEnumerable<string> names, TransitionMode mode = default) {
            _ = LoadScenesAsync(names, mode);
        }

        public static Awaitable LoadSceneAsync(SceneReference reference, TransitionMode mode = default) {
            return SceneManagementSettings.overlapLoading
                ? Transition.Transition.Create(reference, mode).NotifyExit().Load().Unload().NotifyEnter().ExecuteAsync()
                : Transition.Transition.Create(reference, mode).NotifyExit().Unload().Load().NotifyEnter().ExecuteAsync();
        }

        public static Awaitable LoadSceneAsync(int buildIndex, TransitionMode mode = default) {
            return LoadSceneAsync(SceneReference.FromBuildIndex(buildIndex), mode);
        }

        public static Awaitable LoadSceneAsync(string name, TransitionMode mode = default) {
            return LoadSceneAsync(SceneReference.FromName(name), mode);
        }

        // async so an empty list still returns an Awaitable that can be awaited, not null.
        public static async Awaitable LoadScenesAsync(IEnumerable<SceneReference> references, TransitionMode mode = default) {
            if (references.IsNullOrEmpty()) {
                return;
            }

            await (SceneManagementSettings.overlapLoading
                ? Transition.Transition.Create(references, mode).NotifyExit().Load().Unload().NotifyEnter().ExecuteAsync()
                : Transition.Transition.Create(references, mode).NotifyExit().Unload().Load().NotifyEnter().ExecuteAsync());
        }

        public static Awaitable LoadScenesAsync(IEnumerable<int> buildIndices, TransitionMode mode = default) {
            var references = new List<SceneReference>();
            foreach (var index in buildIndices) {
                references.Add(SceneReference.FromBuildIndex(index));
            }

            return LoadScenesAsync(references, mode);
        }

        public static Awaitable LoadScenesAsync(IEnumerable<string> names, TransitionMode mode = default) {
            var references = new List<SceneReference>();
            foreach (var name in names) {
                references.Add(SceneReference.FromName(name));
            }

            return LoadScenesAsync(references, mode);
        }

        public static void UnloadScene(SceneReference reference) {
            _ = UnloadSceneAsync(reference);
        }

        public static void UnloadScene(int buildIndex) {
            _ = UnloadSceneAsync(buildIndex);
        }

        public static void UnloadScene(string name) {
            _ = UnloadSceneAsync(name);
        }

        public static void UnloadScenes(IEnumerable<SceneReference> references) {
            _ = UnloadScenesAsync(references);
        }

        public static void UnloadScenes(IEnumerable<int> buildIndices) {
            _ = UnloadScenesAsync(buildIndices);
        }

        public static void UnloadScenes(IEnumerable<string> names) {
            _ = UnloadScenesAsync(names);
        }

        public static Awaitable UnloadSceneAsync(SceneReference reference) {
            using var pool = HashSetPool<UniScene>.Get(out var targets);
            AddUnloadTarget(reference, targets);

            return UnloadTargetsAsync(targets);
        }

        public static Awaitable UnloadSceneAsync(int buildIndex) {
            return UnloadSceneAsync(SceneReference.FromBuildIndex(buildIndex));
        }

        public static Awaitable UnloadSceneAsync(string name) {
            return UnloadSceneAsync(SceneReference.FromName(name));
        }

        public static Awaitable UnloadScenesAsync(IEnumerable<SceneReference> references) {
            using var pool = HashSetPool<UniScene>.Get(out var targets);
            if (references != null) {
                foreach (var reference in references) {
                    AddUnloadTarget(reference, targets);
                }
            }

            return UnloadTargetsAsync(targets);
        }

        private static void AddUnloadTarget(SceneReference reference, HashSet<UniScene> targets) {
            if (reference.type == SceneReferenceType.None) {
                Debug.LogError($"Scene (GUID: {reference.guid}) cannot be unloaded. Assign a scene and add it to the Build Profile scene list or to Addressables.");
                return;
            }

            if (reference.isLoaded) {
                targets.Add(reference.scene);
            }
        }

        // async so no targets still returns an Awaitable that can be awaited, not null.
        // Everything before the await runs synchronously, so callers may release the pooled targets once it returns.
        private static async Awaitable UnloadTargetsAsync(HashSet<UniScene> targets) {
            if (targets.Count == 0) {
                return;
            }

            foreach (var scene in targets) {
                // Pinning only protects scenes from CleanUp; naming a scene here unloads it anyway.
                Unpin(scene);
            }

            // CreateUnload copies the targets, so the pooled set can be released right after.
            var transition = Transition.Transition.CreateUnload(targets);

            await transition
                .NotifyExit()
                .Unload()
                .ExecuteAsync();
        }

        public static Awaitable UnloadScenesAsync(IEnumerable<int> buildIndices) {
            var references = new List<SceneReference>();
            foreach (var index in buildIndices) {
                references.Add(SceneReference.FromBuildIndex(index));
            }

            return UnloadScenesAsync(references);
        }

        public static Awaitable UnloadScenesAsync(IEnumerable<string> names) {
            var references = new List<SceneReference>();
            foreach (var name in names) {
                references.Add(SceneReference.FromName(name));
            }

            return UnloadScenesAsync(references);
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

        internal static async Awaitable LoadSceneAsync_Internal(SceneLoader loader, bool activateOnLoad = true) {
            if (loader == null || loader.scene.isLoaded) {
                return;
            }

            await loader.LoadAsync(activateOnLoad);

            // A scene that isn't activated yet has no objects; Activate registers it.
            if (activateOnLoad) {
                RegisterLoadedScene(loader);
            }
        }

        internal static async Awaitable ActivateSceneAsync_Internal(SceneLoader loader) {
            if (loader == null) {
                return;
            }

            await loader.ActivateAsync();
            RegisterLoadedScene(loader);
        }

        private static void RegisterLoadedScene(SceneLoader loader) {
            // Not loaded: the load failed. Already cached: it was activated on load.
            if (!loader.scene.IsValid() || !loader.scene.isLoaded || _handlersCache.ContainsKey(loader.scene)) {
                return;
            }

            sceneLoaded?.Invoke(loader.scene);

            RemoveBarrierIfExists();

            _loadedScenes.Add(loader.scene);

            var handlers = new SceneHandlerCollection();
            handlers.Initialize(loader.scene);

            _handlersCache[loader.scene] = handlers;
        }

        internal static async Awaitable UnloadSceneAsync_Internal(SceneLoader loader) {
            if (loader?.scene.isLoaded != true) {
                return;
            }

            if (SceneManagementSettings.useBootstrap && loader.scene == SceneManagementSettings.bootstrapScene.scene) {
                // Debug.LogWarning("Cannot unload the bootstrap scene.");
                return;
            }

            CreateBarrierIfNeeded();

            await loader.UnloadAsync();
            sceneUnloaded?.Invoke(loader.scene);
            
            _loadedScenes.Remove(loader.scene);
            // An unloaded scene's handle is dead; an explicitly unloaded pinned scene must not stay pinned.
            _pinnedScenes.Remove(loader.scene);

            if (_handlersCache.TryGetValue(loader.scene, out var handlers)) {
                handlers.Clear();
                _handlersCache.Remove(loader.scene);
            }
        }

        internal static Awaitable NotifyEnter_Internal(UniScene scene, bool global = false) {
            if (_handlersCache.TryGetValue(scene, out var handlers)) {
                var list = global ? handlers.enter.always : handlers.enter.normal;

                for (int i = 0; i < list.Count; i++) {
                    var handler = list[i];
                    if (IsHandlerDestroyed(handler)) continue;

                    // One failing handler must not stop the others or leave the transition half done.
                    try {
                        handler.OnSceneEnter();
                    }
                    catch (Exception e) {
                        Debug.LogException(e, handler as MonoBehaviour);
                    }
                }
            }

            return null;
        }

        internal static async Awaitable NotifyAsyncEnter_Internal(UniScene scene, bool global = false) {
            if (!_handlersCache.TryGetValue(scene, out var handlers)) {
                return;
            }

            using var pool = ListPool<Awaitable>.Get(out var awaitables);

            // A handler may await a transition of its own, which must not queue behind this one.
            Transition.Transition.BeginUserCode();
            try {
                var list = global ? handlers.asyncEnter.always : handlers.asyncEnter.normal;
                for (int i = 0; i < list.Count; i++) {
                    var handler = list[i];
                    if (IsHandlerDestroyed(handler)) continue;

                    try {
                        awaitables.Add(handler.OnSceneEnterAsync());
                    }
                    catch (Exception e) {
                        Debug.LogException(e, handler as MonoBehaviour);
                    }
                }

                await AwaitAllLogged(awaitables);
            }
            finally {
                Transition.Transition.EndUserCode();
            }
        }

        internal static Awaitable NotifyExit_Internal(UniScene scene, bool global = false) {
            if (_handlersCache.TryGetValue(scene, out var handlers)) {
                var list = global ? handlers.exit.always : handlers.exit.normal;
                
                for (int i = 0; i < list.Count; i++) {
                    var handler = list[i];
                    if (IsHandlerDestroyed(handler)) continue;

                    try {
                        handler.OnSceneExit();
                    }
                    catch (Exception e) {
                        Debug.LogException(e, handler as MonoBehaviour);
                    }
                }
            }
            
            return null;
        }

        internal static async Awaitable NotifyAsyncExit_Internal(UniScene scene, bool global = false) {
            if (!_handlersCache.TryGetValue(scene, out var handlers)) {
                return;
            }

            using var pool = ListPool<Awaitable>.Get(out var awaitables);

            Transition.Transition.BeginUserCode();
            try {
                var list = global ? handlers.asyncExit.always : handlers.asyncExit.normal;
                for (int i = 0; i < list.Count; i++) {
                    var handler = list[i];
                    if (IsHandlerDestroyed(handler)) continue;

                    try {
                        awaitables.Add(handler.OnSceneExitAsync());
                    }
                    catch (Exception e) {
                        Debug.LogException(e, handler as MonoBehaviour);
                    }
                }

                await AwaitAllLogged(awaitables);
            }
            finally {
                Transition.Transition.EndUserCode();
            }
        }

        private static async Awaitable AwaitAllLogged(List<Awaitable> awaitables) {
            foreach (var awaitable in awaitables) {
                if (awaitable == null) {
                    continue;
                }

                try {
                    await awaitable;
                }
                catch (OperationCanceledException) {
                    // Cancelled on purpose (e.g. exiting Play Mode); nothing to report.
                }
                catch (Exception e) {
                    Debug.LogException(e);
                }
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
