#if UNITY_EDITOR

using System.IO;
using Onion.SceneManagement.Utility;
using UnityEngine;

namespace Onion.SceneManagement.Editor {
    internal static class SceneDictionaryEditorStorage {
        private readonly static UnityPath userSettingPath 
            = new(Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "UserSettings"));
        public readonly static UnityPath guidToPathDictionaryPath 
            = new(Path.Combine(userSettingPath, $"{SceneDictionaryStorage.GuidToPathDictionaryKey}.json"));
        public readonly static UnityPath guidToAddressDictionaryPath 
            = new(Path.Combine(userSettingPath, $"{SceneDictionaryStorage.GuidToAddressDictionaryKey}.json"));

        public static string guidToPathDictionaryJson {
            get => LoadRaw(guidToPathDictionaryPath);
            set => SaveRaw(guidToPathDictionaryPath, value);
        }

        public static string guidToAddressDictionaryJson {
            get => LoadRaw(guidToAddressDictionaryPath);
            set => SaveRaw(guidToAddressDictionaryPath, value);
        }

        private static void SaveRaw(UnityPath path, string json) {
            if (!Directory.Exists(userSettingPath)) {
                Directory.CreateDirectory(userSettingPath);
            }

            File.WriteAllText(path, json);
        }

        private static string LoadRaw(UnityPath path) {
            if (!File.Exists(path)) {
                return string.Empty;
            }

            return File.ReadAllText(path);
        }
    }
}

#endif