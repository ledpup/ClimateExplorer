# CSS and CLS Standardisation Plan

- **Date:** 2026-06-30
- **Status:** Proposed
- **Author:** Codex
- **Scope:** `ClimateExplorer.Web/wwwroot/css/app.css`, shared table CSS, Blazor client scoped CSS, duplicated visual styles, layout reservation, and CLS on the location page
- **Builds on:** None
- **Branch context:** `development`

## Summary

This plan covers CSS consolidation and CLS only. Blazorise inventory, Blazorise replacement/removal options, dropdown-toggle semantics, and accessible-name fixes have moved to [Blazorise and UI Controls Plan](2026-07-01-01-blazorise-ui-controls-plan.md).

Current CLS baseline supplied for this work:

- Desktop CLS: `0.052`
- Mobile CLS: `0.114`

The CSS is split across global CSS in `ClimateExplorer.Web/wwwroot/css/app.css`, shared table CSS, and many scoped `.razor.css` files in `ClimateExplorer.Web.Client`. The same visual control pattern appears in multiple places with small differences, especially chart controls, top-100 actions, climate record filters, map controls, dataset buttons, and link-like buttons.

The most suspicious CLS area is the location page composition:

- `Index.razor.css` reserves fixed space for `.location-info-container`.
- `MapContainer.razor.css` positions the map absolutely inside `.map-space-reserver` and coordinates with `Index.razor.css`.
- `LocationDashboard` loads stripe and extreme-year content after render.
- `ExtremeYears` is rendered only after `DataRecords` arrives and has no explicit stable height.
- `showOrHideMap` toggles the map container with `display: none/block`, which can affect layout if used outside already reserved states.

## Phase 1: CSS Inventory and Duplication Review

| Area | Plan |
| --- | --- |
| Files/components to inspect | `ClimateExplorer.Web/wwwroot/css/app.css`, `ClimateExplorer.Web/wwwroot/css/table.css`, `ClimateExplorer.Web.Client/Components/Chart/ChartView.razor.css`, `ClimateExplorer.Web.Client/Components/ClimateRecords/Top100.razor.css`, `ClimateExplorer.Web.Client/Components/ClimateRecords/ClimateRecords.razor.css`, `ClimateExplorer.Web.Client/Components/MapContainer.razor.css`, `ClimateExplorer.Web.Client/Components/Location/LocationDashboard.razor.css`, `ClimateExplorer.Web.Client/Components/DataSetBrowser.razor.css`, `ClimateExplorer.Web.Client/Components/Notifications/*.razor.css`, `ClimateExplorer.Web.Client/Pages/*.razor.css`. |
| Problems to look for | Repeated declarations: `padding: 8px`, `#f8f8f8` backgrounds, `#425f59` text, `8px` radius, `#cfc` hover, `rgb(0 0 0 / 20%)` shadows, `font: inherit`, icon margin rules, negative margins, `display: contents` wrappers, `!important` overrides, and historical comments like "CLS fix". |
| Proposed changes | Create a markdown checklist grouped by primitive: color tokens, typography, spacing, card/panel, table, button, icon button, link button, control group, loading/skeleton, and layout reservation. Mark each rule as `keep`, `replace with primitive`, `component-specific`, or `delete after verification`. |
| Risks | A duplicated rule may be compensating for CSS isolation or third-party rendered markup. Global rules in `app.css` can unintentionally restyle Bootstrap or component-library controls site-wide. |
| Validation steps | Before editing, capture screenshots for home/location, locations table, climate records, recent observations, change-location modal, side panels, chart controls, and top-100 actions at mobile `<=767px`, tablet `768-1024px`, and desktop `>=1025px`. After each cleanup batch, diff screenshots and run Playwright smoke tests. |

## Phase 2: CSS Consolidation and Naming Proposal

| Area | Plan |
| --- | --- |
| Files/components to inspect | Start with `app.css` and `ClimateExplorer.Web.Client/Components/Common/Control`, then migrate scoped usages in `ChartView`, `Top100`, `ClimateRecords`, `LocationDashboard`, `MapContainer`, `DataSetBrowser`, `Notifications`, and selected page CSS. |
| Problems to look for | Class names describe location of use rather than behavior: `.chart-control`, `.top100-btn`, `.top100-download-btn`, `.download-button`, `.today-btn`, `.sort-toggle-btn`, `.map-toggle-button`, `.add-series-button`, `.add-dataset`, `.action-link`, `.record-link`. Different shadows, icon gaps, hover colors, disabled states, and `inline-flex` versus `inline-block` behavior make controls visually inconsistent. |
| Proposed changes | Keep `app.css` as the visual token and primitive layer, but move repeated markup into small Blazor components under `Components/Common/Control`: `ClimateButton`, `ClimateDropdownButton`, and `ClimateLinkButton`. Components own the consistent button type, icon rendering, class composition, click callback, disabled state where relevant, and accessible-name parameters. Component-specific classes should remain layout/modifier hooks only. |
| Risks | A component layer can become too broad if every one-off control variant becomes a parameter. Keep the components small and let child content or local wrapper CSS handle unusual cases. Button migration can affect wrapping and CLS because inline controls currently have different margins and display modes. Blazorise dropdown toggles still depend on CSS specificity against Bootstrap/Blazorise button styles. |
| Validation steps | Migrate one visual family at a time. First prove parity on native buttons, dropdown buttons, and link-like buttons, then compact/icon variants. Use visual regression screenshots and a focused manual pass for hover, focus, disabled, active, wrapping, keyboard focus, accessible names, and touch target size. |

