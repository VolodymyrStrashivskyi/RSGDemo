# uGUI performance reference

Non-obvious cost model of `com.unity.ugui` **2.5.0**. Everything here is a trap the layout
"just works" hides until profiling. Profile rebuilds with the Frame Debugger + `UI` profiler
module; a canvas rebuilds when any of its graphics is marked dirty.

## Canvas rebuild = the whole canvas rebatches

Changing any `Graphic` under a Canvas (text, color, sprite, enabled) dirties that Canvas and
triggers a **rebatch of the entire canvas** — not just the changed element. A Canvas is the
batching boundary.

- **Split frequently-changing content onto its own nested Canvas.** A nested `Canvas`
  component is a rebuild/batch island: a per-frame score counter on its own sub-canvas won't
  rebatch the static HUD around it. This is the single biggest uGUI win.
- Static screens: keep them on one canvas, change nothing at runtime → zero rebuild cost.
- Toggling `Canvas.enabled` hides/shows without destroying the batch; cheaper than moving
  objects in/out of the hierarchy.

## Raycast Target — off by default on non-interactive graphics

Every `Graphic` with `raycastTarget = true` is walked on every pointer event (GraphicRaycaster
iterates all of them). Labels, icons, backgrounds, decorative images need it **off**. Only
buttons, toggles, input fields, and explicit input-catchers keep it on. Default is on — this
is the most common silent cost.

## Layout components are recomputed, not free

`LayoutGroup` (Horizontal/Vertical/Grid) and `ContentSizeFitter` run a layout pass on every
rebuild, and **nested** layout groups multiply it (each level re-solves its children).

- For a **static** arrangement, don't use a layout group at all — set anchors/pivots once.
- Use layout groups only where content size is genuinely dynamic, and keep nesting shallow.
- Never let a layout group drive a large scroll list — see lists-and-pooling.md.

## Masking: RectMask2D over Mask

- `RectMask2D` — rectangular clip, cheap, no extra draw call, preserves batching. Use for
  scroll views and rectangular clips.
- `Mask` — stencil-based, draws the mask graphic an extra time and **breaks batching** across
  the boundary. Use only for non-rectangular (shaped) masks.

## Batching depends on atlas + draw order

uGUI batches consecutive graphics sharing a texture/material. A graphic from a different atlas
inserted between them (by hierarchy/draw order) **breaks the batch into three**.

- Put UI sprites in a **Sprite Atlas** so they share one texture.
- Watch depth order: don't interleave different atlases; group same-atlas elements together.

## Animation: never animate what dirties layout

- Fade with `CanvasGroup.alpha` — one value, no per-graphic dirtying, no rebuild. Never fade
  by animating each `Graphic.color` alpha.
- Move/scale via `RectTransform`/`Transform` — animating anchored position does not rebuild
  the mesh; animating layout-driven properties (sizeDelta under a fitter, layout spacing)
  rebuilds every frame.
- A per-frame changing `Text` belongs on its own sub-canvas (see rebuild section).

## Overdraw

Stacked transparent full-screen images are GPU fill-rate cost (mobile). A full-screen input
blocker should be one graphic; don't leave layers of invisible alpha images stacked.
