# Lifecycle: sequences, control, cancellation, destruction

## Sequence composition

```csharp
Sequence.Create()
  .Chain(Tween.PositionX(t, 10f, 1f))      // starts after everything added so far
  .Group(Tween.Scale(t, 2f, 0.5f))         // parallel with the PREVIOUS Chain/Group/Insert
  .Insert(atTime: 0.5f, Tween.Color(img, Color.red, 0.5f))  // absolute time, may overlap
  .ChainDelay(0.2f)
  .ChainCallback(this, self => self.OnDone());
```

`Group` attaches to the immediately preceding entry, not to the sequence as a whole — a `Group`
before any `Chain` starts at time 0.

Callbacks in sequences have the same target-first overloads: `ChainCallback<T>`,
`InsertCallback<T>`, `GroupCallback<T>`.

`Sequence.Create(sequenceEase: Ease.OutBounce)` eases the whole sequence's timeline.

## Nesting rule

Adding a tween or sequence to a parent makes it *nested*. Any direct `Stop()`, `Complete()`,
`isPaused`, `timeScale`, or `elapsedTime` on the nested animation logs an error, because the
parent owns the timeline and the result would be ambiguous.

Pattern for a re-triggerable composite animation — keep the root, replace it:

```csharp
Sequence _current;

void Play() {
  if (_current.isAlive) {
    _current.Stop();
  }
  _current = Sequence.Create().Chain(BuildInner());
}
```

If you need to stop parts independently, they should not have been one sequence — run separate
sequences instead of deep nesting.

## Controlling live animations

```csharp
if (tween.isAlive) { /* duration, elapsedTime, progress, interpolationFactor readable */ }

tween.Stop();                          // freeze at current value
tween.Complete();                      // jump to endValue and fire OnComplete
tween.isPaused = true;
tween.elapsedTime = 0.5f;              // also elapsedTimeTotal (all cycles)
tween.progress = 0.5f;                 // normalized, current cycle
tween.timeScale = 2f;

Tween.StopAll(onTarget: transform);    // all three return int = affected count
Tween.CompleteAll(onTarget: transform);
Tween.SetPausedAll(true, onTarget: transform);
```

`onTarget: null` hits **every** tween in the game — only appropriate on scene teardown.

`Stop()` and `Complete()` on a dead tween are safe no-ops; no null/alive check is required
before them (only before reading properties).

## Cancellation

```csharp
Sequence.Create()
  .Chain(Tween.PositionX(t, 10f, 1.5f))
  .SetCancellationToken(ct);
```

- Applies to the root only; calling it on a nested child is an error, and a second call on the
  same animation is an error.
- Cancellation acts like `Stop()` and lands on the next PrimeTween update — not immediately.
  An already-cancelled token stops the animation right away.
- `await`ing a cancelled animation throws `OperationCanceledException`, *even if the animation
  had already completed naturally*. Awaiting one that was cancelled and recycled (typically the
  next frame) is a silent no-op.

## Waiting

```csharp
await Tween.Scale(t, 2f, 0.5f);              // implicit UniTask (UNITASK_INSTALLED)
await Tween.Delay(1f);                       // non-allocating replacement for Task.Delay
yield return Tween.Delay(1f).ToYieldInstruction();   // coroutines
```

Continuations always resume from `PrimeTweenManager.Update`, regardless of the tween's
`updateType`. Async/await and coroutines allocate (C#/Unity, not PrimeTween) — in hot paths
compose a `Sequence` instead of awaiting a chain of tweens.

## Destroyed targets

Destroying a GameObject with a running tween is safe: PrimeTween checks the Unity target before
each update and stops the animation. The consequence is that **`OnComplete` never fires** — an
error is logged so the skipped callback does not pass unnoticed.

- Cosmetic callback that may legitimately be skipped: `.OnComplete(…, warnIfTargetDestroyed: false)`.
- Logic that must run regardless: keep it out of the tween entirely.
- Before unloading a scene, call `Tween.StopAll()` to avoid a burst of these warnings.
- There is deliberately no `OnTargetDestroyed` callback — cleanup belongs before destruction.

## Update loop

`updateType` picks the driving loop: `Update` (default), `LateUpdate`, `FixedUpdate` — passed via
`TweenSettings`/`TweenSettings<T>` for tweens, or `Sequence.Create(updateType: …)` for sequences.
`useUnscaledTime: true` ignores `Time.timeScale`. Project-wide default:
`PrimeTweenConfig.defaultUpdateType`.
