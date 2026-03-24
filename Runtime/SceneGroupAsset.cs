using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Onion.SceneManagement {
    [CreateAssetMenu(fileName = "SceneGroup", menuName = "Scene/Scene Group Asset")]
    public sealed class SceneGroupAsset : ScriptableObject, IEnumerable<SceneReference> {
        public SceneReference[] scenes;

        public bool isLoaded {
            get {
                if (scenes == null || scenes.Length == 0) {
                    return false;
                }

                bool hasValid = false;
                foreach (var scene in scenes) {
                    if (scene.type == SceneReferenceType.None) {
                        continue;
                    }

                    hasValid = true;
                    if (!scene.isLoaded) {
                        return false;
                    }
                }
                
                return hasValid;
            }
        }

        public IEnumerator<SceneReference> GetEnumerator() {
            foreach (var reference in scenes) {
                yield return reference;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}