using UnityEngine;

namespace Onion.SceneManagement.Handler { 
    public interface IAsyncSceneExitHandler : ISceneHandler { 
        Awaitable OnSceneExitAsync();
    }
}
    