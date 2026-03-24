// using UnityEditor;
// using UnityEngine;

// namespace Onion.SceneManagement {
//     [CustomPropertyDrawer(typeof(SceneEntry))]
//     internal sealed class SceneEntryDrawer : PropertyDrawer {
//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {           
//             EditorGUI.BeginProperty(position, label, property);

//             var sceneProp = property.FindPropertyRelative(nameof(SceneEntry._scene));
//             var scenesProp = property.FindPropertyRelative(nameof(SceneEntry._scenes));

//             var assetProp = sceneProp.FindPropertyRelative(nameof(SceneReference._asset));
//             var guidProp = sceneProp.FindPropertyRelative(nameof(SceneReference._guid));

//             Object current = null;
//             if (scenesProp.objectReferenceValue != null) {
//                 current = scenesProp.objectReferenceValue;
//             }
//             else if (assetProp != null && assetProp.objectReferenceValue != null) {
//                 current = assetProp.objectReferenceValue;
//             }

//             EditorGUI.BeginChangeCheck();
//             Object newValue = EditorGUI.ObjectField(position, label, current, typeof(Object), false);

//             if (EditorGUI.EndChangeCheck()) {
//                 if (newValue == null) {
//                     sceneProp.objectReferenceValue = null;
//                     if (assetProp != null) assetProp.objectReferenceValue = null;
//                     if (guidProp != null) guidProp.stringValue = string.Empty;
//                 }
//                 else if (newValue is SceneAsset sceneAsset) {
//                     scenesProp.objectReferenceValue = null;
//                     if (assetProp != null) assetProp.objectReferenceValue = sceneAsset;
//                     if (guidProp != null) {
//                         if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sceneAsset, out string guid, out long _)) {
//                             guidProp.stringValue = guid;
//                         }
//                         else {
//                             guidProp.stringValue = SceneReference.defaultGuid;
//                         }
//                     }
//                 }
//                 else if (newValue is SceneGroupAsset sceneGroupAsset) {
//                     scenesProp.objectReferenceValue = sceneGroupAsset;

//                     if (assetProp != null) assetProp.objectReferenceValue = null;
//                     if (guidProp != null) guidProp.stringValue = string.Empty;
//                 }
//                 else {
//                     // ignore
//                 }
//             }

//             EditorGUI.EndProperty();
//         }
//     }
// }