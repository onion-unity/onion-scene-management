using System;
using System.Collections.Generic;
using System.Reflection;

namespace Onion.SceneManagement {
    internal static class SceneHandlerUtility {
        private static readonly Dictionary<Type, SceneHandlerFlags> _flagsCache = new();
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

        // Computed once per handler type; reflection never runs twice for the same type.
        public static SceneHandlerFlags GetFlags(this ISceneHandler handler) {
            var type = handler.GetType();
            if (_flagsCache.TryGetValue(type, out var flags)) {
                return flags;
            }

            flags = SceneHandlerFlags.None;
            if (handler is ISceneEnterHandler) {
                flags |= IsExecuteAlways(type, SceneHandlerType.Enter) ? SceneHandlerFlags.GlobalEnter : SceneHandlerFlags.Enter;
            }

            if (handler is ISceneExitHandler) {
                flags |= IsExecuteAlways(type, SceneHandlerType.Exit) ? SceneHandlerFlags.GlobalExit : SceneHandlerFlags.Exit;
            }

            if (handler is IAsyncSceneEnterHandler) {
                flags |= IsExecuteAlways(type, SceneHandlerType.AsyncEnter) ? SceneHandlerFlags.GlobalAsyncEnter : SceneHandlerFlags.AsyncEnter;
            }

            if (handler is IAsyncSceneExitHandler) {
                flags |= IsExecuteAlways(type, SceneHandlerType.AsyncExit) ? SceneHandlerFlags.GlobalAsyncExit : SceneHandlerFlags.AsyncExit;
            }

            if (Implements(type, typeof(ISceneEnterHandler<>))) {
                flags |= SceneHandlerFlags.ArgsEnter;
            }

            if (Implements(type, typeof(IAsyncSceneEnterHandler<>))) {
                flags |= SceneHandlerFlags.ArgsAsyncEnter;
            }

            _flagsCache[type] = flags;
            return flags;
        }

        private static bool IsExecuteAlways(Type type, SceneHandlerType handlerType) {
            var method = GetHandlerMethod(type, handlerType);
            return method?.IsDefined(typeof(ExecuteByAnySceneAttribute), inherit: false) ?? false;
        }

        // Whether the type implements any closed form of an open generic interface, e.g. ISceneEnterHandler<>.
        private static bool Implements(Type type, Type genericInterface) {
            foreach (var i in type.GetInterfaces()) {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface) {
                    return true;
                }
            }

            return false;
        }

        private static MethodInfo GetHandlerMethod(Type handlerType, SceneHandlerType handlerTypeEnum) {
            var methodName = _methodNameCache[handlerTypeEnum];
            // Parameterless only: a handler may also have the OnSceneEnter(T args) overload of ISceneEnterHandler<T>.
            var method = handlerType.GetMethod(methodName,
                  BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic,
                binder: null, Type.EmptyTypes, modifiers: null);
            
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