# Recent observations: abstract observation domains and CO₂ support

- **Date:** 2026-07-27
- **Status:** Implemented
- **Author:** Patrick Lea (with Claude)
- **Scope:** `ClimateExplorer.Web.Client` recent-observations stack
  (`Components/RecentObservations`, `Services/RecentObservations`,
  `UiModel/RecentObservations`, `Components/Co2`, `Layout/NavMenu`),
  `ClimateExplorer.Core` (`DataSetDefinitionsBuilder`,
  `Calculators/RecentObservationComparison`), and the CO₂ dataset assets.
- **Builds on:** [recent-observations trend tab](2026-07-16-01-recent-observations-trend-tab-plan.md),
  [automated dataset downloads](2026-07-13-01-automated-dataset-downloads-plan.md)
- **Branch context:** `master`

## Goal

Let the Recent Observations panel display atmospheric CO₂ (parts per million),
loaded daily up to the latest available measurement, opened from the existing
`Co2NavTile` in the nav bar. Reuse the existing default tiles (yesterday, latest
7 days, current month, previous months, year to date, previous years).

Doing this well requires removing the assumption baked through the whole stack
that Recent Observations is a fixed two-tab (Temperature + Precipitation)
location feature. This plan makes the set of observation "domains" a per
location/region configuration so future data-types can be added without another
special case.

## Design decisions (settled)

1. **No season tiles for CO₂.** Mauna Loa CO₂ is a proxy for global CO₂;
   meteorological seasons are not meaningful. The current-season and
   previous-season tiles **and** the "Add season" button are suppressed for the
   CO₂ domain. This becomes a per-domain capability flag (`SupportsSeasonTiles`),
   not a CO₂ special case.
2. **Trend framing, not record framing.** CO₂ fluctuates within a year and there
   is no law that it must keep rising — the feature exists to show the *trend*,
   which the existing Trend expanded tab already computes. The CO₂ domain keeps
   honest record/rank detection (the data speaks for itself) but uses
   neutral rise/fall vocabulary ("highest"/"lowest"/"higher than usual") rather
   than temperature's "warmest/coolest" or precipitation's "wettest/driest", and
   uses a neutral tile tone (no good/bad colouring).
3. **Break the built-in two-tab structure.** `Temperature` + `Precipitation` is
   the *default for locations only*. The panel is driven by an ordered list of
   observation domains resolved from the location/region id, so a place can
   prescribe any set of data-types. **Precipitation is the template** (single
   primary metric, no adjustment, mean/sum aggregation); **Temperature is the
   exception** (max/min/mean metrics, adjusted/unadjusted). CO₂ is
   precipitation-shaped with mean aggregation and ppm units.
4. **Only Temperature has adjusted/unadjusted.** The adjustment concept becomes a
   per-domain flag (`SupportsAdjustment`), which also fixes the latent
   data-loading bug (below).

## Current architecture (what has to change)

The two-tab assumption is a hard-coded enum switch at every layer:

- `UiModel/RecentObservations/RecentObservationsTab.cs` — the `{ Temperature,
  Precipitation }` enum.
- `RecentObservationTabs.razor` — two literal `<Tab>` elements.
- `RecentObservationsPanel.razor(.cs)` — two `RecentObservationsTabState`
  fields, `GetState`/`EnsureTabLoaded`/`RecalculateLoadedTabs`/
  `UpdateAvailableDataAdjustments` all `switch` on the tab; an `ActiveTab`
  default of `Temperature`; the adjustment checkbox gated on
  `ActiveTab == Temperature`.
- `IRecentObservationsService` / `RecentObservationsService` — `Load*Data`,
  `Get*Records`, `GetTemperatureRecords`, `GetPrecipitationRecords`.
- `IRecentObservationsDataProvider` / `RecentObservationsDataProvider` — one
  `Load*Data` method per tab; `GetAdjustmentCandidates` special-cases
  `DataType.Precipitation`.
- `RecentObservationsDataSet.cs` — per-tab factory methods and per-tab record
  fields.
- `RecentObservationsCalculator.cs` — private `MetricDomain` records
  (`TemperatureDomain`, `PrecipitationDomain`), `Calculate` switches on tab,
  `BuildTiles` reads `location.Coordinates.Latitude` unconditionally, and
  season periods are always built.
- `Co2NavTile.razor(.cs)` — display-only; no click handler; lives in
  `NavMenu.razor`, which renders on every page **outside any location context**.

### The latent bug to fix

`RecentObservationsDataProvider.GetAdjustmentCandidates` yields a `null`
adjustment only when `dataType == DataType.Precipitation`; every other type
falls through to `DataAdjustment.Unadjusted`. The CO₂ measurement definitions use
`DataAdjustment = null`, so a CO₂ query would ask for `Unadjusted`, never match,
and return zero records. The fix falls out of decision 4: drive the candidate
list from the domain's `SupportsAdjustment` flag (non-adjustment domains yield
the single `null` candidate).

## Target architecture

### Observation domain descriptor

