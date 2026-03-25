using System;

namespace Onion.SceneManagement {
    public enum UnloadMode {
        None,    // don't unload.
        CleanUp, // unload if the scene is loaded.
    }

    public enum LoadMode {
        None,     // if the scene is already loaded, do nothing.
        Additive, // whatever the scene is loaded or not, just add it.
        Refresh,  // if the scene is already loaded, unload it first then load it again.
    }

    public struct TransitionMode {
        public LoadMode loadMode;
        public UnloadMode unloadMode;

        public TransitionMode(LoadMode loadMode, UnloadMode unloadMode) {
            this.loadMode = loadMode;
            this.unloadMode = unloadMode;
        }

        public static readonly TransitionMode Default = new(LoadMode.None, UnloadMode.CleanUp);
        public static readonly TransitionMode Additive = new(LoadMode.Additive, UnloadMode.CleanUp);
        public static readonly TransitionMode Refresh = new(LoadMode.Refresh, UnloadMode.CleanUp);
    }
}