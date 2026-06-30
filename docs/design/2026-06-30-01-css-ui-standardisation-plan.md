# CSS and UI Control Standardisation Plan

- **Date:** 2026-06-30
- **Status:** Proposed
- **Author:** Codex
- **Scope:** `ClimateExplorer.Web/wwwroot/css/app.css`, shared table CSS, Blazor client scoped CSS, layout components, Blazorise usage, CLS and accessible button names in the web UI
- **Builds on:** None
- **Branch context:** `development`

## Summary

This is an investigation and implementation plan only. It does not remove Blazorise, change component behavior, or alter CSS yet.

Current CLS baseline supplied for this work:

- Desktop CLS: `0.052`
- Mobile CLS: `0.114`

The CSS and UI are currently split across global CSS in `ClimateExplorer.Web/wwwroot/css/app.css`, shared table CSS, and many scoped `.razor.css` files in `ClimateExplorer.Web.Client`. The same visual control pattern appears in multiple places with small differences, especially for chart controls, top-100 actions, climate record filters, map controls, and Blazorise dropdown toggles.

The most suspicious CLS area is the location page composition:

- `Index.razor.css` reserves fixed space for `.location-info-container`.
- `MapContainer.razor.css` positions the map absolutely inside `.map-space-reserver` and coordinates with `Index.razor.css`.
- `LocationDashboard` loads stripe and extreme-year content after render.
- `ExtremeYears` is rendered only after `DataRecords` arrives and has no explicit stable height.
- `showOrHideMap` toggles the map container with `display: none/block`, which can affect layout if used outside the already reserved states.

## Phase 1: CSS Inventory and Duplication Review

| Area | Plan |
| --- | --- |
| Files/components to inspect | `ClimateExplorer.Web/wwwroot/css/app.css`, `ClimateExplorer.Web/wwwroot/css/table.css`, `ClimateExplorer.Web.Client/Components/Chart/ChartView.razor.css`, `ClimateExplorer.Web.Client/Components/ClimateRecords/Top100.razor.css`, `ClimateExplorer.Web.Client/Components/ClimateRecords/ClimateRecords.razor.css`, `ClimateExplorer.Web.Client/Components/MapContainer.razor.css`, `ClimateExplorer.Web.Client/Components/Location/LocationDashboard.razor.css`, `ClimateExplorer.Web.Client/Components/DataSetBrowser.razor.css`, `ClimateExplorer.Web.Client/Components/Notifications/*.razor.css`, `ClimateExplorer.Web.Client/Pages/*.razor.css`. |
| Problems to look for | Repeated button declarations: `padding: 8px`, `#f8f8f8` backgrounds, `#425f59` text, `8px` radius, `#cfc` hover, `rgb(0 0 0 / 20%)` shadows, `font: inherit`, icon margin rules, and scoped `::deep` Blazorise overrides. Historical comments like "CLS fix", negative margins, `display: contents` wrappers, and `!important` button overrides need review. |
| Proposed changes | Create a CSS inventory spreadsheet or markdown checklist grouped by primitive: color tokens, typography, spacing, card/panel, table, button, icon button, dropdown toggle, control group, loading/skeleton, layout reservation. Mark each rule as `keep`, `replace with primitive`, `component-specific`, or `delete after verification`. |
| Risks | Removing a duplicated scoped rule can expose CSS isolation details, especially for Blazorise rendered markup. Global rules in `app.css` can unintentionally restyle Bootstrap or Blazorise controls site-wide. |
| Validation steps | Before editing, capture screenshots for home/location, locations table, climate records, recent observations, change-location modal, side panels, chart controls, and top-100 actions at mobile `<=767px`, tablet `768-1024px`, and desktop `>=1025px`. After each cleanup batch, diff screenshots and run Playwright smoke tests. |

## Phase 2: UI Control Inventory, Including Blazorise Usage