Promote the calculator's private `MetricDomain` into a first-class, shared
descriptor (proposed name `ObservationDomain`) that carries everything the three
layers need. Fields, on top of today's `MetricDomain` (primary/supporting
metrics, groups, labels, headline/percentile builders, tone):

- `Key` — stable string id (`"temperature"`, `"precipitation"`, `"co2"`),
  replacing `RecentObservationsTab` for state keying, tab identity, and caching.
- `TabLabel` — display text (`"Temperature"`, `"Precipitation"`, `"CO₂"`).
- `DataTypeRequests` — which `DataType`(s) to load (temperature loads
  Max+Min+Mean; precipitation loads Precipitation; CO₂ loads the new daily
  `DataType.CO2`).
- `SupportsAdjustment` — `true` only for temperature.
- `SupportsSeasonTiles` — `false` for CO₂.
- `Framing` — `Record` (temp/precip) vs `Trend` (CO₂), selecting the
  vocabulary/tone; records still computed either way.

A small **domain catalog** holds the three hand-written descriptors (each needs
bespoke metrics and copy, so the *set* stays known code). What becomes
configurable is the **selection**: a resolver maps a location/region id to an
ordered list of domain keys.

- Default (any normal `Location`): `[temperature, precipitation]`.
- `Region.Atmosphere` id: `[co2]`.

This is the abstraction the feature needs: "prescribe what data-types we want for
the location/region guid."

### Panel context (Location → abstract context)

The panel currently requires `Location` (with `required Coordinates`), used only
for `location.Id` (caching/adjustment lookup) and `location.Coordinates.Latitude`
(season math). The Atmosphere is a `Region` with no coordinates, and CO₂ has no
season tiles, so:

- Introduce a lightweight `RecentObservationsContext` (Id, display Name,
  `double? Latitude`, ordered `Domains`). Locations build it from themselves;
  `Co2NavTile` builds an Atmosphere context with `Latitude = null` and
  `Domains = [co2]`.
- The calculator takes `double? latitude` instead of `Location`. Season periods
  are built only when `latitude.HasValue && domain.SupportsSeasonTiles`.
- Existing call sites (`LocationDashboard.razor`, `Locations.razor`) construct
  the context from their `Location` — mechanical.

### Panel state, generalised

- Replace the two `RecentObservationsTabState` fields with a
  `Dictionary<string, RecentObservationsTabState>` keyed by domain key,
  populated from the context's domain list.
- `ActiveTab` becomes the active domain key; default = first domain in the
  context (so a location still opens on Temperature; the CO₂ entry opens on CO₂).
- `RecentObservationTabs.razor` renders one `<Tab>` per domain (label from
  descriptor). A single-domain context (CO₂) still renders one tab.
- Adjustment checkbox shows only when the active domain `SupportsAdjustment`.
- "Add season" button shows only when the active domain `SupportsSeasonTiles`.

### Data provider, generalised

- One `LoadData(context id, ObservationDomain, preferredAdjustment)` that loads
  the domain's `DataTypeRequests` and builds a `RecentObservationsDataSet` from
  the raw `DataRecord` lists. Cache key becomes `(id, domainKey, adjustment)`.
- `GetAdjustmentCandidates` driven by `domain.SupportsAdjustment` — fixes the
  latent bug and removes the `DataType.Precipitation` special case.
- `RecentObservationsDataSet` factory methods collapse to a generic shape
  (domain key + the metric record lists the domain declares), keeping the
  temperature max/min/mean + `HasHistoricalTemperatureMaxMin` handling that the
  calculator already relies on.

## CO₂ specifics

### Daily data source (`DataSetDefinitionsBuilder`)

Add a **daily** measurement definition to the existing Mauna Loa CO₂ dataset
(`42c9195e-…`) alongside the two monthly ones:

- `DataType = DataType.CO2`, `DataResolution = DataResolution.Daily`,
  `UnitOfMeasure = PartsPerMillion`, `DataAdjustment = null`.
- Source: NOAA daily Mauna Loa file
  `https://gml.noaa.gov/webdata/ccgg/trends/co2/co2_daily_mlo.txt`, stored as
  `CO2/co2_daily_mlo.txt`; `DataDownloaderKey = "direct-http"` (already handles
  the 24-hour daily cadence).
- Row regex over `year month day decimal_date value`:
  `^\s*(?<year>\d+)\s+(?<month>\d+)\s+(?<day>\d+)\s+(?<decimalDate>\d+\.\d+)\s+(?<value>\d+\.\d+).*$`
  (confirm exact column layout against the live file during implementation).

**Precedent for same DataType at two resolutions in one dataset:** HadCET already
defines `TempMean` at both `Monthly` and `Daily` (`DataSetDefinitionsBuilder`
lines ~483–501). The client passes `monthly: false`, so the daily CO₂ definition
is selected for the panel while the monthly definitions keep serving the existing
charts and the `Co2NavTile` tooltip. **Verify** the Web API resolves
`DataType.CO2 + monthly:false` to the daily definition once both exist.

### CO₂ domain descriptor

