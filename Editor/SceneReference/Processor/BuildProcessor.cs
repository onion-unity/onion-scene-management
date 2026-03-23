using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Onion.SceneManagement.Editor.Processor {
    internal class BuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) {
            try {
                SceneDictionaryBaker.Bake(withFiles: true);
            }
            catch (Exception e) {
                Debug.LogError($"Failed to bake scene dictionary: {e}");
            }
        }

        public void OnPostprocessBuild(BuildReport report) {
            SceneDictionaryBaker.Clear();
        }
    }
}