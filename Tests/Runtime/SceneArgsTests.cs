using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UniSceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Onion.SceneManagement.Tests {
    // Scene args and handler flags. The scene tests load an in-memory scene through a fake loader,
    // so they run the SceneManager's register/notify path without test scene assets.
    public class SceneArgsTests {
        private const int timeoutFrames = 600;

        private static readonly List<string> _log = new();

        private class BaseArgs : SceneArgs { }
        private sealed class DerivedArgs : BaseArgs { }
        private sealed class OtherArgs : SceneArgs { }

        [SetUp]
        public void SetUp() {
            _log.Clear();
            Transition.ResetStatics();
        }

        [TearDown]
        public void TearDown() {
            Transition.ResetStatics();
        }

        private static IEnumerator Await(Awaitable awaitable) {
            if (awaitable == null) {
                yield break;
            }

            for (int frame = 0; !awaitable.IsCompleted; frame++) {
                if (frame > timeoutFrames) {
                    Assert.Fail($"Didn't finish within {timeoutFrames} frames.");
                }

                yield return null;
            }

            awaitable.GetAwaiter().GetResult();
        }

        // --- Flags ---

        private sealed class PerMethodGlobalHandler : ISceneEnterHandler, ISceneExitHandler {
            [ExecuteByAnyScene]
            public void OnSceneEnter() { }
            public void OnSceneExit() { }
        }

        // Both OnSceneEnter overloads: looking up the parameterless one must not be ambiguous.
        private sealed class PlainAndArgsHandler : ISceneEnterHandler, ISceneEnterHandler<BaseArgs>, IAsyncSceneEnterHandler<BaseArgs> {
            public void OnSceneEnter() { }
            public void OnSceneEnter(BaseArgs args) { }
            public Awaitable OnSceneEnterAsync(BaseArgs args) => null;
        }

        [Test]
        public void Flags_GlobalIsDecidedPerMethod() {
            Assert.That(new PerMethodGlobalHandler().GetFlags(),
                Is.EqualTo(SceneHandlerFlags.GlobalEnter | SceneHandlerFlags.Exit));
        }

        [Test]
        public void Flags_PlainAndArgsEnterCoexist() {
            Assert.That(new PlainAndArgsHandler().GetFlags(),
                Is.EqualTo(SceneHandlerFlags.Enter | SceneHandlerFlags.ArgsEnter | SceneHandlerFlags.ArgsAsyncEnter));
        }

        // --- Invoker ---

        private sealed class BaseArgsHandler : ISceneEnterHandler<BaseArgs>, IAsyncSceneEnterHandler<BaseArgs> {
            public SceneArgs received;

            public void OnSceneEnter(BaseArgs args) => received = args;

            public async Awaitable OnSceneEnterAsync(BaseArgs args) {
                await Awaitable.NextFrameAsync();
                received = args;
            }
        }

        [Test]
        public void Invoker_HandlerOfABaseType_ReceivesDerivedArgs() {
            var handler = new BaseArgsHandler();
            var args = new DerivedArgs();

            SceneArgsInvoker.InvokeEnter(handler, args);

            Assert.That(handler.received, Is.SameAs(args));
        }

        [Test]
        public void Invoker_ArgsOfAnotherType_AreSkipped() {
            var handler = new BaseArgsHandler();

            SceneArgsInvoker.InvokeEnter(handler, new OtherArgs());

            Assert.That(handler.received, Is.Null);
            Assert.That(SceneArgsInvoker.InvokeEnterAsync(handler, new OtherArgs()), Is.Null);
        }

        [UnityTest]
        public IEnumerator Invoker_Async_ReturnsTheHandlersAwaitable() {
            var handler = new BaseArgsHandler();
            var args = new BaseArgs();

            yield return Await(SceneArgsInvoker.InvokeEnterAsync(handler, args));

            Assert.That(handler.received, Is.SameAs(args));
        }

        // --- Scene: register and notify ---

        private sealed class ArgsReceiver : MonoBehaviour, ISceneEnterHandler, ISceneEnterHandler<BaseArgs>, IAsyncSceneEnterHandler<BaseArgs> {
            public void OnSceneEnter() => _log.Add($"{name} enter");
            public void OnSceneEnter(BaseArgs args) => _log.Add($"{name} enter {args.GetType().Name}");

            public async Awaitable OnSceneEnterAsync(BaseArgs args) {
                await Awaitable.NextFrameAsync();
                _log.Add($"{name} enter async {args.GetType().Name}");
            }
        }

        private sealed class GlobalReceiver : MonoBehaviour, ISceneEnterHandler {
            [ExecuteByAnyScene]
            public void OnSceneEnter() => _log.Add($"{name} global enter");
        }

        // Creates the scene in memory: "root" and its inactive "child" receive args, "global" is [ExecuteByAnyScene].
        private sealed class FakeSceneLoader : SceneLoader {
            public FakeSceneLoader() : base(new SceneReference()) { }

            public override float progress => 1f;

            public override async Awaitable LoadAsync(bool activateOnLoad = true) {
                await Awaitable.NextFrameAsync();

                var created = UniSceneManager.CreateScene($"SceneArgsTests_{Guid.NewGuid():N}");
                var root = new GameObject("root", typeof(ArgsReceiver));
                var global = new GameObject("global", typeof(GlobalReceiver));
                UniSceneManager.MoveGameObjectToScene(root, created);
                UniSceneManager.MoveGameObjectToScene(global, created);

                var child = new GameObject("child", typeof(ArgsReceiver));
                child.transform.SetParent(root.transform);
                child.SetActive(false);

                scene = created;
            }

            public override Awaitable ActivateAsync() => null;

            public override async Awaitable UnloadAsync() {
                await UniSceneManager.UnloadSceneAsync(scene);
                scene.Unregister();
            }
        }

        private FakeSceneLoader _loader;

        // Unloads the scene even when the test failed halfway.
        [UnityTearDown]
        public IEnumerator UnloadScene() {
            if (_loader != null) {
                yield return Await(SceneManager.UnloadSceneAsync_Internal(_loader));
                _loader = null;
            }
        }

        private IEnumerator LoadScene() {
            _loader = new FakeSceneLoader();
            yield return Await(SceneManager.LoadSceneAsync_Internal(_loader));

            Assert.That(_loader.scene.isLoaded, Is.True);
            Assert.That(SceneManager.loadedScenes, Has.Member(_loader.scene));
        }

        [UnityTest]
        public IEnumerator Scene_EnterWithArgs_ReachesEveryArgsHandlerOfTheScene() {
            yield return LoadScene();

            SceneManager.NotifyEnter_Internal(_loader.scene, global: false, new DerivedArgs());

            Assert.That(_log, Is.EquivalentTo(new[] {
                "root enter", "root enter DerivedArgs",
                "child enter", "child enter DerivedArgs",
            }));
        }

        [UnityTest]
        public IEnumerator Scene_AsyncEnterWithArgs_IsAwaited() {
            yield return LoadScene();

            yield return Await(SceneManager.NotifyAsyncEnter_Internal(_loader.scene, global: false, new BaseArgs()));

            Assert.That(_log, Is.EquivalentTo(new[] { "root enter async BaseArgs", "child enter async BaseArgs" }));
        }

        [UnityTest]
        public IEnumerator Scene_EnterWithoutArgs_CallsOnlyThePlainHandlers() {
            yield return LoadScene();

            SceneManager.NotifyEnter_Internal(_loader.scene, global: false);

            Assert.That(_log, Is.EquivalentTo(new[] { "root enter", "child enter" }));
        }

        [UnityTest]
        public IEnumerator Scene_ArgsOfAnotherType_AreNotDelivered() {
            yield return LoadScene();

            SceneManager.NotifyEnter_Internal(_loader.scene, global: false, new OtherArgs());

            Assert.That(_log, Is.EquivalentTo(new[] { "root enter", "child enter" }));
        }

        [UnityTest]
        public IEnumerator Scene_GlobalEnter_CallsOnlyTheGlobalHandlers() {
            yield return LoadScene();

            SceneManager.NotifyEnter_Internal(_loader.scene, global: true);

            Assert.That(_log, Is.EqualTo(new[] { "global global enter" }));
        }

        [UnityTest]
        public IEnumerator Scene_Unloaded_ReleasesItsHandlers() {
            yield return LoadScene();
            var scene = _loader.scene;

            yield return Await(SceneManager.UnloadSceneAsync_Internal(_loader));
            _loader = null;

            Assert.That(SceneManager.loadedScenes, Has.No.Member(scene));
            Assert.That(SceneManager.NotifyEnter_Internal(scene, global: false, new BaseArgs()), Is.Null);
            Assert.That(_log, Is.Empty);
        }
    }
}
