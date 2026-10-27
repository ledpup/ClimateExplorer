# Checkbox — Replace Blazorise `Check` with `ClimateCheckbox`

- **Date:** 2026-10-28
- **Status:** Proposed
- **Author:** Claude
- **Scope:** `Components/Common/ClimateCheckbox.*`; Blazorise `Check` consumers `AggregationOptions`,
  `ChartAxisListView`, `ChartSeriesView`, `LocationDashboard`; existing `ClimateCheckbox` consumers
  `LocalDataSetBrowser`, `ClimateRecords`, `RecentObservationsPanel`; scoped CSS
- **Builds on:** [Blazorise Removal — Overview](2026-10-28-01-blazorise-removal-overview-plan.md)
- **Branch context:** `development`
- **Implementation order:** step 7 of 18; see [overview](2026-10-28-01-blazorise-removal-overview-plan.md#implementation-order)

## Current state

`ClimateCheckbox` already exists. It's a native `<label><input type="checkbox">…</label>` with
`Text`, `Checked`/`CheckedChanged`, `Disabled`, `Class`, `ChildContent` and captured attributes,
styled by `.climate-checkbox` in `app.css`. It's used 4 times.

Five Blazorise `Check<bool>` uses remain:

| File | Use | Accessible name today |
|---|---|---|
| `Components/Chart/AggregationOptions.razor` | "Override preset threshold" | child text ✔ |
| `Components/Chart/ChartAxisListView.razor` | one per axis, in the Axes dropdown | `@axis.Label` ✔ |
| `Components/Chart/ChartSeriesView.razor` | Show fitted line | `aria-label` ✔ (visible label is an unassociated `form-label` span) |
| `Components/Chart/ChartSeriesView.razor` | Lock series | **none** (visible "Lock?" is an unassociated `form-label` span) |
| `Components/Location/LocationDashboard.razor` | "Precipitation stripe", in the cog dropdown | child text ✔ |

Bootstrap `.form-check*` styling is adjusted in `ChartSeriesView.razor.css` and `LocationDashboard.razor.css`.

## Decision: align `ClimateCheckbox` to `Value`/`ValueChanged`

All the other new inputs (`Select`, `TextInput`) and every Blazorise input today use
`Value`/`ValueChanged`. Having the checkbox alone on `Checked`/`CheckedChanged` would be the
exception every author trips on. So: rename the parameters to `Value`/`ValueChanged`, which also
enables `@bind-Value`, and update the 4 existing call sites. The component keeps the `Climate`
prefix until [17](2026-10-28-17-blazorise-package-removal-and-renames-plan.md) renames it to
`Checkbox` (`Check` vs `Checkbox` don't collide, but the rename belongs with the other renames).

## Changes

1. `ClimateCheckbox.razor.cs`: `Checked` → `Value`, `CheckedChanged` → `ValueChanged`. Add an `Id`
   parameter forwarded to the `<input>`, so external `<label for>` works where the visible label sits
   elsewhere (the chart form rows).
2. Existing call sites (`LocalDataSetBrowser`, `ClimateRecords`, `RecentObservationsPanel` ×2):
   rename the attributes.
3. Replace the five `Check`s:
   - `AggregationOptions`, `ChartAxisListView`, `LocationDashboard`: `<ClimateCheckbox Value=… ValueChanged=…>text</ClimateCheckbox>`.
   - `ChartSeriesView` fitted line and lock: render the checkbox with an `Id`, and turn the row's
     `<span class="form-label">` into `<label class="form-label" for="@id">` (same pattern as
     [07 Select](2026-10-28-07-select-plan.md)). That gives "Lock?" a real accessible name. Drop the
     now-redundant `aria-label="Show fitted line"` only if the visible label text says the same thing.
4. Inside dropdown menus (Axes, cog), the checkbox `label` should fill the row and meet the 44px
   touch target. Add a `climate-checkbox--menu-row` modifier, or style via the menu's `::deep` rules
   from [05 Dropdown](2026-10-28-05-dropdown-plan.md).
5. Remove the `.form-check*` rules from the two scoped CSS files and re-express any layout tweaks against `.climate-checkbox`.
6. AGENTS.md: replace the "Blazorise 2.x inputs bind with `Value`/`ValueChanged`" line with
   "Common inputs (`Select`, `TextInput`, `ClimateCheckbox`) bind with `Value`/`ValueChanged`."

Files that still `@using Blazorise` for other components are fine: `ClimateCheckbox` doesn't
collide. Removing `Check` alone doesn't need any qualifiers.

## Manual checks (developer)

- All 9 checkboxes toggle and update state. Disabled ones can't be toggled.
- Clicking "Lock?" / the fitted-line label toggles its checkbox. The screen reader announces the names.
- Axis and precipitation checkboxes in dropdown menus are easy to hit on mobile, and toggling
  doesn't close the menu.

## Summary

The five Blazorise `Check`s become the existing native `ClimateCheckbox`, whose parameters move to
`Value`/`ValueChanged` to match the other inputs. The lock checkbox, which has no accessible name,
gets one through a proper `<label for>`.

### Change size

- **Projects:** `ClimateExplorer.Web.Client`, `AGENTS.md`.

| Area | Added | Changed | Removed |
|---|---|---|---|
| `ClimateCheckbox.razor` / `.razor.cs` (rename params, add `Id`) | — | 2 files, ~10 | — |
| Existing `ClimateCheckbox` call sites | — | 3 files, ~8 | — |
| `Check` → `ClimateCheckbox` (4 files) incl. labels | — | 4 files, ~25 | — |
| Scoped CSS / `app.css` | — | 3 files, ~15 | ~10 lines of `.form-check` rules |
| AGENTS.md | — | 1 file, ~1 | — |
| **Total** | — | **13 files, ~59** | **~10 lines** |

Risk: low.
