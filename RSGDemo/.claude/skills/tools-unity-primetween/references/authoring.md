# Authoring animations

## Finding the right method

`Tween.<Property>(target, endValue, duration, ease)` covers Transform, RectTransform/uGUI,
TextMeshPro, Material, Camera, AudioSource, Light, CanvasGroup, UI Toolkit, and more.
Type `Tween.` and read the completion list before considering `Tween.Custom`.

Naming pattern of the generated API:

| Suffix | Meaning |
|---|---|
| `Tween.PositionX/Y/Z`, `LocalPosition…`, `UIAnchoredPosition…` | single-axis variants exist for every vector property |
| `Tween.…AtSpeed(target, endValue, speed)` | duration derived as `distance / speed` |
| `Tween.…Additive(target, deltaValue, …)` | experimental; stacks with other tweens instead of overwriting |
| `Tween.Shake…` / `Tween.Punch…` | `strength`, `duration`, `frequency`; `Tween.ShakeCamera(cam, strengthFactor)` for screen shake |
| `Tween.GlobalTimeScale`, `Tween.TweenTimeScale(tween, …)` | animate time itself |

Overloads: every method also accepts an explicit `startValue` (the DOTween `.From()`
equivalent is `Tween.PositionX(t, from, to, duration)`), plus `startDelay`, `endDelay`,
`cycles`, `cycleMode`, `useUnscaledTime`.

## Inspector integration (free-edition authoring)

Serialize the settings, not the numbers. This is the project's substitute for the PRO
`TweenAnimation` component.

```csharp
[SerializeField] TweenSettings<float> _windowSlide;
[SerializeField] ShakeSettings _hitShake;

void SetOpened(bool isOpened) =>
  Tween.UIAnchoredPositionY(_window, _windowSlide.WithDirection(toEndValue: isOpened));

void OnHit() => Tween.ShakeLocalPosition(transform, _hitShake);
```

- `TweenSettings<T>` carries startValue/endValue/duration/ease (or an AnimationCurve, switchable
  in the Inspector without a code change) plus cycles and updateType.
- `.WithDirection(bool toEndValue)` swaps start/end — use it for every open/close, show/hide toggle
  instead of a ternary on the end value.
- Non-generic `TweenSettings` carries only timing; use it to pass `updateType`:
  `Tween.PositionX(t, 10f, new TweenSettings(1f, updateType: UpdateType.LateUpdate))`.

## Easing

`Ease.InOutSine` etc. by enum; an `AnimationCurve` in the same argument slot; or parametric:

```csharp
Easing.Overshoot(strength)          // tunes Ease.OutBack
Easing.Bounce(strength)             // tunes Ease.OutBounce
Easing.BounceExact(amplitude)       // first bounce is exactly N meters/degrees
Easing.Elastic(strength, period)    // tunes Ease.OutElastic
```

`PrimeTweenConfig.defaultEase` sets the project-wide default (it may not be `Ease.Custom`
or `Ease.Default`).

## Custom tweens

`Tween.Custom` supports `float, Color, Vector2/3/4, Quaternion, Rect` only. Always use the
target-first overload, and accept `TweenSettings<T>` so it stays Inspector-tunable:

```csharp
[SerializeField] TweenSettings<float> _fill;
Tween.Custom(this, _fill, (self, v) => self.SetFill(v));
```

## Cycles

`cycles: -1` is infinite. `CycleMode`: `Restart` (default), `Yoyo` (easing stays forward),
`Incremental` (end value shifts by the delta each cycle), `Rewind` (easing reverses — this is
DOTween's Yoyo).

Stop an infinite yoyo cleanly with `tween.SetRemainingCycles(stopAtEndValue: true)`, which lets
it finish at a defined value instead of freezing mid-motion like `Stop()`.

`Sequence.Create(cycles, SequenceCycleMode.…)` — note the *separate* enum; `Incremental` is not
supported there and is silently downgraded to `Restart`.

## Capacity

PrimeTween pre-allocates a tween pool and grows it (allocating) on demand. Read
'Max alive tweens' from the PrimeTweenManager inspector after a play session, then pin it:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
static void Prewarm() => PrimeTweenConfig.SetTweensCapacity(64);
```

Called later (after the manager exists) it reallocates and produces garbage — that is why it
belongs in `BeforeSplashScreen`.

## Diagnostics

`PrimeTweenManager` under DontDestroyOnLoad lists every live tween with its Unity target.
Warning toggles on `PrimeTweenConfig`: `warnZeroDuration`, `warnEndValueEqualsCurrent`,
`warnTweenOnDisabledTarget`, `warnStructBoxingAllocationInCoroutine`, `warnIfTargetDestroyed`.
Leave them on; silence individual cases at the call site instead.
