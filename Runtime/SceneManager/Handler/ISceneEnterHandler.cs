namespace Onion.SceneManagement { 
    public interface ISceneEnterHandler : ISceneHandler { 
        void OnSceneEnter();
    }

    public interface ISceneEnterHandler<in T> : ISceneHandler where T : SceneArgs {
        void OnSceneEnter(T args);
    }
}
    