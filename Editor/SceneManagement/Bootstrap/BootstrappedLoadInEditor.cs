using Onion.SceneManagement.Setting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Onion.SceneManagement.Editor {
    [InitializeOnLoad]
    internal static class BootstrappedLoad {
        static BootstrappedLoad() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (!SceneManagementSettings.useBootstrappedLoad) {
                return;
            }

            var bootstrapScene = SceneManagementSettings.bootstrapScene;
            if (bootstrapScene.type == SceneReferenceType.None) {
                Debug.LogWarning("Bootstrapped Load is enabled but no bootstrap scene is assigned.");
                return;
            }

            switch (state) {
                case PlayModeStateChange.ExitingEditMode:
                    EditorSceneManager.playModeStartScene = bootstrapScene.asset;

                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    EditorSceneManager.playModeStartScene = null;

                    break;
            }
        }
    }
}