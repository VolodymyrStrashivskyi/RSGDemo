---
name: tools-unity-unitask
description: >
  UniTask async/await mechanics and pitfalls in this Unity project. Use when writing or
  debugging async code — EnterAsync/ExitAsync, cancellation tokens, .Forget() / fire-and-
  forget, UniTaskCompletionSource, coroutine interop, an "await twice" error, a task that
  never cancels, or a swallowed async exception. Out of scope: the project's async decisions
  (which token, teardown order, async-at-edges) live in the bank's async-and-cancellation.md.
---

# UniTask mechanics

API verified against installed package source
(`Library/PackageCache/com.cysharp.unitask@*`), version **2.5.11**.

**Decisions live in the bank, not here.** Which token to use, cancel-then-dispose order,
and "async only at the edges" are settled in
`MemoryBank/Development Knowledge/Methodology/async-and-cancellation.md`.
This skill is the *how* (API + traps); that document is the *what/why*. Read it before wiring
new async flow.

Heavy interop (`.ToUniTask`, completion-source bridge, WhenAll tuples) →
[references/interop.md](references/interop.md).

## Cancellation token mechanics

The bank decides *which* token; these are the calls to compose them:

```csharp
// Link tokens so either cancels the work (project uses this for scope + shutdown):
CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(a, b);

// MonoBehaviour View self-cancel token (Unity 2022.3+ built-in property):
this.destroyCancellationToken            // == GetCancellationTokenOnDestroy()

// Bail early at an await-free point:
ct.ThrowIfCancellationRequested();

// Attach a token to an awaitable that takes none (completion source, stored UniTask):
await someTask.AttachExternalCancellation(ct);
```

## Gotchas (the delta)

- **A `UniTask` is single-await.** It is a pooled struct — awaiting the *same* instance
  twice throws. To await more than once, store `.Preserve()`, or use a
  `UniTaskCompletionSource` whose `.Task` is multi-consumption.
- **`UniTask.WhenAll` is fail-fast.** It throws the **first** exception immediately and
  does **not** wrap failures in an `AggregateException` like `Task.WhenAll`. Catch the
  concrete type you expect; you won't see the other tasks' exceptions.
- **`.Forget()` swallows exceptions silently** unless the awaited method is a guarded
  `UniTaskVoid` (own try/catch). See Avoid + the bank's fire-and-forget rule.
- **`SuppressCancellationThrow()` returns `(bool isCanceled, T result)`** instead of
  throwing — use it only when cancellation is an ordinary outcome, not an error.

## Avoid

- Don't `.Forget()` a task that can throw — exceptions vanish. Wrap the body in a guarded
  `UniTaskVoid` that catches all but `OperationCanceledException` (bank fire-and-forget rule).
- Don't log `OperationCanceledException` as an error — it's control flow on teardown; catch
  it separately and produce no side effects after it.
- Don't block the main thread with `.Result` or `.GetAwaiter().GetResult()` — it deadlocks
  Unity's single-threaded loop.
- Don't await the same `UniTask` twice — it throws; `.Preserve()` it or use a completion source.
- Don't create an ad-hoc `CancellationTokenSource` in scope-owned code — use the injected
  scope token, so teardown cancels it (bank cancellation rule).
- Don't make pure game logic `async` — async lives only at the edges (transitions, loading,
  I/O). Bagh-Chal rules stay synchronous.
