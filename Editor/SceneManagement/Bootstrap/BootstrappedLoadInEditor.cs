using Onion.SceneManagement.Setting;
using Onion.SceneManagement.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Onion.SceneManagement.Editor {
    [InitializeOnLoad]
    internal static class BootstrappedLoad {
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

            var group = SceneManagementSettings.bootstrapGroup;
            if (group != null) {
                await group.LoadAsync();
            }
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
                    var group = SceneManagementSettings.bootstrapGroup;
                    group.scenes.Clear();

                    int sceneCount = EditorSceneManager.sceneCount;
                    for (int i = 0; i < sceneCount; i++) {
                        var scene = EditorSceneManager.GetSceneAt(i);
                        if (!scene.isLoaded) {
                            continue;
                        }

                        group.scenes.Add(SceneReference.FromPath(scene.path));
                    }

                    EditorSceneManager.playModeStartScene = bootstrapScene.asset;

                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    EditorSceneManager.playModeStartScene = null;

                    break;
            }
        }
    }
}