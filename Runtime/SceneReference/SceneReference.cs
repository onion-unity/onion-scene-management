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

        public static SceneReference FromPath(string path) {
            if (!ScenePathDictionary.pathToGuid.TryGetValue(path, out var guid)) {
                throw new InvalidOperationException($"Scene with path '{path}' not found.");
            }

            return new(guid);
        }

        public static SceneReference FromAddress(string address) {
#if ONION_ADDRESSABLES
            if (!SceneAddressDictionary.addressToGuid.TryGetValue(address, out var guid)) {
                throw new InvalidOperationException($"Scene with address '{address}' not found.");
            }

            return new(guid);
#else
            throw new NotSupportedException("SceneReference.FromAddress is not supported because the addressable integration is not enabled.");
#endif
        }

        public static SceneReference FromName(string name) {
            if (!ScenePathDictionary.nameToGuid.TryGetValue(name, out var guid)) {
                throw new InvalidOperationException($"Scene with name '{name}' not found.");
            }

            return new(guid);
        }

        public static SceneReference FromBuildIndex(int buildIndex) {
            var path = SceneUtility.GetScenePathByBuildIndex(buildIndex);

            if (string.IsNullOrEmpty(path)) {
                throw new InvalidOperationException($"Scene with build index '{buildIndex}' not found.");
            }

            return FromPath(path);
        }

        public static SceneReference FromScene(Scene scene) {
            if (!scene.IsValid()) {
                throw new InvalidOperationException("Scene is not valid.");
            }

            return FromPath(scene.path);
        }

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