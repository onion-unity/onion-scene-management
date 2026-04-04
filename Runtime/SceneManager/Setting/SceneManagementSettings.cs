using UnityEngine;

namespace Onion.SceneManagement.Setting {
    public static class SceneManagementSettings {
        public static bool useBootstrap = false;
        internal static SceneReference bootstrapScene = default;
        public static bool overlapLoading = false;
        public static bool canUseBootstrapScene =>
            useBootstrap && bootstrapScene.type != SceneReferenceType.None;

#if UNITY_EDITOR
        internal static BootstrapGroup bootstrapGroup;
#endif

        internal static void LoadFromAsset_Internal(SceneManagementSettingsAsset asset) {
            useBootstrap = asset.useBootstrap;
            bootstrapScene = asset.bootstrapScene;
            overlapLoading = asset.overlapLoading;
#if UNITY_EDITOR
            bootstrapGroup = asset.bootstrapGroup;
#endif
        }
    }
}