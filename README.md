# Onion.SceneManagement

Unity scene management package with async loading, stable scene references, transition flows, and editor support.

## Requirements

- Unity 6.0 or newer
- Optional: Addressables package

## Quick Start

```csharp
using Onion.SceneManagement;
using Onion.SceneManagement.Transition;
```

```csharp
SceneReference _reference;
List<SceneReference> _scenes;

// by SceneReference (also supports scene name and build index)
await SceneManager.LoadSceneAsync(_reference); 

// Addressable scenes go through a SceneReference
await SceneManager.LoadSceneAsync(SceneReference.FromAddress("Scenes/Title"));

// with transition mode (Default, Additive, Refresh)
await SceneManager.LoadSceneAsync(_reference, TransitionMode.Additive);

// unloads exactly the given scenes
await SceneManager.UnloadSceneAsync(_reference);

// extensions for IEnumerable<SceneReference>
await _scenes.LoadAsync();   
await _scenes.UnloadAsync();
```

```csharp
List<SceneReference> _scenes;
await Transition.Create(_scenes)
	.NotifyExit()
	.Unload()
	.Load()
	.NotifyEnter()
	.ExecuteAsync();
```

## Features

### Loading and Unloading

`LoadScene(s)` and `LoadScene(s)Async` take a `SceneReference`, a scene name, or a build index. The destinations are the scenes that should be loaded when the transition ends; how the other scenes are treated depends on the `TransitionMode`:

| `LoadMode` | Behavior |
|---|---|
| `None` | Loads a destination only if it isn't loaded yet. |
| `Additive` | Always loads another instance of each destination. |
| `Refresh` | Unloads a loaded destination first, then loads it again. |

| `UnloadMode` | Behavior |
|---|---|
| `CleanUp` | Unloads every loaded scene that isn't a destination, except pinned scenes and the Bootstrap scene. |
| `None` | Keeps every loaded scene. |

`TransitionMode.Default` is `None` + `CleanUp`; `TransitionMode.Additive` and `TransitionMode.Refresh` also use `CleanUp`.

`UnloadScene(s)` and `UnloadScene(s)Async` unload exactly the given scenes, pinned ones included. The Bootstrap scene is never unloaded.

When **Overlapped Load** is on in the settings, `LoadScene(s)Async` loads the new scenes before unloading the old ones.

### Pinning

Pinned scenes are never unloaded by `CleanUp`. Unloading one explicitly also unpins it.

```csharp
SceneManager.Pin(_reference);       // or _reference.Pin()
SceneManager.Unpin(_reference);
bool pinned = SceneManager.IsPinned(_reference);
```

### Transition

- Create custom transition flows with fluent API
```csharp
await Transition.Create(scenesToLoad)
    .NotifyExit()
    .Unload()
    .Delay(1f)
    .Load()
    .NotifyEnter()
    .Callback(() => Debug.Log("Enter complete"))
    .ExecuteAsync();
```

- Available built-in transition steps:
    - `Load`, `Unload` - load the destinations and unload the scenes the `TransitionMode` removes
    - `NotifyExit`, `NotifyEnter` - call the scene handlers (see below)
    - `Callback` - execute custom code during transition; an `Func<Awaitable>` overload is awaited
    - `Delay`, `DelayFrame`, `NextFrame` - wait for seconds (optionally ignoring time scale) or frames
    - `WaitWhile`, `WaitUntil` - wait for custom conditions, like Unity's `WaitWhile` / `WaitUntil`
    - `WaitForEndOfFrame`, `WaitForFixedUpdate` - wait for specific Unity events
- `Callback`, `Delay`, `DelayFrame`, `WaitWhile` and `WaitUntil` take `parallel: true` to run alongside the next steps; the next non-parallel step waits for them.
- A transition runs once. Create a new one for every transition.

### Scene Handlers

`MonoBehaviour`s in a scene that implement these interfaces are called when the scene enters or exits a transition:

```csharp
using Onion.SceneManagement.Handler;
using Onion.SceneManagement.Handler.Attributes;

public class TitleScreen : MonoBehaviour, ISceneEnterHandler, IAsyncSceneExitHandler {
    public void OnSceneEnter() { }

    public async Awaitable OnSceneExitAsync() {
        await FadeOutAsync();
    }
}
```

- `ISceneEnterHandler`, `ISceneExitHandler`, `IAsyncSceneEnterHandler`, `IAsyncSceneExitHandler`. Async handlers are awaited before the transition continues.
- Mark the handler method with `[ExecuteByAnyScene]` to call it on every transition, not only when its own scene enters or exits.
- `SceneManager.sceneLoaded` / `SceneManager.sceneUnloaded` are raised for every scene loaded or unloaded through the package.

### Bootstrap

A Bootstrap scene holds objects that live for the whole game (managers, services). Turn on **Use Bootstrap** in the settings and assign the **Bootstrap Scene** (the inspector can create one).

- In a build, put the Bootstrap scene first in the scene list. The **Initial Scenes** are loaded after it on startup.
- In the Editor, entering Play Mode always starts from the Bootstrap scene, then reopens the scenes that were open.
- The Bootstrap scene is never unloaded, and it gets no exit notifications.
- Add a `Placeholder` component to objects a scene only needs when opened on its own (for example a camera). It is destroyed when the Bootstrap scene is running and removed from builds.

### SceneReference

- Stable scene references by GUID
- Inspector validation with quick fixes for the Build Profile scene list
  
    <img src=".github/images/reference_0.png" width="300">

- Also created with `SceneReference.FromPath`, `FromName`, `FromBuildIndex`, `FromAddress` (Addressables) and `FromScene`.

### Settings

The settings asset is created at `Assets/Settings/Onion_SceneManagementSetting.asset` when the Editor loads (not in batch mode), and registered in `EditorBuildSettings`. It is added to Preloaded Assets only while a player build runs, so Project Settings aren't changed. The asset's inspector edits it; there is no Project Settings page yet.

`SceneManagementSettings.useBootstrap` and `SceneManagementSettings.overlapLoading` can be changed at runtime. They are reloaded from the asset at the start of every Play session, also with domain reload disabled.

## Notes

- `SceneReference` is based on the ideas from [Eflatun.SceneReference](https://github.com/starikcetin/Eflatun.SceneReference) by starikcetin.
- Package name: `com.onion.scenemanagement`.
- See [CHANGELOG.md](CHANGELOG.md) for changes and migration notes.
