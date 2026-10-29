# Tooltip — Native `DelayedTooltip` and Shared Positioning Module

- **Date:** 2026-10-28
- **Status:** Implemented 2026-10-28 (see addendum)
- **Author:** Claude
- **Scope:** `Components/Common/DelayedTooltip.razor` (rewritten, gains `.razor.cs`/`.razor.css`), new
  `TooltipPlacement.cs`, new `ClimateExplorer.Web.Client/wwwroot/js/floating.js` and `tooltip.js`,
  `App.razor`, `Components/Location/ClimateStripe.razor`, `Components/Location/ExtremeYears.razor`,
  `app.css` (Tippy overrides)
- **Builds on:** [Blazorise Removal — Overview](2026-10-28-01-blazorise-removal-overview-plan.md)
- **Branch context:** `development`
- **Implementation order:** step 12 of 18; see [overview](2026-10-28-01-blazorise-removal-overview-plan.md#implementation-order)

## Current state

- `DelayedTooltip` wraps `Blazorise.Tooltip`, which is built on Tippy.js. It has **72 uses in 19 files**.
  Parameters: `Text` (required), `Style`, `Multiline`, `Placement` (Blazorise `TooltipPlacement`,
  default Top). It wraps `ChildContent` in `<div style="display: contents" @onpointerdown>` and
  forces Tippy's wrapper to `display: inline-flex` to avoid the descender-strut gap (see the comment
  in the file).
- On pointer-down it hides the tooltip for 700 ms. That costs two re-renders per tap. It exists so
  touch taps don't leave a tooltip stuck open.
- It puts `aria-label="@Text"` on the Blazorise wrapper. That isn't a reliable description of the
  wrapped control, and it can override the child's own name.
- **Direct `Tooltip` uses:**
  - `ClimateStripe.razor`: **one `Tooltip` per year** (~100–170 stripes per location), each with
    `Placement="Bottom"` and `Style="flex: 1; display: block; height: inherit"`. Each instance is a
    Blazorise component with its own JS interop registration.
  - `ExtremeYears.razor`: two lists of year links with `Multiline="true"`, `Style="display: inline"`.
- `app.css` styles Tippy via `--b-tooltip-*` custom properties and `.tippy-box[data-theme~=blazorise]`
  (white background, primary-green text).
