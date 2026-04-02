using UnityEngine;

namespace Onion.SceneManagement.Setting {
    public static class SceneManagementSettings {
        public static bool useBootstrapScene = false;
        internal static SceneReference bootstrapScene = default;
        internal static bool useBootstrappedLoad = false;
        public static bool overlapLoading = false;
        public static bool canUseBootstrapScene =>
            useBootstrapScene && bootstrapScene.type != SceneReferenceType.None;

#if UNITY_EDITOR
        internal static BootstrapGroup bootstrapGroup;
#endif

        internal static void LoadFromAsset_Internal(SceneManagementSettingsAsset asset) {
            useBootstrapScene = asset.useBootstrapScene;
            bootstrapScene = asset.bootstrapScene;
            useBootstrappedLoad = asset.useBootstrappedLoad;
            overlapLoading = asset.overlapLoading;
#if UNITY_EDITOR
            bootstrapGroup = asset.bootstrapGroup;
#endif
        }
    }
}