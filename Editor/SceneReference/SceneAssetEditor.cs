using System.Linq;
using UnityEditor;
using UnityEngine;

#if ONION_ADDRESSABLES
using UnityEditor.AddressableAssets;
#endif

namespace Onion.SceneManagement.Editor {
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SceneAsset))]
    internal class SceneAssetEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            bool inheritEnabled = GUI.enabled;
            GUI.enabled = true;
            GUILayout.Space(8);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.BeginVertical();

            var assets = targets.Cast<SceneAsset>();
            int validCount = 0;

            foreach (var asset in assets) {
                string path = AssetDatabase.GetAssetPath(asset);
                bool isInBuild = SceneAssetExtensions.IsInBuild(path);
                bool isAddressable = false;
#if ONION_ADDRESSABLES
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings != null) {
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    isAddressable = settings.FindAssetEntry(guid) != null;
                }
#endif
                if (isInBuild || isAddressable) {
                    validCount++;
                }

                EditorGUILayout.BeginHorizontal();

                using (new EditorGUI.DisabledScope(true)) {
                    EditorGUI.ObjectField(EditorGUILayout.GetControlRect(), asset, typeof(SceneAsset), false);
                }

                using (new EditorGUI.DisabledScope(isInBuild || isAddressable)) {    
                    if (GUILayout.Button("Add to Build", GUILayout.Width(96))) {
                        asset.AddToBuild();
                    }
                }

                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            using (new EditorGUI.DisabledScope(validCount == 0)) {
                GUILayout.Space(8);
                if (GUILayout.Button("Add All to Build", GUILayout.Height(28))) {
                    assets.AddToBuild();
                }
            }

            GUILayout.EndVertical();
            GUILayout.Space(8);
            GUILayout.EndHorizontal();

            EditorGUI.indentLevel = indent;
            GUI.enabled = inheritEnabled;
        }
    }
}