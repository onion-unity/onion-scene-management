# Changelog

All notable changes to this package are documented in this file.

## [0.4.0] - 2026-10-23

### Breaking changes

- `UnloadScene(s)` and `UnloadScene(s)Async` no longer take an `UnloadMode` and now unload exactly the given scenes. Before, the default `CleanUp` unloaded every *other* scene and kept the given one, and `None` unloaded nothing. Pinned scenes are unloaded (and unpinned) when given explicitly; the Bootstrap scene is never unloaded.
- `WaitWhile` and `WaitUntil` were swapped and now behave like Unity's: `WaitWhile` waits while the condition is true, `WaitUntil` waits until it becomes true. Swap any calls written against the old behavior.
- A `Transition` created with no destinations and run with `UnloadMode.CleanUp` now unloads every loaded scene except pinned scenes and the Bootstrap scene. Before, it did nothing.

- With Use Bootstrap on, a player build now fails unless the Bootstrap scene is the first scene being built. The settings inspector warns when it isn't first in the active Build Profile scene list and has a **Fix** button; a player whose first scene isn't the Bootstrap scene logs an error on startup.

### Changed

- The settings asset is now registered in `EditorBuildSettings` (config key `com.onion.scenemanagement`) instead of permanently in Preloaded Assets. It is added to Preloaded Assets only while a player build runs, so Project Settings no longer change.
- The Bootstrap scene is no longer included in `CleanUp`, so its exit handlers aren't called while it stays loaded.
- The scenes open before entering Play Mode with Bootstrap are kept in `SessionState` instead of a sub-asset of the settings asset.
- `Transition` instances are no longer pooled.
- Fewer GC allocations during transitions.

### Fixed

- `Callback(Func<Awaitable>)` never ran its callback.
- `LoadScenesAsync` / `UnloadScenesAsync` returned `null` for an empty list, so awaiting them threw.
- Loading a scene that isn't in the Build Profile scene list (or an invalid `SceneReference`) threw a `NullReferenceException`; it now logs an error and skips the scene.
- Holding a finished `Transition` and executing it again could run another caller's transition.
- With domain reload disabled, static state (pinned scenes, events, loaders, the Bootstrap state, settings changed at runtime) carried over into the next Play session, e.g. a `Placeholder` destroyed itself with Bootstrap turned off.
- The settings asset was created, and Preloaded Assets changed, in batch mode.

### Migration

Open the project in the Editor once after updating. The package then automatically:

- registers the settings asset that was in Preloaded Assets under the new config key,
- removes it from Preloaded Assets,
- clears the `hideFlags` earlier versions saved into the asset.

Commit the resulting changes to `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ProjectSettings.asset` and the settings asset. CI builds in batch mode use the registered asset and create nothing.

If the settings asset still contains the `BootstrapGroup` sub-asset from 0.3.0 or earlier, it now shows as a missing script; delete that sub-asset from the file.

### Known issues

- `Load(activateOnLoad: false)` is ignored; scenes are always activated on load, so `Activate` has nothing to do.

## Earlier versions

Releases up to 0.3.0 are not documented here; see the `v0.1.0` – `v0.2.3` tags and the commit history.
