# Multiple trends per chart series

- **Date:** 2026-08-18
- **Status:** Implemented 2026-08-18 (see addenda)
- **Author:** Patrick Lea (with Claude)
- **Scope:** `ClimateExplorer.Web.Client` — `UiModel/ChartSeriesDefinition`, `UiModel/SeriesWithData`,
  `UiModel/Trends/*`, `Services/Chart/ChartDataBuilder`, `Services/Chart/ChartSeriesLocationSubstitutionService`,
  `Services/Trends/ChartSeriesTrendCalculator`, `Services/Trends/ChartSeriesTrendNotificationBuilder`,
  `UiLogic/ChartSeriesListSerializer`, `UiLogic/ChartLogic`, `UiLogic/ChartOptionsFactory`,
  `Components/Chart/ChartView*`, `Components/Chart/ChartSeriesView*`, `Components/Chart/ChartSeriesListView.razor.cs`,
  new `Components/Chart/Trend/ChartSeriesTrendControls.razor*`, `Components/Chart/Trend/ChartTrendPanel.razor*`.
  No changes to `ClimateExplorer.Core` regression internals.
- **Builds on:** [Chart series trend module](2026-08-11-01-chart-series-trend-module.md),
  [Polynomial regression for chart trends](2026-08-14-01-polynomial-regression-chart-trends.md)
- **Branch context:** `issues/tooltips-and-trends`

## Goal

