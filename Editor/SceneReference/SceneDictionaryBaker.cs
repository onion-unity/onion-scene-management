using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Onion.SceneManagement.Utility;
using UnityEngine;


#if ONION_ADDRESSABLES
using UnityEditor.AddressableAssets;
#endif

namespace Onion.SceneManagement.Editor {
    internal static class SceneDictionaryBaker {
        internal static void Bake(bool withFiles) {
            try {
                string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");

                var pathTable = GetScenePathDictionaryTable(sceneGuids);
                var addressTable = GetSceneAddressDictionaryTable(sceneGuids);

                var pathTableJson = JsonUtility.ToJson(pathTable);
                var addressTableJson = JsonUtility.ToJson(addressTable);

                if (withFiles) {
                    Directory.CreateDirectory(SceneDictionaryStorage.resourceFolderPath);

                    File.WriteAllText(SceneDictionaryStorage.guidToPathDictionaryPath.platformPath, pathTableJson);
                    File.WriteAllText(SceneDictionaryStorage.guidToAddressDictionaryPath.platformPath, addressTableJson);
                }

                SceneDictionaryEditorStorage.guidToPathDictionaryJson = pathTableJson;
                SceneDictionaryEditorStorage.guidToAddressDictionaryJson = addressTableJson;

                ScenePathDictionary.LoadFromTable(pathTable);
                SceneAddressDictionary.LoadFromTable(addressTable);
            }
            finally {
                AssetDatabase.Refresh();
            }
        }

        private static ScenePathDictionary.Table GetScenePathDictionaryTable(string[] guids) {
            var table = new ScenePathDictionary.Table();
            foreach (string guid in guids) {
                table.entries.Add(new() {
                    guid = guid,
                    path = AssetDatabase.GUIDToAssetPath(guid)
                });
            }

            return table;
        }

        private static SceneAddressDictionary.Table GetSceneAddressDictionaryTable(string[] guids) {
            var table = new SceneAddressDictionary.Table();
#if ONION_ADDRESSABLES
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null) {
                foreach (string guid in guids) {
                    var entry = settings.FindAssetEntry(guid);
                    if (entry == null) {
                        continue;
                    }

                    table.entries.Add(new() {
                        guid = guid,
                        address = entry.address
                    });
                }
            }
#endif
            return table;
        }

        internal static void Clear() {
            DeleteAssetIfExists(SceneDictionaryStorage.guidToPathDictionaryPath);
            DeleteAssetIfExists(SceneDictionaryStorage.guidToAddressDictionaryPath);
        }

        private static void DeleteAssetIfExists(UnityPath path) {
            if (File.Exists(path.platformPath)) {
                File.Delete(path.platformPath);
            }

            if (File.Exists(path.metaPath)) {
                File.Delete(path.metaPath);
            }
        }
    }
}