- Multiline tooltips are capped at 15rem wide (relied on by `OverviewField`'s `TooltipMultiline` doc comment).

## Design

**One global tooltip element, driven by event delegation, with no per-instance .NET state or JS interop.**

`DelayedTooltip` becomes pure markup: a wrapper element carrying `data-tooltip` attributes. A single
ES module, `tooltip.js`, loaded once from `App.razor`, listens at the `document` level, finds the
nearest `[data-tooltip]` ancestor of the hovered/focused element, and shows one shared tooltip
element. The tooltip element is a `popover="manual"`, so it renders in the **top layer**: it's never
clipped by `overflow: hidden` ancestors (Collapsible, side panels, chart cards), and it appears
above an open `<dialog>`.

Why this shape:

- **Performance.** ClimateStripe goes from 100+ interop-registered components to 100+ plain `<span>`s.
  The 72 `DelayedTooltip` uses stop re-rendering on pointer-down.
- **Works before interactivity.** The script is global, so tooltips work during SSR prerender and the
  Interactive Auto handoff.
- **Dynamic text for free.** The script reads `data-tooltip` at show time, so text changes in Blazor
  render without extra wiring.

Rejected: CSS-only `::after` tooltips (clipped by overflow ancestors, no viewport flip), and
per-instance interop components (the cost we're removing).

### Shared positioning module: `wwwroot/js/floating.js`

Introduced here, and reused by [05 Dropdown](2026-10-28-05-dropdown-plan.md) and
[13 Autocomplete](2026-10-28-13-autocomplete-plan.md). Served from the site root like `lib/chartjs`.

```js
// placement: "top" | "bottom" | "start" | "end", optionally with "-start"/"-end" alignment
export function place(anchor, floating, { placement = "bottom", offset = 6, padding = 8 } = {}) { … }
// Re-place on scroll (capture) / resize while open; returns a cleanup function
export function autoUpdate(anchor, floating, options) { … }
```

`place` positions the floating element with `position: fixed` (top-layer popovers are fixed by
default), computed from `anchor.getBoundingClientRect()`. If the preferred side lacks room, it flips
to the opposite side; then it shifts along the cross axis to stay `padding` px inside the viewport.
It sets a `data-placement` attribute with the final side, for an optional arrow. About 80 lines.
`App.razor`'s inline `showFloatingTooltip` already has similar clamping for the chart tooltip and is
the model. Leave the chart tooltip alone; it's driven by Chart.js callbacks.

If CSS anchor positioning is Baseline by the time this is implemented, `place` can become a thin
fallback behind `@supports (anchor-name: --a)`. That isn't required.

### `wwwroot/js/tooltip.js`

- Loaded once with `<script type="module" src="@Assets["js/tooltip.js"]"></script>` in `App.razor`
  (modules are deferred by default). It imports `floating.js`.
- Lazily creates `<div id="climate-tooltip" popover="manual" role="tooltip" class="climate-tooltip">`
  on `document.body`. The styles for it go in `app.css`, because it lives outside any component's scope.
- Listeners on `document`, with the matching element found via `closest("[data-tooltip]")`:
  - `pointerover`: if `event.pointerType === "touch"`, do nothing (this replaces the 700 ms C#
    suppression). Otherwise start a **500 ms** show timer. Moving to another anchor while a tooltip
    is showing switches immediately (no second delay).
  - `pointerout` (leaving the anchor): start a **500 ms** hide timer (matches the current
    `HideDelay`). Entering the tooltip doesn't cancel it; tooltips aren't interactive.
  - `focusin` from keyboard (`:focus-visible` matches): show **immediately**. `focusout`: hide.
  - `pointerdown`, `keydown` Escape, `scroll` (capture), `resize`: hide immediately.
- On show: copy `data-tooltip` into the element's `textContent` (never `innerHTML`). Toggle the
  `is-multiline` class from `data-tooltip-multiline`. `showPopover()`, then
  `floating.place(anchor, tip, { placement })`. Skip if the text is empty.
- On show from focus, set `aria-describedby="climate-tooltip"` on the focused element, and remove it
  on hide. A screen reader then reads the tooltip text as the control's description, in addition
  to its own name.
- A `MutationObserver` isn't needed. If the anchor is removed while shown, the next
  pointer/scroll event hides the tooltip, and `place` bails if `anchor.isConnected` is false.

### `DelayedTooltip` component

```razor
<span class="tooltip-anchor @Class" style="@Style"
      data-tooltip="@Text"
      data-tooltip-placement="@PlacementValue"
      data-tooltip-multiline="@(Multiline ? "true" : null)">@ChildContent</span>
```

- Parameters kept as they are: `Text` (`[EditorRequired]`), `Style`, `Multiline`, `Placement`. `Class` is added.
- `TooltipPlacement` becomes our own enum in `Components/Common` (`Top`, `Bottom`, `Start`, `End`).
  Only `DelayedTooltip` and `ClimateStripe` name it. Both drop `@using Blazorise` in this change,
  so there's no collision.
- `.tooltip-anchor { display: inline-flex; }`: the same strut fix as today, documented in the
  `.razor.css` with the existing comment trimmed to fit. Callers' `Style="display: block"` /
  `display: inline-block` continue to override.
- The `aria-label="@Text"` is **removed**. It's replaced by `aria-describedby` on focus (above).
  Controls must already carry their own accessible names (AGENTS.md). Audit point: any control that
  relied on the tooltip's `aria-label` as its only name needs its own `aria-label`. That's likely
  only icon-only buttons, which `ClimateButton`/`DropdownButton` already label.
- The `OnPointerDownAsync` suppression and its two re-renders are removed.

### Direct uses

- `ClimateStripe.razor`: `<Tooltip …>` → `<DelayedTooltip Text="@GetTooltip(record)" Placement="TooltipPlacement.Bottom" Style="flex: 1; display: block; height: inherit">`.
  The stripe `div` keeps its `role="button"` and `aria-label`.
- `ExtremeYears.razor`: `<Tooltip … Multiline="true" Style="display: inline">` → `<DelayedTooltip … Multiline="true" Style="display: inline">`.
  While here: `span.year-link` with `@onclick` is a clickable non-control. Convert it to
  `<button type="button" class="year-link">` (styled as a link) so it's keyboard reachable.

### Styles (`app.css`)

Replace the `--b-tooltip-*` variables and the `.tippy-box[data-theme~=blazorise]` block with:

```css
.climate-tooltip {
    /* top-layer popover: reset UA popover styles */
    margin: 0; inset: auto; border: 1px solid #c4d4d1; border-radius: 8px;
    padding: 0.375rem 0.625rem; background: #fff; color: var(--climate-color-control-text);
    font-size: 0.875rem; line-height: 1.35; box-shadow: var(--climate-shadow-control);
    white-space: nowrap; pointer-events: none; max-width: calc(100vw - 16px);
}
.climate-tooltip.is-multiline { white-space: normal; max-width: 15rem; }
```

Add an optional fade (`opacity` + `@starting-style`), disabled under `prefers-reduced-motion`.

## Risks

- **Behaviour on touch.** Today a long-press may show a Tippy tooltip. After this, touch never shows
  tooltips. That matches the current intent of the pointer-down suppression, and tooltip text
  mustn't be the only place information lives. If a specific tooltip matters on mobile, it needs
  visible text or an info button.
- **Hover over disabled buttons.** Disabled `<button>`s don't fire pointer events in all browsers.
  The wrapper is the `[data-tooltip]` element, and pointer events fire on the wrapper's box, so this
  works as long as the wrapper has a box (`inline-flex`).
- **Top-layer stacking.** A tooltip shown before a dialog opens would sit below it. Hiding on
  `pointerdown` covers the normal path, because opening a dialog starts with a click.

## Manual checks (developer)

- Hover: 500 ms show delay, 500 ms hide, positioned above (or below for ClimateStripe), flips near
  viewport edges, not clipped inside Collapsible, SidePanel, InfoPanel or modals.
- Keyboard: Tab onto a tooltipped button. The tooltip shows immediately and the screen reader reads
  name then description. Escape hides it.
- Touch (device emulation): taps never leave a tooltip open.
- ClimateStripe on a long-record location: hover across stripes switches tooltips smoothly, and
  rendering the dashboard is noticeably cheaper (Performance panel).
- Multiline tooltips wrap at 15rem. `OverviewField` with `TooltipMultiline="false"` (table content) grows to fit.

## Summary

`DelayedTooltip` becomes a `<span>` carrying `data-tooltip` attributes. One global `tooltip.js` shows
a single top-layer popover after the same 500 ms delay, positioned by a new shared `floating.js`
that Dropdown and Autocomplete also use. This removes Tippy, cuts 100+ interop registrations on the
location dashboard, and replaces the misleading `aria-label` with `aria-describedby`.

### Change size

- **Projects:** `ClimateExplorer.Web.Client` (component, wwwroot/js, 2 consumers),
  `ClimateExplorer.Web` (App.razor, app.css).
- Unchanged: the 17 other files that use `DelayedTooltip`; the API stays the same.

| Area | Added | Changed | Removed |
|---|---|---|---|
| `floating.js` | 1 file, +80 | — | — |
| `tooltip.js` | 1 file, +110 | — | — |
| `DelayedTooltip.razor` (+ new `.razor.cs`, `.razor.css`), `TooltipPlacement.cs` | 3 files, +60 | 1 file, ~45 (rewrite) | — |
| `ClimateStripe.razor`, `ExtremeYears.razor` (+ year-link CSS) | — | 3 files, ~15 | — |
| `App.razor` script tag | — | 1 file, ~1 | — |
| `app.css` tooltip styles | — | 1 file, ~20 | ~12 lines (Tippy overrides) |
| **Total** | **5 files, +250** | **6 files, ~81** | **~12 lines** |

Risk: medium. It changes the hover behaviour on every page. The API is unchanged, so call sites don't move.

## Addendum — implementation notes

Shipped as planned, with these deviations:

- **Tooltip text is HTML, not plain text.** The plan said to set `textContent`. But Tippy ran with
  `allowHTML: true`, and many tooltips depend on it: `<br>` (`YearlyValuesHelper`, `AggregationOptions`,
  `Locations`, `LocationDashboard`), `<p>`/`<b>` (`Co2NavTile`, the recent-observations trend tooltips,
  the adjusted-data checkboxes), and a `<table>` with inline styles (`LocationTrendFieldBuilder`).
  `tooltip.js` therefore parses the text into an inert `<template>` and sanitises it before inserting it.
  It keeps an allow-list of tags (`p`, `br`, `b`, `strong`, `i`, `em`, `span`, `div`, lists, table
  parts) and drops every attribute except `style`. Any other element is replaced by its text, and
  comments are removed.
- **No `white-space: nowrap` for single-line tooltips.** Tippy didn't apply it, and several
  non-multiline texts are long sentences. The popover is `fit-content`, capped at
  `calc(100vw - 16px)`, so short text stays on one line and long text wraps instead of overflowing.
  `.climate-tooltip p` gets a 0.5rem bottom margin (none on the last paragraph). Previously Bootstrap's
  1rem margin applied.
- **`ClimateStripe` keeps `@using Blazorise`.** It still uses `Modal`, so it names the enum as
  `Common.TooltipPlacement.Bottom` to avoid the clash with `Blazorise.TooltipPlacement`.
  `ExtremeYears` drops the using.
- **`aria-describedby` is skipped when the focused control's `aria-label` equals the tooltip text**,
  which is common for icon buttons. Otherwise a screen reader would read the name twice. The audit
  found no control relying on the old wrapper `aria-label` for its name.
- **Escape and pointer-down** keep the tooltip hidden until the pointer leaves that anchor. This
  replaces the old 700 ms suppression, and moving between children of the same anchor doesn't
  re-show it.
- `floating.js`'s `place` measures the element at the top-left before positioning it, so the
  viewport's right edge doesn't shrink its `fit-content` width. `autoUpdate` is unused for now and
  is ready for Dropdown/Autocomplete.

### Files

- New: `wwwroot/js/floating.js`, `wwwroot/js/tooltip.js`, `DelayedTooltip.razor.cs`,
  `DelayedTooltip.razor.css`, `TooltipPlacement.cs`.
- Changed: `DelayedTooltip.razor`, `ClimateStripe.razor`, `ExtremeYears.razor` (+ `.razor.css`),
  `App.razor`, `app.css`.

### Follow-ups

- Run the manual checks above. The JS couldn't be run or syntax-checked here: there's no browser
  testing, and Node isn't installed.
