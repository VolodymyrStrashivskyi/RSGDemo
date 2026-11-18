# Lists, scroll views, pooling

## Long lists must be virtualized

A `ScrollRect` with one instantiated item per data row pays instantiation + GC + rebuild +
draw for **every** row, on-screen or not. Past a few dozen rows this stutters on open and
scroll.

- **Recycle a fixed pool of cells** sized to the viewport + a small buffer. On scroll, move a
  cell that left the view to the other end and rebind its data — never instantiate/destroy
  during scroll.
- Content height = `rowCount * rowHeight` (set the content `RectTransform` directly); position
  each pooled cell by index. Don't drive the list with a `VerticalLayoutGroup` — it re-solves
  all rows every rebuild.
- Clip with `RectMask2D`, not `Mask`.

If the list is short and static (a 4-item menu), skip all of this — plain children are fine.

## Pool popups and frequently spawned UI

Instantiate/Destroy per open causes GC spikes and a canvas rebuild each time.

- Pool dialogs, toasts, floating combat numbers, tooltip instances — deactivate on close
  (`SetActive(false)` or `Canvas.enabled = false`), reactivate + rebind on reopen.
- One-off screens that live for a whole state don't need pooling; the state/scope owns them.

## Show / hide a screen

Screens, popups, and buttons are shown and hidden only through their `UiView` visibility
(`UISystemCapability`, bank `ui-composition.md`): it fades through `CanvasGroup`, blocks input while
hiding, and deactivates the root once hidden. Don't toggle `SetActive` or `Canvas.enabled` on
them yourself.
