using UnityEngine;

namespace Onion.SceneManagement.Handlers { 
    public interface IAsyncSceneExitHandler : ISceneHandler { 
        Awaitable OnSceneExitAsync();
    }
}
    