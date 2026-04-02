using UnityEngine;

namespace Onion.SceneManagement {
    [DisallowMultipleComponent]
    [AddComponentMenu("Placeholder")]
    public sealed class Placeholder : MonoBehaviour {
        private void Awake() {
            if (Bootstrapper.isReady) {
                Destroy(gameObject);
            }
        }
    }
}