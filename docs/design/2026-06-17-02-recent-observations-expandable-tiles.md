# Recent Observations: expandable tiles with Period / Daily Extremes metric groups

- **Date:** 2026-06-17
- **Status:** Implemented 2026-06-17 (see addendum)
- **Author:** Patrick Lea (with Claude Code)
- **Scope:** `RecentObservationsService`, Recent Observation tile UI, `RecentObservationComparison`
- **Builds on:** [2026-06-17-01-recent-observations-metric-architecture.md](2026-06-17-01-recent-observations-metric-architecture.md)
- **Branch context:** `issues/recent-ghcnd`

## Goal

Each tile gains a collapsed-by-default expander. Expanded, it shows a two-state
toggle — **Period** (default) and **Daily Extremes** — surfacing additional
statistics per period. Period model is fixed; this is purely additive *statistics*
work, implemented on the metric architecture from the prior refactor. The existing
summary, rankings, and look-and-feel are preserved unchanged.

## Constraints (from the brief)

- Collapsed by default; each tile expands independently; multiple open at once.
- Expansion is **UI-only state** — no data reloads. Tiles are keyed by
  `GetTileKey` in the panel, so component-local state survives the per-render
  `ApplyCompletenessThreshold` re-creation of view models.
- All historical/metric computation happens **once in the service** and is baked
  into the view model; the component only shows/hides already-computed data.
- Chevron: bottom-right, matching the app's Open Iconic chevron (see note).
- No redesign; preserve visual hierarchy. Mobile grows vertically; desktop may use
  a wider expanded layout when space allows.

## Architecture: metric groups

A **metric group** is a named, ordered list of metrics rendered as one toggle
state. Adding a future group = add a `MetricGroup` to a domain; the UI renders
whatever groups exist. No period-specific or view-specific branching.

`Metric` (service-internal) gains two fields:

```csharp
private sealed record Metric(
    string Key,
    string SingularLabel,   // summary stat label (existing)
    string PluralLabel,     // summary stat label (existing)
    Func<DailyObservation, double?> Select,
    MetricAggregation Aggregation,   // Mean | Sum | Max | Min
    Func<double, string> Format,
    string DetailLabel,                          // expanded-view label
    RecentObservationRecordDirection RecordDirection);  // High | Low
```

Domains declare groups:

| Domain | Period group | Daily Extremes group |
|---|---|---|
| Temperature | Average maximum (`Mean` of `Max`), Average minimum (`Mean` of `Min`), Mean temperature (`Mean` of `Mean`) | Highest daily maximum (`Max` of `Max`), Lowest daily maximum (`Min` of `Max`), Highest daily minimum (`Max` of `Min`), Lowest daily minimum (`Min` of `Min`) |
| Precipitation | Total precipitation (`Sum` of `Rainfall`) | Highest daily rainfall (`Max` of `Rainfall`) |

Every metric is defined as `(Select daily field, Aggregation)`. The **same**
definition computes the current-period value *and* each equivalent historical
period's value — so a metric is comparable for free.

`MetricDomain` gains `IReadOnlyList<MetricGroup> Groups`; `AllMetrics` becomes the
distinct union of primary + supporting + all group metrics (drives both
`ComputeMetrics` current values and the historical distribution pass).

## Historical comparison: one grouping pass, many metrics

The prior refactor left this latent; now we activate it. `GetHistoricalDistributions`
groups the equivalent historical years **once per period**, then computes every
metric's aggregate inside those cached groups, returning
`IReadOnlyDictionary<string, HistoricalValues>` keyed by metric. The summary tile
consumes `distributions[Primary.Key]` (identical to today's single-metric result —
verified output-identical against existing tests); the expanded views consume the
rest. No per-metric or per-render re-scan of history.

Temperature historical series now sources **TMax + TMin** (deriving
`mean = (max+min)/2`, consistent with how recent daily mean is computed), falling
back to `TempMean` only when max/min are unavailable. This is required so daily
max/min extremes have history; it keeps existing tests green (their mocks expose
only `TempMean`, so the fallback path is taken and the mean distribution is
unchanged).

