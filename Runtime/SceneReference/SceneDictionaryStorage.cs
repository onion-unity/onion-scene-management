using System.IO;
using Onion.SceneManagement.Utility;
using UnityEngine;

namespace Onion.SceneManagement {
    internal static class SceneDictionaryStorage {
        public const string PathPrefix = "Onion.SceneManagement.";
        public const string GuidToPathDictionaryKey = PathPrefix + "GuidToPath";
        public const string GuidToAddressDictionaryKey = PathPrefix + "GuidToAddress";

        public readonly static UnityPath assetFolderPath = new(Application.dataPath);
        public readonly static UnityPath resourceFolderPath = new(Path.Combine(assetFolderPath, "Resources"));
        public readonly static UnityPath guidToPathDictionaryPath = new(Path.Combine(resourceFolderPath, $"{GuidToPathDictionaryKey}.json"));
        public readonly static UnityPath guidToAddressDictionaryPath = new(Path.Combine(resourceFolderPath, $"{GuidToAddressDictionaryKey}.json"));

        public static string guidToPathDictionary {
            get => LoadResource(guidToPathDictionaryPath);
        }

        public static string guidToAddressDictionary {
            get => LoadResource(guidToAddressDictionaryPath);
        }

        private static string LoadResource(UnityPath path) {
            var asset = Resources.Load<TextAsset>(path.fileNameWithoutExtension);

            if (asset == null) {
                return string.Empty;
            }
            
            return asset.text;
        }
    }
}