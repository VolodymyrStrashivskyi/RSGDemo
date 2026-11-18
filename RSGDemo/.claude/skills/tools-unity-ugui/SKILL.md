---
name: tools-unity-ugui
description: >
  Building and optimizing Unity uGUI (Canvas-based UI) — screens, menus, HUD, popups, scroll
  views — and keeping them fast: canvas rebuild batching, raycast-target cost, masking, layout
  cost, sprite-atlas batching, list virtualization, pooling. Use when making a screen, button,
  list, scroll view, dialog, or HUD, when a UI element does not react to clicks, or when UI
  stutters, lags, or rebuilds every frame. uGUI only — for UXML/USS/UIDocument (UI Toolkit)
  this is the wrong skill. How a surface is assembled and shown is in the bank's ui-composition.md;
  the View/Mediator split (where button logic lives) is in the bank's ui-mediator.md.
---

# uGUI — build it fast by construction

Mechanics + performance for `com.unity.ugui` **2.5.0** (Unity 6; TextMeshPro bundled — prefer
it over legacy `Text`). uGUI perf is decided at authoring time: the anti-patterns below are
baked into the prefab, not fixable later without rework.

**Architecture is the bank's, not here.** How a screen, popup, or button is assembled from the
visibility, transition, and feedback parts of `UISystemCapability` is settled in
`MemoryBank/Development Knowledge/Methodology/ui-composition.md`; View = dumb `MonoBehaviour`,
Mediator coordinates — in `ui-mediator.md` and `module-communication.md`. This skill is the
uGUI *how* + cost model.

Read on demand:
- Canvas/scaler/anchors, screen-prefab wiring, render mode → [references/screen-setup.md](references/screen-setup.md)
- Rebuild batching, raycast, masking, atlas, animation cost → [references/performance.md](references/performance.md)
- Scroll-list virtualization, pooling, show/hide → [references/lists-and-pooling.md](references/lists-and-pooling.md)

## The one cost model to hold

A **Canvas is the batch/rebuild unit**: dirtying any graphic under it rebatches the *whole*
canvas. So the core discipline is — keep static UI static, and isolate anything that changes
per-frame onto its own nested Canvas. Everything in performance.md follows from this.

## Avoid

- Don't leave `Raycast Target` on non-interactive graphics (labels, icons, backgrounds) — the
  raycaster walks every one on each pointer event. On only for real input targets.
- Don't put per-frame-changing content (score, timer, health bar) on the same Canvas as static
  UI — it rebatches everything each frame. Give it its own nested Canvas.
- Don't drive a long scroll list with a `LayoutGroup` + one item per row — it re-solves and
  instantiates all rows. Virtualize with a recycled cell pool (lists-and-pooling.md).
- Don't fade UI by animating each graphic's color alpha — use `CanvasGroup.alpha` (no rebuild).
- Don't use `Mask` for a rectangular clip — `RectMask2D` is cheaper and keeps batching.
- Don't show or hide a screen, popup, or button yourself — neither `SetActive` nor
  `Canvas.enabled`; go through its `UiView` visibility (bank ui-composition.md).
- Don't add a second `EventSystem` per screen prefab — one lives in the root scene; duplicates
  swallow input.
- Don't put game rules in the View — it only raises events and updates visuals (bank ui-mediator).
