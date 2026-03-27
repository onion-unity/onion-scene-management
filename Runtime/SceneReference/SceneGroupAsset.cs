using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Onion.SceneManagement {
    [CreateAssetMenu(fileName = "SceneGroup", menuName = "Scene/Scene Group Asset")]
    public sealed class SceneGroupAsset : ScriptableObject, IEnumerable<SceneReference> {
        public SceneReference[] scenes;

        public struct Enumerator : IEnumerator<SceneReference> {
            private readonly SceneReference[] _scenes;
            private int _index;

            internal Enumerator(SceneReference[] scenes) {
                _scenes = scenes;
                _index = -1;
            }

            public readonly SceneReference Current => _scenes[_index];
            readonly object IEnumerator.Current => Current;

            public bool MoveNext() {
                _index++;
                return _index < _scenes.Length;
            }

            public void Reset() {
                _index = -1;
            }

            public readonly void Dispose() { }
        }

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

        public Enumerator GetEnumerator() => new(scenes);

        IEnumerator<SceneReference> IEnumerable<SceneReference>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}