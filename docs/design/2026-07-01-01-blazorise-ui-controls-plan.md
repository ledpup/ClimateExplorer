# Blazorise and UI Controls Plan

- **Date:** 2026-07-01
- **Status:** Proposed
- **Author:** Codex
- **Scope:** Blazorise usage inventory, UI control semantics, accessible button names, and future non-chart Blazorise replacement/removal analysis
- **Builds on:** [CSS and CLS Standardisation Plan](2026-06-30-01-css-ui-standardisation-plan.md)
- **Branch context:** `development`

## Summary

This plan covers Blazorise and UI control semantics only. CSS visual consolidation and CLS fixes live in [CSS and CLS Standardisation Plan](2026-06-30-01-css-ui-standardisation-plan.md).

Blazorise is used for layout/navigation (`Bar`), modals, dropdowns, dropdown toggles/items, selects, checks, tables, pagination, tooltips, loading indicators, buttons, links, autocomplete, tabs, and chart components. Chart-related Blazorise controls are explicitly out of scope for removal in this plan.

Known accessibility concerns include icon-only native buttons and Blazorise-rendered dropdown buttons without visible text or an accessible name. Examples include the desktop site-overview nav button, the `LocationDashboard` cog dropdown, and rendered `.btn.dropdown-toggle` controls.

## Phase 1: Blazorise Usage Inventory

| Area | Plan |
| --- | --- |
| Files/components to inspect | Setup: `ClimateExplorer.Web/App.razor`, `ClimateExplorer.Web/Program.cs`, `ClimateExplorer.Web.Client/Program.cs`, `ClimateExplorer.Web.Client/ClimateExplorer.Web.Client.csproj`. Usage: `NavMenu`, `MainLayout`, `Index`, `RegionalAndGlobal`, `ChangeLocation`, `DataSetBrowser`, `OverviewField`, `DelayedTooltip`, `DelayedLoadingIndicator`, `PaginationControl`, `MapContainer`, `LocationDashboard`, `ClimateRecords`, `Top100`, `ChartView`, `ChartAxisListView`, `AggregationOptions`, `ChartSeriesView`, `RecentObservationsPanel`, `RecentObservationTabs`, `HeatingScoreInfo`, `ClimateStripeInfo`. |
| Problems to look for | Controls whose rendered markup differs from the Razor source; components that forward arbitrary attributes inconsistently; controls styled with component-specific `::deep` selectors; Blazorise package references at version `2.0.3` while `App.razor` asset links include `v=1.8.8.0` query strings. |
| Proposed changes | Produce a rendered-control inventory grouped by Blazorise component type: keep, wrap, candidate replacement, and chart-owned. Confirm whether stale asset query strings are intentional cache markers or should be updated separately. |
| Risks | Blazorise replacement affects DI registration, static assets, keyboard behavior, focus management, modal semantics, dropdown positioning, autocomplete behavior, and table/pagination markup. |
| Validation steps | Use Playwright to record tag, role, accessible name, classes, component source, and keyboard behavior for each rendered control family. |

## Phase 2: UI Control Semantics and Accessible-Name Audit

| Area | Plan |
| --- | --- |
| Files/components to inspect | `NavMenu.razor`, `LocationDashboard.razor`, `ChartView.razor`, `ChartAxisListView.razor`, `AggregationOptions.razor`, `Top100.razor`, `DataSetBrowser.razor`, `MapContainer.razor`, `ClimateStripe.razor`, `NotificationHost.razor`, `RecentObservationsPanel.razor`, `RecentObservationTile.razor`, `InfoPanel.razor`, `SidePanel.razor`, `ReconnectModal.razor`. |
| Problems to look for | Icon-only buttons without `aria-label`; Blazorise `DropdownToggle` buttons without visible text or accessible name; clickable `div`/`span` elements used as controls; decorative icons exposed to assistive tech; tooltip text being treated as if it were a reliable accessible name. |
| Proposed changes | Add explicit names to icon-only controls: `aria-label="Show site overview"` for the desktop info nav button, `aria-label="Location display options"` for the location dashboard cog dropdown, and labels for any icon-only dropdown toggles discovered in rendered DOM. Hide decorative icons with `aria-hidden="true"`. Prefer real `<button type="button">` for clickable icons when touching nearby code. |
| Risks | A label applied to a Razor component must be verified in the rendered DOM because Blazorise may not forward all attributes as expected. Changing clickable `div`s to buttons can affect CSS and event propagation. |
| Validation steps | Add or run a Playwright accessibility scan that enumerates all `button`, `[role=button]`, and dropdown toggles and fails when the computed accessible name is empty. Verify keyboard-only tab order, Enter/Space activation, visible focus, menu open/close, modal close buttons, and side-panel close buttons. |

