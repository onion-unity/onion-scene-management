using UnityEngine;
using UniScene = UnityEngine.SceneManagement.Scene; 
using UniSceneManagement = UnityEngine.SceneManagement;
using UniSceneManager = UnityEngine.SceneManagement.SceneManager;
using Onion.SceneManagement.Utility;


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
            set {
                if (value == _scene) return;

                _scene = value;
                _scene.Register(loader: this);
            }
        }

        public abstract float progress { get; }
        protected SceneLoader(SceneReference reference) {
            this.reference = reference;
        }

        public abstract Awaitable LoadAsync(bool activateOnLoad = true);
        public abstract Awaitable ActivateAsync();
        public abstract Awaitable UnloadAsync();
    }

    internal class BuiltInSceneLoader : SceneLoader {
        private const float loadThreshold = 0.9f;
        private AsyncOperation _operation;
        public BuiltInSceneLoader(SceneReference reference) : base(reference) { }
        public override float progress => _operation != null ? _operation.progress : 0f;

        public override async Awaitable LoadAsync(bool activateOnLoad = true) {
            UniSceneManager.sceneLoaded += OnSceneLoaded;

            _operation = UniSceneManager.LoadSceneAsync(
                reference.path,
                UniSceneManagement.LoadSceneMode.Additive);
            _operation.allowSceneActivation = activateOnLoad;

            while (_operation.progress < loadThreshold) {
                await Awaitable.NextFrameAsync();
            }

            if (!activateOnLoad) {
                return;
            }

            await _operation;
            await Awaitable.NextFrameAsync(); // wait for scene to be fully loaded

            UniSceneManager.sceneLoaded -= OnSceneLoaded;
            _operation = null;
        }

        public override async Awaitable ActivateAsync() {
            if (_operation == null) return;
            
            _operation.allowSceneActivation = true;
            await AwaitableEx.WaitUntil(_operation, o => o.isDone);
            _operation = null;
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
        private SceneInstance _instance;
#endif
        
        public override float progress {
            get {
#if ONION_ADDRESSABLES
                return _handle.IsValid() ? _handle.PercentComplete : 0f;
#else
                return 0f;
#endif
            }
        }

        public override async Awaitable LoadAsync(bool activateOnLoad = true) {
#if ONION_ADDRESSABLES
            _handle = Addressables.LoadSceneAsync(reference.path, 
                UniSceneManagement.LoadSceneMode.Additive, 
                activateOnLoad);
            
            _instance = await _handle.Task;
            scene = _instance.Scene;
#else
#pragma warning disable 1998
            throw new System.NotSupportedException("Addressable scene loading is not supported.");
#pragma warning restore 1998
#endif
        }

        public override async Awaitable ActivateAsync() {
#if ONION_ADDRESSABLES
            if (_handle.IsValid() && _handle.Status == AsyncOperationStatus.Succeeded) {
                await _instance.ActivateAsync();
            }
#else
#pragma warning disable 1998
            throw new System.NotSupportedException("Addressable scene activation is not supported.");
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