// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// namespace Onion.SceneManagement {
//     [Serializable]
//     public class SceneEntry : IEnumerable<SceneReference> {
//         [SerializeField]
//         internal SceneReference _scene;

//         [SerializeField]
//         internal SceneGroupAsset _scenes;

//         public SceneReference scene {
//             get {
//                 if (_scenes != null) {
//                     return _scenes.scenes.Length > 0 
//                         ? _scenes.scenes[0] 
//                         : null;
//                 }
//                 return _scene;
//             }
//         }

//         public IEnumerator<SceneReference> scenes => GetEnumerator();

//         public bool isLoaded {
//             get {
//                 if (_scenes != null) {
//                     return _scenes.isLoaded;
//                 }

//                 return _scene?.isLoaded ?? false;
//             }
//         }

//         public IEnumerator<SceneReference> GetEnumerator() {
//             if (_scenes != null) {
//                 foreach (var scene in _scenes) {
//                     yield return scene;
//                 }
//             }
//             else if (_scene != null) {
//                 yield return _scene;
//             }
//         }

//         IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
//     }
// }