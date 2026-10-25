using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Onion.SceneManagement {
    [InitializeOnLoad]
    internal static class SceneManagementSettingsProvider {
        private static readonly UnityPath settingsDirectory = new("Assets/Settings/");
        private static readonly UnityPath settingsPath = new(Path.Combine(settingsDirectory, "Onion_SceneManagementSetting.asset"));
        // private static readonly UnityPath settingsProviderPath = new("Project/Onion Scene Management");

        // [SettingsProvider]
        // public static SettingsProvider CreateProvider() {
        //     var provider = new SettingsProvider(settingsProviderPath, SettingsScope.Project) {
        //         guiHandler = OnGUI
        //     };

        //     return provider;
        // }

        // private static void OnGUI(string searchContext) {
        //     EditorGUILayout.Space();
        //     EditorGUI.BeginChangeCheck();

        //     var settings = GetOrCreateSettings();
        //     if (settings == null) {
        //         EditorGUILayout.HelpBox("Scene Management Settings is not assigned. Please assign it to ensure proper functionality.", MessageType.Warning);

        //         if (GUILayout.Button("Create Or Assign Settings")) {
        //             settings = GetOrCreateSettings();
        //             EditorGUIUtility.PingObject(settings);
        //         }
        //     }

        //     var asset = EditorGUILayout.ObjectField("Settings", settings, typeof(SceneManagementSettings), false) as SceneManagementSettings;
        //     if (EditorGUI.EndChangeCheck()) {
        //         RegisterToPreloadAssets(asset);
        //     }
        // }

        static SceneManagementSettingsProvider() {
            // Never create assets on build servers.
            if (Application.isBatchMode) {
                return;
            }

            EditorApplication.delayCall += () => GetOrCreateSettings();
        }

        internal static SceneManagementSettingsAsset GetSettings() {
            return SceneManagementSettingsAsset.instance;
        }

        internal static void AssignSettings(SceneManagementSettingsAsset settings) {
            EditorBuildSettings.AddConfigObject(SceneManagementSettingsAsset.ConfigKey, settings, true);
            SceneManagementSettings.Reload_Internal();
        }

        internal static SceneManagementSettingsAsset GetOrCreateSettings() {
            SceneManagementSettingsAsset asset = GetSettings();
            if (asset == null) {
                asset = FindPreloadedSettings();
            }

            if (asset == null) {
                string[] guids = AssetDatabase.FindAssets($"t:{nameof(SceneManagementSettingsAsset)}");
                if (guids.Length > 0) {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);

                    asset = AssetDatabase.LoadAssetAtPath<SceneManagementSettingsAsset>(path);
                }
                else {
                    asset = ScriptableObject.CreateInstance<SceneManagementSettingsAsset>();

                    if (!Directory.Exists(settingsDirectory.platformPath)) {
                        Directory.CreateDirectory(settingsDirectory.platformPath);
                    }

                    AssetDatabase.CreateAsset(asset, settingsPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            AssignSettings(asset);
            RemoveLegacyData(asset);
            return asset;
        }

        // Projects from before the config key kept the settings registered in Preloaded Assets.
        private static SceneManagementSettingsAsset FindPreloadedSettings() {
            foreach (var obj in PlayerSettings.GetPreloadedAssets()) {
                if (obj is SceneManagementSettingsAsset settings) {
                    return settings;
                }
            }

            return null;
        }

        /// <summary>
        /// Removes what earlier versions left behind: the permanent Preloaded Assets entry
        /// (also left by a build that failed before its postprocess) and the saved hideFlags.
        /// </summary>
        private static void RemoveLegacyData(SceneManagementSettingsAsset settings) {
            RemoveFromPreloadedAssets();

            if (settings.hideFlags != HideFlags.None) {
                settings.hideFlags = HideFlags.None;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }
        }

        private static void RemoveFromPreloadedAssets() {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (Array.FindIndex(preloadedAssets, obj => obj is SceneManagementSettingsAsset) < 0) {
                return;
            }

            using var pool = ListPool<UnityEngine.Object>.Get(out var list);

            foreach (var asset in preloadedAssets) {
                if (asset is SceneManagementSettingsAsset) {
                    continue;
                }

                list.Add(asset);
            }

            PlayerSettings.SetPreloadedAssets(list.ToArray());
        }
    }
}
