using UnityEditor;
using UnityEngine;

namespace Onion.SceneManagement.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    internal sealed class SceneReferenceDrawer : PropertyDrawer {
        private static class Styles {
            public static readonly GUIContent validIcon = EditorGUIUtility.IconContent("d_Valid");
            public static readonly GUIContent warningIcon = EditorGUIUtility.IconContent("d_SettingsIcon");
            public static readonly GUIStyle iconStyle = EditorStyles.iconButton;
            
            public static readonly Color errorColor = new(1f, 0.1f, 0.2f);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty assetProp = property.FindPropertyRelative("asset");
            SceneAsset asset = assetProp.objectReferenceValue as SceneAsset;

            float buttonSize = EditorGUIUtility.singleLineHeight;
            Rect fieldRect = new(position.x, position.y, position.width - buttonSize - 2, position.height);
            Rect buttonRect = new(position.xMax - buttonSize, position.y + 1, buttonSize, buttonSize);

            bool hasAsset = asset != null;

            string path = hasAsset ? AssetDatabase.GetAssetPath(asset) : string.Empty;
            bool isInBuild = hasAsset && SceneAssetExtensions.IsInBuild(path);

            Color inheritColor = GUI.color;
            GUIContent icon = isInBuild ? Styles.validIcon : Styles.warningIcon;
            
            if (hasAsset && !isInBuild) {
                GUI.color = Styles.errorColor;
            }
            
            EditorGUI.PropertyField(fieldRect, assetProp, label);
            GUI.color = inheritColor;

            using (new EditorGUI.DisabledScope(!hasAsset || isInBuild)) {
                if (GUI.Button(buttonRect, icon, Styles.iconStyle)) {
                    if (hasAsset && !isInBuild)
                        ShowMenu(buttonRect, asset);
                }
            }

            EditorGUI.EndProperty();
        }

        private void ShowMenu(Rect buttonRect, SceneAsset asset) {
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Add to Build"), false, () => asset.AddToBuild());
            menu.DropDown(buttonRect);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}