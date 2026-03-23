using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Onion.SceneManagement.Editor.Processor {
    internal class SceneAssetProcessor : AssetPostprocessor {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            bool hasChanged = false;
            hasChanged |= deletedAssets.Any(IsSceneAsset);
            hasChanged |= movedAssets.Any(IsSceneAsset);
            hasChanged |= movedFromAssetPaths.Any(IsSceneAsset);

            hasChanged |= importedAssets.Any(path => {
                if (!IsSceneAsset(path)) return false;
                if (ScenePathDictionary.pathToGuid.ContainsKey(path)) return false;

                return true; 
            });

            if (!hasChanged) {
                return;
            }

            try {
                SceneDictionaryBaker.Bake(withFiles: false);
            }
            catch (System.Exception e) {
                Debug.LogError($"Failed to bake scene dictionary: {e}");
            }
        }

        private static bool IsSceneAsset(string path) 
            => path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase);
    }
}