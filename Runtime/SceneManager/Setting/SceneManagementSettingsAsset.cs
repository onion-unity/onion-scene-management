using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Onion.SceneManagement.Setting {
    internal sealed class SceneManagementSettingsAsset : ScriptableObject {
        private static SceneManagementSettingsAsset _instance;
        internal static SceneManagementSettingsAsset instance {
            get => _instance;
            set {
                if (_instance == value) {
                    return;
                }

                if (_instance != null) {
                    _instance.hideFlags = HideFlags.None;
                }

                _instance = value;
                if (_instance == null) {
                    return;
                }
                
                _instance.hideFlags = HideFlags.DontUnloadUnusedAsset;
                SceneManagementSettings.LoadFromAsset_Internal(value);
            }
        }

        public bool useBootstrapScene = false;

        [SerializeField]
        internal SceneReference bootstrapScene;
        
        public bool overlapLoading = false;
        
        private void OnEnable() {
#if UNITY_EDITOR
            bool foundInPreloadedAssets = false;
            foreach (var obj in PlayerSettings.GetPreloadedAssets()) {
                if (obj is SceneManagementSettingsAsset asset && asset == this) {
                    foundInPreloadedAssets = true;
                    break;
                }
            }

            if (!foundInPreloadedAssets) {
                return;
            }
#endif
            instance = this;
        }

        private void OnValidate() {
            if (instance != this) {
                return;
            }
            
            SceneManagementSettings.LoadFromAsset_Internal(this);
        }
    }
}