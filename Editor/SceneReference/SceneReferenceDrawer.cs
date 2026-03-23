using UnityEditor;
using UnityEngine;

namespace Onion.SceneManagement.Editor {
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty assetProp = property.FindPropertyRelative("_asset");
            EditorGUI.PropertyField(position, assetProp, label);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}