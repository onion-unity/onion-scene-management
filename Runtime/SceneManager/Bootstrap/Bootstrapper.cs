// using UnityEngine;
// using UnityEngine.Events;

// #if UNITY_EDITOR
// using UnityEditor;
// #endif

using UnityEngine;

namespace Onion.SceneManagement {
    public static class Bootstrapper {
        public static bool isReady { get; internal set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize() {
            
        }
    }

//     [AddComponentMenu("Bootstrapper")]
//     [DisallowMultipleComponent]
//     public class Bootstrapper : MonoBehaviour {
//         public static bool isReady { get; private set; }

//         [SerializeField]
//         private GameObject[] preloadPrefabs;

//         [Space(8)]
//         [SerializeField]
//         private UnityEvent completed;

//         protected virtual void Awake() {
//             foreach (var obj in preloadPrefabs) {
//                 if (obj == null) {
//                     continue;
//                 }

//                 Instantiate(obj);
//             }

//             isReady = true;
//             completed?.Invoke();
//         }

//         protected virtual void OnValidate() {
// #if UNITY_EDITOR
//             if (preloadPrefabs == null) {
//                 return;
//             }

//             for (int i = 0; i < preloadPrefabs.Length; i++) {
//                 if (preloadPrefabs[i] == null) {
//                     continue;
//                 }

//                 if (PrefabUtility.GetPrefabAssetType(preloadPrefabs[i]) == PrefabAssetType.NotAPrefab) {
//                     Debug.LogError($"Element at index {i} in _preloadPrefabs is not a prefab.", this);
//                     preloadPrefabs[i] = null;
//                 }
//             }
// #endif
//         }
//     }
}