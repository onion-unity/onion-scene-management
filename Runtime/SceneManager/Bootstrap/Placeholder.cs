using UnityEngine;

namespace Onion.SceneManagement {
    [DisallowMultipleComponent]
    [AddComponentMenu("Onion/SceneManagement/Placeholder")]
    public sealed class Placeholder : MonoBehaviour {
        private void Awake() {
            if (Bootstrapper.isReady) {
                // Destroy is deferred to the end of the frame. Deactivate first so the
                // placeholder (e.g. a camera or audio listener) never coexists with the real one.
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }
    }
}