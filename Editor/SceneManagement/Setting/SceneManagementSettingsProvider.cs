using System;
using System.IO;
using Onion.SceneManagement.Setting;
using Onion.SceneManagement.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Onion.SceneManagement.Editor {
    internal static class SceneManagementSettingsProvider {
        private static readonly UnityPath settingsDirectory = new("Assets/Settings/");
        private static readonly UnityPath settingsPath = new(Path.Combine(settingsDirectory, "Onion_SceneManagementSetting.asset"));
        private static readonly UnityPath settingsProviderPath = new("Project/Onion Scene Management");

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

        internal static SceneManagementSettingsAsset GetSettings() {
            foreach (var obj in PlayerSettings.GetPreloadedAssets()) {
                if (obj is SceneManagementSettingsAsset settings) {
                    return settings;
                }
            }

            return null;
        }

        internal static void AssignSettings(SceneManagementSettingsAsset settings) {
            RegisterToPreloadAssets(settings);

            SceneManagementSettingsAsset.instance = settings;
        }

        internal static SceneManagementSettingsAsset GetOrCreateSettings() {
            SceneManagementSettingsAsset asset = GetSettings();
            if (asset != null) {
                return asset;
            }

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(SceneManagementSettingsAsset)}");
            if (guids.Length > 0) {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);

                asset = AssetDatabase.LoadAssetAtPath<SceneManagementSettingsAsset>(path);
            }
            else {
                asset = ScriptableObject.CreateInstance<SceneManagementSettingsAsset>();
                // SceneManagementSettings.Sync_Internal(asset);

                if (!Directory.Exists(settingsDirectory.platformPath)) {
                    Directory.CreateDirectory(settingsDirectory.platformPath);
                }

                AssetDatabase.CreateAsset(asset, settingsPath);
                AssetDatabase.SaveAssets();
            }

            AssignSettings(asset);
            return asset;
        }

        private static void RegisterToPreloadAssets(SceneManagementSettingsAsset settings) {
            using var pool = ListPool<UnityEngine.Object>.Get(out var list);

            foreach (var asset in PlayerSettings.GetPreloadedAssets()) {
                if (asset is SceneManagementSettingsAsset) {
                    continue;
                }

                list.Add(asset);
            }

            if (settings != null) {
                list.Add(settings);
            }
            
            PlayerSettings.SetPreloadedAssets(list.ToArray());
        }
    }
}