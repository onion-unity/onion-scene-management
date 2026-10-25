using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Onion.SceneManagement {
    // Calls ISceneEnterHandler<T> / IAsyncSceneEnterHandler<T> with T known only at runtime (the args' type).
    // One delegate is built per args type and cached; the `is` cast inside honours `in T`, so a handler of a base args type also receives derived args.
    internal static class SceneArgsInvoker {
        private const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly MethodInfo _enterMethod = typeof(SceneArgsInvoker).GetMethod(nameof(Enter), flags);
        private static readonly MethodInfo _enterAsyncMethod = typeof(SceneArgsInvoker).GetMethod(nameof(EnterAsync), flags);

        private static readonly Dictionary<Type, Action<ISceneHandler, SceneArgs>> _enterCache = new();
        private static readonly Dictionary<Type, Func<ISceneHandler, SceneArgs, Awaitable>> _enterAsyncCache = new();

        public static void InvokeEnter(ISceneHandler handler, SceneArgs args) {
            var type = args.GetType();
            if (!_enterCache.TryGetValue(type, out var invoke)) {
                invoke = (Action<ISceneHandler, SceneArgs>)_enterMethod.MakeGenericMethod(type)
                    .CreateDelegate(typeof(Action<ISceneHandler, SceneArgs>));
                _enterCache[type] = invoke;
            }

            invoke(handler, args);
        }

        public static Awaitable InvokeEnterAsync(ISceneHandler handler, SceneArgs args) {
            var type = args.GetType();
            if (!_enterAsyncCache.TryGetValue(type, out var invoke)) {
                invoke = (Func<ISceneHandler, SceneArgs, Awaitable>)_enterAsyncMethod.MakeGenericMethod(type)
                    .CreateDelegate(typeof(Func<ISceneHandler, SceneArgs, Awaitable>));
                _enterAsyncCache[type] = invoke;
            }

            return invoke(handler, args);
        }

        // A handler whose T doesn't accept these args is skipped.
        private static void Enter<T>(ISceneHandler handler, SceneArgs args) where T : SceneArgs {
            if (handler is ISceneEnterHandler<T> h) {
                h.OnSceneEnter((T)args);
            }
        }

        private static Awaitable EnterAsync<T>(ISceneHandler handler, SceneArgs args) where T : SceneArgs {
            return handler is IAsyncSceneEnterHandler<T> h
                ? h.OnSceneEnterAsync((T)args)
                : null;
        }
    }
}
