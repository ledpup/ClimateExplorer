# Bulk "add 12 months" / "add 4 seasons" sub-buttons

- **Date:** 2026-09-07
- **Status:** Implemented 2026-09-07 (see addendum)
- **Author:** Patrick Lea (with Claude)
- **Scope:** `ClimateExplorer.Web.Client` — `Components/RecentObservations/RecentObservationsPanel.razor`,
  `RecentObservationsPanel.razor.cs`, `RecentObservationsPanel.razor.css`. No changes to
  `UiModel/RecentObservations/RecentObservationPeriodSelection.cs` or the calculator.
- **Branch context:** `development`

## Goal

Give the existing **Add month** and **Add season** buttons on the Recent Observations panel
([RecentObservationsPanel.razor:38-42](../../ClimateExplorer.Web.Client/Components/RecentObservations/RecentObservationsPanel.razor#L38-L42))
a small attached sub-button, to the right of the label, that fills in a whole year's worth of
tiles in one click instead of one tile per click:

- **Add month**'s sub-button shows **"12"** and adds up to 12 earlier month tiles.
- **Add season**'s sub-button shows **"4"** and adds up to 4 earlier season tiles.

Each sub-button is its own `<button>`, visually attached to its parent button the way a
dropdown-toggle caret sits flush against a button's edge (see `.climate-dropdown-toggle` in
[app.css:206-240](../../ClimateExplorer.Web/wwwroot/css/app.css#L206-L240)) — but here it's a
second real button, not a pseudo-element, since it carries its own click behaviour and disabled
state.

## Current state (recap)

`RecentObservationsPanel` renders four "Add earlier" buttons (day/month/season/year), each backed
by a single-step method on the code-behind (`AddEarlierMonth`, `AddEarlierSeason`, etc.,
[RecentObservationsPanel.razor.cs:216-226](../../ClimateExplorer.Web.Client/Components/RecentObservations/RecentObservationsPanel.razor.cs#L216-L226))
that calls the matching method on the shared `RecentObservationPeriodSelection`
([RecentObservationPeriodSelection.cs:61-69](../../ClimateExplorer.Web.Client/UiModel/RecentObservations/RecentObservationPeriodSelection.cs#L61-L69))
and then `RecalculateLoadedTabs()` to refresh the one-tile lookahead buffer every loaded tab needs
(`CreateOptions`,
[RecentObservationsPanel.razor.cs:404-422](../../ClimateExplorer.Web.Client/Components/RecentObservations/RecentObservationsPanel.razor.cs#L404-L422)).
Each click adds exactly one more earlier tile, up to whatever the station's data supports
(`IsAddEarlierMonthDisabled`/`IsAddEarlierSeasonDisabled`, driven by `GetAvailableOffsets`).

Tile removal already exists per-tile: each rendered tile has its own remove button wired to
`RemoveTile(tile)` → `periodSelection.Remove(tile)`
([RecentObservationsPanel.razor.cs:234-237](../../ClimateExplorer.Web.Client/Components/RecentObservations/RecentObservationsPanel.razor.cs#L234-L237)),
which is a pure visibility change — `CurrentTiles` re-filters `CurrentState.Result.Tiles` by
`IsVisibleTile` fresh on every access, so no recalculation is needed for a removal alone.

`CurrentTiles` is scoped to the active tab (`ActiveDomain`); `periodSelection` itself is shared
across every tab, so any add/remove made from one tab's controls is visible on every other loaded
tab too — this plan follows that existing behaviour rather than introducing a new per-tab scope.

## What the sub-buttons do

Clicking a sub-button:

1. **Removal check**, scoped to the active tab's `CurrentTiles`: if every currently visible tile's
   `PeriodKind` already belongs to that button's own pair (Month sub-button:
   `{CurrentMonth, PreviousMonth}`; Season sub-button: `{CurrentSeason, PreviousSeason}`), skip
   removal. Otherwise, remove every currently visible tile (day, month, season, year, and any
   singleton tile) via the existing per-tile `RemoveTile`, giving a clean board before the bulk-add
   runs.
2. **Bulk add**, from wherever `periodSelection` now stands: call the existing single-step
   `AddEarlierMonth()` (or `AddEarlierSeason()`) up to 12 (or 4) times in a loop, stopping early —
   via the existing `IsAddEarlierMonthDisabled`/`IsAddEarlierSeasonDisabled` check — once the
   station's data runs out. This is "append up to N more earlier tiles from the current position",
   not "replace with exactly N tiles": if the board (after step 1, or because step 1 was skipped)
   already shows some earlier months/seasons, the bulk add tops it up rather than restarting the
   count from zero.

Because each loop iteration reuses the existing `AddEarlierMonth`/`AddEarlierSeason` methods
unchanged (including their `RecalculateLoadedTabs()` call), the one-tile lookahead buffer is
refreshed correctly between iterations exactly as it is for a single click — no changes are needed
to `CreateOptions`, `RecentObservationPeriodSelection`, or the calculator to make repeated calls
safe.

## Design

### Code-behind: `RecentObservationsPanel.razor.cs`

- Two new constants: `private const int BulkAddMonthCount = 12;` and
  `private const int BulkAddSeasonCount = 4;`.
- Two new static readonly arrays identifying each button's own period-kind pair:
  ```csharp
  private static readonly RecentObservationPeriodKind[] MonthPeriodKinds =
      [RecentObservationPeriodKind.CurrentMonth, RecentObservationPeriodKind.PreviousMonth];
  private static readonly RecentObservationPeriodKind[] SeasonPeriodKinds =
      [RecentObservationPeriodKind.CurrentSeason, RecentObservationPeriodKind.PreviousSeason];
  ```
- New private helper:
  ```csharp
  private void ClearCurrentTilesUnlessAllMatch(IReadOnlyCollection<RecentObservationPeriodKind> allowedKinds)
  {
      if (CurrentTiles.All(tile => allowedKinds.Contains(tile.PeriodKind)))
      {
          return;
      }

      foreach (var tile in CurrentTiles)
      {
          RemoveTile(tile);
      }
  }
  ```
  (`CurrentTiles` already materialises a fresh `List<>` on every access, so iterating it while
  mutating `periodSelection` inside the loop is safe — the enumeration isn't a live view.)
- New handlers, mirroring `AddEarlierMonth`/`AddEarlierSeason`'s existing shape:
  ```csharp
  private void AddEarlierMonthsBulk()
  {
      ClearCurrentTilesUnlessAllMatch(MonthPeriodKinds);
      for (var i = 0; i < BulkAddMonthCount && !IsAddEarlierMonthDisabled; i++)
      {
          AddEarlierMonth();
      }
  }

  private void AddEarlierSeasonsBulk()
  {
      ClearCurrentTilesUnlessAllMatch(SeasonPeriodKinds);
      for (var i = 0; i < BulkAddSeasonCount && !IsAddEarlierSeasonDisabled; i++)
      {
          AddEarlierSeason();
      }
  }
  ```
- New label properties for the sub-buttons' `AriaLabel`/`title` (the visible text is just the
  numeral, so the accessible name needs to say what it does):
  ```csharp
  private string AddMonthBulkAriaLabel => $"Add {BulkAddMonthCount} earlier months";
  private string AddSeasonBulkAriaLabel => $"Add {BulkAddSeasonCount} earlier seasons";
  ```
- Disabled state reuses the existing single-step flags as-is (`IsAddEarlierMonthDisabled`,
  `IsAddEarlierSeasonDisabled`) — if not even one more tile can be added, the bulk button has
  nothing to do either, regardless of whether the removal step would fire.

### Markup: `RecentObservationsPanel.razor`

Wrap the month and season buttons in a small group container so the sub-button renders flush
against the parent button's right edge:

```razor
<div class="climate-button-group">
    <ClimateButton Text="@AddMonthButtonLabel" Disabled="@IsAddEarlierMonthDisabled" OnClick="AddEarlierMonth" Class="climate-button-group-primary" />
    <DelayedTooltip Text="@AddMonthBulkAriaLabel">
        <ClimateButton Text="12" AriaLabel="@AddMonthBulkAriaLabel" Disabled="@IsAddEarlierMonthDisabled" OnClick="AddEarlierMonthsBulk" Class="climate-button-group-suffix" />
    </DelayedTooltip>
</div>
```

and, inside the existing `@if (ActiveDomain?.SupportsSeasonTiles == true)` block
([RecentObservationsPanel.razor:39-42](../../ClimateExplorer.Web.Client/Components/RecentObservations/RecentObservationsPanel.razor#L39-L42)),
the same shape for season/"4" — so the sub-button only appears where the season button itself
already does. The month sub-button has no such gate, matching the ungated month button today.

`DelayedTooltip` is the existing hover-tooltip component already used elsewhere on this panel
(e.g. the Adjusted checkbox and the reference-date reset button) — reused here rather than adding
a new tooltip mechanism, since a bare "12"/"4" label needs the hover text to explain what it does.

### Styling: `RecentObservationsPanel.razor.css`

New rules for the group container and its two children, modelled on the flush-caret look of
`.climate-dropdown-toggle` but as two real buttons instead of one button plus a pseudo-element:

```css
.climate-button-group {
    display: inline-flex;
}

.climate-button-group-primary {
    border-top-right-radius: 0;
    border-bottom-right-radius: 0;
}

.climate-button-group-suffix {
    border-left: 1px solid var(--climate-color-control-hover);
    border-top-left-radius: 0;
    border-bottom-left-radius: 0;
    min-width: 1.6rem;
    padding-left: 0.5rem;
    padding-right: 0.5rem;
    justify-content: center;
}
```

These are additive modifier classes layered on top of `ClimateButton`'s own `.climate-button`
class (via its existing `Class` parameter,
[ClimateButton.razor.cs:29-30](../../ClimateExplorer.Web.Client/Components/Common/ClimateButton.razor.cs#L29-L30)) —
no changes to `ClimateButton` itself are needed. The exact spacing/border values are a starting
point to eyeball once built (see Assumptions).

## Out of scope

- Day and year "Add earlier" buttons — the user asked for this only on month/season.
- Any change to `RecentObservationPeriodSelection`, the calculator, or the lookahead-buffer sizing
  in `CreateOptions` — the bulk buttons are pure UI-layer composition of existing single-step
  behaviour.
- A "remove all tiles" affordance exposed on its own — the clear-and-refill only happens as an
  internal step of the bulk-add click, not as a separately reachable action.

## Assumptions

1. The removal check is scoped to the active tab's `CurrentTiles`, not every loaded tab's tiles —
   consistent with `CurrentTiles` itself being tab-scoped everywhere else in this file, even though
   the resulting `RemoveTile`/`AddEarlierMonth` calls act on the shared `periodSelection` and so are
   still visible on other tabs, exactly as the existing single "Add earlier" buttons already are.
2. "Same kind" for the removal check means the button's own current/previous pair (e.g. Month
   sub-button treats `CurrentMonth` and `PreviousMonth` as one allowed set), not a stricter
   exact-enum-value match — confirmed by the user.
3. The bulk add tops up from wherever the selection currently stands rather than resetting to
   exactly 12/4 total tiles — confirmed by the user.
4. Sub-button visual styling (colours, border, min-width) is a starting point, sanity-checked
   visually by the user after implementation — per AGENTS.md, no dev server or browser testing is
   run as part of this plan's own verification (see [[feedback_no_playwright_or_dev_servers]]).
5. No new unit tests: the added logic (`ClearCurrentTilesUnlessAllMatch`, the two bulk-add loops)
   lives entirely in `RecentObservationsPanel.razor.cs`, and Blazor component code-behind isn't
   unit-tested in this repo (per AGENTS.md's Blazor-conventions/verification split, as already
   noted in [2026-08-18-01-multi-trend-chart-series-plan.md](2026-08-18-01-multi-trend-chart-series-plan.md#addendum-2--add-a-trend-to-an-existing-series-from-the-data-set-browser-2026-08-18)).
   Verification for this plan is `dotnet build` clean, nothing more.

## Addendum — implementation notes (2026-09-07)

Shipped exactly as planned, no deviations. `RecentObservationsPanel.razor.cs` gained the two
constants, the two period-kind arrays, `ClearCurrentTilesUnlessAllMatch`, `AddEarlierMonthsBulk`,
`AddEarlierSeasonsBulk`, and the two `AriaLabel` properties. `RecentObservationsPanel.razor` wraps
the month button and the season button (inside its existing `SupportsSeasonTiles` gate) each in a
`.climate-button-group` `div` with the new "12"/"4" `ClimateButton` wrapped in `DelayedTooltip`.
`RecentObservationsPanel.razor.css` gained the `.climate-button-group`/`-primary`/`-suffix` rules
as specified.

`dotnet build` on the full solution is clean (0 warnings/errors beyond pre-existing analyzer
warnings unrelated to this change), and the full unit suite passes at 579/579 — no new tests were
added, matching Assumption 5. No dev server or browser testing was run (AGENTS.md); the compound
button's visual layout, and the two behaviours themselves (clear-if-mixed, bulk top-up), are
unverified beyond compilation and code review.

### Bugfix 1 (2026-09-07): sub-button styling used un-scoped CSS

The `.climate-button-group-primary`/`-suffix` selectors in `RecentObservationsPanel.razor.css`
never matched anything: `ClimateButton` renders its own `<button>` from inside its own `.razor`
file, so Blazor's CSS isolation never stamps that element with `RecentObservationsPanel`'s scope
attribute, and a plain scoped selector can't reach past a child component's boundary. The result
was two fully-independent, fully-rounded buttons instead of one fused control. Fixed by adding
`::deep` to both selectors (`.climate-button-group ::deep .climate-button-group-primary`, and
likewise for `-suffix`), which drops the scope requirement on the right-hand selector so it matches
regardless of which component rendered the element. Also added `color: var(--climate-color-link)`
(hover: `--climate-color-link-hover`) to the suffix button, so the "12"/"4" numeral reads in a
visually distinct colour from the parent button's label, per user feedback after the first look.

### Bugfix 2 (2026-09-07): bulk add lost the current month/season as its starting point

`ClearCurrentTilesUnlessAllMatch` originally removed *every* currently visible tile once the
mixed-kind check failed, including the CurrentMonth/CurrentSeason "to date" singleton tile even
though its own `PeriodKind` is already inside `allowedKinds`. Once removed, that singleton is
recorded in `RecentObservationPeriodSelection`'s `removedSingletonPeriodKinds` set and stays hidden
indefinitely — nothing in the subsequent bulk-add loop ever re-shows it, since `AddEarlierMonth`/
`AddEarlierSeason` only grow the `PreviousMonth`/`PreviousSeason` offset sets, they don't touch
singleton visibility. The bulk add therefore produced 12 (or 4) *earlier* tiles with no current
one anchoring them — reported by the user, who also called out that this needs to keep working
when the reference date is the 1st of the month (or the first month of a season), where there's no
meaningful "to date" tile at all and `RecalculateTab`'s `EnsureDefaults` seeds a `PreviousMonth`/
`PreviousSeason` offset-1 tile as the visible stand-in instead
([RecentObservationPeriodSelection.cs:40-54](../../ClimateExplorer.Web.Client/UiModel/RecentObservations/RecentObservationPeriodSelection.cs#L40-L54)) —
that seeded substitute is just as vulnerable to being wiped by an unconditional clear.

**First attempt (superseded below):** narrowed the clear to skip tiles already matching
`allowedKinds`, on the theory that the CurrentMonth/CurrentSeason singleton (or its EnsureDefaults
offset-1 substitute) was always already visible going into the click, so leaving it alone would be
enough. The user then reported the real trigger: removing every tile *manually* first (each
already gone, matching or not) and only then clicking the bulk button. With nothing left to "leave
alone", the loop's first `AddEarlierMonth()` picked offset 1 (a full past month, e.g. August) as
the earliest addition — there is no path in `AddEarlierMonth`/`AddEarlierSeason` that ever adds the
*current* singleton, only earlier offsets, so the anchor was simply never asked for. The first
attempt's premise — "the anchor is already showing, so don't disturb it" — doesn't hold once the
user removes it themselves before clicking.

**Actual fix:** restored the clear step to remove everything unconditionally (matching the
original spec literally: remove all tiles if they're not already all month/season tiles), and
added a second, explicit step that puts the anchor back before the earlier-period loop runs. New
`RecentObservationPeriodSelection.EnsureVisible(RecentObservationPeriodKind kind)` un-hides a
singleton kind previously hidden by `Remove` (a no-op for offset-based kinds, which were never
tracked in `removedSingletonPeriodKinds` to begin with). New private `AddBulkTiles` helper in the
panel: checks whether the current-to-date period actually exists in this reference date's
calculation (`CurrentState.Result.Tiles.Any(tile => tile.PeriodKind == currentPeriodKind)` — this
reads the full computed tile list, not the filtered/visible `CurrentTiles`, so it still sees the
period even right after the clear removed it from view); if it exists, calls `EnsureVisible` on it
and asks the earlier-period loop for one fewer tile (11/3) so the total lands on 12/4; if it
doesn't exist (reference date is the 1st of the month, or the first month of a season),
`EnsureDefaults` has already seeded the `PreviousMonth`/`PreviousSeason` offset-1 tile as the
stand-in anchor, so the loop is asked for the full 12/4 and picks that seeded offset up as its
first addition. `AddEarlierMonthsBulk`/`AddEarlierSeasonsBulk` are now two-line wrappers calling
`ClearCurrentTilesUnlessAllMatch` then `AddBulkTiles` with their own period kind, count, and
add/disabled delegates.

`dotnet build` clean; unit suite still 579/579 (this logic isn't unit-tested, per Assumption 5). No
dev server or browser testing was run — reasoned from the tile-visibility model, not observed live,
same caveat as every other change in this doc.

## Addendum 3 — superseded by the period-model simplification (2026-09-07)

[Recent Observations period model: collapsing Current/Previous and the duplicated date-walking
code](2026-09-07-02-recent-observations-period-model-simplification-plan.md) rewrote the mechanics
this doc describes above, though not the user-facing behaviour: `RecentObservationPeriodKind`
merged `CurrentMonth`/`PreviousMonth` (etc.) into one `Month`/`Season`/`Year` kind with `PeriodOffset
0` meaning "to date"; `MonthPeriodKinds`/`SeasonPeriodKinds` and the `IReadOnlyCollection<kind>`
overload of `ClearCurrentTilesUnlessAllMatch` no longer exist (it now takes a single kind, since
each family is one kind); `EnsureVisible` is a one-line `SortedSet.Add(0)` instead of a
`HashSet.Remove` against a separate singleton-tracking set. The bulk-add buttons themselves - what
they look like, when they clear, how they anchor on the current month/season - are unchanged;
only the code underneath moved. See that doc's own addendum for what shipped.