Let a chart series carry **up to three** simultaneous trend projections instead of one — e.g. a
"Last 30 years" linear trend alongside a "Full period" quadratic trend on the same series, each
drawn in its own colour so they stay visually distinguishable. Trend #1 keeps *behaving* exactly
as it does today (same fitting, same rendering, same look) even though its storage changes — see
"Decisions" below. An **Add** button, sitting after the existing trend controls, appends a second
and third trend. Each additional trend is rendered in a colour derived from the series' own
colour, made darker (or lighter, if the series is drawn in black) — reviving the HSL transform the
previous trend-module plan specified and then dropped once a single trend was rendered in the
parent's own colour (see [that plan's Addendum
2](2026-08-11-01-chart-series-trend-module.md#addendum-2--rendering-and-default-window-revision-2026-08-12)).
With more than one trend, that same-colour choice stops being able to tell trends apart, so the
transform is needed again — now generalised to produce two distinct tiers, not one.

The trend controls (regression type, period, predict-until, "About this trend") are pulled out of
`ChartSeriesView` into their own reusable component so the same block can be instantiated once per
trend, and — in a later stage, sketched but not built here — reused a second time from the
existing "Add data set" modal, with a series-picker dropdown in front of it.

## Current state (recap)

Today a chart series has exactly one trend, expressed as five scalar fields directly on
`ChartSeriesDefinition`: `ShowTrend`, `RegressionType`, `TrendPeriod`, `TrendPredictionYears`,
`TrendPredictionTargetYear`. `ChartDataBuilder.ApplyTrends` fits it once per series via
`ChartSeriesTrendCalculator.Calculate` (all four windows — Full/Recent/RecentDecade/FirstHalf — at
the series' chosen degree), stores the single result on `SeriesWithData.Trend`, and
`ChartView.AddTrendDataSetsToChart` draws it as one extra Chart.js dataset per series, in the
series' own colour, as unconnected scatter points. `ChartSeriesView.razor` renders the "Show
trend" checkbox plus the regression-type/period/predict-until controls inline, and owns a single
`ChartTrendPanel` ("About trends") for that one trend.

## Decisions

### Unified model: one `Trends` list, replacing every scalar trend field

The trend module hasn't shipped — there's no shared URL, saved preset, or existing test asserting
today's five-field shape that needs to keep working. That removes the only reason to keep trend #1
special: a single `List<ChartSeriesTrendRequest> Trends` (0–3 entries) replaces `ShowTrend`,
`RegressionType`, `TrendPeriod`, `TrendPredictionYears` and `TrendPredictionTargetYear` outright.
`ShowTrend` disappears entirely — the module is "on" whenever `Trends.Count > 0`. First, second and
third trend are just entries in the same list, so `ChartDataBuilder`, the serializer and
`ChartSeriesView`'s markup can all iterate `Trends` uniformly, with **no first-slot special case
anywhere** — this is what makes the model simpler overall, not just shorter: the additive
alternative (keep the five fields, bolt on a second list for trends #2/#3) would have pushed that
asymmetry into a UI-layer adapter (see Phase 5) instead of removing it.

```csharp
namespace ClimateExplorer.Web.Client.UiModel.Trends;

public sealed class ChartSeriesTrendRequest
{
    public TrendRegressionType RegressionType { get; set; } = TrendRegressionType.Linear;
    public TrendWindow? TrendPeriod { get; set; }
    public int TrendPredictionYears { get; set; } = TrendPredictionRange.Default;
    public int? TrendPredictionTargetYear { get; set; }
}
```

A mutable class, matching `ChartSeriesDefinition`'s own style (not the immutable-record style used
for *derived* trend state like `ChartSeriesTrend`) — this is user intent, edited in place by the UI.

`ChartSeriesDefinition` gains `public List<ChartSeriesTrendRequest> Trends { get; set; } = [];` and
loses the five scalar fields. Every call site that reads or writes
`csd.ShowTrend`/`csd.RegressionType`/`csd.TrendPeriod`/`csd.TrendPredictionYears`/
`csd.TrendPredictionTargetYear` is mechanically rewritten to operate on `csd.Trends` — real churn
(`ChartDataBuilder`, both equality comparers, `ChartSeriesListSerializer`,
`ChartSeriesLocationSubstitutionService`, `ChartSeriesListView`, `ChartSeriesView` and its Razor
markup all touch it), but each site becomes a loop body over a list instead of five one-off field
references, so the total logic shrinks rather than just moving — the "ultimately simpler" trade
the additive design would have given up.

**No backward compatibility work.** The usual "add a new tolerant trailing URL segment, default
old links to off" pattern — used for every trend field added so far — is dropped for this change.
Segment 19 (today's `ShowTrend`) is repurposed to encode the whole `Trends` list, and segments
20–23 (today's `TrendPeriod`/`TrendPredictionYears`/`RegressionType`/`TrendPredictionTargetYear`)
are simply dropped, since nothing has shipped depending on their old meaning. See Phase 6.

**Cap of three**, enforced the same way regardless of model: the UI hides/disables "Add" once
`Trends.Count == 3`, and the URL parser defensively truncates to three entries if a hand-edited
link ever carries more.

### Each trend gets its own regression type, not one shared per series

Today one `RegressionType` governs all four windows for the whole series. Under the unified model
each entry in `Trends` carries its own — not collapsed back to one shared value now that there's a
list. It's a real capability, not just a side effect of the model change: it lets a user compare,
say, a linear fit against a quadratic fit over the same window on the same series.
`ChartSeriesTrendCalculator.Calculate` is called once per entry in `Trends` (up to three times per
series instead of once), each call still fitting all four windows at that entry's own degree —
unchanged in shape, just invoked per list entry instead of once per series.

### Trends may overlap; only the *default* pick for a freshly-added trend avoids collisions

Two trends showing the identical window at the identical degree would draw two coincident lines,
which is pointless — but two trends sharing a window at *different* degrees (the comparison case
above), or the same window/degree with a different "predict until" horizon, are legitimate and not
blocked. So there's no hard rule preventing a chosen period from colliding with another entry in
`Trends`. What does change: when the user clicks **Add**, the new entry's period starts unset
(auto-resolve, exactly like turning on trend #1 today), and the auto-resolve fallback prefers a
window **not already shown** by an earlier entry on the same series, so "Add" tends to surface
something new rather than a duplicate of what's already on the chart.
`ChartSeriesTrendCalculator.ResolveWindow` gains an optional exclusion set for exactly this,
applied only to the priority fallback — an explicit user selection (from the dropdown, or arriving
already set from a URL) is always honoured regardless of what other entries in `Trends` are
showing, matching today's rule that an explicit choice is never second-guessed.

```csharp
public static TrendWindow? ResolveWindow(
    IReadOnlyList<TrendWindow> significantWindows,
    TrendWindow? requestedWindow,
    IReadOnlySet<TrendWindow>? excludedFromDefault = null)
```

### Colour transform — revived, generalised to two tiers

Restated from the trend-module plan, with the saturation floor already dropped there (Addendum 2:
plain `s' = s * 0.55` keeps achromatic parents grey; a floor would have turned black into dark
red) — except saturation isn't touched at all here. The single-trend rendering (undecorated scatter
points) doesn't need desaturation to read correctly, and a multi-trend rendering needs its tiers to
stay just as legible as the base colour, not fade out. Only lightness moves:

```
tier 0 (trend #1): unchanged — the series' own colour, exactly as today.
tier 1 (trend #2), tier 2 (trend #3):
  h' = h                                        (hue unchanged — still reads as "this series")
  s' = s                                        (saturation unchanged)
  l' = l <= 0.18 ? min(l + 0.16·tier, 0.94)      (near-black parents go lighter)
                  : max(l - 0.16·tier, 0.06)     (everything else goes darker)
```

Implemented as `TrendSeriesColour.Derive(string parentHexColour, int tier) → string`, pure and
static (in `UiLogic`, alongside `ColourServer`), unit-tested for determinism, for tier-0
pass-through, and for both branches of the near-black case. The `0.16`/threshold `0.18` constants
are a starting point to sanity-check visually once built (per AGENTS.md, that check is manual and
out of the scope this doc's own verification can cover — see "Assumptions").

## Design

### Phase 1 — Data model

- New `UiModel/Trends/ChartSeriesTrendRequest.cs` (above).
- `ChartSeriesDefinition`: remove `ShowTrend`, `RegressionType`, `TrendPeriod`,
  `TrendPredictionYears`, `TrendPredictionTargetYear`; add
  `public List<ChartSeriesTrendRequest> Trends { get; set; } = [];`.
- Both equality comparers (`ChartSeriesDefinitionComparerWhichIgnoresYearAndIsLocked.BaseComparer`
  and `GetHashCode`, in both places at
  [ChartSeriesDefinition.cs:437-455](../../ClimateExplorer.Web.Client/UiModel/ChartSeriesDefinition.cs#L437-L455)
  and the two hash blocks): the five separate scalar-field checks/hash terms are removed and
  replaced with a single count check plus an index-by-index loop over `Trends` comparing all four
  fields per entry — the same pattern the existing `SourceSeriesSpecifications` loop already uses
  ([ChartSeriesDefinition.cs:492-516](../../ClimateExplorer.Web.Client/UiModel/ChartSeriesDefinition.cs#L492-L516)).
  Net fewer lines than today's five `if` blocks, and it scales to any number of trends without
  further edits. Omitting the loop would let `CreateNewListWithoutDuplicates` silently collapse two
  series that differ only by their trends.

**Files:** `UiModel/ChartSeriesDefinition.cs`, new `UiModel/Trends/ChartSeriesTrendRequest.cs`.

### Phase 2 — Colour transform

- New `UiLogic/TrendSeriesColour.cs`: `Derive(string parentHexColour, int tier)` as specified above.
  Parses the hex colour to HSL, applies the tier-0-passthrough/tier-N-shift rule, converts back.

**Files:** new `UiLogic/TrendSeriesColour.cs`.

### Phase 3 — Calculation and build wiring

- `ChartSeriesTrendCalculator.ResolveWindow` gains the optional `excludedFromDefault` parameter
  (above); `Calculate` itself is otherwise unchanged.
- `UiModel/SeriesWithData.Trend` (`ChartSeriesTrend?`) becomes `Trends`
  (`IReadOnlyList<ChartSeriesTrend>`, default `[]`) — one entry per `ChartSeriesDefinition.Trends`
  request that was attempted, in the same list order. Index alignment between a series' trend
  requests and its `Trends` results is load-bearing: it's how rendering (Phase 4) assigns a colour
  tier without needing a separate key.
- `ChartDataBuilder.ApplyTrends` (currently one `ChartSeriesTrendCalculator.Calculate` call per
  series) becomes a uniform loop per series over `csd.Trends`, up to three iterations, with no
  first-entry special case:
  - Each iteration builds its own `excludedFromDefault` set from the windows already resolved by
    earlier iterations on the same series.
  - Each iteration writes its resolved window back to `csd.Trends[i].TrendPeriod`, exactly as
    today's single write-back does.
  - Results are collected into `cs.Trends` in list order.
  - `ExtendBinsForProjections` changes from "the furthest projection across series" to "the
    furthest projection across every trend of every series" — a `SelectMany` over `Trends` instead
    of reading a single `Trend`.
- `ChartSeriesTrendNotificationBuilder.Build` is called once per entry in `Trends`, as today (up to
  three times per series instead of once). It gains an optional ordinal label so three
  notifications about one series don't read as duplicates of each other — e.g. the message is
  prefixed `"{series}, trend 2: "` instead of `"{series}: "` whenever a series has more than one
  trend; unchanged (no prefix) when it has exactly one, so the single-trend wording is
  byte-identical to today.

**Files:** `Services/Trends/ChartSeriesTrendCalculator.cs`, `Services/Trends/ChartSeriesTrendNotificationBuilder.cs`,
`Services/Chart/ChartDataBuilder.cs`, `UiModel/SeriesWithData.cs`.

### Phase 4 — Rendering

- `ChartView.AddTrendDataSetsToChart`: loop over `chartSeries.Trends` with its index as the colour
  tier (`TrendSeriesColour.Derive(csd.Colour!, tierIndex)`), building one Chart.js dataset per
  entry whose `Projection` is non-empty — same dataset shape as today
  (`ChartLogic.GetTrendChartDataset`), just parameterised by tier colour instead of always using
  `csd.Colour` directly.
- **Label disambiguation:** the existing label — `"{parent short title} | {period label} trend"` —
  is unchanged when a series has exactly one trend. When it has more than one, the regression type
  and predict-until year are folded in so two trends can never produce the same label even if they
  share a window: `"{parent short title} | {regression type} {period label} trend to {year}"`. Same
  rule for the tooltip label built via `ChartTooltipMetadataBuilder.BuildForTrendSeries`.
- `ChartOptionsFactory.CalculateAxisMinMax`: the single `swd.Trend?.Projection` fold-in
  ([ChartOptionsFactory.cs:51](../../ClimateExplorer.Web.Client/UiLogic/ChartOptionsFactory.cs#L51))
  becomes a loop over `swd.Trends`, folding in every trend's projection — otherwise a second or
  third trend that projects higher/lower than the first would be clipped at the axis edge.
- Click handling (`OnLineChartClicked`) and CSV export (`OnDownloadDataClicked`) both already guard
  by "is this a real series dataset or something appended after them" / "is this year past the end
  of real data" — neither cares how many trend datasets were appended, so neither needs a change.

**Files:** `Components/Chart/ChartView.razor.cs`, `UiLogic/ChartOptionsFactory.cs`, `UiLogic/ChartLogic.cs`.

### Phase 5 — The trend controls component, and `ChartSeriesView`

- New `Components/Chart/Trend/ChartSeriesTrendControls.razor` + `.razor.cs`: the regression-type
  select, period select, predict-until input + validation, and "About this trend" button + its own
  `ChartTrendPanel`, extracted verbatim from `ChartSeriesView.razor`/`.razor.cs`
  ([ChartSeriesView.razor:225-290](../../ClimateExplorer.Web.Client/Components/Chart/ChartSeriesView.razor#L225-L290)
  and the corresponding private members in the code-behind). Parameters: the `ChartSeriesTrendRequest`
  to bind directly (a real list element, not a proxy), the matching `ChartSeriesTrend?` result, an
  optional `SlotLabel` (`"Trend 2"` etc., shown only when the parent has more than one trend, next
  to the "About this trend" button's tooltip and the panel's title — see `ChartTrendPanel` below),
  a `CanRemove` flag, and `OnChanged`/`OnRemove` callbacks. This is the "trend component" referred
  to throughout this doc.
- `ChartSeriesView.razor`'s trend section becomes: if `ChartSeries.Trends.Count == 0`, a single
  **Add trend** button (`ChartSeries.Trends.Add(new ChartSeriesTrendRequest())`); otherwise, one
  `ChartSeriesTrendControls` per entry (`@foreach` over `ChartSeries.Trends` with its index), each
  with its own remove button (`ChartSeries.Trends.RemoveAt(i)`), followed by **Add trend** again
  while `Trends.Count < 3`. There's no promotion/reindexing logic to write and no "first entry is
  special" invariant to maintain — removing any entry, including the first, is just `RemoveAt`,
  because there are no scalar fields left for it to be special relative to.
- **UX change, called out explicitly:** this replaces the old single-trend design's "Show trend"
  *checkbox* with a plain **Add trend** *button*; the first, second and third trend are all added
  and removed the same way. With no scalar fields left to hang a checkbox on specifically for the
  first trend, a button reads more consistently across all three — but it is a visible change from
  today's checkbox, flagged here in case a dedicated first-trend checkbox is still wanted.
- `ChartTrendPanel` gains the optional `SlotLabel` parameter: its title reads "About trends" when
  absent (today's wording, for the common single-trend case) and "About trends — Trend 2" etc. when
  present.

**Files:** new `Components/Chart/Trend/ChartSeriesTrendControls.razor`, `.razor.cs`;
`Components/Chart/ChartSeriesView.razor`, `.razor.cs`; `Components/Chart/Trend/ChartTrendPanel.razor`, `.razor.cs`.

### Phase 6 — Persistence and propagation

- `ChartSeriesListSerializer`: segment 19 — today's `ShowTrend` — is repurposed to encode the whole
  `Trends` list, and segments 20–23 (today's `TrendPeriod`/`TrendPredictionYears`/`RegressionType`/
  `TrendPredictionTargetYear`) are dropped outright; nothing else follows them in the segment order,
  so there's nothing to shift. Encoded with the serializer's existing nested-separator scheme —
  level 2 (`|`) between entries, level 3 (`*`) between one entry's four fields — the same two
  separators `BuildSourceSeriesSpecificationsUrlComponent` already uses for its own nested list:
  `regressionType*trendPeriod*trendPredictionYears*trendPredictionTargetYear|regressionType*...`.
  Each entry is parsed and clamped the same way the old fields were
  (`TrendPredictionRange.Clamp`, `TryParse`-or-default), and the whole list is truncated to at most
  three entries defensively. An empty or missing segment 19 parses to `[]` (module off). Because
  nothing has shipped depending on the old five-field/five-segment shape (see Decisions), this is a
  straight replacement of segment 19's meaning rather than the "keep the old segment, add a new
  tolerant one after it" pattern the trend fields used up to now — and any existing serializer
  tests asserting the old segment layout are updated in place, not preserved.
- `ChartSeriesLocationSubstitutionService` (around the trend-field copy block): deep-copy `Trends`
  (new `ChartSeriesTrendRequest` instances with the same field values, not the same list/instances)
  in place of the five scalar-field copies, so switching location keeps all of a series' trends and
  each is refitted against the new location's data.
- `ChartSeriesListView.OnDuplicateSeries` ([ChartSeriesListView.razor.cs:83-87](../../ClimateExplorer.Web.Client/Components/Chart/ChartSeriesListView.razor.cs#L83-L87)):
  same deep-copy, for the same reason "Clone" carries every other setting today.
- `ChartSeriesListView.GetTrend` becomes `GetTrends`, returning `IReadOnlyList<ChartSeriesTrend>`
  (`SeriesWithData.Trends`, or `[]` when the series isn't found) — one caller-side rename following
  Phase 3's `SeriesWithData` change.

**Files:** `UiLogic/ChartSeriesListSerializer.cs`, `Services/Chart/ChartSeriesLocationSubstitutionService.cs`,
`Components/Chart/ChartSeriesListView.razor.cs`.

### Phase 7 — Tests

New/updated tests in `ClimateExplorer.UnitTests`, names per AGENTS.md:

- `TrendSeriesColourTests` — tier 0 returns the input unchanged; tier 1/2 progressively darken a
  light/mid-lightness colour and progressively lighten a near-black one; hue and saturation are
  preserved in both cases; determinism (same input/tier always produces the same output).
- `ChartSeriesTrendCalculatorTests` — new cases for `ResolveWindow`'s `excludedFromDefault`: the
  priority fallback skips an excluded window in favour of the next one; an explicit
  `requestedWindow` is still honoured even when it's in the excluded set.
- `ChartSeriesListSerializerTrendTests` — updated for the new segment 19 shape: round-trip of
  0/1/2/3-entry `Trends` lists; a hand-edited URL with more than three entries truncates to three;
  per-entry clamping matches the old single-trend behaviour. The previous "legacy ≤24-segment URL"
  compatibility case is removed, since there's no old shape left to be compatible with.
- New `ChartSeriesDefinitionEqualityTests` — two definitions differing only in `Trends` (different
  count, or same count but one field different on one entry) are unequal under both comparers;
  identical `Trends` contents compare equal. (No prior test file covered the comparers directly —
  this is new coverage for genuinely new collection-equality logic, not a gap reopened from an
  earlier plan.)
- `ChartSeriesTrendNotificationBuilderTests` — the ordinal prefix appears only when a series has
  more than one trend, and is absent (byte-identical to today's wording) for a single one.
- Existing `TrendWindowCalculatorTests` and the Recent Observations trend tests must pass unchanged
  — this plan doesn't touch the shared windows/significance machinery, only how many times per
  series it's invoked. Existing `ChartSeriesTrendCalculatorTests` cases that construct a
  `ChartSeriesDefinition` with the old scalar fields are updated mechanically to use `Trends`.

Verification is `dotnet build` plus the unit test suite only — no dev server, no browser tests
(AGENTS.md). The colour tiers' actual visual legibility (the `0.16`/`0.18` constants) is the one
thing this doc can't verify that way; flag it for a manual look once built, same caveat the
trend-module plan's own Addendum 2 already accepted for the single-trend rendering.

## Out of scope

- **The "Add data set" modal entry point.** Sketched below as a later stage; not built in this
  plan. `ChartSeriesTrendControls` is designed to be reusable there without changes — the modal
  side only needs to add a chart-series picker in front of it — but wiring that up, and deciding
  how a trend added from the modal targets an existing series versus creates one, is separate work.
- More than three trends per series.
- Changing the four fixed windows (Full/Recent/RecentDecade/FirstHalf), the significance rule, or
  the 60-year minimum.
- Rendering the prediction-interval band (already out of scope in the original trend-module plan;
  unaffected by this one).
- Retiring `ShowTrendline` ("Show fitted line").

## Later stage — trend controls from "Add data set" (sketch only)

The user asked for this to be flagged, not designed in full. The shape it would take: the "Add
data set" modal ([Global.razor:41](../../ClimateExplorer.Web.Client/Pages/Global.razor#L41) and its
`Index.razor` counterpart) gains a second mode — "Add a trend to an existing series" — with a
`Select` populated from the current `ChartSeriesList`, and, once a series is chosen, an instance of
`ChartSeriesTrendControls` bound to a new entry appended to that series' `Trends` list (subject to
the same three-trend cap). This reuses everything from Phases 1–5 unchanged; the only new work is
the modal's own layout and the series picker.

## Assumptions

1. `alpha` stays at the calculator's `0.05` default throughout, per trend, unchanged from today.
2. The 60-point minimum and the four window definitions are unchanged; this plan only changes how
   many times they're applied per series.
3. A trend's regression type is independent per entry in `Trends` (see Decisions) rather than
   shared across a series' trends — a deliberate capability, not an oversight.
4. The colour transform's constants (`0.16` lightness step per tier, `0.18` near-black threshold)
   are a starting point; AGENTS.md's no-dev-server rule means they're sanity-checked visually by
   the user after implementation, not provable by a unit test beyond "it moves lightness in the
   right direction by the right amount."
5. Switching the "Show trend" checkbox to an "Add trend" button (Phase 5) is accepted as part of
   this change — with no scalar fields left, there's no natural place left to hang a checkbox
   specifically for the first trend. If that's not wanted, it's a small adjustment to Phase 5 alone
   and doesn't affect Phases 1–4 or 6–7.
6. No backward compatibility for the pre-existing single-trend URL segments/fields is required —
   confirmed by the user; the trend module has not shipped to any real user yet.

## Addendum 1 — implementation notes (2026-08-18)

Shipped as planned, Phases 1-7. `dotnet build` on the solution is clean and the full unit suite
passes at **538 tests** (18 new: 7 in `TrendSeriesColourTests`, 3 new `ResolveWindow` exclusion
cases in `ChartSeriesTrendCalculatorTests`, 6 new/rewritten cases in
`ChartSeriesListSerializerTrendTests`, 2 ordinal-prefix cases in
`ChartSeriesTrendNotificationBuilderTests`, and a new `ChartSeriesDefinitionEqualityTests` file
with 6 cases). No dev server or browser testing was run, per AGENTS.md.

### What shipped, matching the plan

- `ChartSeriesTrendRequest` (`UiModel/Trends/`) and `ChartSeriesDefinition.Trends` (`List<>`,
  default `[]`, capped at `ChartSeriesDefinition.MaxTrends = 3`) replaced the five scalar trend
  fields exactly as decided. Both equality comparers now do a count check plus an index-by-index
  loop over `Trends`, mirroring the existing `SourceSeriesSpecifications` pattern.
- `TrendSeriesColour.Derive(parentHex, tier)` (`UiLogic/`) implements the HSL lightness-shift
  transform as specified - tier 0 passthrough, tier 1/2 darker or (for near-black parents)
  lighter, hue and saturation untouched.
- `ChartSeriesTrendCalculator.ResolveWindow` gained `excludedFromDefault`, with one refinement
  beyond the plan: if excluding already-shown windows would leave *no* default candidate even
  though a significant window exists, it falls back to allowing a repeat rather than reporting
  "no significant trend". `Calculate` threads the same parameter through to it. `ChartDataBuilder.ApplyTrends`
  loops uniformly over `csd.Trends`, building the exclusion set from windows already resolved
  earlier in the same series' loop, and writes each resolved window back to its own request.
- `SeriesWithData.Trend` became `Trends` (`IReadOnlyList<ChartSeriesTrend>`), index-aligned with
  `ChartSeriesDefinition.Trends`. `ChartView.AddTrendDataSetsToChart` uses the index as the colour
  tier and builds the trend legend/tooltip label with the planned disambiguation (regression type
  + "trend to {year}") only once a series has more than one trend. `ChartOptionsFactory.CalculateAxisMinMax`
  folds in every trend's projection, not just one.
- `ChartSeriesTrendNotificationBuilder.Build` gained `trendOrdinal` (int?), prefixing
  `", trend {n}"` only when a series has more than one trend - byte-identical wording otherwise.
- `ChartSeriesTrendControls.razor`/`.razor.cs`/`.razor.css` (`Components/Chart/Trend/`) is the
  extracted trend component - regression type, period, predict-until, and its own embedded "About
  this trend" icon button + `ChartTrendPanel`, plus a "remove this trend" icon button. Its own
  `.razor.css` duplicates the `.form-row`/`.form-label`/`.form-control-wrap` rules from
  `ChartSeriesView.razor.css`, since Blazor CSS isolation doesn't cascade a parent's scoped styles
  into a child component - the same reason `AggregationOptions.razor.css` keeps its own copy.
  `ChartTrendPanel` gained the optional `SlotLabel` parameter, changing its title to "About trends
  — Trend 2" etc. only when set.
- `ChartSeriesView`'s trend section is now a loop over `ChartSeries.Trends` rendering one
  `ChartSeriesTrendControls` per entry (each capturing its own loop-local `index`), followed by an
  "Add trend" button while `Trends.Count < 3`. The old "Show trend" checkbox is gone, per the
  planned UX change - an empty `Trends` list just means the loop renders nothing and only the "Add
  trend" button shows.
- Persistence: segment 19 (`ShowTrend`'s old slot) now holds the whole encoded `Trends` list,
  reusing the serializer's existing `|`/`*` nested-separator levels; segments 20-23 are gone.
  `ChartSeriesTrendRequest.Clone()` is the shared deep-copy used by both
  `ChartSeriesLocationSubstitutionService` and `ChartSeriesListView.OnDuplicateSeries`, avoiding a
  duplicated copy helper across the two call sites (not explicitly planned, but a direct
  consequence of "duplicate this state in two places" during implementation).
- The three `SuggestedPresetLists.*.cs` files carrying trend-enabled presets ("Atmospheric CO₂ vs
  emissions" and the two "recent decade" location presets) were updated to construct a
  `ChartSeriesTrendRequest` inside `Trends` instead of setting the old scalar fields.

### Verification

`dotnet build` clean; 538/538 tests pass. No dev server or browser testing was run (AGENTS.md) -
the "Add trend"/"remove trend" UI flow and the colour tiers' visual legibility are unverified
beyond compilation and the unit-tested logic behind them, same caveat the trend-module plan's own
addenda already carried for rendering changes.

### Not done (as scoped)

The "Add data set" modal entry point remains a later-stage sketch only, per "Out of scope".

## Addendum 2 — "add a trend to an existing series" from the data set browser (2026-08-18)

Built the "Later stage" sketch above, with two corrections made during implementation after the
user reviewed the first pass:

- **Not a new nested `SidePanel`.** `DataSetBrowser` *is* the "Add data set" side panel already
  (opened from `ChartablePage.AddDataSetSidePanel`) - the feature is a section added inline inside
  it (behind a collapsed-by-default `Collapsible` titled "Add a trend"), not a second panel opened
  from within the first.
- **Local and Global each need their own picker, cross-filtered.** A chart can carry both
  location-tied series (temperature at the current location) and region-tied "global" series (CO2,
  sea ice extent) at once, since both tabs are reachable from the same location page. The Local
  tab's picker only offers location-tied series; the Global tab's only offers region-tied ones -
  each excludes the other's kind, rather than one shared picker listing everything.

New `ChartSeriesDefinition.IsGlobalSeries` (`UiModel/ChartSeriesDefinition.cs`) and
`Region.IsRegionId` (`ClimateExplorer.Core/Model/Region.cs`) do the classification: a series is
global when every one of its `SourceSeriesSpecifications` points at a region ID rather than a real
location's ID. New `Components/Chart/DataSetBrowser/AddTrendSection.razor`/`.razor.cs`/`.razor.css`
is the shared picker + "Add trend" button + (once added) an embedded `ChartSeriesTrendControls` for
the newly-added entry, reused once each by `LocalDataSetBrowser` (filtered to
`!IsGlobalSeries`) and `GlobalDataSetBrowser` (filtered to `IsGlobalSeries`) - both further filtered
to series with data, a year x-axis, and not already at the three-trend cap. `ChartablePage` gained
`AllChartSeriesWithData` (the same `SeriesWithData ++ NonRenderedSeriesWithData` union `ChartView`
builds internally, needed here too so the newly-added trend's significant windows show up once the
rebuild it triggers completes) and `OnTrendsChanged` (rebuilds against `CurrentChartState`, since
the mutation already happened in place on the shared `ChartSeriesDefinition` instance - same
pattern `ChartSeriesView`'s own trend controls use, just from a different entry point). `Index.razor`
and `Global.razor` wire both through to `DataSetBrowser`.

`dotnet build` clean; unit suite at 545/545 (7 new: 5 `ChartSeriesDefinitionIsGlobalSeriesTests`, 2
`RegionIsRegionIdTests`). No dev server or browser testing, per AGENTS.md - the picker/"Add
trend"/inline-controls UI flow is unverified beyond compilation, same caveat as Addendum 1.

### Bugfix (2026-08-18): "pending trend" local state was wrong

The first pass tracked "the trend I just added" as a single `PendingSeries`/`PendingTrendIndex`
pair of fields local to `AddTrendSection`, shown instead of the "Add trend" button only for that
one freshly-added entry. The user found two bugs from this, both the same root cause: that pending
state is ephemeral (reset on selection change, and gone entirely on remount) while the real
`Trends` list on the series is not.

- Reopening the side panel (or re-selecting a series) after adding a trend showed only the "Add
  trend" button again, with no sign the series already had trends - either from an earlier visit to
  this panel, or added via the series' own controls below the chart.
- Adding a third trend to a series could make its controls vanish instead of appearing: once a
  series hit the three-trend cap, the picker's own eligibility filter (`Trends.Count < MaxTrends`)
  dropped it from `EligibleSeries`; if it had been the only eligible series, `EligibleSeries.Count`
  went to zero and the whole picker branch - including the just-added trend's controls, which don't
  depend on `EligibleSeries` - stopped rendering, replaced by the empty-state message.

Fixed by dropping the pending-state tracking entirely: `AddTrendSection` now loops over
`SelectedSeries.Trends` directly and renders one `ChartSeriesTrendControls` per entry (with its own
remove button), exactly the pattern `ChartSeriesView` already uses for the same list - always in
sync with what's really on the series, whichever way it got there. The picker's eligibility filter
(`LocalTrendEligibleSeries`/`GlobalTrendEligibleSeries`) no longer excludes series at the cap
either, so a maxed-out series stays pickable and its trends stay visible/removable; the cap is
enforced only by hiding `AddTrendSection`'s own "Add trend" button once the *selected* series
reaches it - unaffected by how many other series are on the chart.

No test coverage added for this - it's pure Razor rendering logic (Blazor components aren't unit
tested in this repo, per AGENTS.md's Blazor-conventions/verification split). `dotnet build` clean;
545/545 unit tests still pass (the shape of the fix didn't touch anything test-covered). Still no
dev server or browser testing - the fix is reasoned from the render logic, not observed live.