- Single primary metric `co2.value`, mean aggregation, ppm formatting
  (`"###0"` ppm for headline value; one decimal where the temperature/precip
  domains show one — decide precision during build, ppm is conventionally shown
  to ~1 decimal for daily and integer for headline).
- No supporting metrics; day-records group is just the single value (mirrors the
  precipitation domain shape).
- `SupportsAdjustment = false`, `SupportsSeasonTiles = false`,
  `Framing = Trend`.
- Tone: neutral.

### CO₂ vocabulary (`RecentObservationComparison`)

Add `BuildCo2Headline` / `BuildCo2PercentileSentence` mirroring the precipitation
builders but with neutral vocabulary: "Highest/Lowest [label]",
"Nth highest/lowest", "Top 5%/10% highest", "Higher/Lower than usual",
"Near average". Add a ppm branch to `FormatTrendPerDecade`
(currently hard-codes `°C` vs `mm`) → `"+X ppm /decade"`, and ensure
`FormatAnomaly` renders ppm.

### Entry point (`Co2NavTile` + `NavMenu`)

- Add a click handler to `Co2NavTile` that opens a wide `SidePanel` hosting
  `RecentObservationsPanel` with the Atmosphere context (`Domains = [co2]`,
  `Latitude = null`).
- Host the `SidePanel` at the nav/layout level (NavMenu renders everywhere,
  outside location scope). NavMenu must obtain `DataSetDefinitions` (via
  `IDataService`, as other consumers do) to pass to the panel.
- Keep the tile's own display (latest monthly deseasonalised value + tooltip)
  unchanged; only add the open-panel affordance and an accessible name.

## Files touched (summary)

Core: `DataSetDefinitionsBuilder.cs` (daily CO₂ def),
`Calculators/RecentObservationComparison.cs` (CO₂ vocab + ppm trend),
new asset `ClimateExplorer.WebApi/Datasets/CO2/co2_daily_mlo.txt`.

Web client: new `ObservationDomain` descriptor + catalog + per-id resolver;
`RecentObservationsTab.cs` retired/replaced by domain keys;
`RecentObservationTabs.razor`, `RecentObservationsPanel.razor(.cs)`,
`RecentObservationsService(.cs)`/interface,
`RecentObservationsDataProvider(.cs)`/interface, `RecentObservationsDataSet.cs`,
`RecentObservationsCalculator.cs` (latitude param, season gating, domain
dispatch by key, add CO₂ domain), `Co2NavTile.razor(.cs)`, `NavMenu.razor`,
and the two existing panel call sites (`LocationDashboard.razor`,
`Locations.razor`) to build a context.

## Testing

Per AGENTS.md: `dotnet build` + `dotnet test ClimateExplorer.UnitTests` only
(no dev servers / browser). Expect to update:

- `RecentObservationsServiceTests` — convenience methods change with the
  generalised service; keep temperature/precipitation coverage, add CO₂ tests:
  ppm formatting, **no season tiles produced**, daily/7-day/month/year tiles
  present, Trend tab populated, neutral headline vocabulary.
- Data provider tests — CO₂ loads with a `null` adjustment (the bug fix); add a
  regression test that a non-adjustment domain yields the `null` candidate.
- `DataPackageDefinitionTests`, `DataSetDownloadMetadataTests`,
  `DataSetSourceInfrastructureTests` — review for the new daily CO₂ asset
  (`co2_daily_mlo.txt`) and its asset key / freshness record; the monthly
  `co2_mm_mlo.txt` continues to back `CO2`/`CO2Deseasoned`.

## Phasing

1. **Refactor to domains (no behaviour change):** introduce `ObservationDomain`
   descriptor + catalog + id→domains resolver; convert panel/service/provider/
   calculator/dataset off the `RecentObservationsTab` enum onto domain keys;
   make latitude nullable and gate seasons on `SupportsSeasonTiles`; fix the
   adjustment-candidate bug via `SupportsAdjustment`. Locations still show
   `[temperature, precipitation]` exactly as today. Green build + tests.
2. **Add CO₂ domain + daily data:** daily CO₂ measurement definition, CO₂ domain
   descriptor, CO₂ vocabulary + ppm trend formatting; verify API resolution of
   daily CO₂. Add CO₂ unit tests.
3. **Entry point:** `Co2NavTile` opens the panel with the Atmosphere context;
   host the SidePanel in the nav/layout and wire `DataSetDefinitions`.

## Open questions / risks

- **API resolution** of `DataType.CO2` at daily vs monthly once both definitions
  exist — verify (HadCET precedent suggests it works, but confirm).
- **Daily Mauna Loa lag & gaps.** The daily file typically trails "today" by a
  few days and has missing days (volcano suspension Nov 2022–Jul 2023). The
  reference-date resolver already snaps to the latest available date, and
  completeness handling already tolerates gaps, so "up to yesterday" is
  best-effort "up to the latest available measurement". No comparison exists
  before 1974 (daily record start) even though the monthly record starts 1958.
- **ppm display precision** for the headline vs tiles — pick during build.
- **NavMenu hosting** a wide SidePanel from the global bar — confirm no layout/
  z-index conflicts with the existing per-page SidePanels.
