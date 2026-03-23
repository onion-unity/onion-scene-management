using UnityEngine;

namespace Onion.SceneManagement.Handlers {
    public interface IAsyncSceneEnterHandler : ISceneHandler { 
        Awaitable OnSceneEnterAsync();
    }
}