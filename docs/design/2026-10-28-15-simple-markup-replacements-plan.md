# Simple Markup Replacements — `Accordion`, `Div`, `Field`, `Link`, `Button`

- **Date:** 2026-10-28
- **Status:** Implemented 2026-10-28 (see addendum). Build and unit tests are clean; the UI is unverified.
- **Author:** Claude
- **Scope:** `Pages/About.razor`, `Components/Location/ChangeLocation.razor`, `Components/Chart/AggregationOptions.razor`
  (+ scoped CSS), `ClimateExplorer.Web.UiTests/ChartTests.cs`
- **Builds on:** [Blazorise Removal — Overview](2026-10-28-01-blazorise-removal-overview-plan.md)
- **Branch context:** `development`
- **Implementation order:** step 4 of 18; see [overview](2026-10-28-01-blazorise-removal-overview-plan.md#implementation-order)

## Current state

A handful of Blazorise components with no behaviour worth keeping, each used once or twice:

| Blazorise | Where | Renders | Replacement |
|---|---|---|---|
| `Accordion` (no `AccordionItem`s) | `Pages/About.razor`, wraps the table-of-contents link groups | `div.accordion` | `<nav class="toc-nav" aria-label="About this site contents">`. The TOC is navigation, and this adds a landmark. |
| `Div` (×2) | `ChangeLocation.razor` (button row; error message) | `div` | `div`. The button row's `mt-1 d-flex gap-4` utility classes stay; plan 16 keeps them in `utilities.css`. |
| `Field` | `ChangeLocation.razor`, wraps the autocomplete | `div.mb-3` (+ `mt-3`) | `<div class="mt-3">`, or let plan 13's `Autocomplete` take `Class="mt-3"` and drop the wrapper |
| `Blazorise.Link To="#" Clicked=…` | `ChangeLocation.razor`, the nearby-locations table, one per row | `<a href="#">` acting as a button | `<ClimateButton Variant="ButtonVariant.Link" Text="@location.Name" title="@location.FullTitle" OnClick="…" />`, a real `<button>` styled as a link |
| `Button Color="Color.Primary" Size="Size.Small"` | `AggregationOptions.razor` ("Reset") | `button.btn.btn-primary.btn-sm` | `<ClimateButton Compact Text="Reset" Disabled="…" OnClick="ResetThreshold" />` |

`Strong` is handled with the tables in [11](2026-10-28-11-tables-plan.md), since all 22 uses are inside table headers.

## Notes per change

- **About TOC.** Check `About.razor.css` for `.accordion` selectors (none found; the rules target
  `.toc-panel`, `.toc-items`, `.toc-link`). Bootstrap's `.accordion` sets CSS variables only, so
  visually nothing should change.
- **ChangeLocation link → button.** The `title` attribute is kept (passed through `ClimateButton`'s
  captured attributes) for the full-title hover. `ClimateButton`'s link variant must render
  inline-level, so the table row height doesn't change. Check `climate-link-button` has no
  44px min-height forced on it inside table cells. If it does, add a `climate-table` cell override.
- **UI test.** `ChartTests.cs` clicks `.modal-body .table-card a`. After this change the element is a
  `button.climate-link-button`. Update the selector to `.table-card button.climate-link-button`,
  combined with plan 06's `.climate-modal-body` rename, in whichever change lands last.
- **AggregationOptions Reset.** This was the only Bootstrap-blue (`btn-primary`) button in the app.
  Moving to `ClimateButton` brings it in line with every other button (control background, green
  text, `#cfc` hover). Remove `.btn-primary` from `app.css` in plan 16, once nothing renders it.
- **AggregationOptions info icon.** While in this file: `<i class="fas fa-circle-info info-icon" @onclick=…>`
  is a clickable icon with no accessible name. Make it
  `<ClimateButton Variant="ButtonVariant.Link" Icon="fas fa-circle-info" AriaLabel="About aggregation options" Class="info-icon" OnClick="ShowAggregationOptionsInfo" />`.

None of these tags collide with Common components, so files keep `@using Blazorise` if they still
need other Blazorise components.

## Manual checks (developer)

- About page TOC unchanged visually.
- Change location: nearby location names look like links, are keyboard focusable, and pick the location on Enter/click.
- Aggregation options: Reset matches other buttons and is disabled until options change. The info
  icon opens its panel by click and keyboard.

## Summary

Five Blazorise components that render plain elements are replaced with plain HTML or the existing
`ClimateButton`. The pseudo-link in the nearby-locations table becomes a real button, the TOC
becomes a labelled `nav`, and the clickable info icon gets an accessible name.

### Change size

- **Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.Web.UiTests`.

| Area | Added | Changed | Removed |
|---|---|---|---|
| `About.razor` | — | 1 file, ~3 | — |
| `ChangeLocation.razor` | — | 1 file, ~15 | — |
| `AggregationOptions.razor` (+ `.razor.css`) | — | 2 files, ~8 | — |
| UI test selector | — | 1 file, ~1 | — |
| **Total** | — | **5 files, ~27** | — |

Risk: low.

## Addendum — implementation notes

### What shipped

- **About.** `<Accordion>` is now `<nav class="toc-nav" aria-label="About this site contents">`. No CSS
  targeted `.accordion`. `@using Blazorise` stays because the pipeline `Modal` still needs it.
- **ChangeLocation.** Both `Div`s are plain `div`s. The nearby-location `Blazorise.Link` is a
  `ClimateButton Variant="ButtonVariant.Link"` with `title="@location.FullTitle"` passed through.
  `climate-link-button` has `padding: 0` and no min-height, so no table-cell override was needed.
- **AggregationOptions.** Reset is `<ClimateButton Compact Text="Reset" … />`. The info icon is a
  link-variant `ClimateButton` with `AriaLabel="About aggregation options"` and `Class="info-icon"`. The
  existing `::deep .info-icon` rule (colour `#79c6f4`, `font-size: larger`) still applies and outranks
  the link colour.
- **UI test.** `ChartTests.cs` now clicks `.modal-body .table-card button.climate-link-button`. Plan 06
  still has to rename `.modal-body` to `.climate-modal-body` when it lands.

### Deviations from the plan

- **`Field` became `<div class="mt-3 mb-3">`, not `<div class="mt-3">`.** Blazorise's `Field` renders
  `mb-3`, so keeping it leaves the gap above the nearby-locations table unchanged. Plan 13 can
  fold the wrapper into `Autocomplete` later.
- `.btn-primary` stays in `app.css`. Nothing renders it now, and plan 16 removes it as planned.

### Visible changes to expect

- Reset is now `ClimateButton`-styled: control background, green text, `#cfc` hover, and 44px high instead of
  Bootstrap's ~31px `btn-sm`.

### Verification

- `dotnet build` of `ClimateExplorer.Web`: 0 errors, 0 warnings. A running instance had the local `bin`
  locked, so the build went to a separate artifacts path.
- `ClimateExplorer.UnitTests`: 945 passed, 0 failed.
- The manual checks above are still to do.
