# Since-1961 trend and heating score

- **Date:** 2026-11-12
- **Status:** Stage 1 implemented 2026-11-12 (build and unit tests clean); dry run done, see the
  [addendum](#addendum-stage-1-dry-run-2026-11-12). Stage 2 implemented 2026-11-13 (build and unit
  tests clean, UI unverified; see [Stage 2 notes](#stage-2-notes-2026-11-13)). Stage 3 implemented
  2026-11-13 (build and unit tests clean, UI unverified; see [Stage 3 notes](#stage-3-notes-2026-11-13)).
  Stage 4 implemented 2026-11-13 (build and unit tests clean, UI unverified; see
  [Stage 4 notes](#stage-4-notes-2026-11-13)). Stage 5 implemented 2026-11-13 (build and unit tests
  clean, UI unverified; see [Stage 5 notes](#stage-5-notes-2026-11-13)). All stages done
- **Author:** Patrick (with Claude)
- **Scope:** `ClimateExplorer.Core` (trend windows, `Location.SetHeatingScores`, `HeatingScoreRow`),
  `ClimateExplorer.WebApi` (location cache), `ClimateExplorer.Web.Client` (location dashboard trend field
  and modal, heating score info, Locations page, Recent Observations trend tab, chart trends, presets)
- **Builds on:** [Heating score: trend vs warming anomaly](2026-11-12-02-heating-score-trend-vs-anomaly.md)
- **Branch context:** written on `development`

## Goal

Add a fourth trend window, **Since 1961**: an OLS slope over the complete years from 1961 to the last
year. Use it everywhere trends appear, and rank the heating score on it instead of on the warming
anomaly. The research note explains the choice: it's the same period for every station, so it
doesn't depend on record length, and it's about three times less noisy than the last-30-years trend.
1961 is the start of the WMO reference period.

## Decisions

| Question | Decision |
|---|---|
| Heating score basis | Since-1961 slope, ranked into deciles 9–0 as now |
| Negative scores | A negative since-1961 slope scores **−1**. *Chosen: any negative slope, matching the info page's "highlight any location that has not warmed". Scoring only significant negatives as −1 (others 0) is a one-line change if you prefer it.* |
| Trends stored on `Location` | Full, Last30 and Since1961. Full stays: `MapMarkerZoomTiers.GetRecordLength` uses its `FirstYear`, and the dashboard tooltip shows it |
| Dashboard trend field value | Last 30 years if significant, otherwise Since 1961 if significant, otherwise Full period if significant, otherwise nothing. *Changed in Stage 3: dropping the Full fallback would have blanked the field for 621 locations* |
| Dashboard trend tooltip | Lists every significant trend: Last 30 years, Since 1961, Full period |
| Recent Observations tile | Last 30 years (headline), Since 1961, Early period |
| Recent Observations About trends | Window toggle: Last 30 years, Since 1961, Early period, Full period. Default is Last 30 years, matching the tile headline |
| Chart | Since 1961 becomes a trend window choice for any yearly series |
| Chart: adding a trend | A trend added by clicking defaults to **Since 1961**, falling back to the other windows when it isn't available or significant |
| Presets | Unchanged: they keep their explicit `TrendPeriod`s (mostly Full) |
| Rate units | As now: everything users see is per decade (dashboard field and tooltip, heating score info and table, Locations page, Recent Observations tile, chart). Only the statistics table in About trends (`TrendStatSectionBuilder`) shows per year, and the Since 1961 tab follows it unchanged. Stored data stays per year (`TrendSummary.Slope`), except the heating score bands, which are stored per decade because they're only ever displayed |
| Window name | `TrendWindow.Since1961`, labelled "Since 1961". The URL serialises the enum by name, so adding a value doesn't break links |

## The Since-1961 window

All three trend code paths (location trends, chart, Recent Observations) use one definition. It goes
in `TrendDefaults` next to the existing windows:

- `FixedPeriodStartYear = 1961`.
- **Points:** the complete years with year ≥ 1961.
- **Fitted only when** the first complete year in the window falls within the first
  `FixedPeriodStartToleranceYears` years (proposed: 5, the start of 1961 to the end of 1965, so
  `firstYear < FixedPeriodStartYear + FixedPeriodStartToleranceYears`), and the window has at
  least `MinimumYearsForTrend` (30) complete years. The start check stops a series that begins in 1990
  from getting a "since 1961" trend that is really a full-period trend. The 30-year floor is the rule
  every other trend uses.
- **Shared helper:** `TrendWindows.GetSince1961Points(orderedPoints)` returns the window, or null when
  the window isn't eligible. `TemperatureTrendCalculator.GetWindowPoints`,
  `ChartSeriesTrendCalculator` and `TrendWindowCalculator` all call it, so the rule lives in one place.

Two thresholds are proposals for Stage 1's dry run to confirm: the start tolerance, and whether the
heating score should need more than 30 years in the window.

The heating score also needs the window to be recent: **the last year must be within 10 years of the
newest last year across all locations** (2015 or later today). Without this, a station that closed in
1995 would be ranked on 1961–1995. That's 24 locations today with a last year before 2018. This rule
is for the score only; the trend itself is still shown for those stations.

## Stages

Each stage builds and passes the unit tests on its own, and can ship on its own.

### Stage 1: calculate and store the Since-1961 trend

No visible change apart from the new window existing.

- `TrendWindow`: add `Since1961`.
- `TrendDefaults`: the constants above. The shared window helper goes here or in a small `TrendWindows`
  class in `Core/Stats`.
- `TemperatureTrendFits`: add `Since1961` (nullable). `TemperatureTrendCalculator.Fit` fits it when
  eligible, `GetWindowPoints` handles it, and `Summarise` adds its `TrendSummary` after Full and Last30.
- `Location`: add a `Since1961TemperatureTrend` accessor like the other two.
- `TrendWindowLabel`: add "Since 1961" and the description "since 1961, the start of the WMO
  reference period". Both switches throw on an unknown window, so this has to go in the same stage.
- Update the `CachingTool` comment and the `Location.TemperatureTrends` doc comment.

**Dry run before Stage 2.** Rebuild the location cache (`CachingTool`, against a locally running
WebApi; you run this, since agents don't start the API) or deploy Stage 1. Then re-run the research
note's analysis on `/location`:

- how many locations get a since-1961 trend, and how many of those are significant
- the decile bands
- the score change for each location compared with the current anomaly score
- the record-length correlation, expected to be close to 0

Use the results to settle the start tolerance and minimum years, and add them to this doc as an
addendum.

**Deployment:** the API serialises `TrendWindow` by name with `JsonStringEnumConverter`, which **throws
on an unknown name**. A web client that predates Stage 1 would fail to deserialise `/location` once
the API sends `"Since1961"`. Deploy the Web app first (or both together), then rebuild the API cache.
Browser tabs left open on the old WebAssembly build will fail to load locations until they're
reloaded.

### Stage 2: heating score on the Since-1961 slope

- `Location.SetHeatingScores`: rank on `Since1961TemperatureTrend.Slope` for eligible locations
  (Stage 1's recency rule). A negative slope scores −1. Locations without an eligible trend get no
  score (null), as they do now when there's no anomaly.
- `HeatingScoreRow`: rename `MinimumWarmingAnomaly`/`MaximumWarmingAnomaly` to
  `MinimumRatePerDecade`/`MaximumRatePerDecade`, stored in **°C per decade**, the unit the info page
  shows. `TrendSummary.Slope` stays per year, so `SetHeatingScores` multiplies by 10 when it writes the
  bands, and the names say "per decade" so nobody mixes the two up. Round them in `CacheJsonOptions`
  (3 decimal places, like the anomaly). **Deployment:** these are
  `required` properties, so an old client and a new API (or the reverse) fail on the heating score
  table. Deploy Web and API together. The table only loads when someone opens the info modal, so the
  impact is small.
- `ApiMeasures.HeatingScoreAnomalyBound` → `HeatingScoreRateBound`: the bound is already per decade, so
  °F conversion only needs the difference factor (no per-year to per-decade scaling).
- `HeatingScoreInfo`: rewrite the text and table.
  - The score ranks the since-1961 warming rate (°C/decade) into tenths.
  - Why 1961: the WMO reference period. It's the same period for every station, so a long record no
    longer raises or lowers the score.
  - Why not the last 30 years: a 30-year trend at a single station is too noisy to rank on.
  - The dashboard's trend field shows the last 30 years first, so it can differ from the rate the
    score uses. Say so, and show this location's since-1961 rate.
  - Negative rates score −1.
  - Remove the "not a fully standardised metric" caveat about long records. Keep the inland vs coastal
    point.
  - Table columns: Heating score, Warming rate since 1961 (°C/decade).
- `Locations.razor`: update the heating score filter tooltip ("a measure of how much a location has
  warmed" → how fast it has warmed since 1961).
- The warming anomaly field, its info modal and the Locations page anomaly column are unchanged.

### Stage 3: location dashboard trend field and modal

- `LocationTrendFieldBuilder`: value = Last30 if significant, otherwise Since1961 if significant,
  otherwise Full if significant, otherwise null (see the Stage 3 notes). The tooltip table lists each significant trend in the order Last 30 years, Since
  1961, Full period, with year ranges.
- `LocationTrendDetailsBuilder`: add the Since1961 window. The modal list and the About-trends tabs
  (`TemperatureTrendAboutPanel`) pick it up automatically. Order: Full, Last 30, Since 1961, Early.
  Or reorder to match the tooltip; decide this in the stage.
- `TemperatureTrendInfo.razor`: update the "which trend is shown" paragraph and the minimum-years
  paragraph to cover Since 1961's eligibility rule.
- **Locations page trend column:** it follows `LocationTrendFieldBuilder`, so it switches to
  Last30 → Since1961 automatically. Sorting would still mix two windows. Since the heating score column
  now gives the standardised comparison, leave this as it is unless you'd rather the column show
  Since 1961 only.

### Stage 4: chart

- `ChartSeriesTrendCalculator`: fit Since1961 when the plotted points are eligible, and insert it after
  Full in `windows` (the dropdown and tab order).
- **Adding a trend defaults to Since 1961.** Both add-trend buttons (`ChartSeriesView.OnAddTrendClicked`
  and `AddTrendSection`) add a `new ChartSeriesTrendRequest()` with no `TrendPeriod`, so
  `ResolveWindow` picks from `SelectionPriority`. Make it Since1961, Full, Last30, RecentDecade,
  FirstHalf, and rewrite its doc comment ("Full period first, since that's the trend most people
  mean"): Since 1961 is the same period for every series and matches the heating score. Results:
  - A first trend added by clicking draws Since 1961 when it's available and significant, otherwise
    Full.
  - A second trend added on the same series skips the windows already shown (`excludedFromDefault`),
    so it lands on Full.
  - **Presets are unchanged.** They request their window explicitly, and an explicit request is
    honoured while it's significant. Only when a preset's window isn't significant for a location does
    the fallback now try Since 1961 before Full.
  - Existing shared URLs carry the resolved window name, so they keep the window they had.
- The window applies to whatever series is plotted (temperature, precipitation, CO₂, global series),
  using the plotted (possibly smoothed) points like every other window. A chart whose `startYear` is
  after 1965 doesn't get it.
- `ChartSeriesTrendNotificationBuilder`, `ChartDisplayBuilder`, the trend dropdown and the About-trends
  panel already loop over windows and labels. Check that each one offers and names Since 1961 without
  special handling.
- Update the remarks on `ChartSeriesTrendCalculator`: it's now five windows, and Since 1961 has its own
  eligibility rule.

### Stage 5: Recent Observations trend tab

- `TrendWindowSet`: add `Since1961Trend` (nullable). `TrendWindowCalculator.Calculate` fits it with the
  shared helper, at degree 1 like the other windows.
- `RecentObservationsCalculator.Trend`: build the since-1961 points, value text, positive flag and
  tooltip, as it does for the other windows. `RecentObservationTrendViewModel` gets the matching
  properties.
- `RecentObservationTrend.razor`: the headline becomes **Last 30 years**, then Since 1961, then Early
  period. Full period moves off the tile. Labels use `TrendWindowLabel`.
- `AboutTrends`: add the Since 1961 toggle option, order the options as in Decisions, and default to
  Last 30 years when a metric tab is selected.
- `TrendsOverviewExplainer`: mention Since 1961, if its text lists the windows.
- Keep Recent Observations' 60-year minimum. The 1961 window is eligible on its own terms on top of
  that.

## Testing

Each stage adds or updates tests using `MethodName_StateUnderTest_ExpectedBehavior` names:

- **Core:** `TemperatureTrendCalculatorTests` (eligibility: starts 1961, starts 1965, starts 1966, under
  30 years in the window; the slope only uses ≥ 1961), `LocationTemperatureTrendsSerializationTests`
  (round-trips `Since1961`), `HeatingScoreTests` (ranked on the slope, negative → −1, recency rule,
  no eligible trend → null), `TrendWindowCalculatorTests`.
- **Web:** `LocationTrendFieldBuilderTests` (Last30 → Since1961 → null, the tooltip lists only
  significant ones), `LocationTrendDetailsBuilderTests`, `ApiMeasuresTests` (rate bound in °F),
  `ChartSeriesTrendCalculatorTests` (eligible/ineligible; with no requested window, Since 1961 when it's
  significant, otherwise Full; a second trend skips it; an explicit Full request stays Full),
  `ChartSeriesListSerializerTrendTests`
  (the `Since1961` name round-trips), Recent Observations calculator tests.
- **WebApi:** `CacheJsonOptionsTests` (the per-decade bounds are rounded).

## Risks

- **Scores change a lot.** In the research note, 60% of locations moved by 2 or more under the
  last-30 trend. Since-1961 should move fewer, but well-known locations will still change. The info
  page should say the method changed, and a short blog post would help.
- **Deployment order** (Stages 1 and 2, above).
- **Coverage:** locations without an eligible since-1961 trend lose their score. The research note
  estimates up to 1,757 eligible against 1,750 scored today, but gaps will lower that. The Stage 1 dry
  run gives the real number.
- **Old blog posts** (2022) describe the anomaly-based score. Leave them as a record of the time; a new
  post can explain the change.

## Summary

The plan adds a Since-1961 trend window, defined once in Core and used by location trends, the chart
and Recent Observations. It ranks the heating score on that slope, with negative slopes scoring −1.
The dashboard trend field shows the last 30 years first and then Since 1961, with every significant
trend in its tooltip. Recent Observations shows Last 30 years, Since 1961 and Early period on the
tile, and keeps Full period in About trends. On the chart, a trend added by clicking defaults to Since
1961, and presets keep their windows. Stage 1 stores the trend and includes a dry run that sets the
thresholds before the score changes.

### Change size

- **Projects:** `ClimateExplorer.Core`, `ClimateExplorer.WebApi` (rounding the heating score bands,
  comments), `ClimateExplorer.Web.Client`, `ClimateExplorer.CachingTool` (comment), and the Core, Web
  and WebApi unit test projects.
- **Not changed:** `ClimateExplorer.WebApiClient`, `ClimateExplorer.DataPipeline`, datasets.

| Stage | Area | Added | Changed | Removed |
|---|---|---|---|---|
| 1 | Core: enum, defaults, window helper, calculator, fits, `Location` | 1 file, +30 | 5 files, ~50 | — |
| 1 | Web.Client: `TrendWindowLabel`; CachingTool comment | — | 2 files, ~6 | — |
| 1 | Tests: trend calculator, serialisation | — | 2 files, ~90 | — |
| 2 | Core: `SetHeatingScores`, `HeatingScoreRow`; WebApi: `CacheJsonOptions` rounding | — | 3 files, ~50 | — |
| 2 | Web.Client: `HeatingScoreInfo` (razor + cs), `ApiMeasures`, Locations tooltip | — | 4 files, ~50 | — |
| 2 | Tests: heating score, `ApiMeasures`, `CacheJsonOptions` | — | 3 files, ~80 | — |
| 3 | Web.Client: field builder, details builder, `TemperatureTrendInfo` | — | 3 files, ~50 | — |
| 3 | Tests: field builder, details builder | — | 2 files, ~70 | — |
| 4 | Web.Client: `ChartSeriesTrendCalculator` window and `SelectionPriority` (+ checks elsewhere) | — | 1–3 files, ~35 | — |
| 4 | Tests: chart trend calculator (incl. default window), serializer | — | 2 files, ~90 | — |
| 5 | Core: `TrendWindowSet`, `TrendWindowCalculator` | — | 2 files, ~20 | — |
| 5 | Web.Client: calculator, view models, tile, About trends, explainer | — | 6 files, ~70 | — |
| 5 | Tests: trend window calculator, Recent Observations | — | 2 files, ~60 | — |
| | **Total** | **1 file, +30** | **~38 file edits (~28 distinct files), ~720** | — |

Tests are roughly half the lines (~390 of ~720).

## Addendum: Stage 1 dry run (2026-11-12)

Taken from the local location cache rebuilt with Stage 1 (`CachingTool`, 2026-11-12). That cache has
5,535 locations, against 2,014 in production when the research note was written, because of the new
stations on `development`. So the counts are larger than the note's, and the score-change table isn't
directly comparable with the note's last-30 one. The Stage 2 scoring was simulated in a scratch script
with the same decile algorithm as `SetHeatingScores`.

### Implementation notes

- The start rule is `firstYear < FixedPeriodStartYear + FixedPeriodStartToleranceYears`: the first
  complete year in the window must fall between the start of 1961 and the end of 1965.
- `TemperatureTrendCalculator.GetWindowPoints(..., Since1961)` returns an empty list when the window isn't
  eligible.
- `LocationTrendDetailsBuilderTests.Build_SameSeries_MatchesTheLocationsPreCalculatedSummaries` skips
  the Since 1961 summary until Stage 3 adds the window to the modal. Remove that filter in Stage 3.

### Coverage

| | Locations |
|---|---|
| With a warming anomaly (scored today) | 3,484 |
| With a since-1961 trend | 3,994 (3,888 significant, 97%) |
| … first complete year 1961 / 1962–1965 | 3,388 / 606 |
| … last year before 2015 (fails the recency rule; newest last year is 2025) | 41 |
| **Eligible for the score** | **3,953** |
| Scored today but not eligible | 101 (89 have no eligible window: gaps around 1961–65 or under 30 complete years; 12 fail the recency rule) |
| Eligible but not scored today | 570 (under 60 complete years, so no anomaly) |

So the number of scored locations rises from 3,484 to 3,953.

### Rates and bands

Since-1961 rate across eligible locations: mean 0.291, median 0.283, SD 0.108 °C/decade. The 103
non-significant rates are all below 0.23 °C/decade.

| Score | Since-1961 rate (°C/decade) |
|---|---|
| 9 | 0.425 – 1.040 |
| 8 | 0.380 – 0.425 |
| 7 | 0.344 – 0.380 |
| 6 | 0.311 – 0.344 |
| 5 | 0.283 – 0.311 |
| 4 | 0.257 – 0.283 |
| 3 | 0.234 – 0.257 |
| 2 | 0.203 – 0.233 |
| 1 | 0.159 – 0.203 |
| 0 | 0.012 – 0.159 |
| −1 | negative (7 locations) |

The 7 negative rates are Halley (−0.28, significant), Francistown (−0.12, significant), Dumont
d'Urville, Seminole, Alum Fork, Rapa and Casey (−0.03 to −0.07, not significant). Scoring only
significant negatives as −1 would move the last five to 0.

### Score change, current anomaly score → since-1961 score

| Change | 0 | 1 | 2 | 3 | 4 | 5+ |
|---|---|---|---|---|---|---|
| Locations (n = 3,383) | 928 | 1,222 | 655 | 297 | 173 | 108 |

37% move by 2 or more. The biggest movers are of two kinds:

- **Long records with a cold early period**, which have a high anomaly but an ordinary rate since 1961:
  Buenos Aires Observatorio 9 → 1 (record from 1856), Pasadena 9 → 1, Hiroshima 9 → 2, Vladivostok 9 → 2,
  Groton 8 → 1.
- **Flat early records, then strong warming after 1961**: Savannah 0 → 7, Tasiilaq 0 → 7, Inukjuak
  0 → 7, Danville 1 → 8, Kolguev Severnyi 1 → 9.

Rank correlation of the since-1961 rate with the anomaly is 0.78, and with the last-30 trend 0.74.

### Record length

| Measure | Spearman ρ with record length |
|---|---|
| Since-1961 rate, all eligible | −0.12 |
| … within the US / outside the US | −0.02 / −0.00 |
| … within each country (countries with 30+ locations) | −0.01 |
| Warming anomaly | +0.01 |
| Full-record trend | −0.64 |
| Last-30 trend | −0.09 |

The −0.12 overall comes from geography, not the method. US stations warmed more slowly since 1961
(median 0.245 against 0.322 °C/decade elsewhere) and make up most of the 100–149-year records (964 of
1,555). Within any one group the correlation is about zero. Medians by record length:

| Record length (years) | n | Since-1961 rate (°C/dec) | Significant | Mean new score |
|---|---|---|---|---|
| under 60 | 56 | 0.317 | 96% | 5.5 |
| 60–79 | 1,221 | 0.303 | 98% | 4.9 |
| 80–99 | 857 | 0.295 | 99% | 4.8 |
| 100–119 | 591 | 0.251 | 94% | 3.6 |
| 120–149 | 964 | 0.262 | 96% | 3.9 |
| 150+ | 264 | 0.327 | 100% | 5.4 |

(The anomaly's ρ is now +0.01, against +0.21 in the research note, also because of the new stations.)

### Noise

With n ≈ 63 consecutive years the slope's standard error is about σ × 0.07 °C/decade: about
0.035 at a maritime station and up to 0.1 inland. The middle bands are only 0.023–0.045 wide, so a
score still carries about ±1 band of noise at the coast and up to ±3 inland. That's better than the
last-30 trend's ±2 to ±4, but the score isn't precise. Taking σ ≈ 0.8 °C as typical, roughly three
quarters of the spread between stations is real, against under half for the last-30 trend. One unusual
final year (1 °C off) moves the slope by about 0.015 °C/decade, under half a band, so scores won't jump
every year as the window slides.

### Thresholds settled

- **Start tolerance: keep 5 years (1961–1965).** The median rate doesn't drift with the start year
  (1961: 0.285, 1962: 0.258, 1963: 0.262, 1964: 0.341, 1965: 0.337 °C/decade; the later years have small
  samples and fewer US stations). Widening to 10 years would add about 170 locations whose trend would
  really start in 1966–1970.
- **Minimum years: keep 30 for both the trend and the score.** The cache doesn't record how many
  complete years each window has, so gaps can't be measured here. But the start and recency rules
  already make every score-eligible window span at least 51 calendar years, so the floor of 30 only
  applies when over 40% of the years are incomplete. Thirty points spread over 50+ years also fit the
  slope more tightly than 30 consecutive years do.
- **Recency rule: keep it** (last year within 10 years of the newest). It removes 41 locations,
  mostly ending in 2005–2014.

## Stage 2 notes (2026-11-13)

- `Location.SetHeatingScores` clears every location's score first, then scores only the eligible ones,
  so a location that loses its since-1961 trend can't keep an old score. The recency rule is
  `Location.HeatingScoreMaximumYearsBehindNewest` (10) and the negative score is
  `Location.NegativeRateHeatingScore` (−1).
- `ApiMeasures` has `HeatingScoreRate(location)` (the location's since-1961 slope, scaled to per decade)
  next to `HeatingScoreRateBound`, so the info page doesn't scale slopes itself.
- `HeatingScoreInfo` shows the rates and bounds to three decimal places, because the middle bands are
  only 0.02–0.05 °C/decade wide. It says the method changed in November 2026. For a location with no
  score, it explains why. A −1 score isn't in the table, so the text only says "highlighted below" for
  scores of 0 to 9.
- The Recent Observations "too short" message said its 60-year minimum matched the warming anomaly
  *and heating score*. The heating score no longer uses that minimum, so the message now names only the
  warming anomaly.
- **Deployment:** after deploying the API, rebuild the cache with `CachingTool`. Until then the cached
  heating score table still has the old property names. `FileBackedTwoLayerCache.Get` swallows the
  deserialisation error and returns null, so the heating score info stays empty until the rebuild.

## Stage 3 notes (2026-11-13)

- **The full-period fallback stays.** On the Stage 1 cache, dropping it would have blanked the
  dashboard trend field for 621 locations (184 → 728 with no field). Those are mostly 30–59-year records
  starting after 1965, which have no since-1961 window. The field shows the first significant trend of
  Last 30 years → Since 1961 → Full period. The heating score still ranks only the since-1961 trend, and
  the trend info page says the two can differ.
- **Field labels:** "Last 30 years trend", "Trend since 1961", "Trend". The hover tooltip lists every
  significant trend stored on `Location`, in the same order. Early period isn't stored there, so it
  isn't in the tooltip.
- **Modal and About trends:** the order is Last 30 years, Since 1961, Full period, Early period: the
  field's order, then the early period.
- **Locations page trend column:** unchanged. It uses `LocationTrendFieldBuilder`, so it follows the
  same order.
- Removed the Stage 1 Since1961 filter from
  `LocationTrendDetailsBuilderTests.Build_SameSeries_MatchesTheLocationsPreCalculatedSummaries`.

## Stage 4 notes (2026-11-13)

- `ChartSeriesTrendCalculator` fits Since 1961 from `TrendWindows.GetSince1961Points` over the plotted
  points, at the chosen regression degree like every other window. Window order: Full, Since 1961,
  Last 30 years, Last 10 years, Early period.
- `SelectionPriority` is Since 1961, Full, Last 30, Last 10, Early, so a trend added by clicking draws
  Since 1961, and a second one lands on Full. Explicit requests (presets, shared URLs) are unchanged.
- The dropdown, About-trends tabs, legend label ("Since 1961 trend") and notifications all name the
  window through `TrendWindowLabel`, so none of them needed changing.
- `TrendsOverviewExplainer` lists only some windows ("among them …"), so it's still accurate. Stage 5
  adds Since 1961 to it.
- Tests: tests that used records from 1960 with 59 years now start in 1900, so they still have no
  since-1961 window. New tests cover the window's points, the 1965/1966 start, the default pick, an
  explicit Full request and the second-trend fallback, plus a serializer round trip.

## Stage 5 notes (2026-11-13)

- `TrendWindowSet` gains `Since1961Trend` (nullable), fitted by `TrendWindowCalculator` at degree 1 from
  `TrendWindows.GetSince1961Points`. The points are the metric's yearly values for the period (plus
  the current, possibly partial, year), so the window is per metric and per tile.
- **Tile:** the headline is Last 30 years, followed by Since 1961 (only when eligible), then Early
  period. Labels come from `TrendWindowLabel`. The full period is no longer on the tile, so its display
  fields (`HeadlineText`, `IsHeadlinePositive`, `HeadlineCaption`, `FullPeriodTooltip`) were removed from
  `RecentObservationTrendViewModel`. About trends still has the full-period regression and points.
- **About trends:** the toggle is Last 30 years, Since 1961 (only when eligible), Early period, Full
  period. It opens on Last 30 years, and goes back to it when you switch metric tab.
- `TrendsOverviewExplainer` (shared by the chart, the location trend modal and Recent Observations) now
  lists Since 1961 and why it's the same period everywhere.
- Recent Observations keeps its 60-year minimum. Since 1961 needs that as well as its own rule.
