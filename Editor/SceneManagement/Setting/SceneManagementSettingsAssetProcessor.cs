using UnityEditor;
using UnityEngine;

namespace Onion.SceneManagement.Editor {
    internal class SceneManagementSettingsAssetProcessor : AssetPostprocessor {
        private const string deletedAssetMessage = "Don't delete the Scene Management Settings asset!\nIt is required for the proper functioning of the Onion Scene Management system.\nA new settings asset has been created to replace the deleted one.";

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            if (deletedAssets.Length > 0) {
                EditorApplication.delayCall += ValidateSettings;
            }
            else if (importedAssets.Length > 0) {
                EditorApplication.delayCall += InitializeOnLoadMethod;
            }
        }

        private static void ValidateSettings() {
            if (SceneManagementSettingsProvider.GetSettings() != null) {
                return;
            }

            SceneManagementSettingsProvider.GetOrCreateSettings();
            var settings = SceneManagementSettingsProvider.GetSettings();
            if (settings != null) {
                EditorGUIUtility.PingObject(settings);
                Debug.LogError(deletedAssetMessage);
            }
        }

        private static void InitializeOnLoadMethod() {
            if (SceneManagementSettingsProvider.GetSettings() != null) {
                return;
            }
            SceneManagementSettingsProvider.GetOrCreateSettings();
        }
    }
}