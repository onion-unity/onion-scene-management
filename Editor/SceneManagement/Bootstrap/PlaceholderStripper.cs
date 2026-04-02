using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Onion.SceneManagement.Editor {
    internal sealed class PlaceholderStripper : IProcessSceneWithReport {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (report == null) {
                return;
            }

            var placeholders = Object.FindObjectsByType<Placeholder>(
                FindObjectsInactive.Include, 
                FindObjectsSortMode.None);
            
            foreach (var placeholder in placeholders) {
                Object.DestroyImmediate(placeholder.gameObject);
            }
        }
    }
}