using System;
using Onion.SceneManagement.Setting;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Onion.SceneManagement.Editor {
    /// <summary>
    /// Adds the settings to Preloaded Assets only while a build runs, like the Input System does,
    /// so Project Settings aren't changed permanently.
    /// </summary>
    internal sealed class SceneManagementSettingsBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport {
        private static SceneManagementSettingsAsset _added;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) {
            var settings = SceneManagementSettingsAsset.instance;
            if (settings == null) {
                Debug.LogWarning("Scene Management Settings asset is not assigned. The build will use the default settings.");
                return;
            }

            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (Array.IndexOf(preloadedAssets, settings) >= 0) {
                return;
            }

            Array.Resize(ref preloadedAssets, preloadedAssets.Length + 1);
            preloadedAssets[^1] = settings;
            PlayerSettings.SetPreloadedAssets(preloadedAssets);

            _added = settings;
        }

        // Not called when the build fails; SceneManagementSettingsProvider removes the entry on the next editor load.
        public void OnPostprocessBuild(BuildReport report) {
            if (_added == null) {
                return;
            }

            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            int index = Array.IndexOf(preloadedAssets, _added);
            _added = null;

            if (index < 0) {
                return;
            }

            var assets = new UnityEngine.Object[preloadedAssets.Length - 1];
            Array.Copy(preloadedAssets, 0, assets, 0, index);
            Array.Copy(preloadedAssets, index + 1, assets, index, assets.Length - index);
            PlayerSettings.SetPreloadedAssets(assets);
        }
    }
}
