# Blazorise Package Removal and Component Renames

- **Date:** 2026-10-28
- **Status:** Implemented 2026-10-28 (Parts A–C; see addenda). Build and unit tests are clean; the UI is unverified.
- **Author:** Claude
- **Scope:** `ClimateExplorer.Web.Client.csproj`, both `Program.cs` files, `ClimateExplorer.Web/App.razor`,
  `app.css`, remaining `@using Blazorise` lines, `ClimateButton`/`ClimateCheckbox`/`DelayedTooltip`
  and all their call sites, `AGENTS.md`, `CONTEXT.md`
- **Builds on:** [Blazorise Removal — Overview](2026-10-28-01-blazorise-removal-overview-plan.md), and every
  family plan (02–15) plus [16 Bootstrap removal](2026-10-28-16-bootstrap-removal-plan.md) step 4
- **Branch context:** `development`
- **Implementation order:** step 18 of 18; see [overview](2026-10-28-01-blazorise-removal-overview-plan.md#implementation-order)

## Preconditions

All family plans have landed. These greps over `ClimateExplorer.Web.Client` (excluding `obj/`/`bin/`)
return nothing except this plan's targets:

- `@using Blazorise` / `using Blazorise`
- `<Blazorise.` (fully-qualified leftovers from the coexistence rule)
- `_content/Blazorise` in `ClimateExplorer.Web`

## Part A — Remove Blazorise

1. **Packages.** Remove from `ClimateExplorer.Web.Client.csproj`: `Blazorise`, `Blazorise.Bootstrap5`,
   `Blazorise.Components`, `Blazorise.Icons.FontAwesome`, `Blazorise.LoadingIndicator`.
2. **DI.** In both `ClimateExplorer.Web/Program.cs` and `ClimateExplorer.Web.Client/Program.cs`,
   remove `.AddBlazorise(options => { options.ProductToken = "…"; })`, `.AddBootstrap5Providers()`,
   `.AddFontAwesomeIcons()` and the three `using Blazorise…;` lines. The chain then starts at
   `.AddMapService()`, called on `builder.Services`.
3. **Static assets.** Remove the `blazorise.css` and `blazorise.bootstrap5.css` links from `App.razor`
   (the LoadingIndicator CSS went in plan 03, Font Awesome in plan 02).
4. **CSS leftovers.** Remove any remaining `--b-*` variables, `.b-*`/`.tippy-*` selectors, and
   comments about Blazorise workarounds (`display: contents` isolation wrappers whose child is no
   longer a Blazorise component; check each `style="display: contents"` wrapper and delete it if its
   only purpose was Blazorise scoping).
5. **Unit test comment.** `ChartLogicDatasetTests.cs` mentions Blazorise.Charts as historical
   context. Leave it; it explains why the JSON shape is pinned.
6. **Verify.** `dotnet build` (warnings are errors in Web.Client) and `dotnet test`.

## Part B — Rename components

Now there's no Blazorise namespace to collide with:

| From | To | Files referencing (approx.) | Notes |
|---|---|---|---|
| `ClimateButton` | `Button` | 16 files, ~45 references | The user asked for this. `ButtonVariant` keeps its name. |
| `ClimateCheckbox` | `Checkbox` | ~8 files after plan 08 | Same reasoning as `Button` |
| `DelayedTooltip` | `Tooltip` | 19 files, 72 uses | **Optional.** The "Delayed" prefix describes behaviour every tooltip now has. Do it in the same change if wanted, or skip it. |

Steps per rename:

1. `git mv` the `.razor`, `.razor.cs` and `.razor.css` files, and rename the partial class.
2. Replace tags across Razor files (`<ClimateButton` → `<Button`, `</ClimateButton>` → `</Button>`) and
   any C# references (`PillButton.razor.cs` mentions `ClimateButton` in a comment, and
   `PillButton.razor.css` references it).
3. **Drop the `climate-` prefix from every common component's CSS classes**, so each class matches
   its component (`climate-button` → `button`, `climate-select` → `select`, `climate-modal` → `modal`, …).
   *(Revised 2026-10-28. This step first said to keep the names, on the grounds that the prefix guards
   against third-party CSS clashes. No such CSS is left: Font Awesome uses only `fa-*` and Leaflet only
   `leaflet-*`, and nothing in our CSS used the bare names. The component renames are for
   source readability, and the same reasoning applies to the classes.)* Keep the `--climate-*` custom
   properties (design tokens, a separate namespace) and the `climate-…-{guid}` element-id prefixes.
