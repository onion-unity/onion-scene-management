using UnityEngine;
using UniScene = UnityEngine.SceneManagement.Scene; 
using UniSceneManagement = UnityEngine.SceneManagement;
using UniSceneManager = UnityEngine.SceneManagement.SceneManager;

#if ONION_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

namespace Onion.SceneManagement {
    internal abstract class SceneLoader {
        protected SceneReference reference { get; }

        private UniScene _scene;
        public UniScene scene {
            get => _scene;
            protected set {
                if (value == _scene) return;

                _scene = value;
                _scene.Register(loader: this);
            }
        }

        protected SceneLoader(SceneReference reference) {
            this.reference = reference;
        }

        public abstract Awaitable<bool> LoadAsync();
        public abstract Awaitable UnloadAsync();
    }

    internal class BuiltInSceneLoader : SceneLoader {
        private const float loadThreshold = 0.9f;
        public BuiltInSceneLoader(SceneReference reference) : base(reference) { }

        public override async Awaitable<bool> LoadAsync() {
            UniSceneManager.sceneLoaded += OnSceneLoaded;

            var operation = UniSceneManager.LoadSceneAsync(
                reference.path,
                UniSceneManagement.LoadSceneMode.Additive);
            operation.allowSceneActivation = false;

            while (operation.progress < loadThreshold) {
                await Awaitable.NextFrameAsync();
            }

            operation.allowSceneActivation = true;

            await operation;
            await Awaitable.NextFrameAsync(); // wait for scene to be fully loaded
            UniSceneManager.sceneLoaded -= OnSceneLoaded;

            return scene.isLoaded;
        }

        public override async Awaitable UnloadAsync() {
            await UniSceneManager.UnloadSceneAsync(scene);

            scene.Unregister();
        }

        private void OnSceneLoaded(UniScene loadedScene, UniSceneManagement.LoadSceneMode mode) {
            if (loadedScene.HasLoader()) return;
            if (loadedScene.path == reference.path) {
                scene = loadedScene;
            }
        }
    }

    internal class AddressableSceneLoader : SceneLoader {
        public AddressableSceneLoader(SceneReference reference) : base(reference) { }

#if ONION_ADDRESSABLES
        private AsyncOperationHandle<SceneInstance> _handle;
#endif

        public override async Awaitable<bool> LoadAsync() {
#if ONION_ADDRESSABLES
            _handle = Addressables.LoadSceneAsync(reference.path, UniSceneManagement.LoadSceneMode.Additive);

            var instance = await _handle.Task;
            scene = instance.Scene;

            return _handle.Status == AsyncOperationStatus.Succeeded;
#else
#pragma warning disable 1998
            throw new System.NotSupportedException("Addressable scene loading is not supported.");
#pragma warning restore 1998
#endif
        }

        public override async Awaitable UnloadAsync() {
#if ONION_ADDRESSABLES
            if (!_handle.IsValid()) return;
            
            await Addressables.UnloadSceneAsync(_handle, true).Task;
            _handle = default;

            scene.Unregister();
#else
#pragma warning disable 1998
            throw new System.NotSupportedException("Addressable scene unloading is not supported.");
#pragma warning restore 1998
#endif
        }
    }
}