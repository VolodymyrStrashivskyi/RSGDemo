---
name: tools-unity-primetween
description: >
  PrimeTween mechanics and traps in this Unity project — animating transforms, UI, colors,
  shakes, delays, and Sequences. Use when writing or debugging tween code: "animate",
  "tween", "fade", "shake", "punch", "Tween.Delay", "Sequence.Create", "ease", "PrimeTween",
  a tween that won't stop, or "manipulating nested animation is not allowed". Out of scope:
  UniTask mechanics (tools-unity-unitask) and the project's animation decisions — who owns
  timing, awaited vs fire-and-forget (bank: view-animation.md).
---

# PrimeTween mechanics

API verified against installed package source
(`Library/PackageCache/com.kyrylokuzyk.primetween@*`), version **1.4.10**.

**Decisions live in the bank, not here.** That animation belongs to the View, that timing is
serialized rather than passed in, and that gating animations are awaited while cosmetic ones
are fire-and-forget are settled in `MemoryBank/Development Knowledge/Methodology/view-animation.md`.
This skill is the *how*; that document is the *what/why*. Read it before adding animation to a feature.

## Edition gate — free, not PRO

`PRIME_TWEEN_PRO` is defined only for the `[1.4.10-pro]` package; this project has the free
one. So `TweenAnimation`, `TweenAnimationComponent`, and `Tween.MaterialPropertyBlock*()`
**do not exist here** — never write them and never propose "author the animation in the
Inspector, no code". Inspector authoring in this project means a serialized
`TweenSettings<T>` field passed into a `Tween.` call → [references/authoring.md](references/authoring.md).

## Non-negotiable mechanics

**Tweens are non-reusable.** A finished tween is dead; there is no `PlayForward`/
`PlayBackwards`/`Restart`/cached-paused-tween pattern. To reverse, start a new tween toward
the other end value — it seamlessly overwrites the running one on the same target.

```csharp
void SetOpened(bool isOpened) =>
  Tween.UIAnchoredPositionY(window, isOpened ? 0f : -500f, 0.5f);
```

**Every callback takes a target-first overload — use it.** The closure form allocates on
every call; the target form allocates nothing.

```csharp
Tween.Scale(transform, 0f, 0.3f).OnComplete(this, self => self.Despawn());
Tween.Delay(this, 1f, self => self.Despawn());
Tween.Custom(this, 0f, 10f, 1f, (self, v) => self.value = v);
```

**Pass `target` even where it is optional** (`Tween.Delay`, `Tween.Custom`) — it is what
makes `Tween.StopAll(onTarget: x)` work and what shows the tween in the PrimeTweenManager
inspector. There is no `SetId()`; target *is* the identity.

**Never touch a nested animation.** Once a tween/sequence is added to a Sequence, calling
`Stop()`, `Complete()`, `isPaused`, `timeScale`, or `elapsedTime` on it logs an error. Store
the **root** Sequence and act on that.

**`await tween` yields a `UniTask` here.** `UNITASK_INSTALLED` is auto-defined (versionDefines
sees `com.cysharp.unitask`), so `Tween`/`Sequence` implicitly convert to `UniTask` and expose
`.ToUniTask()`. A gating animation takes the awaiting caller's token as a parameter and applies
`.SetCancellationToken(ct)` to the root — the caller is what must unwind on teardown (bank
`view-animation.md`). Lifecycle, cancellation, and destroy semantics →
[references/lifecycle.md](references/lifecycle.md).

## Avoid

- Don't cache a tween to replay it later — it's dead after completion; start a new one.
  Caching leaks a stale struct and the replay silently no-ops.
- Don't write `.OnComplete(() => Method())` — the closure captures `this` and allocates every
  call; use `.OnComplete(this, self => self.Method())`.
- Don't put business logic in `OnComplete` — if the target is destroyed mid-tween the callback
  is **silently skipped**. Only cosmetic work belongs there.
- Don't `Stop()` a tween that lives inside a Sequence — it errors; stop the root Sequence.
- Don't translate DOTween's `LoopType.Yoyo` to `CycleMode.Yoyo` — DOTween's Yoyo is PrimeTween's
  `CycleMode.Rewind` (Yoyo keeps easing forward, Rewind reverses it).
- Don't `yield return tween` in a coroutine — the `Tween` struct boxes; use
  `yield return tween.ToYieldInstruction()`.
- Don't call `SetCancellationToken()` twice on one animation — the second call logs an error
  and is ignored. On a Sequence, call it on the root, not on children.
- Don't chain speed-based tweens without an explicit `startValue` — duration is computed at
  creation time from the *current* value, so the second tween mis-measures its distance.
- Don't reach for `Tween.Custom` before typing `Tween.` — the built-in method almost always
  exists and is allocation-free ([references/authoring.md](references/authoring.md)).
