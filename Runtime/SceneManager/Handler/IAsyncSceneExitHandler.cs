using UnityEngine;

namespace Onion.SceneManagement { 
    public interface IAsyncSceneExitHandler : ISceneHandler { 
        Awaitable OnSceneExitAsync();
    }
}
    