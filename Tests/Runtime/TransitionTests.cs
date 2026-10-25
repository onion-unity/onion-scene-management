using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Onion.SceneManagement.Tests {
    // Transitions with no destinations and UnloadMode.None load and unload nothing,
    // so these run only the non-scene steps (callbacks, delays) and need no test scenes.
    public class TransitionTests {
        // Generous: the slowest case takes a few dozen frames. Hitting it means a deadlock.
        private const int timeoutFrames = 600;

        private static readonly TransitionMode keepScenes = new(LoadMode.None, UnloadMode.None);

        private readonly List<string> _log = new();

        [SetUp]
        public void SetUp() {
            _log.Clear();
            Transition.ResetStatics();
        }

        [TearDown]
        public void TearDown() {
            // A failed test can leave a transition waiting; it must not block the next test.
            Transition.ResetStatics();
        }

        private static Transition Create() {
            return Transition.Create(Array.Empty<SceneReference>(), keepScenes);
        }

        private static IEnumerator Await(Awaitable awaitable) {
            for (int frame = 0; !awaitable.IsCompleted; frame++) {
                if (frame > timeoutFrames) {
                    Assert.Fail($"The transition didn't finish within {timeoutFrames} frames (deadlock?).");
                }

                yield return null;
            }

            // Rethrows an exception the transition ended with.
            awaitable.GetAwaiter().GetResult();
        }

        private static IEnumerator AwaitAll(params Awaitable[] awaitables) {
            foreach (var awaitable in awaitables) {
                yield return Await(awaitable);
            }
        }

        [UnityTest]
        public IEnumerator Queued_StartsAfterTheRunningOneEnds() {
            var a = Create()
                .Callback(() => _log.Add("A start"))
                .DelayFrame(5)
                .Callback(() => _log.Add("A end"))
                .ExecuteAsync();
            var b = Create()
                .Callback(() => _log.Add("B start"))
                .ExecuteAsync();

            yield return AwaitAll(a, b);

            Assert.That(_log, Is.EqualTo(new[] { "A start", "A end", "B start" }));
        }

        [UnityTest]
        public IEnumerator Queued_RunInTheOrderTheyWereStarted() {
            var awaitables = new Awaitable[4];
            for (int i = 0; i < awaitables.Length; i++) {
                int index = i;
                // Earlier ones take longer, so running them side by side would finish them in reverse.
                awaitables[i] = Create()
                    .DelayFrame((awaitables.Length - i) * 2)
                    .Callback(() => _log.Add(index.ToString()))
                    .ExecuteAsync();
            }

            yield return AwaitAll(awaitables);

            Assert.That(_log, Is.EqualTo(new[] { "0", "1", "2", "3" }));
        }

        [UnityTest]
        public IEnumerator NotQueued_StartsWhileAnotherIsRunning() {
            var a = Create()
                .Callback(() => _log.Add("A start"))
                .DelayFrame(5)
                .Callback(() => _log.Add("A end"))
                .ExecuteAsync();
            var b = Create()
                .Callback(() => _log.Add("B start"))
                .ExecuteAsync(queued: false);

            yield return AwaitAll(a, b);

            Assert.That(_log, Is.EqualTo(new[] { "A start", "B start", "A end" }));
        }

        [UnityTest]
        public IEnumerator Queued_WaitsForANotQueuedOne() {
            var a = Create()
                .Callback(() => _log.Add("A start"))
                .DelayFrame(5)
                .Callback(() => _log.Add("A end"))
                .ExecuteAsync(queued: false);
            var b = Create()
                .Callback(() => _log.Add("B start"))
                .ExecuteAsync();

            yield return AwaitAll(a, b);

            Assert.That(_log, Is.EqualTo(new[] { "A start", "A end", "B start" }));
        }

        [UnityTest]
        public IEnumerator AwaitingATransitionFromAnAsyncCallback_DoesNotDeadlock() {
            var outer = Create()
                .Callback(() => _log.Add("outer start"))
                .Callback(async () => {
                    await Create()
                        .DelayFrame(2)
                        .Callback(() => _log.Add("inner"))
                        .ExecuteAsync();
                })
                .Callback(() => _log.Add("outer end"))
                .ExecuteAsync();

            yield return Await(outer);

            Assert.That(_log, Is.EqualTo(new[] { "outer start", "inner", "outer end" }));
        }

        [UnityTest]
        public IEnumerator AwaitingATransitionFromWaitUntil_DoesNotDeadlock() {
            bool innerDone = false;
            var outer = Create()
                // The inner transition starts while the outer one waits on this condition.
                .Callback(() => _ = RunInnerAsync())
                .WaitUntil(() => innerDone)
                .Callback(() => _log.Add("outer end"))
                .ExecuteAsync();

            yield return Await(outer);

            Assert.That(_log, Is.EqualTo(new[] { "inner", "outer end" }));

            async Awaitable RunInnerAsync() {
                await Awaitable.NextFrameAsync(); // by then the outer transition is in WaitUntil
                await Create().Callback(() => _log.Add("inner")).ExecuteAsync();
                innerDone = true;
            }
        }

        [UnityTest]
        public IEnumerator Parallel_StepsRunAlongsideEachOther() {
            int startFrame = 0;
            int endFrame = 0;
            var transition = Create()
                .Callback(() => startFrame = Time.frameCount)
                .DelayFrame(10, parallel: true)
                .DelayFrame(10, parallel: true)
                .Callback(() => endFrame = Time.frameCount)
                .ExecuteAsync();

            yield return Await(transition);

            // Sequential delays would take 20 frames.
            Assert.That(endFrame - startFrame, Is.InRange(10, 15));
        }

        [UnityTest]
        public IEnumerator Parallel_NextStepWithoutItWaitsForThem() {
            var transition = Create()
                .Callback(async () => {
                    await Awaitable.NextFrameAsync();
                    await Awaitable.NextFrameAsync();
                    _log.Add("parallel callback");
                }, parallel: true)
                .Callback(() => _log.Add("next step"))
                .ExecuteAsync();

            yield return Await(transition);

            Assert.That(_log, Is.EqualTo(new[] { "parallel callback", "next step" }));
        }

        [UnityTest]
        public IEnumerator Executed_Twice_RunsOnlyOnce() {
            int runs = 0;
            var transition = Create().Callback(() => runs++);

            yield return Await(transition.ExecuteAsync());

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("already been used"));
            yield return Await(transition.ExecuteAsync());

            Assert.That(runs, Is.EqualTo(1));
        }
    }
}
