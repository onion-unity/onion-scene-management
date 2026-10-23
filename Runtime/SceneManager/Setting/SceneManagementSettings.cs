using UnityEngine;

namespace Onion.SceneManagement.Setting {
    public static class SceneManagementSettings {
        public static bool useBootstrap = false;
        internal static SceneReference bootstrapScene = default;
        internal static SceneReference[] initialScenes = default;

        public static bool overlapLoading = false;
        public static bool canUseBootstrapScene =>
            useBootstrap && bootstrapScene.type != SceneReferenceType.None;

        // Values changed at runtime would otherwise carry over into the next Play session when domain reload is off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() {
            Reload_Internal();
        }

        internal static void Reload_Internal() {
            var asset = SceneManagementSettingsAsset.instance;
            if (asset == null) {
                useBootstrap = false;
                bootstrapScene = default;
                initialScenes = default;
                overlapLoading = false;

                return;
            }

            useBootstrap = asset.useBootstrap;
            bootstrapScene = asset.bootstrapScene;
            initialScenes = asset.initialScenes;
            overlapLoading = asset.overlappedLoad;
        }
    }
}