## Record detection (Core, unit-testable)

Added to `ClimateExplorer.Core.Calculators`:

```csharp
public enum RecentObservationRecordDirection { High, Low }
public enum RecentObservationRecordStatus { None, NewRecord, EqualRecord, BelowRecord }

public static RecentObservationRecordStatus RecentObservationComparison.DetermineRecordStatus(
    RecentObservationComparisonResult ranking, RecentObservationRecordDirection direction);
```

`High`: `NewRecord` if `IsNewHighRecord`; `EqualRecord` if tied at rank 1; else
`BelowRecord`. `Low`: symmetric on the low flags. The existing `Rank` already
produces all flags, ranks, percentiles, and `HistoricalMax/Min`; detection is a
pure mapping over it, fully testable without the service.

Per-metric record direction:
- Daily Extremes: `Highest *` → `High`; `Lowest *` → `Low`.
- Period temperature + precipitation: `High` (record = highest on record). Rank /
  comparison text is direction-aware, so cold/dry anomalies are still conveyed.
  **Product assumption** — average-minimum's "record" is treated as the warmest
  average minimum; flip one `RecordDirection` field if the coldest is wanted.

## View models (UiModel)

```csharp
public enum RecentObservationRecordStatus { ... }   // re-exported from Core mapping

public sealed record RecentObservationMetricViewModel
{
    public string Label { get; init; }
    public string CurrentValue { get; init; }
    public string? RecordValue { get; init; }       // historical record (formatted)
    public string? RecordYear { get; init; }
    public RecentObservationRecordStatus RecordStatus { get; init; }
    public string? RecordStatusText { get; init; }   // "New record" / "Equal record" (not below; not notable)
    public string? RankText { get; init; }           // e.g. "3rd highest of 27" (when available)
}

public sealed record RecentObservationMetricGroupViewModel
{
    public string Key { get; init; }     // "period" | "daily-extremes"
    public string Title { get; init; }   // "Period" | "Daily Extremes"
    public IReadOnlyList<RecentObservationMetricViewModel> Metrics { get; init; } = [];
}
```

`RecentObservationTileViewModel` gains
`IReadOnlyList<RecentObservationMetricGroupViewModel> MetricGroups`.
`ApplyCompletenessThreshold`, when it suppresses a comparison, also strips the
record/rank fields from the expanded metrics (keeps current values) so we never
show comparisons we deemed below the completeness bar.

Expansion state is **not** in the view model. A small testable
`RecentObservationTileExpansionState` (UI-only) holds `IsExpanded`, the selected
group key, `Toggle()`, `SelectGroup(key)`, `IsGroupSelected(key)`, and the chevron
CSS state; the component owns one instance.

## Chevron

