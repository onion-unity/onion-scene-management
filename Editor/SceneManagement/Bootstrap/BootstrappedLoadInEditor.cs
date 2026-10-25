using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Onion.SceneManagement {
    [InitializeOnLoad]
    internal static class BootstrappedLoad {
        private const string OpenScenesKey = "Onion.SceneManagement.Bootstrap.OpenScenes";

        static BootstrappedLoad() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize() {
            if (!SceneManagementSettings.canUseBootstrapScene) {
                if (SceneManagementSettings.useBootstrap) {
                    Debug.LogWarning("useBootstrap is enabled but bootstrap scene cannot be used. Please check the settings.", 
                        SceneManagementSettingsAsset.instance);
                }

                return;
            }

            _ = WaitForBootstrapper();
        }

        private static async Awaitable WaitForBootstrapper() {
            await AwaitableEx.WaitWhile(() => !Bootstrapper.isReady, 
                cancellationToken: Application.exitCancellationToken);

            string openScenes = SessionState.GetString(OpenScenesKey, string.Empty);
            SessionState.EraseString(OpenScenesKey);

            if (string.IsNullOrEmpty(openScenes)) {
                return;
            }

            var references = new List<SceneReference>();
            foreach (var path in openScenes.Split('\n')) {
                references.Add(SceneReference.FromPath(path));
            }

            await references.LoadAsync();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (!SceneManagementSettings.canUseBootstrapScene) {
                return;
            }

            var bootstrapScene = SceneManagementSettings.bootstrapScene;
            if (bootstrapScene.type == SceneReferenceType.None) {
                Debug.LogWarning("Bootstrapped Load is enabled but no bootstrap scene is assigned.");
                return;
            }

            switch (state) {
                case PlayModeStateChange.ExitingEditMode:
                    var paths = new List<string>();

                    int sceneCount = EditorSceneManager.sceneCount;
                    for (int i = 0; i < sceneCount; i++) {
                        var scene = EditorSceneManager.GetSceneAt(i);
                        if (!scene.isLoaded) {
                            continue;
                        }

                        paths.Add(scene.path);
                    }

                    // Survives the domain reload into Play Mode, and isn't saved into any asset.
                    SessionState.SetString(OpenScenesKey, string.Join('\n', paths));
                    EditorSceneManager.playModeStartScene = bootstrapScene.asset;

                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    EditorSceneManager.playModeStartScene = null;

                    break;
            }
        }
    }
}