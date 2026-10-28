# Blazorise Removal — Overview Plan

- **Date:** 2026-10-28
- **Status:** Proposed
- **Author:** Claude
- **Scope:** All remaining Blazorise components in `ClimateExplorer.Web.Client`, Bootstrap 5 (CSS and JS),
  Font Awesome and Open Iconic sourcing, `ClimateExplorer.Web/App.razor`, both `Program.cs` files
- **Builds on:** [Blazorise and UI Controls Plan](2026-07-01-01-blazorise-ui-controls-plan.md),
  [CSS and CLS Standardisation Plan](2026-06-30-01-css-ui-standardisation-plan.md)
- **Branch context:** `development`

## Goal

Replace every remaining Blazorise component with a small native HTML/CSS component in
`Components/Common` (or with plain markup where a component adds nothing), then remove the Blazorise
packages, the Blazorise CSS, and Bootstrap. Charts were already moved to the custom Chart.js wrapper
(`ChartJsChart`); this plan covers everything else.

This overview is the index. It holds the inventory, the cross-cutting decisions and the order of
work. Each component family has its own plan doc (listed under [Component plans](#component-plans)).

## Why

- **Licensing (primary reason).** Both `Program.cs` files carry a Blazorise product token, and the
  WASM payload includes `Blazorise.Licensing`. Removing the packages removes the licensed dependency
  and its renewals. The remaining third-party UI assets are MIT/OFL/CC-BY (Chart.js, Font Awesome
  Free, Leaflet).
- **Page weight and render-blocking CSS.** Four render-blocking stylesheets come from Blazorise or
  Bootstrap. Bootstrap's JS bundle is also loaded (deferred), but the app never uses it. Measured sizes:

  | Asset | When it costs | Raw | Gzipped |
  |---|---|---|---|
  | `bootstrap.min.css` (jsdelivr, cross-origin) | Render-blocking, every first visit | 233 KB | 31 KB |
  | `blazorise.bootstrap5.css` (unminified) | Render-blocking | 105 KB | 12 KB |
  | `blazorise.css` (unminified) | Render-blocking | 72 KB | 10 KB |
  | `blazorise.loadingindicator.css` | Render-blocking | 1 KB | <1 KB |
  | `bootstrap.bundle.min.js` (unused) | Deferred download, parse, compile | 81 KB | 24 KB |
  | Blazorise `*.wasm` assemblies (7, incl. Licensing and Snackbar) | Background WASM download (InteractiveAuto) | ~2.2 MB | ~714 KB |

  About 53 KB gzipped of render-blocking CSS (roughly 410 KB to parse) is replaced by our own
  `base.css`, `utilities.css` and component CSS, likely under 10 KB gzipped. The WASM figures are from
  an untrimmed Debug build. A published build trims the .NET framework assemblies, so Blazorise is a
  larger share of what's left. Font Awesome's size barely changes: the solid webfont is the same file,
  and only the stylesheet shrinks by dropping brands/regular rules.
- **We already override most of it.** `app.css`, `table.css` and the scoped CSS restyle tabs,
  dropdowns, tables, selects and the navbar. `NavMenu.razor.css` alone has ~39 selectors aimed at
  Bootstrap navbar markup. The site's look already comes from the `--climate-*` tokens, not from Bootstrap.
- **Markup we don't control.** Several existing workarounds only exist because of Blazorise: the
  `display: contents` wrappers for scoped CSS, the inline-flex fix in `DelayedTooltip` for Tippy's
  wrapper, the wrapper `<div>` in `NavMenu` (Blazorise issue 1599), and `aria-label` that may not reach
  the rendered element.
- **Accessibility gaps** found during the inventory (17 of 21 `Select`s have no accessible name,
  one `Check` has no name, icon-only controls). Fixing them in components we own is straightforward.

## Current inventory

Counts are Razor tags in `ClimateExplorer.Web.Client` (excluding `obj/`).

| Blazorise family | Tags (uses) | Files | Plan |
|---|---|---|---|
| `Select` / `SelectItem` | 21 / 63 | 7 | [07 Select](2026-10-28-07-select-plan.md) |
| `Tooltip` (incl. via `DelayedTooltip`) | 4 direct; `DelayedTooltip` 72 uses in 19 files | 3 | [04 Tooltip](2026-10-28-04-tooltip-plan.md) |
| `Table`, `TableHeader/Body/Row/HeaderCell/RowCell` | 5 tables, ~60 cell tags | 4 | [11 Tables](2026-10-28-11-tables-plan.md) |
| `Tabs` / `Tab` / `Items` (header only, no panels) | 7 / 15 | 7 | [10 Tabs](2026-10-28-10-tabs-plan.md) |
| `Modal`, `ModalContent/Header/Title/Body`, `CloseButton` | 5 | 5 | [06 Modal](2026-10-28-06-modal-plan.md) |
| `Dropdown`, `DropdownToggle/Menu/Item/Header/Divider` (incl. via `DropdownButton`) | 3 raw + 4 `DropdownButton` uses, 7 items | 6 | [05 Dropdown](2026-10-28-05-dropdown-plan.md) |
| `Check` | 5 | 4 | [08 Checkbox](2026-10-28-08-checkbox-plan.md) |
| `TextInput`, `Validation`, `ValidationError`, `Feedback` | 2 | 2 | [09 Text input](2026-10-28-09-text-input-validation-plan.md) |
| `LoadingIndicator` (via `DelayedLoadingIndicator`, 7 uses) + 2 hand-rolled spinners | 1 | 1 (+8 consumers) | [03 Loading indicator](2026-10-28-03-loading-indicator-plan.md) |
| `Pagination`, `PaginationItem`, `PaginationLink` | 1 | 1 (`PaginationControl`) | [12 Pagination](2026-10-28-12-pagination-plan.md) |
| `Autocomplete` (Blazorise.Components) | 1 | 1 (`ChangeLocation`) | [13 Autocomplete](2026-10-28-13-autocomplete-plan.md) |
| `Layout*`, `Bar*` | 1 layout, 1 navbar | 2 | [14 Layout and navbar](2026-10-28-14-layout-navbar-plan.md) |
| `Accordion` (empty), `Div`, `Field`, `Blazorise.Link`, `Button`, `Strong` | 1, 2, 1, 1, 1, 22 | 5 | [15 Simple markup](2026-10-28-15-simple-markup-replacements-plan.md) |

Not Blazorise components, but in scope:

| Item | Where | Plan |
|---|---|---|
| Font Awesome 6.6.0 CSS/fonts served from the `Blazorise.Icons.FontAwesome` package | `App.razor` (async preload) | [02 Icons](2026-10-28-02-icons-plan.md) |
| Open Iconic font (`oi oi-chevron-*`, `oi-plus`), 8 uses | `app.css` `@import`, 7 Razor files | [02 Icons](2026-10-28-02-icons-plan.md) |
| Bootstrap 5.3.3 CSS + JS bundle from jsdelivr | `App.razor` | [16 Bootstrap removal](2026-10-28-16-bootstrap-removal-plan.md) |
| Blazorise types in code-behind: `Modal`, `ModalClosingEventArgs`, `ValidationStatus`, `ValidatorEventArgs`, `Autocomplete<,>` | 5 `.razor.cs` files | covered by the family plans |

Already native (no work beyond the final rename): `ClimateButton`, `ClimateCheckbox`, `Collapsible`,
`InfoPanel`, `SidePanel`, `PillButton`, `UnitSwitch`, `OverviewField` (its modal is covered by plan 06).

## Decisions

### 1. Bootstrap: remove it outright, don't use it directly

Bootstrap does four jobs today:

| Job | Size of the dependency | After this plan |
|---|---|---|
| Styles the markup Blazorise renders (`.navbar`, `.nav-tabs`, `.dropdown-menu`, `.form-select`, `.form-check`, `.form-control`, `.modal`, `.table`, `.pagination`, `.btn`, `.btn-close`) | Most of what we use | Gone with the Blazorise components; each new component has its own CSS |
| Reboot / base element styles (box-sizing, body margin, line-height, heading/paragraph margins, form-control font inheritance) | ~40 rules we rely on implicitly | A small `base.css` with just the rules we depend on |
| Utility classes in our markup (`d-flex`, `mb-4`, `text-center`, `gap-4`, `align-items-center`, `visually-hidden`, …) | ~25 distinct classes, ~75 uses | A `utilities.css` with exactly those classes, under the same names, so no markup changes |
| JS bundle (`bootstrap.bundle.min.js`) | No `data-bs-*` attributes and no `bootstrap.*` calls in the app; Blazorise ships its own JS | Delete early (quick win) |

Using Bootstrap directly (instead of through Blazorise) would mean writing the new components against
Bootstrap's class names and keeping ~230 KB minified (~30 KB gzipped) of render-blocking CSS
from a third-party CDN, most of which we then override. We'd also be tied to Bootstrap's markup
conventions. We already have our own design tokens. Detail and staging are in
[16 Bootstrap removal](2026-10-28-16-bootstrap-removal-plan.md).

### 2. Font Awesome: keep the classes, change where the files come from

Vendor Font Awesome Free 6.x into `ClimateExplorer.Web.Client/wwwroot/lib/fontawesome`, next to the
vendored Chart.js, so it is served same-origin with `@Assets` fingerprinting (a CDN was considered
and is about equal, not faster; see the icons plan). Ship only the solid style (the app only uses `fas`/`fa-solid`), and keep the
existing async-preload `<link>`. That changes no call sites. Open Iconic's 8 uses move to the
equivalent Font Awesome icons, and the Open Iconic font is deleted. An inline-SVG `Icon` component is
an optional later phase. See [02 Icons](2026-10-28-02-icons-plan.md).

### 3. Native platform first, small JS modules second, no new UI library

Modern browser primitives cover the hard parts that Blazorise did for us:

| Need | Primitive |
|---|---|
| Modal focus trap, Escape, inert background, top layer | `<dialog>` + `showModal()` |
| Dropdown light-dismiss, Escape, top layer (no clipping by `overflow: hidden` ancestors) | `popover="auto"` + `popovertarget` |
| Tooltip above everything, no clipping | one shared `popover="manual"` element |
| Positioning popovers/tooltips against their anchor, with flip and viewport shift | one shared ~80-line ES module, `wwwroot/js/floating.js` (introduced by plan 04, reused by 05 and 13) |

CSS anchor positioning could later replace `floating.js`. Check browser support when implementing.
Until then, the JS module is the fallback anyway.

### 4. Component API conventions

- Components live in `Components/Common`, with C# in `.razor.cs` and styles in `.razor.css`.
- Inputs bind with `Value` / `ValueChanged`, the same convention AGENTS.md documents for Blazorise
  today, so `@bind-Value` works and call sites change little. `ClimateCheckbox` moves from
  `Checked`/`CheckedChanged` to match (plan 08).
- Every component takes `Class` and captures unmatched attributes (`AdditionalAttributes`) onto its
  main element, like `ClimateButton` does.
- Size variants are a `Compact` bool (as `ClimateButton` has), not a size enum. This replaces
  `Size="Size.Small"`.
- Every interactive component must produce an accessible name. Where none can be derived, it takes
  an `AriaLabel` parameter marked `[EditorRequired]`.
- No new Bootstrap class names in new markup. New CSS uses the `--climate-*` tokens.
- **Interactive Auto / prerender.** Components must render sensibly during SSR prerender. JS interop
  only runs in `OnAfterRenderAsync`, and disposal swallows `JSDisconnectedException`. Where a
  control would misbehave before interactivity (e.g. a popover opening unpositioned), it checks
  `RendererInfo.IsInteractive` and renders disabled until then.

### 5. Coexistence while Blazorise is still referenced

`Components.Common` is imported in `_Imports.razor`. Any file that also has `@using Blazorise` sees
both namespaces, and a tag that exists in both (`Select`, `Modal`, `Tabs`, `Tab`, `DropdownItem`,
`TextInput`, `Autocomplete`, `Button`, `Tooltip`) is a Razor compile error (RZ9985, ambiguous
component).

Rules:

1. **Migrate a family across all of its call sites in one change.** Each family plan lists every file.
2. In a touched file that still needs other Blazorise components, remove `@using Blazorise` and
   fully qualify what remains (e.g. `<Blazorise.Modal>`). There's precedent: `ChangeLocation` already
   uses `<Blazorise.Link>`. These qualifiers disappear as later families land.
3. New components that collide with a Blazorise name get their final name straight away (`Select`,
   `Modal`, `Tabs`, …). **Renames of existing components** to names Blazorise also uses
   (`ClimateButton` → `Button`, `ClimateCheckbox` → `Checkbox`, optionally `DelayedTooltip` → `Tooltip`)
   wait until the packages are gone (plan 17).
4. `dotnet build` catches any collision we miss. It's the check for every step.

### 6. Verification

Per AGENTS.md, verification is `dotnet build` plus the unit test suite. Where a family has pure logic
(page windowing, select value mapping, autocomplete filtering, input validation), that logic goes
into a static helper and gets unit tests. Visual and keyboard checks are listed in each plan as
**manual checks for the developer**. Agents don't run the site, Playwright or Lighthouse.

`ClimateExplorer.Web.UiTests` references two Bootstrap classes (`.table`, `.modal-body` in
`ChartTests.cs`). Plans 06 and 11 update those selectors. Running the UI tests is left to the developer.

## Implementation order

An order is required: a few plans depend on code or markup introduced by others, and the removal
steps can only run once nothing renders Blazorise or Bootstrap markup. The plan numbers (02–17)
group plans by topic. They aren't the build order. **Follow the step numbers below.** Each step
builds, passes the unit tests and can ship on its own. The site keeps working between steps.
**Implemented** is the date a step landed; the plan's addendum records what shipped.

| Step | Plan | Hard dependencies (must be done first) | Why it sits here | Implemented |
|---|---|---|---|---|
| 1 | [02 Icons](2026-10-28-02-icons-plan.md) | — | Removes the Font Awesome dependency on a Blazorise package. Independent. | 2026-10-28 |
| 2 | [16 Bootstrap removal](2026-10-28-16-bootstrap-removal-plan.md), step 1 (JS bundle) | — | Independent quick win | 2026-10-28 |
| 3 | [16 Bootstrap removal](2026-10-28-16-bootstrap-removal-plan.md), step 2 (`base.css`, `utilities.css`) | — | Must land long before step 17, so reboot drift surfaces while Bootstrap can still be compared | 2026-10-28 |
| 4 | [15 Simple markup](2026-10-28-15-simple-markup-replacements-plan.md) | — | No JS, tiny | — |
| 5 | [12 Pagination](2026-10-28-12-pagination-plan.md) | — | Internal to one component | — |
| 6 | [11 Tables](2026-10-28-11-tables-plan.md) | — | Mechanical. No name collisions. | — |
| 7 | [08 Checkbox](2026-10-28-08-checkbox-plan.md) | — | Settles the `Value`/`ValueChanged` convention before the other inputs | — |
| 8 | [03 Loading indicator](2026-10-28-03-loading-indicator-plan.md) | — | Self-contained | — |
| 9 | [07 Select](2026-10-28-07-select-plan.md) | (step 7 recommended) | Introduces the shared `climate-control` look and the `<label for>` pattern | — |
| 10 | [09 Text input](2026-10-28-09-text-input-validation-plan.md) | step 9 (reuses `climate-control`; same files) | Do straight after Select so the `Blazorise.*` qualifiers in the chart forms are short-lived | — |
| 11 | [10 Tabs](2026-10-28-10-tabs-plan.md) | — | Mechanical | — |
| 12 | [04 Tooltip](2026-10-28-04-tooltip-plan.md) | — | **Introduces `wwwroot/js/floating.js`** | — |
| 13 | [05 Dropdown](2026-10-28-05-dropdown-plan.md) | step 12 (`floating.js`) | Converts NavMenu's two dropdowns too | — |
| 14 | [06 Modal](2026-10-28-06-modal-plan.md) | — (after 13 recommended: tooltips/menus inside modals can then be checked together) | | — |
| 15 | [13 Autocomplete](2026-10-28-13-autocomplete-plan.md) | step 14 recommended (same file; enables the `autofocus` simplification), step 12 if the list needs the popover fallback | | — |
| 16 | [14 Layout and navbar](2026-10-28-14-layout-navbar-plan.md) | step 13 (NavMenu's dropdowns must already be `DropdownButton`) | | — |
| 17 | [16 Bootstrap removal](2026-10-28-16-bootstrap-removal-plan.md), steps 3–4 (cleanup, remove CSS) | steps 3–16 | Only once nothing renders Bootstrap markup | — |
| 18 | [17 Package removal and renames](2026-10-28-17-blazorise-package-removal-and-renames-plan.md) | all previous steps | Packages can't go while anything references them. Renames can't happen while the Blazorise namespace is imported. | — |

Flexibility: steps 4–11 have no dependencies on each other (apart from 9 → 10). They can be
reordered or done in parallel branches. Steps 12 → 13 → 16 and 3 → 17 → 18 are fixed chains.

The UI test selector updates in plans 06, 11 and 15 touch the same line (`ChartTests.cs`). Whichever
of steps 6, 4 or 14 lands last sets the final selector.

## Component plans

| # | Plan | New/changed Common components |
|---|---|---|
| 02 | [Icons](2026-10-28-02-icons-plan.md) | none (vendored assets) |
| 03 | [Loading indicator](2026-10-28-03-loading-indicator-plan.md) | `LoadingIndicator` (replaces `DelayedLoadingIndicator`) |
| 04 | [Tooltip](2026-10-28-04-tooltip-plan.md) | `DelayedTooltip` rewritten, `TooltipPlacement`, `wwwroot/js/floating.js`, `wwwroot/js/tooltip.js` |
| 05 | [Dropdown](2026-10-28-05-dropdown-plan.md) | `DropdownButton` rewritten, `DropdownItem` |
| 06 | [Modal](2026-10-28-06-modal-plan.md) | `Modal`, `ModalSize` |
| 07 | [Select](2026-10-28-07-select-plan.md) | `Select<TValue>`, `SelectItem<TValue>` |
| 08 | [Checkbox](2026-10-28-08-checkbox-plan.md) | `ClimateCheckbox` (API aligned) |
| 09 | [Text input and validation](2026-10-28-09-text-input-validation-plan.md) | `TextInput` |
| 10 | [Tabs](2026-10-28-10-tabs-plan.md) | `Tabs`, `Tab` |
| 11 | [Tables](2026-10-28-11-tables-plan.md) | none (plain `<table class="climate-table">`) |
| 12 | [Pagination](2026-10-28-12-pagination-plan.md) | `PaginationControl` internals |
| 13 | [Autocomplete](2026-10-28-13-autocomplete-plan.md) | `Autocomplete<TItem, TValue>` |
| 14 | [Layout and navbar](2026-10-28-14-layout-navbar-plan.md) | none (`MainLayout`, `NavMenu` markup) |
| 15 | [Simple markup replacements](2026-10-28-15-simple-markup-replacements-plan.md) | none |
| 16 | [Bootstrap removal](2026-10-28-16-bootstrap-removal-plan.md) | `base.css`, `utilities.css` |
| 17 | [Package removal and renames](2026-10-28-17-blazorise-package-removal-and-renames-plan.md) | `ClimateButton` → `Button`, `ClimateCheckbox` → `Checkbox`, optional `DelayedTooltip` → `Tooltip` |

## Risks

- **Visual drift from losing Bootstrap reboot.** Margins, line-height and form-control fonts are
  inherited implicitly today. Mitigation: `base.css` is added *before* Bootstrap is removed and
  loaded after it, so the later removal is close to a no-op (plan 16).
- **Keyboard and focus regressions** in overlays. Mitigation: native `<dialog>`/`popover` handle
  focus trap, Escape and focus return. Each plan lists manual keyboard checks.
- **Positioning edge cases** (tooltips/menus near viewport edges, inside side panels and modals).
  Mitigation: top layer avoids clipping, and `floating.js` flips and shifts. Floating UI
  (`@floating-ui/dom`, vendored) is the fallback if edge cases pile up.
- **Browser support.** `<dialog>`, `popover` and `:has()` are Baseline (2022–2024). Older browsers
  aren't a target. Each plan notes any newer feature it relies on and the fallback.

## Summary

Blazorise is replaced family by family with small native components in `Components/Common`, built on
`<dialog>`, `popover` and one shared positioning module, with CSS using the existing `--climate-*`
tokens. Font Awesome is vendored (solid only) and Open Iconic is folded into it. Bootstrap's JS goes
first, Bootstrap's CSS goes last, after a small `base.css`/`utilities.css` has taken over the parts
we rely on. The packages, DI registration and product token are then removed, and `ClimateButton`
becomes `Button`.

### Change size

- **Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.Web` (App.razor, Program.cs, wwwroot/css),
  `ClimateExplorer.UnitTests`, `ClimateExplorer.Web.UiTests` (2 selectors), `AGENTS.md`, `CONTEXT.md`.
- Unchanged: Core, WebApi, WebApiClient, data projects.

Totals are rolled up from the family plans and are approximate.

| Area | Added | Changed | Removed |
|---|---|---|---|
| New/rewritten Common components (`.razor`, `.razor.cs`, `.razor.css`) | ~26 files, +1,450 | ~8 files, ~250 | 2 files (`DelayedLoadingIndicator.*`), −90 |
| JS modules (`floating.js`, `tooltip.js`, `Modal.razor.js`, `DropdownButton.razor.js`) | 4 files, +260 | — | — |
| Call-site migrations (Razor + code-behind) | — | ~45 files, ~700 | — |
| Global CSS (`app.css`, `table.css`, new `base.css`, `utilities.css`) | 2 files, +200 | 2 files, ~200 | ~120 lines of Bootstrap/Blazorise overrides |
| Scoped CSS of consumers (`NavMenu`, `ChartSeriesView`, `LocationDashboard`, …) | — | ~15 files, ~300 | — |
| Vendored Font Awesome (CSS + 1 font) | 3 files (vendored) | — | Open Iconic folder (7 files) |
| Host / DI / packages (`App.razor`, 2× `Program.cs`, `.csproj`) | — | 4 files, ~40 | 5 package refs |
| Renames (plan 17) | — | ~35 files, ~150 | — |
| Unit tests | 5 files, +350 | — | — |
| UI test selectors | — | 1 file, ~2 | — |
| Docs (AGENTS.md, CONTEXT.md) | — | 2 files, ~30 | — |
| **Total** | **~40 files, ~+2,260** (plus vendored assets) | **~110 file-touches, ~1,670** | **~10 files, ~−210** |

A fair share of the "changed" lines are mechanical tag swaps (`<TableRowCell>` → `<td>`, `Size="Size.Small"` → `Compact`,
`ClimateButton` → `Button`).