`.C` (Collapsible's expanded-state class) is **undefined** in the app's CSS, so
Collapsible currently renders `oi-chevron-right` when collapsed and *no glyph*
when expanded. To stay within the same icon set and colour while giving a usable
affordance in the bottom-right corner, the tile uses the same Open Iconic
`oi oi-chevron-right` glyph, same colour (`#595959`, hover `#0071c1`) and size
(`1.2rem`) as `.collapser`, and **rotates it 90°** on expand ("same icon, same
size, same colour, rotation behaviour"). No new chevron style is introduced.

## Layout

Expanded content renders beneath the summary inside the tile `<article>`. Mobile:
vertical stack, current tile width preserved. Desktop (`min-width: 641px`): the
metric list may use a 2-column grid (reusing the existing
`.recent-observation-stats` grid idiom) when room allows. Toggle is a small
segmented control reusing existing tile typography/colours. No hierarchy change.

## Implementation steps

1. **Core** — record-detection enums + `DetermineRecordStatus` (+ tests).
2. **UiModel** — metric/group view models, expansion-state class, extend tile VM
   and `ApplyCompletenessThreshold`.
3. **Service** — extend `Metric`, define groups, distinct `AllMetrics`,
   `GetHistoricalDistributions` (single-pass multi-metric), build `MetricGroups`
   per tile; route summary through `distributions[Primary.Key]`.
4. **Razor + CSS** — expander, chevron, toggle, expanded content.
5. **Tests** — service-level metric/record/historical tests (temp + precip),
   expansion-state tests, and assert no regression of existing summary tiles.

## Testing approach

- **Correctness** (MSTest, existing project): record detection (Core);
  service-produced `MetricGroups` — labels, current values, record value/year,
  record status (new/equal/below), rank text; temperature and precipitation;
  historical record calculations via mocked TMax/TMin and precipitation history.
- **Expansion logic** (MSTest): `RecentObservationTileExpansionState` —
  collapse-by-default, independent toggle, group selection, chevron state.
- **No-regression**: the existing 48 `RecentObservations*` tests must stay green
  and output-identical for the summary fields.
- **Rendering** (mobile/desktop, chevron visuals): belongs in the Playwright
  `Web.UiTests` project; not added here (needs a running app + browser). Noted as
  follow-up rather than introducing bUnit into an MSTest/Playwright repo.

## Out of scope

- New period types (period model is complete).
- Redesign of the summary tile or panel.
- bUnit adoption (kept to existing MSTest + Playwright tooling).

## Addendum — implementation notes (2026-06-17)

Implemented across Core, UiModel, the service, and the tile component. Full suite:
**178 tests green** (160 pre-existing unchanged + 18 new); full solution builds clean.
The existing summary tiles remain output-identical (the new multi-metric extractor
returns the primary distribution identically to the old single-metric one).

As-built specifics and decisions:

- **Single-pass extractor activated.** `GetHistoricalDistributions` groups
  equivalent years once and computes every metric inside the cached groups,
  returning `IReadOnlyDictionary<string, HistoricalValues>`. Summary consumes
  `[Primary.Key]`; expanded views consume the rest. No per-render or per-metric
  re-scan.
- **Temperature history now sources TMax + TMin first** (deriving mean), falling
  back to `TempMean`. Required for daily-extreme history; existing tests expose
  only `TempMean` so they exercise the fallback and stay green.
- **Record direction (product assumption).** Daily extremes use High for
  "Highest *" and Low for "Lowest *". Period temperature + precipitation metrics
  use High (record = highest on record); direction-aware rank text still conveys
  cold/dry anomalies. Average-minimum's "record" is therefore the *warmest*
  average minimum — flip the single `RecordDirection` on
  `AverageMinTemperatureMetric` if the coldest is wanted.
- **Chevron.** Reuses Open Iconic `oi oi-chevron-right` at `1.2rem`, colour
  `#595959` / hover `#0071c1` (matching `.collapser`), rotated 90° on expand. The
  tile reserves bottom padding so the absolutely-positioned chevron sits in the
  bottom-right corner without overlapping content. (`.C`, Collapsible's expanded
  class, is undefined app-wide and renders no glyph — not reused.)
- **Expansion state** lives in a UI-only `RecentObservationTileExpansionState`
  held by the component; tiles are keyed by `GetTileKey`, so state survives the
  per-render `ApplyCompletenessThreshold` re-creation with no reload.
- **Completeness suppression** also strips record/rank from the expanded metrics
  (current values retained), so we never show comparisons below the bar.
- **Rank text** ("3rd highest of 27") is rendered for every ranked metric in both
  groups, not just Period — consistent and within the brief's "rank and comparison".

### Follow-ups (not done)

- Playwright `Web.UiTests` coverage for the actual expand/collapse rendering,
  chevron rotation, and mobile/desktop layout (needs a running app + browser).
- Revisit per-metric `RecordDirection` for period averages if product wants
  cold-side records surfaced explicitly rather than via rank text.
