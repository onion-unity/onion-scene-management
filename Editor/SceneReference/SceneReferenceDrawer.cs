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

// using UnityEditor;
// using UnityEngine;
// using System.Linq;

// namespace Onion.SceneManagement.Editor {
//     [CustomPropertyDrawer(typeof(SceneReference))]
//     internal sealed class SceneReferenceDrawer : PropertyDrawer {
//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
//             EditorGUI.BeginProperty(position, label, property);

//             SerializedProperty assetProp = property.FindPropertyRelative(nameof(SceneReference.asset));
//             SerializedProperty guidProp = property.FindPropertyRelative(nameof(SceneReference._guid));

//             SceneAsset asset = assetProp.objectReferenceValue as SceneAsset;
//             string guid = guidProp.stringValue;

//             string path = asset != null ? AssetDatabase.GetAssetPath(asset) : string.Empty;

//             float size = EditorGUIUtility.singleLineHeight;
//             Rect rectField = new(position.x, position.y, position.width - size - 2, position.height);
//             Rect rectButton = new(position.xMax - size, position.y + 1f, size, size);

//             Color color = Color.white;
//             GUIContent icon = EditorGUIUtility.IconContent("d_SettingsIcon");

//             if (asset != null) {
//                 bool isInBuild = SceneAssetExtensions.IsInBuild(path);
//                 bool isAddressable = IsAddressable(path);

//                 if (!isInBuild && !isAddressable) {
//                     color = new Color(1f, 0.2f, 0.3f);
//                 }
//                 else if (isInBuild) {
//                     icon = EditorGUIUtility.IconContent("d_Valid");
//                 }
//                 else if (isAddressable) {
//                     icon = EditorGUIUtility.IconContent("d_Valid");
//                     color = Color.cyan;
//                 }
//             }

//             Color originalContentColor = GUI.color;
//             GUI.color = color;

//             EditorGUI.PropertyField(rectField, assetProp, label);

//             using (new EditorGUI.DisabledScope(asset == null || SceneAssetExtensions.IsInBuild(path) || IsAddressable(path))) {
//                 if (GUI.Button(rectButton, icon, EditorStyles.iconButton)) {
//                     ShowMenu(rectButton, asset, path, 
//                         asset != null && SceneAssetExtensions.IsInBuild(path), 
//                         IsAddressable(path));
//                 }
//             }

//             GUI.color = originalContentColor;
//             EditorGUI.EndProperty();
//         }

//         private void ShowMenu(Rect buttonRect, SceneAsset asset, string path, bool isInBuild, bool isAddressable) {
//             if (isInBuild || isAddressable) {
//                 return;
//             }

//             GenericMenu menu = new();

//             if (!isInBuild) {
//                 menu.AddItem(new GUIContent("Add to Build Settings"), false, () => asset.AddToBuild());
//             }

// #if ONION_ADDRESSABLES
//             if (!isAddressable) {
//                 menu.AddItem(new GUIContent("Make Addressable"), false, () => {
//                     var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
//                     if (settings != null) {
//                         string guid = AssetDatabase.AssetPathToGUID(path);
//                         settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
//                         AssetDatabase.SaveAssets();
//                     }
//                 });
//             }
// #endif
//             if (menu.GetItemCount() > 0) {
//                 menu.DropDown(buttonRect);
//             }
//         }

//         private bool IsAddressable(string path) {
// #if ONION_ADDRESSABLES
//             string guid = AssetDatabase.AssetPathToGUID(path);
//             if (string.IsNullOrEmpty(guid)) return false;

//             var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
//             return settings != null && settings.FindAssetEntry(guid) != null;
// #else
//             return false;
// #endif
//         }

//         public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
//             return EditorGUIUtility.singleLineHeight;
//         }
//     }
// }