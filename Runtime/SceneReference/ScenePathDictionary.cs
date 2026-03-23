using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


#if UNITY_EDITOR
using Onion.SceneManagement.Editor;
#endif

namespace Onion.SceneManagement {
    internal static class ScenePathDictionary {
        [Serializable]
        internal struct Entry {
            public string guid;
            public string path;
        }

        [Serializable]
        internal class Table {
            public List<Entry> entries = new();
        }

        private static Dictionary<string, string> _guidToPath = null;
        private static Dictionary<string, string> _pathToGuid = null;
        private static Dictionary<string, string> _nameToGuid = null;

        internal static IReadOnlyDictionary<string, string> guidToPath {
            get {
                EnsureInitialized();
                return _guidToPath;
            }
        }

        internal static IReadOnlyDictionary<string, string> pathToGuid {
            get {
                EnsureInitialized();
                return _pathToGuid;
            }
        }

        internal static IReadOnlyDictionary<string, string> nameToGuid {
            get {
                EnsureInitialized();
                return _nameToGuid;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() {
            EnsureInitialized();
        }

        private static void EnsureInitialized() {
            if (_guidToPath != null && _pathToGuid != null && _nameToGuid != null) {
                return;
            }

            var json = LoadJson_Internal();
            if (string.IsNullOrEmpty(json)) {
                LoadFromTable(null);
            
                return;
            }

            var table = JsonUtility.FromJson<Table>(json);
            LoadFromTable(table);
        }

        public static void LoadFromTable(Table table) {
            _guidToPath ??= new(); _pathToGuid ??= new(); _nameToGuid ??= new();
            _guidToPath.Clear(); _pathToGuid.Clear(); _nameToGuid.Clear();

            if (table == null || table.entries == null) {
                return;
            }

            _guidToPath.EnsureCapacity(table.entries.Count);
            _pathToGuid.EnsureCapacity(table.entries.Count);
            _nameToGuid.EnsureCapacity(table.entries.Count);

            foreach (var entry in table.entries) {
                _guidToPath[entry.guid] = entry.path;
                _pathToGuid[entry.path] = entry.guid;

                var name = Path.GetFileNameWithoutExtension(entry.path);
                if (_nameToGuid.ContainsKey(name)) {
                    Debug.LogWarning($"Duplicate scene name detected: {name}.");
                    continue;
                }

                _nameToGuid[name] = entry.guid;
            }
        }

        private static string LoadJson_Internal() {
            string json = null;
#if UNITY_EDITOR
            json = SceneDictionaryEditorStorage.guidToPathDictionaryJson;
#else
            json = SceneDictionaryStorage.guidToPathDictionary;
#endif

            return json;
        }
    }
}