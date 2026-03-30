namespace Onion.SceneManagement.Setting {
    public static class SceneManagementSettings {
        public static bool useBootstrapScene = false;
        internal static SceneReference bootstrapScene = default;
        public static bool overlapLoading = false;

        internal static void LoadFromAsset_Internal(SceneManagementSettingsAsset asset) {
            useBootstrapScene = asset.useBootstrapScene;
            bootstrapScene = asset.bootstrapScene;
            overlapLoading = asset.overlapLoading;
        }
    }
}