using Onion.SceneManagement.Setting;
using UnityEditor;
using UnityEngine;

namespace Onion.SceneManagement.Editor {
    [CustomEditor(typeof(SceneManagementSettingsAsset))]
    internal class SceneManagementSettingsEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            var asset = target as SceneManagementSettingsAsset;
            if (SceneManagementSettingsProvider.GetSettings() != asset) {
                DrawNotMainAssetWarning(asset);
            }

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawNotMainAssetWarning(SceneManagementSettingsAsset asset) {
            EditorGUILayout.HelpBox("This asset is not assigned as the main settings. Please assign it to ensure proper functionality.", MessageType.Warning);

            if (GUILayout.Button("Assign as Main Settings")) {
                SceneManagementSettingsProvider.AssignSettings(asset);
                EditorGUIUtility.PingObject(target);
            }
        }
    }
}