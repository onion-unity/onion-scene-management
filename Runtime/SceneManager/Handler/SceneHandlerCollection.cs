using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UniSceneManagement = UnityEngine.SceneManagement;

namespace Onion.SceneManagement {
    internal readonly struct SceneHandlerEntry {
        public readonly ISceneHandler handler;
        public readonly SceneHandlerFlags flags;

        public SceneHandlerEntry(ISceneHandler handler, SceneHandlerFlags flags) {
            this.handler = handler;
            this.flags = flags;
        }
    }

    // One pooled list per scene; each handler is stored once with the notifications it receives.
    internal sealed class SceneHandlerCollection {
        private List<SceneHandlerEntry> _entries;

        // Empty, not null, for an invalid scene or after Clear.
        public IReadOnlyList<SceneHandlerEntry> entries => (IReadOnlyList<SceneHandlerEntry>)_entries ?? Array.Empty<SceneHandlerEntry>();

        public void Initialize(UniSceneManagement.Scene scene) {
            if (!scene.IsValid()) {
                return;
            }

            Clear(); // ensure no handlers are left from previous scenes
            _entries = ListPool<SceneHandlerEntry>.Get();

            using var objectPool = ListPool<GameObject>.Get(out var objects);
            using var handlerPool = ListPool<ISceneHandler>.Get(out var handlers);

            scene.GetRootGameObjects(objects);
            foreach (var obj in objects) {
                obj.GetComponentsInChildren(includeInactive: true, handlers);

                foreach (var handler in handlers) {
                    var flags = handler.GetFlags();
                    if (flags != SceneHandlerFlags.None) {
                        _entries.Add(new SceneHandlerEntry(handler, flags));
                    }
                }

                handlers.Clear();
            }
        }

        public void Clear() {
            if (_entries != null) {
                ListPool<SceneHandlerEntry>.Release(_entries);
                _entries = null;
            }
        }
    }
}
