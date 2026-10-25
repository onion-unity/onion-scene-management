using UnityEngine;

namespace Onion.SceneManagement {
    public interface IAsyncSceneEnterHandler : ISceneHandler { 
        Awaitable OnSceneEnterAsync();
    }
}