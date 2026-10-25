using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace Onion.SceneManagement {
    /// <summary>
    /// A player always starts with its first enabled scene, so with Use Bootstrap on
    /// that scene must be the Bootstrap scene; nothing can fix the order at runtime.
    /// </summary>
    internal static class BootstrapSceneOrder {
        /// <summary>
        /// The Bootstrap scene's path when Use Bootstrap is on and a scene is assigned; otherwise null.
        /// </summary>
        internal static string GetBootstrapScenePath(SceneManagementSettingsAsset settings) {
            if (settings == null || !settings.useBootstrap || settings.bootstrapScene?.asset == null) {
                return null;
            }

            return AssetDatabase.GetAssetPath(settings.bootstrapScene.asset);
        }

        /// <summary>
        /// Whether the Bootstrap scene is the first enabled scene of the active Build Profile.
        /// </summary>
        internal static bool IsFirstInActiveSceneList(string bootstrapPath) {
            foreach (var scene in EditorBuildSettings.scenes) {
                if (scene.enabled) {
                    return scene.path == bootstrapPath;
                }
            }

            return false;
        }

        /// <summary>
        /// Moves (or adds) the Bootstrap scene to the top of the active Build Profile's scene list, enabled.
        /// </summary>
        internal static void MoveToFirst(string bootstrapPath) {
            var scenes = new List<EditorBuildSettingsScene> { new(bootstrapPath, true) };
            foreach (var scene in EditorBuildSettings.scenes) {
                if (scene.path != bootstrapPath) {
                    scenes.Add(scene);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    /// <summary>
    /// Fails the build when the first scene actually being built isn't the Bootstrap scene.
    /// Checks <see cref="BuildPlayerOptions.scenes"/>, so builds from a non-active Build Profile
    /// or a script with its own scene list are covered too.
    /// </summary>
    internal sealed class BootstrapSceneBuildProcessor : BuildPlayerProcessor {
        public override int callbackOrder => 0;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext) {
            string bootstrapPath = BootstrapSceneOrder.GetBootstrapScenePath(SceneManagementSettingsAsset.instance);
            if (bootstrapPath == null) {
                return;
            }

            var scenes = buildPlayerContext.BuildPlayerOptions.scenes;
            string first = scenes != null && scenes.Length > 0 ? scenes[0] : null;
            if (first == bootstrapPath) {
                return;
            }

            throw new BuildFailedException(
                $"Use Bootstrap is on, so the Bootstrap scene '{bootstrapPath}' must be the first scene in the build, " +
                $"but the first scene is '{first ?? "(none)"}'. Move it to the top of the Build Profile scene list " +
                "(the Scene Management Settings inspector has a Fix button).");
        }
    }
}
