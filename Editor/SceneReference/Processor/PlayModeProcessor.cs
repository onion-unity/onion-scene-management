using UnityEditor;

namespace Onion.SceneManagement.Editor.Processor {
    [InitializeOnLoad]
    internal static class PlayModeProcessor {
        static PlayModeProcessor() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingEditMode) {
                SceneDictionaryBaker.Bake(withFiles: false);
            }
        }
    }
}