4. `<Button>` vs the HTML `<button>`: Razor resolves components case-sensitively, so `<Button>` is
   the component and `<button>` the element. Mention it in AGENTS.md, so nobody "fixes" one into the other.

Historical design docs that mention `ClimateButton` stay as they are; they're dated records.

## Part C — Documentation

- **AGENTS.md**
  - Common components list: `ClimateButton` → `Button`; add `Checkbox`, `Modal`, `Select`, `Tabs`,
    `TextInput`, `Autocomplete`, `DropdownItem`; `DelayedLoadingIndicator` → `LoadingIndicator`
    (Overlay vs Inline guidance from plan 03); `DelayedTooltip` → `Tooltip` if renamed.
  - Replace the Blazorise paragraph ("There are Blazorise components in use…", "Blazorise 2.x inputs
    bind with…", "Style Blazorise child elements with `::deep`…") with: Common inputs bind with
    `Value`/`ValueChanged`; use `::deep` to style child *components'* markup; utility classes are
    limited to `utilities.css`.
  - Keep the Font Awesome vendoring note from plan 02.
- **CONTEXT.md**
  - Stack line: remove "Blazorise (Bootstrap 5)"; note "Font Awesome 6 (vendored, solid)".
  - Line ~63: the chart tree still says `Chart<double?> + ChartTrendline<double?> (Blazorise.Charts)`,
    which was already stale. Update it to `ChartJsChart`.
  - `OverviewField` description: "renders a Blazorise `Modal`" → "renders a `Modal`".
  - Dropdown styling note, CSS isolation section ("Root-element isolation problem"), the `::deep` /
    `display: contents` rows in the conventions table, and the "Blazorise 2.x Component API Notes"
    section: rewrite or remove. Replace them with a short "Common components" section describing the
    native primitives (`<dialog>`, `popover`, `floating.js`, `tooltip.js`).

## Manual checks (developer)

- Smoke-test every page once at the three breakpoints. Nothing should have changed visually since
  the family plans; this is the last chance to catch a missing asset.
- Network panel: no `_content/Blazorise*` requests; the WASM payload no longer includes `Blazorise*.dll`.
- Optional: compare Lighthouse performance (render-blocking resources, total transfer) before and
  after the whole programme, run by the developer.

## Summary

With every component replaced, the five Blazorise packages, their DI registration and product token,
and the last Blazorise stylesheets are removed. `ClimateButton` and `ClimateCheckbox` are then renamed
to `Button` and `Checkbox` (optionally `DelayedTooltip` to `Tooltip`), and AGENTS.md/CONTEXT.md are
updated to describe the native components instead of Blazorise.

### Change size

- **Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.Web`, `AGENTS.md`, `CONTEXT.md`.

| Area | Added | Changed | Removed |
|---|---|---|---|
| `.csproj` | — | 1 file | 5 package references |
| `Program.cs` ×2 | — | 2 files, ~4 | ~16 lines |
| `App.razor` | — | 1 file | 2 lines |
| CSS leftovers / isolation wrappers | — | ~5 files, ~15 | ~15 lines |
| Rename `ClimateButton` → `Button` | 3 files renamed | ~16 files, ~45 | — |
| Rename `ClimateCheckbox` → `Checkbox` | 2 files renamed | ~8 files, ~12 | — |
| Rename `DelayedTooltip` → `Tooltip` (optional) | 3 files renamed | ~19 files, ~145 | — |
| Drop `climate-` class prefix (step 3, revised) | — | 55 files, ~220 | — |
| AGENTS.md, CONTEXT.md | — | 2 files, ~60 | ~40 lines |
| **Total** (with optional rename) | **8 files renamed** | **~54 files, ~281** | **~73 lines + 5 packages** |

Without the optional tooltip rename: ~35 files, ~136 lines changed. Risk: low. It's mechanical, and the build catches misses.

## Addendum — implementation notes (Part A, 2026-10-28)

### What shipped

- **Step 1.** The five `Blazorise*` package references are gone from `ClimateExplorer.Web.Client.csproj`.
  Neither project's `project.assets.json` mentions Blazorise after restore.
- **Step 2.** Both `Program.cs` files lost the three `using Blazorise…;` lines and the `.AddBlazorise(…)`,
  `.AddBootstrap5Providers()`, `.AddFontAwesomeIcons()` calls, product token included. The chain starts at
  `.AddMapService()`.
- **Step 3.** The `blazorise.css` and `blazorise.bootstrap5.css` links are gone from `App.razor`. The
  "Layout-critical CSS" comment above them still applies to `base.css`/`utilities.css`/`app.css`.
- **Step 4.** No `--b-*` variables or `.b-*`/`.tippy-*` selectors were left. Of the three `display: contents`
  wrappers:
  - `ChartAxisListView`'s `axes-dropdown-wrapper` is removed. It only anchored `::deep .axes-menu-content`, and
    that element is in the component's own markup (the `MenuContent` fragment), so it gets the scope attribute
    directly. The rule is now plain `.axes-menu-content`.
  - `AggregationOptions`' `aggregation-options-wrapper` and `Top100`'s `top100-download-dropdown-wrapper` stay.
    They anchor `::deep` rules that reach into child components' markup (`.climate-select`, `.aggregation-toggle`
    on `ClimateButton`, `.climate-dropdown-menu`), which is a general CSS-isolation need, not a Blazorise one.
  - `app.css`'s `.trends-stat-row { display: contents }` is a grid layout rule, unrelated.
- **Step 5.** The `ChartLogicDatasetTests.cs` comment is left as planned.
- **Step 6.** `dotnet build -c Release` has 0 errors (the 11 warnings are existing MSTest analyzer warnings in
  the test project). `ClimateExplorer.UnitTests`: 980 passed.

### Deviations from the plan

- **An extra `@using Blazorise`** was in `SuggestedChartsMini.razor`, so the precondition grep wasn't empty.
  It was unused and is removed.
- **Not touched:** `README.md` still credits Blazorise for charting, and `.github/copilot-instructions.md` still
  has the Blazorise 2.x and CSS-isolation sections. Both are documentation, so they belong with Part C.

## Addendum — implementation notes (Part B, 2026-10-28)

### What shipped

- **All three renames, including the optional `DelayedTooltip` → `Tooltip`.** `git mv` moved `ClimateButton.razor`/`.razor.cs`/
  `.razor.css` to `Button.*`, `ClimateCheckbox.razor`/`.razor.cs` to `Checkbox.*` (it has no `.razor.css`), and
  `DelayedTooltip.razor`/`.razor.cs`/`.razor.css` to `Tooltip.*`. The partial classes are renamed to match.
- **References.** A whole-word replace across `.razor`, `.cs`, `.css` and `.js` in `ClimateExplorer.Web.Client` and
  `ClimateExplorer.Web` updated tags, `<see cref>`s and comments. That's 35 files, including the comments in
  `PillButton.razor.cs`/`.razor.css`, `tooltip.js`, `App.razor` and `app.css`. Comments where the bare name read
  ambiguously ("the tooltip for every Tooltip") now say "the Tooltip component".
- **CSS class prefix dropped** (step 3 as revised). After the component renames, the `climate-` prefix came off
  43 component class names in 55 files (~220 lines): `autocomplete*`, `button*`, `link-button`, `checkbox*`, `control`,
  `dropdown*`, `modal*`, `pagination*`, `select`, `tab`/`tabs`/`tablist`, `table*`, `text-input*` and `tooltip`.
  It was a whole-token replace (`(?<![-\w])climate-<name>(?![-\w])`) over `.razor`, `.cs`, `.css` and `.js` in
  `ClimateExplorer.Web.Client`, `ClimateExplorer.Web` and `ClimateExplorer.Web.UiTests` (one Playwright locator in
  `ChartTests.cs`), plus `AGENTS.md` (`.control`) and `CONTEXT.md` (`dialog.modal[open]`). Before the change, no
  CSS selector, class attribute or JS class string used any of the bare names.
  - **Not renamed:** the `--climate-*` custom properties; the `climate-…-{guid}` id prefixes in `Modal`, `Tabs`,
    `Autocomplete`, `DropdownButton` and `ClimateRecords`; `climate-stripe`, which already matches its `ClimateStripe`
    component.
  - **The shared tooltip's element id** in `tooltip.js` was also `climate-tooltip`, the same string as its class, so it
    is now `tooltip` too. Its `aria-describedby` handling uses the same constant, so it stays consistent.
  - **Static check in place of a UI run:** every renamed class still has at least one CSS selector and at least one
    use in markup or code. The exception is `tabs`, which had no CSS rule before the rename either (it's a root hook).
- **AGENTS.md.** The components list has `Button` (with the `<Button>` vs `<button>` note from step 4), `Checkbox` and
  `Tooltip` in place of `ClimateButton`/`DelayedTooltip`, and the bind-with-`Value` line says `Checkbox`. The rest of
  AGENTS.md is left for Part C.
- **Verify.** `dotnet build -c Release` has 0 errors and the same 11 MSTest analyzer warnings. `ClimateExplorer.UnitTests`:
  980 passed.

### Deviations from the plan

- **`Tooltip` vs `OverviewField.Tooltip`.** `OverviewField` has a `Tooltip` string parameter and now renders
  `<Tooltip Text="@(Tooltip == null ? Title : Tooltip)">`. Razor emits the component by its fully qualified type
  name, so the tag and the property don't clash, and the build confirms it. Nothing else in the solution is called
  `Button`, `Checkbox` or `Tooltip`.
- Historical design docs still say `ClimateButton`/`ClimateCheckbox`/`DelayedTooltip`, as planned.

## Addendum — implementation notes (Part C, 2026-10-28)

### What shipped

- **AGENTS.md.** Most of the components list had already been updated by the family plans and Part B. Part C added
  `DropdownItem` (as `DropdownButton / DropdownItem`, noting the menu is a native `popover`). The Blazorise paragraph is
  replaced: tables are plain `<table class="table">` styled in `table.css`; the `Value`/`ValueChanged` line stays;
  the `::deep`/`display: contents` guidance now covers any component whose root is another component. The Font Awesome
  note and the utility-class rule were already there.
- **CONTEXT.md.**
  - Stack line: "Blazorise (Bootstrap 5)" is gone. It now lists Chart.js 4 (vendored), Font Awesome 6 (vendored, solid)
    and "no UI component library".
  - The component tree shows `ChartJsChart` in place of `Chart<double?> + ChartTrendline<double?> (Blazorise.Charts)`.
    The chart-controls entries are `Button`s and a `DropdownButton`, and `AggregationOptions` is a button that opens a panel.
  - The `chart-controls` bar section describes the current CSS (flex with `gap: 16px`; the Download button's
    `.chart-download-control` wrapper with `margin-left: auto`, `0` on mobile). The Blazorise `<Dropdown>` and
    `chart-control-download` cascade notes are removed.
  - The CSS isolation section and the two conventions-table rows describe the root-element problem for any child
    component. The examples are now `AggregationOptions` and `Top100`, since Part A removed `ChartAxisListView`'s wrapper.
  - "Blazorise 2.x Component API Notes" is replaced by a "Common Components" section with a table of the native
    primitives (`<dialog>`, `popover`, `floating.js`, `tooltip.js`) and which components use each.
  - The `OverviewField` description already said "a `Modal`", so it needed no change.
- **`.github/copilot-instructions.md`** (deferred from Part A) is deleted, along with its entry in the solution's
  Solution Items. AGENTS.md and CONTEXT.md are the agent instructions.
- **README.md** (deferred from Part A). The charting credit is Chart.js, and Font Awesome is added.
- **Verify.** Documentation only, so no build or test run. `git grep -i blazorise` outside `docs/design` now finds
  only the `ChartLogicDatasetTests.cs` comment kept by Part A step 5.

### Deviations from the plan

- **Other stale CONTEXT.md lines fixed along the way:** `ChartablePage` no longer listed a `Snackbar` (Blazorise's,
  now gone); `Collapsible` said "Open Iconic chevron" (it's Font Awesome); `ChartAxisListView` still described the
  `display: contents` wrapper Part A removed; the dropdown section-label typography described Blazorise's style rather
  than `.dropdown-header`.
- **`copilot-instructions.md` removed** rather than updated (the plan only covered its Blazorise sections).
