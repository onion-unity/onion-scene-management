using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UniSceneManagement = UnityEngine.SceneManagement;

namespace Onion.SceneManagement.Handlers {
    internal sealed class HandlerGroup<T> where T : ISceneHandler {
        private readonly List<T> _normalHandlers = new();
        private readonly List<T> _alwaysHandlers = new();

        public IReadOnlyList<T> normalHandlers => _normalHandlers;
        public IReadOnlyList<T> alwaysHandlers => _alwaysHandlers;

        public void Add(T handler) {
            if (handler.ExecuteAlways) {
                _alwaysHandlers.Add(handler);
            } else {
                _normalHandlers.Add(handler);
            }
        }

        public void Clear() {
            _normalHandlers.Clear();
            _alwaysHandlers.Clear();
        }
    }

    internal sealed class SceneHandlerCollection {
        public HandlerGroup<ISceneEnterHandler> enter { get; } = new();
        public HandlerGroup<ISceneExitHandler> exit { get; } = new();
        public HandlerGroup<IAsyncSceneEnterHandler> asyncEnter { get; } = new();
        public HandlerGroup<IAsyncSceneExitHandler> asyncExit { get; } = new();

        public void Initialize(UniSceneManagement.Scene scene) {
            if (!scene.IsValid()) {
                return;
            }
            
            Clear(); // ensure no handlers are left from previous scenes

            using var objectPool = ListPool<GameObject>.Get(out var objects);
            using var handlerPool = ListPool<ISceneHandler>.Get(out var handlers);

            scene.GetRootGameObjects(objects);
            foreach (var obj in objects) {
                obj.GetComponentsInChildren(includeInactive: true, handlers);

                foreach (var handler in handlers) {
                    AddHandler(handler);
                }

                handlers.Clear();
            }
        }

        private void AddHandler(ISceneHandler handler) {
            if (handler is ISceneEnterHandler e) enter.Add(e);
            if (handler is ISceneExitHandler x) exit.Add(x);
            if (handler is IAsyncSceneEnterHandler ae) asyncEnter.Add(ae);
            if (handler is IAsyncSceneExitHandler ax) asyncExit.Add(ax);
        }

        public void Clear() {
            enter.Clear();
            exit.Clear();
            asyncEnter.Clear();
            asyncExit.Clear();
        }
    }
}