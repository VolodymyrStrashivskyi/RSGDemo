# VContainer entry points reference

API verified against version **1.19.0**. `RegisterEntryPoint<T>()` registers a plain C# class into Unity's player loop — no MonoBehaviour needed.

## Interfaces (all in `VContainer.Unity`)

| Interface | Called |
|---|---|
| `IInitializable` / `IPostInitializable` | container built (before Start) |
| `IStartable` / `IPostStartable` | Start |
| `IAsyncStartable` | Start, async |
| `ITickable` / `IPostTickable` | Update |
| `IFixedTickable` / `IPostFixedTickable` | FixedUpdate |
| `ILateTickable` / `IPostLateTickable` | LateUpdate |

```csharp
builder.RegisterEntryPoint<AppEntryPoint>();
builder.RegisterEntryPointExceptionHandler(ex => Debug.LogException(ex));
```

## `IAsyncStartable` signature depends on defines

Source: `Annotations/IAsyncStartable.cs`. The return type changes with the build config — match it or the class won't compile as an entry point:

```csharp
Awaitable StartAsync(CancellationToken cancellation = default);
// VCONTAINER_UNITASK_INTEGRATION (UniTask installed) → Awaitable == UniTask
// else Unity 2023.1+                                  → UnityEngine.Awaitable
// else                                                → System.Threading.Tasks.Task
```

This project has UniTask installed, so the signature resolves to `UniTask StartAsync(...)`.
