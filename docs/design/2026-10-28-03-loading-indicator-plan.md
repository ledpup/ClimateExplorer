# Loading Indicator — Standardised `LoadingIndicator`

- **Date:** 2026-10-28
- **Status:** Proposed
- **Author:** Claude
- **Scope:** `Components/Common/DelayedLoadingIndicator.*` (replaced), new `Components/Common/LoadingIndicator.*`,
  7 overlay call sites, 2 inline spinner call sites, `App.razor` (Blazorise LoadingIndicator CSS link)
- **Builds on:** [Blazorise Removal — Overview](2026-10-28-01-blazorise-removal-overview-plan.md)
- **Branch context:** `development`
- **Implementation order:** step 8 of 18; see [overview](2026-10-28-01-blazorise-removal-overview-plan.md#implementation-order)

## Current state

Two different loading treatments are in use:

**Overlay**, via `DelayedLoadingIndicator`, which wraps `Blazorise.LoadingIndicator`. It waits 1 s
before showing (to avoid flicker), then covers its child content with a translucent overlay and a spinner.

| Call site | Wraps | Notes |
|---|---|---|
| `Components/Chart/ChartView.razor` | whole chart area | `@bind-Visible` |
| `Components/ClimateRecords/ClimateRecords.razor` | records table | `@bind-Visible` |
| `Components/ClimateRecords/Top100.razor` | Top 100 chart | `@bind-Visible` |
| `Components/Location/ChangeLocation.razor` | nearby-locations table | `@bind-Visible`; file also has `@using Blazorise.LoadingIndicator` |
| `Components/Location/LocationDashboard.razor` (×2) | overview fields; climate stripe | `ZIndex="5"` |
| `Pages/Locations.razor` | a placeholder `div` | `Visible="true"` |

**Inline** (spinner + text, shown *instead of* content), hand-rolled with Font Awesome:

| Call site | Markup |
|---|---|
| `Components/RecentObservations/RecentObservationsPanel.razor` | `<i class="fas fa-spinner fa-spin"></i> Loading recent observations...` (icon not `aria-hidden`) |
| `Components/Location/LocationDataSetMetadataSidePanel.razor` | `<i class="fas fa-spinner fa-spin" aria-hidden="true"></i> Loading data sources...` inside an `aria-live="polite"` region |

So the spinner glyph, size, colour, text style and delay all differ. Nothing announces loading to
screen readers in the overlay case. `VisibleChanged` is never raised by the wrapper, so the six
`@bind-Visible` uses are effectively one-way.

## Design

One component, `LoadingIndicator`, with two variants that share a spinner, colour and text style.

**When to use which** (to be added to AGENTS.md's common-components notes):

- **Overlay**: existing content is being refreshed and should stay visible underneath (chart,
  tables, dashboard fields).
- **Inline**: there's no content to show yet (first load of a panel or list). The indicator sits
  where the content will go.

### API

```razor
@* Overlay (default): wraps content *@
<LoadingIndicator Visible="@ChartLoadingIndicatorVisible" Text="Loading chart…">
    ...content...
</LoadingIndicator>

@* Inline: no child content *@
<LoadingIndicator Variant="LoadingIndicatorVariant.Inline" Visible="@CurrentState.IsLoading" Text="Loading recent observations…" />
```

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Visible` | `bool` | `false` | One-way. No `VisibleChanged`; call sites drop `@bind-`. |
| `Variant` | `LoadingIndicatorVariant` (`Overlay`, `Inline`) | `Overlay` | |
| `Text` | `string?` | `null` | Visible text next to the spinner. If null, a visually hidden "Loading…" is still rendered for screen readers. |
| `Delay` | `TimeSpan` | 1 s | Same for both variants. `TimeSpan.Zero` shows immediately. |
| `ZIndex` | `int?` | `null` | Overlay only (LocationDashboard needs 5). |
| `ChildContent` | `RenderFragment?` | | Overlay only. Inline renders `ChildContent` when not visible, so a call site can wrap content and let the component swap it in. |
| `Class` | `string?` | | |

**Delay for Inline.** Using the same 1 s delay for both standardises the behaviour and stops flicker
on fast loads. During the delay the inline slot is empty. Where an empty slot causes layout shift,
give the slot a `min-height` in the consumer's CSS, as `ChartView.razor.css` already does for series
cards. RecentObservations currently shows its text immediately; with the delay, fast loads show
nothing rather than a flash of text.

### Look

A CSS-only ring spinner (no Font Awesome dependency, no image):

- `border: 3px solid` a translucent primary green, with `border-top-color: #425f59`,
  `border-radius: 50%`, `animation: spin 0.8s linear infinite`.
- Overlay: 2.5rem ring centred over the content. Background `rgb(255 255 255 / 60%)`. The container
  is `position: relative` and the overlay `position: absolute; inset: 0`. Optional `Text` sits under the ring.
- Inline: 1rem ring, `display: inline-flex; gap: 0.5rem; align-items: center`. Text in
  `--climate-color-muted-text` at `0.875rem` (above the 0.75rem minimum).
- `@media (prefers-reduced-motion: reduce)`: replace the spin with a slow opacity pulse.

### Accessibility

- The indicator element has `role="status"` (implicit `aria-live="polite"`) and contains the text
  (visible or visually hidden). It's always rendered, empty while not visible, so the live region
  exists before its content changes. That's needed for reliable announcements.
- Overlay sets `aria-busy="true"` on the wrapper while visible, and the overlay intercepts pointer events.
- `LocationDataSetMetadataSidePanel`'s outer `aria-live="polite"` would double-announce. Remove it
  from the loading branch (keep it for the error/result branches if still wanted).

### Implementation

- Move the timer logic from `DelayedLoadingIndicator.razor.cs` unchanged: cancellation token, the
  `disposed` guard, and swallowing render exceptions during the Interactive Auto handoff. Make the
  1 s constant the `Delay` parameter.
- `LoadingIndicator.razor` renders, for Overlay:
  ```razor
  <div class="loading-host @Class" aria-busy="@(showIndicator ? "true" : null)">
      @ChildContent
      <div class="loading-overlay @(showIndicator ? "is-visible" : null)" style="@ZIndexStyle" role="status">
          @if (showIndicator) { <span class="loading-spinner" aria-hidden="true"></span><span class="@TextClass">@EffectiveText</span> }
      </div>
  </div>
  ```
  For Inline:
  ```razor
  <span class="loading-inline @Class" role="status">
      @if (showIndicator) { <span class="loading-spinner loading-spinner--small" aria-hidden="true"></span><span>@EffectiveText</span> }
  </span>
  @if (!Visible) { @ChildContent }
  ```
- Delete `DelayedLoadingIndicator.razor` and `.razor.cs`. There's no name collision:
  `Blazorise.LoadingIndicator` is in its own namespace, imported only by the old wrapper and
  `ChangeLocation` (remove that `@using`).
- Remove the `blazorise.loadingindicator.css` link from `App.razor`. Check that no scoped CSS targets
  Blazorise's loading-indicator classes (`.b-loading-indicator*`); none were found in the inventory.
- Update the AGENTS.md common-component list: `DelayedLoadingIndicator` → `LoadingIndicator`, with the
  Overlay/Inline guideline above.

## Call-site changes

| File | Change |
|---|---|
| `ChartView.razor` | `<DelayedLoadingIndicator @bind-Visible=…>` → `<LoadingIndicator Visible=…>` |
| `ClimateRecords.razor` | same |
| `Top100.razor` | same |
| `ChangeLocation.razor` | same; remove `@using Blazorise.LoadingIndicator` |
| `LocationDashboard.razor` (×2) | same; keep `ZIndex="5"` |
| `Locations.razor` | same |
| `RecentObservationsPanel.razor` | replace the `fa-spinner` block with `<LoadingIndicator Variant="Inline" Visible="@CurrentState.IsLoading" Text="Loading recent observations…" />` inside the existing `recent-observations-state` wrapper |
| `LocationDataSetMetadataSidePanel.razor` | same pattern, `Text="Loading data sources…"` |

## Unit tests

There's no bUnit in the repo. The timing logic is unchanged and stays as it is. No new tests, unless
the delay decision is pulled into a pure helper, which isn't worth it here.

## Manual checks (developer)

- On a slow connection (DevTools throttling), the chart, records table, Top 100, change-location
  table and dashboard fields show the overlay after ~1 s, and not at all on fast loads.
- Recent observations and the data-sources side panel show the inline indicator with the new
  spinner and the same text style.
- Screen reader announces "Loading …" once.
- Reduced-motion setting replaces the spin.

## Summary

`DelayedLoadingIndicator` and the two hand-rolled spinners become one `LoadingIndicator` with
Overlay and Inline variants. They share a CSS spinner, a 1 s delay and `role="status"`
announcements, which removes the `Blazorise.LoadingIndicator` package dependency and its stylesheet.

### Change size

- **Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.Web` (App.razor), `AGENTS.md`.

| Area | Added | Changed | Removed |
|---|---|---|---|
| `LoadingIndicator.razor` / `.razor.cs` / `.razor.css` + `LoadingIndicatorVariant.cs` | 4 files, +190 | — | — |
| `DelayedLoadingIndicator.*` | — | — | 2 files, −90 (logic moves into the new file) |
| Overlay call sites | — | 6 files, ~14 | — |
| Inline call sites (+ their scoped CSS) | — | 2–4 files, ~15 | — |
| `App.razor` CSS link | — | 1 file, ~1 | — |
| AGENTS.md | — | 1 file, ~4 | — |
| **Total** | **4 files, +190** | **~12 files, ~34** | **2 files, −90** |

About 70 of the added lines are timer logic moved from `DelayedLoadingIndicator`. Risk: low.
