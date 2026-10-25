using UnityEngine;

namespace Onion.SceneManagement {
    public interface IAsyncSceneEnterHandler : ISceneHandler { 
        Awaitable OnSceneEnterAsync();
    }

    public interface IAsyncSceneEnterHandler<in T> : ISceneHandler where T : SceneArgs {
        Awaitable OnSceneEnterAsync(T args);
    }
}