using UnityEngine;
using System;


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

        public bool overlapLoading = false;

        public bool useBootstrap = false;

        [SerializeField]
        internal SceneReference bootstrapScene;


#if UNITY_EDITOR
        private BootstrapGroup _bootstrapGroup;
        internal BootstrapGroup bootstrapGroup {
            get {
                if (_bootstrapGroup == null) {
                    var asset = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this));
                    foreach (var obj in asset) {
                        if (obj is BootstrapGroup group) {
                            _bootstrapGroup = group;
                            break;
                        }
                    }

                    if (_bootstrapGroup == null) {
                        _bootstrapGroup = CreateInstance<BootstrapGroup>();
                        _bootstrapGroup.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.DontSave;
                        
                        AssetDatabase.AddObjectToAsset(_bootstrapGroup, this);
                        AssetDatabase.SaveAssets();
                    }
                }

                return _bootstrapGroup;
            }
        }
#endif
        
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