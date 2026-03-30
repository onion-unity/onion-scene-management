using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UniSceneManagement = UnityEngine.SceneManagement;

namespace Onion.SceneManagement.Handler {
    internal sealed class HandlerGroup<T> where T : ISceneHandler {
        private readonly List<T> _normal = new();
        private readonly List<T> _always = new();

        public IReadOnlyList<T> normal => _normal;
        public IReadOnlyList<T> always => _always;

        public void Add(T handler, bool executeAlways) {
            if (executeAlways) {
                _always.Add(handler);
            } else {
                _normal.Add(handler);
            }
        }

        public void Clear() {
            _normal.Clear();
            _always.Clear();
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
            if (handler is ISceneEnterHandler e) {
                enter.Add(e, e.IsExecuteAlways(SceneHandlerType.Enter));
            }

            if (handler is ISceneExitHandler x) {
                exit.Add(x, x.IsExecuteAlways(SceneHandlerType.Exit));
            }

            if (handler is IAsyncSceneEnterHandler ae) {
                asyncEnter.Add(ae, ae.IsExecuteAlways(SceneHandlerType.AsyncEnter));
            }
            
            if (handler is IAsyncSceneExitHandler ax) {
                asyncExit.Add(ax, ax.IsExecuteAlways(SceneHandlerType.AsyncExit));
            }
        }

        public void Clear() {
            enter.Clear();
            exit.Clear();
            asyncEnter.Clear();
            asyncExit.Clear();
        }
    }
}