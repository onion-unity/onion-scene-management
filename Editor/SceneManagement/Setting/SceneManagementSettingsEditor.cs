using Onion.SceneManagement.Setting;
using UnityEditor;
using UnityEditor.SceneManagement;
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

            var iter = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iter.NextVisible(enterChildren)) {
                enterChildren = false;

                switch (iter.name) {
                    case "m_Script": break;
                    case nameof(SceneManagementSettingsAsset.bootstrapScene):
                        if (!SceneManagementSettings.useBootstrap) {
                            break;
                        }

                        DrawBootstrapSceneField(iter);
                        break;
                    default: EditorGUILayout.PropertyField(iter); break;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBootstrapSceneField(SerializedProperty prop) {
            var assetProp = prop.FindPropertyRelative(nameof(SceneReference.asset));
            bool isAssigned = assetProp != null && assetProp.objectReferenceValue != null;

            if (!isAssigned) {
                EditorGUILayout.HelpBox("Bootstrap scene is not assigned.\nPlease create or assign a bootstrap scene.", MessageType.Warning);
            }

            using var _ = new EditorGUILayout.HorizontalScope();
            EditorGUILayout.PropertyField(prop);

            if (isAssigned) return;
            if (GUILayout.Button("Create", GUILayout.Width(60))) {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create Bootstrap Scene",
                    "Bootstrap",
                    "unity",
                    "Select location to create the bootstrap scene"
                );

                if (string.IsNullOrEmpty(path)) {
                    return;
                }

                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
                if (EditorSceneManager.SaveScene(scene, path)) {
                    EditorSceneManager.CloseScene(scene, true);

                    AssetDatabase.Refresh();
                    var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    assetProp.objectReferenceValue = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
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