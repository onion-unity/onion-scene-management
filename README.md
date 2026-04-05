# Onion.SceneManagement

Unity scene management package with async loading, stable scene references, transition flows, and editor support.

## Quick Start

```csharp
using Onion.SceneManagement;
using Onion.SceneManagement.Transition;

public class Example {
	public async Awaitable Load() {
		await SceneManager.LoadSceneAsync("Your scene");
	}
}
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


### SceneReference

- Stable scene references by GUID
- Inspector validation with quick build-setting fixes

## Requirements

- Unity 6.0 or newer
- Optional: Addressables package

## Notes

- `SceneReference` is based on the ideas from [Eflatun.SceneReference](https://github.com/starikcetin/Eflatun.SceneReference) by starikcetin.
- Package name: `com.onion.scenemanagement`.
