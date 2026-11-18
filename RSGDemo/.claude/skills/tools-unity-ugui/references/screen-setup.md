# Screen setup (uGUI mechanics)

The project's UI *architecture* (View = dumb MonoBehaviour raising C# events, one Mediator per
feature) lives in the bank's `ui-mediator.md` + `module-communication.md` — not here. This covers only
the uGUI wiring of a screen.

## One screen = one prefab

A screen or popup is a prefab **without its own Canvas**. `ScreenService` / `PopupService`
instantiate it through the DI resolver under a host (`ScreenHostView` / `PopupHostView`) in the
`Gameplay` scene, whose single `Canvas` renders all UI. The prefab root carries the `UiView`
visibility and transition — the two prefab templates are in the bank's `ui-composition.md`. The
feature View `[SerializeField]`-references its widgets — see `MainMenuView` / `BattleHudView`.

## Canvas render mode

- **Screen Space – Overlay** — default, cheapest, no camera dependency. Use unless you need UI
  composited with the 3D scene.
- **Screen Space – Camera** — only for UI that must respect a camera (perspective, post FX).
- **World Space** — diegetic UI only (in-world panels).

## Resolution independence

- One **`CanvasScaler`** on the root, `Scale With Screen Size`, reference resolution set once,
  Match tuned to the game's orientation. Without it, UI is pixel-fixed and breaks across devices.
- Lay out with **anchors + pivots**, not absolute positions: anchor to the relevant edge/center
  so elements track resolution changes. A common miss: leaving everything anchored to center with
  fixed offsets, which drifts on other aspect ratios.

## Input plumbing

- Exactly one **`EventSystem`** in the scene (the bootstrap/root scene owns it), not one per
  screen prefab — duplicates cause "two EventSystems" warnings and swallowed input.
- Each Canvas that receives input needs a `GraphicRaycaster`; a purely decorative overlay canvas
  does not.