## Phase 3: CLS Investigation by Viewport Size

| Area | Plan |
| --- | --- |
| Files/components to inspect | `Index.razor`, `Index.razor.css`, `Index.razor.cs`, `LocationDashboard.razor`, `LocationDashboard.razor.css`, `LocationDashboard.razor.cs`, `ExtremeYears.razor`, `ExtremeYears.razor.css`, `MapContainer.razor`, `MapContainer.razor.css`, `MapContainer.razor.cs`, `ChartView.razor.css`, `SuggestedCharts*.razor/css`, `Collapsible.razor/css`. |
| Problems to look for | Layout shifts caused by async location resolution, `IsMobileDevice` resolution, data set definition load, map initialization, map tile/style load, stripe data arrival, `ExtremeYears` insertion, recent observations support field appearing, chart data/chart canvas resize, suggested chart variants switching, and map hide/show. Check that component-owned dashboard reservations are enough now that the broad `.location-info-container` height reservation has been removed. |
| Proposed changes | Instrument layout shifts with Playwright and `PerformanceObserver` so each shift records value, viewport, URL, DOM source nodes, and screenshots before/after. Test widths: `390`, `767`, `768`, `820`, `1024`, `1025`, `1366`, and `1440`. Replace fixed outer reservations with component-owned stable layout where possible: dashboard skeleton/reserved regions for overview fields, climate stripes, and extreme years; map aspect-ratio or fixed block reservation in the map wrapper; chart canvas/container stable dimensions. |
| Risks | Reducing the tablet `location-info-container` reservation may improve visual spacing but reintroduce shift when async dashboard content arrives. Increasing reservation inside `LocationDashboard` can create excess whitespace for locations without precipitation, record high, or recent observations. Map initialization may still shift if Leaflet CSS or tile images alter intrinsic dimensions after render. |
| Validation steps | Compare CLS per breakpoint against current baseline, with separate runs for `/`, `/location/hobart`, `/location/launceston-airport`, and a location with limited data. Validate collapsed and expanded dashboard states, map expanded/collapsed states, precipitation stripe toggle, and mobile/tablet/desktop boundary at `1024/1025px`. Target direction: desktop below `0.052`, mobile below `0.114`, with no tablet regression. |

## Phase 4: Layout Reservation Fixes

| Area | Plan |
| --- | --- |
| Files/components to inspect | `Index.razor.css`, `LocationDashboard.razor`, `LocationDashboard.razor.css`, `ExtremeYears.razor.css`, `MapContainer.razor.css`, `ChartView.razor.css`, `SuggestedCharts*.razor.css`. |
| Problems to look for | Fixed reservations on parent containers instead of the async child that grows; breakpoint-specific heights that do not match actual content; `display: none/block` map toggles; absolutely positioned map content that depends on wrapper height; chart and suggested-preset placeholders that assume one data shape. |
| Proposed changes | Move space reservation closer to the component that owns the async content. Add stable min-height or skeleton regions for dashboard summary fields, stripe rows, and extreme-year rows. Make the map wrapper own its collapsed height/aspect ratio across viewport bands. Treat `.location-info-container` as a temporary compatibility rule to remove only after source-node measurements prove the child components reserve their own space. |
| Risks | Component-owned reservations may need conditional variants for collapsed dashboard, expanded dashboard, precipitation enabled, and locations with missing records. Too much reservation can feel like a blank hole even if CLS improves. |
| Validation steps | Record before/after screenshots immediately after SSR/prerender, after hydration, after data load, after map ready, and after chart render. Confirm no overlap between map, dashboard, and chart at `767/768` and `1024/1025` boundaries. |

## Phase 5: Safe Staged CSS Cleanup

| Stage | Work |
| --- | --- |
| 1. Baseline and inventory | Add a temporary or test-only Playwright helper to capture screenshots and CLS/source-node data across the required breakpoints. Record duplicated CSS groups before changes. |
| 2. Design primitives | Add CSS tokens and reusable button/control classes to `app.css` without changing existing markup. Keep them unused until screenshots establish a baseline. |
| 3. Native visual migration | Move repeated native controls to `Components/Common/Control` components: chart download/add controls, top-100 copy/download controls, climate records today/sort/download, change-location actions, map toggle, dataset add/link-like controls where appropriate. Leave compatibility aliases temporarily if needed. |
| 4. CLS fixes | Continue replacing broad fixed-height rules with component-owned reservation. Investigate dashboard fields, stripe/extreme-year insertion, map wrapper height/aspect ratio, and suggested chart reservation independently. Reintroduce an outer `.location-info-container` height only if measured source-node data proves component-owned reservations are insufficient. |
| 5. Cleanup | Delete compatibility aliases, old duplicated selector groups, stale comments, and dead rules after each migrated area passes screenshots, CLS checks, and smoke tests. |

