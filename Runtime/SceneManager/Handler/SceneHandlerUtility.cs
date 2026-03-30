using System;
using System.Collections.Generic;
using System.Reflection;
using Onion.SceneManagement.Handler.Attributes;

namespace Onion.SceneManagement.Handler {
    internal static class SceneHandlerUtility {
        private static readonly Dictionary<(Type, SceneHandlerType), bool> _executeAlwaysCache = new();
        private static readonly IDictionary<SceneHandlerType, string> _methodNameCache = new Dictionary<SceneHandlerType, string> {
            { SceneHandlerType.Enter, nameof(ISceneEnterHandler.OnSceneEnter) },
            { SceneHandlerType.Exit, nameof(ISceneExitHandler.OnSceneExit) },
            { SceneHandlerType.AsyncEnter, nameof(IAsyncSceneEnterHandler.OnSceneEnterAsync) },
            { SceneHandlerType.AsyncExit, nameof(IAsyncSceneExitHandler.OnSceneExitAsync) }
        };

        private static readonly IDictionary<SceneHandlerType, Type> _handlerInterfaceCache = new Dictionary<SceneHandlerType, Type> {
            { SceneHandlerType.Enter, typeof(ISceneEnterHandler) },
            { SceneHandlerType.Exit, typeof(ISceneExitHandler) },
            { SceneHandlerType.AsyncEnter, typeof(IAsyncSceneEnterHandler) },
            { SceneHandlerType.AsyncExit, typeof(IAsyncSceneExitHandler) }
        };

        public static bool IsExecuteAlways(this ISceneHandler handler, SceneHandlerType handlerType) {
            var methodType = handler.GetType();
            var key = (methodType, handlerType);
            if (_executeAlwaysCache.TryGetValue(key, out var executeAlways)) {
                return executeAlways;
            }

            var method = GetHandlerMethod(methodType, handlerType);
            executeAlways = method?.IsDefined(typeof(ExecuteByAnySceneAttribute), inherit: false) ?? false;

            _executeAlwaysCache[key] = executeAlways;
            return executeAlways;
        }

        private static MethodInfo GetHandlerMethod(Type handlerType, SceneHandlerType handlerTypeEnum) {
            var methodName = _methodNameCache[handlerTypeEnum];
            var method = handlerType.GetMethod(methodName, 
                  BindingFlags.Instance 
                | BindingFlags.Public 
                | BindingFlags.NonPublic);
            
            if (method == null) {
                var interfaceType = _handlerInterfaceCache[handlerTypeEnum];
                try {
                    var map = handlerType.GetInterfaceMap(interfaceType);
                    for (int i = 0; i < map.InterfaceMethods.Length; i++) {
                        if (map.InterfaceMethods[i].Name != methodName) {
                            continue;
                        }

                        method = map.TargetMethods[i];
                        break;
                    }
                }
                catch {
                    return null;
                }
            }

            return method;
        }
    }
}