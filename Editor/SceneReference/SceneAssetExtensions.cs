using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Pool;

namespace Onion.SceneManagement.Editor {
    internal static class SceneAssetExtensions {
        public static void AddToBuild(this SceneAsset asset) {
            if (asset == null) {
                return;
            }

            AddToBuild(new[] { asset });
        }

        public static void AddToBuild(this IEnumerable<SceneAsset> assets) {
            var buildScenes = EditorBuildSettings.scenes.ToList();
            using var pool = HashSetPool<string>.Get(out var paths);
            foreach (var scene in buildScenes) {
                paths.Add(scene.path);
            }

            foreach (var asset in assets) {
                string path = AssetDatabase.GetAssetPath(asset);
                if (paths.Contains(path)) {
                    continue;
                }

                buildScenes.Add(new(path, true));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        public static bool IsInBuild(this SceneAsset asset) {
            string path = AssetDatabase.GetAssetPath(asset);

            foreach (var scene in EditorBuildSettings.scenes) {
                if (scene.path == path) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsInBuild(string path) {
            foreach (var scene in EditorBuildSettings.scenes) {
                if (scene.path == path) {
                    return true;
                }
            }

            return false;
        }
    } 
}