| Area | Plan |
| --- | --- |
| Files/components to inspect | Blazorise setup: `ClimateExplorer.Web/App.razor`, `ClimateExplorer.Web/Program.cs`, `ClimateExplorer.Web.Client/Program.cs`, `ClimateExplorer.Web.Client/ClimateExplorer.Web.Client.csproj`. Component usage: `NavMenu`, `MainLayout`, `Index`, `RegionalAndGlobal`, `ChangeLocation`, `DataSetBrowser`, `OverviewField`, `DelayedTooltip`, `DelayedLoadingIndicator`, `PaginationControl`, `MapContainer`, `LocationDashboard`, `ClimateRecords`, `Top100`, `ChartView`, `ChartAxisListView`, `AggregationOptions`, `ChartSeriesView`, `RecentObservationsPanel`, `RecentObservationTabs`, `HeatingScoreInfo`, `ClimateStripeInfo`. |
| Problems to look for | Blazorise is used for layout/navigation (`Bar`), modals, dropdowns, dropdown toggles/items, selects, checks, tables, pagination, tooltips, loading indicators, buttons, links, autocomplete, tabs, and chart components. Several custom classes fight rendered Blazorise markup with `::deep` and `!important`. Package references are version `2.0.3`, while asset links include `v=1.8.8.0` query strings in `App.razor`; confirm whether those query strings are stale cache markers or intentional. |
| Proposed changes | Separate Blazorise usage into three buckets: keep, wrap, and candidate replacement. Keep chart-related Blazorise controls/components in scope of the chart system. Wrap common non-chart controls only where replacement would reduce repeated styling or improve accessibility. Candidate replacement areas include icon-only dropdown toggles, simple buttons, and small link-like buttons. Do not remove Blazorise from chart-related components as part of this work. |
| Risks | Blazorise replacement is not just CSS: it affects DI registration, static assets, keyboard behavior, focus management, modal semantics, dropdown positioning, autocomplete behavior, and table/pagination markup. Removing shared Blazorise packages while chart components still need them would break charts. |
| Validation steps | Produce a rendered-control inventory from Playwright: role/name/class/tag/component source. Verify keyboard navigation for dropdowns, modals, tabs, autocomplete, pagination, and side-panel flows. Keep a manual checklist for controls whose accessible names come from visible text versus `aria-label`. |

## Phase 3: CLS Investigation by Viewport Size

| Area | Plan |
| --- | --- |
| Files/components to inspect | `Index.razor`, `Index.razor.css`, `Index.razor.cs`, `LocationDashboard.razor`, `LocationDashboard.razor.css`, `LocationDashboard.razor.cs`, `ExtremeYears.razor`, `ExtremeYears.razor.css`, `MapContainer.razor`, `MapContainer.razor.css`, `MapContainer.razor.cs`, `ChartView.razor.css`, `SuggestedCharts*.razor/css`, `Collapsible.razor/css`. |
| Problems to look for | Layout shifts caused by async location resolution, `IsMobileDevice` resolution, data set definition load, map initialization, map tile/style load, stripe data arrival, `ExtremeYears` insertion, recent observations support field appearing, chart data/chart canvas resize, suggested chart variants switching, and map hide/show. Review whether `.location-info-container { min-height: 410px; }` masks the dashboard's async expansion rather than reserving the right element size. |
| Proposed changes | Instrument layout shifts with Playwright and `PerformanceObserver` so each shift records value, viewport, URL, DOM source nodes, and screenshots before/after. Test at representative widths: `390`, `767`, `768`, `820`, `1024`, `1025`, `1366`, and `1440`. Replace fixed outer reservations with component-owned stable layout where possible: dashboard skeleton/reserved regions for overview fields, climate stripes, and extreme years; map aspect-ratio or fixed block reservation in the map wrapper; chart canvas/container stable dimensions. |
| Risks | Reducing the tablet `location-info-container` reservation may improve visual spacing but reintroduce shift when async dashboard content arrives. Increasing reservation inside `LocationDashboard` can create excess whitespace for locations without precipitation, record high, or recent observations. Map initialization may still shift if Leaflet CSS or tile images alter intrinsic dimensions after render. |
| Validation steps | Compare CLS per breakpoint against current baseline, with separate runs for `/`, `/location/hobart`, `/location/launceston-airport`, and a location with limited data. Validate collapsed and expanded dashboard states, map expanded/collapsed states, precipitation stripe toggle, and mobile/tablet/desktop boundary at `1024/1025px`. Target direction: desktop below `0.052`, mobile below `0.114`, with no tablet regression. |

## Phase 4: Accessibility Fixes for Unnamed Buttons

| Area | Plan |
| --- | --- |
| Files/components to inspect | `NavMenu.razor`, `LocationDashboard.razor`, `ChartView.razor`, `ChartAxisListView.razor`, `AggregationOptions.razor`, `Top100.razor`, `DataSetBrowser.razor`, `MapContainer.razor`, `ClimateStripe.razor`, `NotificationHost.razor`, `RecentObservationsPanel.razor`, `RecentObservationTile.razor`, `InfoPanel.razor`, `SidePanel.razor`, `ReconnectModal.razor`. |
| Problems to look for | Icon-only native buttons and Blazorise `DropdownToggle` buttons without visible text or `aria-label`: known examples include `button.nav-link.nav-button.info-nav-link`, the `LocationDashboard` cog dropdown, and rendered `.btn.dropdown-toggle` controls. Also note non-button clickable elements such as chart-series action `div`s, the chart info icon, and climate stripe `div role="button"` for a future semantic cleanup pass. |
| Proposed changes | Add explicit names to icon-only controls: `aria-label="Show site overview"` for the desktop info nav button, `aria-label="Location display options"` for the location dashboard cog dropdown, and labels for any icon-only dropdown toggles discovered in rendered DOM. Hide decorative icons with `aria-hidden="true"`. Prefer real `<button type="button">` for clickable icons when touching nearby code. |
| Risks | Blazorise components may forward arbitrary attributes differently by version. A label applied to a Razor component should be verified in the rendered DOM, not assumed. Tooltip text is not a reliable accessible name for all controls. |
| Validation steps | Add or run a Playwright accessibility scan that enumerates all `button`, `[role=button]`, and dropdown toggles and fails when the computed accessible name is empty. Verify with keyboard only: tab order, Enter/Space activation, focus visible, menu open/close, modal close buttons, and side-panel close buttons. |

