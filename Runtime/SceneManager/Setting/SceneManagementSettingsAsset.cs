using UnityEngine;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Onion.SceneManagement {
    /// <summary>
    /// Included in builds through Preloaded Assets, only while a build runs;
    /// in the Editor it is looked up through EditorBuildSettings.
    /// </summary>
    internal sealed class SceneManagementSettingsAsset : ScriptableObject {
        internal const string ConfigKey = "com.onion.scenemanagement";

        private static SceneManagementSettingsAsset _instance;
        internal static SceneManagementSettingsAsset instance {
            get {
#if UNITY_EDITOR
                // Not cached, so assigning another asset as the main settings takes effect immediately.
                EditorBuildSettings.TryGetConfigObject(ConfigKey, out _instance);
#endif
                return _instance;
            }
        }


        [Tooltip("Whether to load the new scene first before unloading the current scene.")]
        public bool overlappedLoad = false;
        public bool useBootstrap = false;

        [SerializeField]
        internal SceneReference bootstrapScene;

        [SerializeField]
        internal SceneReference[] initialScenes;

#if !UNITY_EDITOR
        private void OnEnable() {
            _instance = this;
            SceneManagementSettings.Reload_Internal();
        }
#endif

        private void OnValidate() {
            if (instance != this) {
                return;
            }

            SceneManagementSettings.Reload_Internal();
        }
    }
}
