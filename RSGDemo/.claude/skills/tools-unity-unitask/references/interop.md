# UniTask interop reference

API verified against installed package `Library/PackageCache/com.cysharp.unitask@*`, version **2.5.11**.

## Await a Unity async op — `.ToUniTask(cancellationToken:)`

Every Unity `AsyncOperation` / awaitable converts. Always pass the token.

```csharp
// Scene (this project: SceneService.LoadAsync):
await SceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: ct);

// With progress:
await SceneManager.LoadSceneAsync(sceneName).ToUniTask(progress: progress, cancellationToken: ct);

// UnityWebRequest:
await request.SendWebRequest().ToUniTask(cancellationToken: ct);

// Legacy coroutine → UniTask, and back:
await MyCoroutine().ToUniTask(cancellationToken: ct);
StartCoroutine(MyUniTask().ToCoroutine());
```

Addressables handle conversion has its own release/ref-count rules — see the
`tools-unity-addressables` skill, not here.

## Bridge a callback → await — `UniTaskCompletionSource`

Turn an event/callback (AnimationEvent, native SDK callback) into an awaitable. The
completion-source `.Task` is multi-consumption (unlike a bare `UniTask`).

```csharp
private UniTaskCompletionSource _attackHit;

// Raised by the AnimationEvent placed on the attack clip:
public void OnAttackHit() => _attackHit?.TrySetResult();

public async UniTask PlayAttackAsync(CancellationToken ct)
{
    _attackHit = new UniTaskCompletionSource();
    animator.SetTrigger("Attack");
    await _attackHit.Task.AttachExternalCancellation(ct);
}
```

## Await a Unity event directly — async triggers

UniTask's built-in event awaiters — the project's sanctioned alternative to C# events for
one-shot waits (see the bank's `module-communication.md`). Always pass the lifetime token:

```csharp
await button.OnClickAsync(ct);
Collider other = await gameObject.GetAsyncTriggerEnterTrigger().OnTriggerEnterAsync(ct);
```

For a repeating subscription prefer a C# event; use these only to *await* a single occurrence.

## Add cancellation to a tokenless awaitable — `AttachExternalCancellation`

For an awaitable you can't pass a token into (a completion source, a stored `UniTask`):

```csharp
await someUniTask.AttachExternalCancellation(ct);
```

Used in `AppStateMachine.SwapAsync` to make an in-flight transition cancellable on shutdown.

## Parallel — tuple results

```csharp
var (textures, audio, config) = await UniTask.WhenAll(
    LoadTexturesAsync(), LoadAudioAsync(), LoadConfigAsync());

var (winIndex, a, b) = await UniTask.WhenAny(TryServerAAsync(), TryServerBAsync());
```

`WhenAll` is **fail-fast** — see the gotcha in SKILL.md.