## Phase 5: CSS Consolidation and Naming Proposal

| Area | Plan |
| --- | --- |
| Files/components to inspect | Start with `app.css`, then migrate scoped usages in `ChartView`, `Top100`, `ClimateRecords`, `LocationDashboard`, `MapContainer`, `DataSetBrowser`, `Notifications`, and selected page CSS. |
| Problems to look for | Class names describe location of use rather than behavior: `.chart-control`, `.top100-btn`, `.top100-download-btn`, `.download-button`, `.today-btn`, `.sort-toggle-btn`, `.map-toggle-button`, `.add-series-button`, `.add-dataset`, `.action-link`, `.record-link`. Different shadows, icon gaps, hover colors, disabled states, and `inline-flex` versus `inline-block` behavior make controls visually inconsistent. |
| Proposed changes | Introduce a small global design layer in `app.css`: CSS custom properties for color, radius, shadow, spacing, and transitions; `.climate-button` as the standard button; `.climate-button--subtle`, `.climate-button--primary`, `.climate-button--danger`, `.climate-button--compact`, `.climate-icon-button`, `.climate-link-button`, `.climate-control-group`, and `.climate-dropdown-toggle` as needed. Prefer `.climate-button` over a shorter prefix because it is readable in Razor markup and matches the product name. Keep component-specific classes for layout only. |
| Risks | A global `.climate-button` can drift into a second design system if existing Bootstrap/Blazorise controls are not clearly assigned to it. Too many modifiers would recreate the current duplication under new names. Button migration can affect wrapping and CLS because inline controls currently have different margins and display modes. |
| Validation steps | Migrate one control family at a time. First prove parity on native buttons, then Blazorise dropdown toggles, then compact/icon/link variants. Use visual regression screenshots, accessible-name audit, and a focused manual pass for hover, focus, disabled, active, wrapping, and touch target size. |

## Phase 6: Safe Staged Implementation Plan

| Stage | Work |
| --- | --- |
| 1. Baseline and inventory | Add a temporary or test-only Playwright helper to capture screenshots and CLS/source-node data across the required breakpoints. Record all duplicated CSS groups and unnamed buttons before changes. |
| 2. Accessibility first | Add missing `aria-label`s to known unnamed buttons and dropdown toggles. This is low visual risk and gives immediate accessibility value. Validate in rendered DOM. |
| 3. Design primitives | Add CSS tokens and the reusable button/control classes to `app.css` without changing existing markup. Keep them unused until screenshots establish a baseline. |
| 4. Native button migration | Move native controls from duplicated classes to `.climate-button` variants: chart download/add controls, top-100 copy, climate records today/sort/download, map toggle, dataset add/link-like controls where appropriate. Leave compatibility aliases temporarily if needed. |
| 5. Blazorise control migration | Standardize dropdown toggles by applying the reusable class directly to `DropdownToggle` where Blazorise forwards it to the rendered button. If not reliable, create one wrapper pattern instead of per-component `::deep` overrides. Keep chart-related Blazorise components in place. |
| 6. CLS fixes | Replace broad fixed-height "CLS fix" rules with component-owned reservation. Investigate dashboard fields, stripe/extreme-year insertion, map wrapper height/aspect ratio, and suggested chart reservation independently. Remove or reduce `.location-info-container` only after source-node data proves the underlying shift is handled. |
| 7. CSS cleanup | Delete compatibility aliases, old duplicated selector groups, stale comments, and dead rules after each migrated area passes screenshots, CLS, accessibility, and smoke tests. |

## Initial Priority Order

1. Accessibility names for icon-only buttons and dropdown toggles.
2. Rendered DOM inventory for Blazorise controls and dropdown toggle classes.
3. CLS source-node instrumentation around the location page.
4. Add design tokens and `.climate-button` primitives.
5. Migrate duplicated native buttons.
6. Migrate Blazorise dropdown styling.
7. Replace or delete obsolete CLS reservations.

## Definition of Done

- Shared control styling lives in a small set of named primitives instead of repeated selector groups.
- `app.css` contains design tokens and reusable primitives; scoped CSS only owns layout or truly component-specific visual behavior.
- The `location-info-container` rule is either removed or documented as still required with measured evidence.
- CLS is measured at mobile, tablet, and desktop breakpoints, with source nodes recorded for remaining shifts.
- All rendered buttons and dropdown toggles have accessible names.
- Blazorise removal/replacement requirements are documented, with chart-related Blazorise controls explicitly retained.
- Existing Playwright smoke tests pass, and new visual/CLS/accessibility checks cover the changed surfaces.
