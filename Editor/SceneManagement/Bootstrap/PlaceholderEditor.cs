using Onion.SceneManagement.Setting;
using UnityEditor;

namespace Onion.SceneManagement.Editor {
    [CustomEditor(typeof(Placeholder))]
    [CanEditMultipleObjects]
    internal sealed class PlaceholderEditor : UnityEditor.Editor {
        private const string bootstrapSceneWarning = "This is the bootstrap scene.\nPlaceholder is not allowed here.";
        private const string bootstrapSceneInfo = "Bootstrap scene is not enabled in the settings.";
        private const string placeholderInfo = "This object will be destroyed at runtime.";

        public override void OnInspectorGUI() {
            // base.OnInspectorGUI();

            if (!SceneManagementSettings.useBootstrapScene) {
                EditorGUILayout.HelpBox(bootstrapSceneInfo, MessageType.Info);

                return;
            }

            var component = target as Placeholder;
            var scenePath = component.gameObject.scene.path;
            var bootstrapper = SceneManagementSettings.bootstrapScene.path;

            if (!string.IsNullOrEmpty(bootstrapper) && scenePath == bootstrapper) {
                EditorGUILayout.HelpBox(bootstrapSceneWarning, MessageType.Warning);
            }
            else {
                EditorGUILayout.HelpBox(placeholderInfo, MessageType.Info);
            }
        }
    }
}