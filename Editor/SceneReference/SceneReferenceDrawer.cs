using UnityEditor;
using UnityEngine;

namespace Onion.SceneManagement.Editor {
    [CustomPropertyDrawer(typeof(SceneReference))]
    internal sealed class SceneReferenceDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            
            SerializedProperty assetProp = property.FindPropertyRelative(nameof(SceneReference.asset));
            // SerializedProperty guidProp = property.FindPropertyRelative(nameof(SceneReference._guid));
            // var asset = assetProp.objectReferenceValue as SceneAsset;
            
            // Color color = GUI.color;
            // if (asset == null) {
            //     // ignore
            // }
            // else {
                
            // }
            
            EditorGUI.PropertyField(position, assetProp, label);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}