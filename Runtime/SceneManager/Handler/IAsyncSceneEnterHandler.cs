using UnityEngine;

namespace Onion.SceneManagement.Handler {
    public interface IAsyncSceneEnterHandler : ISceneHandler { 
        Awaitable OnSceneEnterAsync();
    }
}