## Initial Priority Order

1. CLS source-node instrumentation around the location page.
2. CSS inventory and duplicated selector checklist.
3. Add design tokens and `.climate-button` primitives.
4. Migrate duplicated native visual styles.
5. Replace or document obsolete CLS reservations.

## Definition of Done

- Shared visual styling lives in a small set of named CSS primitives instead of repeated selector groups.
- `app.css` contains design tokens and reusable primitives; scoped CSS only owns layout or truly component-specific visual behavior.
- The `location-info-container` rule is either removed or documented as still required with measured evidence.
- CLS is measured at mobile, tablet, and desktop breakpoints, with source nodes recorded for remaining shifts.
- Existing Playwright smoke tests pass, and visual/CLS checks cover the changed surfaces.

## Addendum - implementation notes

### 2026-07-01 initial CSS primitive slice

- Added global CSS tokens and the first reusable control primitives in `app.css`: `.climate-button`, `.climate-button--compact`, and `.climate-link-button`.
- Migrated native button/link-button markup for chart controls, climate records controls, the top-100 copy action, location-dashboard change-location action, dataset add buttons, record-high links, and home-overview action links.
- Trimmed component-scoped CSS so migrated feature classes now keep local layout differences instead of full duplicate button skins.
- Removed the now-empty `HomeOverviewInfo.razor.css`.
- Deferred Blazorise dropdown styling, accessible-name work, browser screenshots, and CLS measurement to their separate plans/validation passes.

### 2026-07-01 feedback follow-up

- Fixed the chart aggregation toggle regression by applying `.climate-button` to the native `AggregationOptions` button.
- Restored the `LocationDashboard` options cog alignment by styling the scoped Blazorise dropdown toggle from the local wrapper and adding an accessible label.
- Verified with `dotnet build`; visual verification remains manual because the repository instructions prohibit running the website, Playwright, Lighthouse, or browser tests in this pass.

### 2026-07-01 Phase 2 dropdown primitive slice

- Added `.climate-dropdown-toggle` to `app.css` for Blazorise dropdown toggle buttons that should match the shared ClimateExplorer control surface.
- Migrated the chart grouping dropdown, chart axes dropdown, top-100 download dropdown, and location-dashboard options cog to the shared dropdown primitive.
- Removed repeated dropdown button surface styling from `ChartView.razor.css`, `Top100.razor.css`, and `LocationDashboard.razor.css`; those scoped files now keep local layout/menu rules only.
- Verified with `dotnet build`; browser visual checks remain deferred by repository instruction.

### 2026-07-01 Phase 2 component pivot

- Pivoted Phase 2 from direct shared CSS classes in feature markup to reusable controls in `ClimateExplorer.Web.Client/Components/Common/Control`.
- Added `ClimateButton`, `ClimateDropdownButton`, and `ClimateLinkButton`; the components compose the shared CSS primitives while centralising icon rendering, default `button` type, callback wiring, disabled state where relevant, and accessible-name parameters.
- Migrated the highest-repeat controls first: chart add/filter/download/aggregation controls, chart grouping and axes dropdowns, top-100 copy/download actions, climate-records today/sort/download actions, location-dashboard change-location/options controls, change-location modal near-me/random actions, map expand/collapse, dataset add actions, location record-high links, and home overview action links.
- Kept `app.css` as the visual source of truth and left scoped CSS classes as layout/modifier hooks.
- Verified with `dotnet build`; browser visual checks remain deferred by repository instruction.

### 2026-07-01 Phase 3 code-level reservation slice

- Began Phase 3 without browser/Playwright instrumentation because repository instructions prohibit running the website, Playwright, Lighthouse, or browser tests.
- Added component-owned reserved regions inside `LocationDashboard` for the async overview field row and the temperature stripe/extreme-year block.
- Removed the broader `location-info-container` height reservations after manual visual feedback confirmed the page still looks good with component-owned dashboard reservations.
- Cleaned stale `CLS fix` wording in `Index.razor.css`; suggested-chart placeholder reservations remain but are now documented as loading reservations rather than permanent CLS workarounds.
- Migrated the remaining shared-surface buttons in `RecentObservationsPanel` to `ClimateButton`.
- Fixed mobile chart-control alignment so the chart download button left-aligns with the other controls on mobile.
- Strengthened `.climate-dropdown-toggle` shadow styling so Blazorise/Bootstrap button rules do not hide the shared control shadow.
- Deferred measured CLS attribution and breakpoint screenshots to a manual/browser-enabled validation pass.
