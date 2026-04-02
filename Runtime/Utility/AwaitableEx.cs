using System;
using System.Threading;
using UnityEngine;

namespace Onion.SceneManagement.Utility {
    internal static class AwaitableEx {
        public static async Awaitable WaitWhile(Func<bool> condition, CancellationToken cancellationToken = default) {
            while (condition()) {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        public static async Awaitable WaitUntil<T>(T target, Func<T, bool> condition, CancellationToken cancellationToken = default) {
            while (!condition(target)) {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        public static async Awaitable WaitUntil(Func<bool> condition, CancellationToken cancellationToken = default) {
            while (!condition()) {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        public static async Awaitable WaitForSeconds(float seconds, CancellationToken cancellationToken = default) {
            float elapsed = 0f;
            while (elapsed < seconds) {
                await Awaitable.NextFrameAsync(cancellationToken);
                elapsed += Time.deltaTime;
            }
        }
    }
}