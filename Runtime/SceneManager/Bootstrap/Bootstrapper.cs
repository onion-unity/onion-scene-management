using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Onion.SceneManagement {
    [AddComponentMenu("Bootstrapper")]
    [DisallowMultipleComponent]
    public class Bootstrapper : MonoBehaviour {
        public static bool isReady { get; private set; }

        [SerializeField]
        private UnityEvent completed;

        [SerializeField]
        private GameObject[] preloadPrefabs;

        protected virtual void Awake() {
            foreach (var obj in preloadPrefabs) {
                if (obj == null) {
                    continue;
                }

                Instantiate(obj);
            }

            isReady = true;
            completed?.Invoke();
        }

        protected virtual void OnValidate() {
#if UNITY_EDITOR
            if (preloadPrefabs == null) {
                return;
            }

            for (int i = 0; i < preloadPrefabs.Length; i++) {
                if (preloadPrefabs[i] == null) {
                    continue;
                }

                if (PrefabUtility.GetPrefabAssetType(preloadPrefabs[i]) == PrefabAssetType.NotAPrefab) {
                    Debug.LogError($"Element at index {i} in _preloadPrefabs is not a prefab.", this);
                    preloadPrefabs[i] = null;
                }
            }
#endif
        }
    }
}