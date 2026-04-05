using System;

namespace Onion.SceneManagement {
    /// <summary>
    /// Defines the strategy for unloading existing scenes during a transition.
    /// </summary>
    public enum UnloadMode {
        /// <summary>
        /// Automatically unloads any active scenes that are not included in the destination list.
        /// </summary>
        CleanUp,

        /// <summary>
        /// Keeps all currently loaded scenes active. No scenes will be automatically unloaded.
        /// </summary>
        None,
    }

    /// <summary>
    /// Defines the behavior for loading target scenes during a transition.
    /// </summary>
    public enum LoadMode {
        /// <summary>
        /// If the scene is already active in the hierarchy, it will be skipped.
        /// </summary>
        None,

        /// <summary>
        /// Always loads a new instance of the scene, regardless of its current state.
        /// </summary>
        Additive,

        /// <summary>
        /// If the scene is already loaded, it will be unloaded first and then reloaded to reset its state.
        /// </summary>
        Refresh,
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