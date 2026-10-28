# Blazorise Package Removal and Component Renames

- **Date:** 2026-10-28
- **Status:** Part A implemented 2026-10-28 (see addendum); Parts B and C proposed. Build and unit tests are clean; the UI is unverified.
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
3. **Keep CSS class names** (`climate-button`, `climate-checkbox`, `tooltip-anchor`). Renaming them
   adds churn across scoped CSS for no user-visible benefit, and the `climate-` prefix keeps global
   classes from clashing with third-party CSS.
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