## Phase 3: Dropdown, Modal, Select, Table, Pagination, Tooltip, and Loading Analysis

| Area | Plan |
| --- | --- |
| Files/components to inspect | Dropdowns in `NavMenu`, `LocationDashboard`, `ChartView`, `ChartAxisListView`, `Top100`; modals in `Index`, `RegionalAndGlobal`, `ChangeLocation`, `OverviewField`; selects/checks in chart and climate record components; tables and pagination in `ClimateRecords`, `ChangeLocation`, and `Locations`; tooltip/loading wrappers in common components. |
| Problems to look for | Where Blazorise provides meaningful behavior versus where it is used for simple markup; focus trapping and escape handling in modals; dropdown positioning and outside-click behavior; select/check accessibility; table semantics; loading overlay behavior. |
| Proposed changes | Document the minimum behavior any replacement must preserve. Prefer wrappers around current Blazorise controls before removal, so accessibility and styling can be improved without taking on modal/dropdown/autocomplete rewrites immediately. |
| Risks | Replacing controls that currently manage keyboard and focus behavior may regress accessibility even if visual output is unchanged. |
| Validation steps | For each control family, create a pass/fail checklist: mouse behavior, keyboard behavior, focus return, screen-reader name, disabled state, validation state, and responsive layout. |

## Phase 4: Non-Chart Candidate Replacement Plan

| Area | Plan |
| --- | --- |
| Files/components to inspect | `NavMenu`, `LocationDashboard`, `Top100`, `ClimateRecords`, `ChangeLocation`, `DataSetBrowser`, common panel components, and recent observations controls. |
| Problems to look for | Simple controls where Blazorise creates extra markup or awkward attribute forwarding; icon-only toggles that can be native buttons; link-like buttons that do not need component-library behavior. |
| Proposed changes | Candidate replacements should start with low-risk native controls: icon buttons, link buttons, simple dropdown toggles with explicit accessible names, and simple action buttons. Keep complex controls such as modal, autocomplete, pagination, and data tables until the inventory proves replacement is worth the risk. |
| Risks | Removing Blazorise piecemeal can produce inconsistent markup and styling unless shared wrappers or clear conventions exist first. |
| Validation steps | Replace one candidate family at a time and run accessibility, keyboard, and screenshot checks before moving to the next family. |

## Phase 5: Chart-Related Blazorise Retention Boundaries

| Area | Plan |
| --- | --- |
| Files/components to inspect | `ChartView`, `ChartAxisListView`, `AggregationOptions`, `ChartSeriesView`, chart info components, `ClimateStripeInfo`, and `UiLogic/ChartLogic.cs`. |
| Problems to look for | Accidental replacement of Blazorise chart components or controls that are tightly coupled to chart rendering, options, or chart state. |
| Proposed changes | Treat chart-related Blazorise controls as retained unless a later chart-specific plan says otherwise. Accessibility fixes may still apply, but removal/replacement does not. |
| Risks | Chart components rely on Blazorise chart lifecycle and JS interop; changing them during UI-control cleanup could reintroduce chart initialization regressions. |
| Validation steps | Existing chart Playwright tests must pass after any accessibility-only changes in chart-adjacent controls. |

## Phase 6: Safe Staged Implementation Plan

| Stage | Work |
| --- | --- |
| 1. Inventory | Generate the rendered-control inventory and classify Blazorise uses as keep, wrap, candidate replacement, or chart-owned. |
| 2. Accessible names | Fix missing accessible names and decorative icon exposure first. This is low visual risk and high value. |
| 3. Wrappers | Introduce wrapper conventions only where they reduce repeated Blazorise-specific attribute or naming work. |
| 4. Candidate replacements | Replace simple non-chart controls one family at a time. Leave modal, autocomplete, pagination, table, and chart controls in place unless explicitly approved later. |
| 5. Cleanup | Remove unused Blazorise-specific classes or imports only after no rendered usage remains and chart-related dependencies are preserved. |

## Definition of Done

- Blazorise usage is inventoried by rendered component family and classified by future action.
- All rendered buttons and dropdown toggles have accessible names.
- Decorative icons inside controls are hidden from assistive tech.
- Non-button clickable elements are documented for semantic cleanup or converted where low risk.
- Chart-related Blazorise controls are explicitly retained.
- Any non-chart replacement preserves keyboard, focus, disabled, and responsive behavior.
