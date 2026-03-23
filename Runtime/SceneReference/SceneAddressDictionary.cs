using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using Onion.SceneManagement.Editor;
#endif

namespace Onion.SceneManagement {
    internal static class SceneAddressDictionary {
        [Serializable]
        internal struct Entry {
            public string guid;
            public string address;
        }

        [Serializable]
        internal class Table {
            public List<Entry> entries;
        }

        private static Dictionary<string, string> _guidToAddress;
        private static Dictionary<string, string> _addressToGuid;

        internal static IReadOnlyDictionary<string, string> guidToAddress {
            get {
                EnsureInitialized();
                return _guidToAddress;
            }
        }

        internal static IReadOnlyDictionary<string, string> addressToGuid {
            get {
                EnsureInitialized();
                return _addressToGuid;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() {
            EnsureInitialized();
        }

        private static void EnsureInitialized() {
            if (_guidToAddress != null && _addressToGuid != null) {
                return;
            }

            var json = LoadJson_Internal();
            if (string.IsNullOrEmpty(json)) {
                Debug.LogError("Failed to load scene dictionary file.");
                LoadFromTable(null);

                return;
            }

            var table = JsonUtility.FromJson<Table>(json);  
            LoadFromTable(table);
        }

        public static void LoadFromTable(Table table) {
            _guidToAddress ??= new();
            _addressToGuid ??= new();

            _guidToAddress.Clear();
            _addressToGuid.Clear();

            if (table == null || table.entries == null) {
                return;
            }

            _guidToAddress.EnsureCapacity(table.entries.Count);
            _addressToGuid.EnsureCapacity(table.entries.Count);

            foreach (var entry in table.entries) {
                _guidToAddress[entry.guid] = entry.address;
                _addressToGuid[entry.address] = entry.guid;
            }
        }

        private static string LoadJson_Internal() {
            string json = null;
#if UNITY_EDITOR
            json = SceneDictionaryEditorStorage.guidToAddressDictionaryJson;
#else
            json = SceneDictionaryStorage.guidToAddressDictionary;
#endif

            return json;
        }
    }
}