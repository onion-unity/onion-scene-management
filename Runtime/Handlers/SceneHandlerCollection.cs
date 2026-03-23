using System.Collections.Generic;

namespace Onion.SceneManagement.Handlers {
    public sealed class HandlerGroup<T> where T : ISceneHandler {
        private readonly List<T> _normalHandlers = new();
        public readonly List<T> _alwaysHandlers = new();

        public void Add(T handler) {
            if (handler.ExecuteAlways) {
                _alwaysHandlers.Add(handler);
            } else {
                _normalHandlers.Add(handler);
            }
        }

        public IEnumerable<T> GetHandlers(bool includeNormal) {
            if (includeNormal) {
                foreach (var handler in _normalHandlers) {
                    yield return handler;
                }
            }

            foreach (var handler in _alwaysHandlers) {
                yield return handler;
            }
        }
    }

    public sealed class SceneHandlerCollection {
        public HandlerGroup<ISceneEnterHandler> EnterHandlers { get; }
        public HandlerGroup<ISceneExitHandler> ExitHandlers { get; }
        public HandlerGroup<IAsyncSceneEnterHandler> AsyncEnterHandlers { get; }
        public HandlerGroup<IAsyncSceneExitHandler> AsyncExitHandlers { get; }

        public SceneHandlerCollection(IEnumerable<ISceneHandler> handlers) {
            EnterHandlers = new HandlerGroup<ISceneEnterHandler>();
            ExitHandlers = new HandlerGroup<ISceneExitHandler>();
            AsyncEnterHandlers = new HandlerGroup<IAsyncSceneEnterHandler>();
            AsyncExitHandlers = new HandlerGroup<IAsyncSceneExitHandler>();

            foreach (var handler in handlers) {
                if (handler is ISceneEnterHandler enterHandler) {
                    EnterHandlers.Add(enterHandler);
                }

                if (handler is ISceneExitHandler exitHandler) {
                    ExitHandlers.Add(exitHandler);
                }

                if (handler is IAsyncSceneEnterHandler asyncEnterHandler) {
                    AsyncEnterHandlers.Add(asyncEnterHandler);
                }

                if (handler is IAsyncSceneExitHandler asyncExitHandler) {
                    AsyncExitHandlers.Add(asyncExitHandler);
                }
            }
        }
    }
}