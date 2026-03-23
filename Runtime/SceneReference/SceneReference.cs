using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Onion.SceneManagement {
    [Serializable]
    public class SceneReference : ISerializationCallbackReceiver {
        internal const string defaultGuid = "00000000000000000000000000000000";

#if UNITY_EDITOR
        [SerializeField] 
        private SceneAsset _asset = null;
#endif

        [SerializeField] 
        [HideInInspector]
        private string _guid = null;
        internal string guid => GetSafeGuid(_guid);
        
        public string path {
            get {
                if (!IsValidGuid()) {
                    throw new InvalidOperationException("SceneReference does not contain a valid GUID.");
                }

                if (!ScenePathDictionary.guidToPath.TryGetValue(guid, out var path)) {
                    throw new InvalidOperationException($"Scene with GUID '{guid}' not found in the scene dictionary.");
                }

                return path;
            }
        }
        public int buildIndex => SceneUtility.GetBuildIndexByScenePath(path);
        public string name => System.IO.Path.GetFileNameWithoutExtension(path);
        public Scene scene => SceneManager.GetSceneByPath(path);

        private bool IsValidGuid() => guid != defaultGuid;
        private string GetSafeGuid(string guid) {
            if (string.IsNullOrEmpty(guid)) return defaultGuid;
            if (guid.Length != defaultGuid.Length) return defaultGuid;

            return guid;
        }

        public SceneReference() { }

        public SceneReference(string guid) {
            if (!ScenePathDictionary.guidToPath.ContainsKey(GetSafeGuid(guid))) {
                return;
            }

            _guid = guid;
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePathDictionary.guidToPath[guid]);
            if (asset == null) {
                return;
            }

            _asset = asset;
#endif
        }

#if UNITY_EDITOR
        internal SceneReference(SceneAsset asset) {
            if (asset == null) {
                return;
            }

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out _)) {
                _guid = guid;
                _asset = asset;
            }
        }
#endif

        public void OnBeforeSerialize() {
#if UNITY_EDITOR
            if (_asset != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_asset, out var guid, out _)) {
                _guid = GetSafeGuid(guid);
            }
            else {
                _guid = string.Empty;
            }
#endif
        }

        public void OnAfterDeserialize() {
#if UNITY_EDITOR

#endif
        }
    }
} 