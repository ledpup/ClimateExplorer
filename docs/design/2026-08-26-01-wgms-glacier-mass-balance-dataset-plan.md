# Add World Glacier Monitoring Service (WGMS) glacier mass balance dataset

- **Date:** 2026-08-26
- **Status:** Implemented 2026-08-27 (see addendum)
- **Author:** Patrick Lea (with Claude)
- **Scope:** `ClimateExplorer.Core` (`Enums`, `Model/Region`, `DataSetDefinitionsBuilder`), `ClimateExplorer.Data.Downloading` (new transformer), `ClimateExplorer.WebApi` (DI wiring, `MetaData/DataFileMapping`, `Datasets/Glaciers`), `ClimateExplorer.SourceData`, `ClimateExplorer.Web.Client` (`SuggestedPresetLists.Global.cs`)
- **Branch context:** `development`

## Goal

Add the World Glacier Monitoring Service's (WGMS, [wgms.ch](https://wgms.ch/))
"Fluctuations of Glaciers" database as a new global dataset: a single yearly
**global glacier mass balance index**, built from `mass_balance.csv`'s
`annual_balance` column across all glaciers with a long-enough, sufficiently
current observation record. Add a "Global glacier mass balance" preset to
`SuggestedPresetLists.Global.cs`, positioned after "Greenland ice melt area".

## Verified against the live source (not assumed)

- Current release: `https://wgms.ch/downloads/DOI-WGMS-FoG-2026-02-10.zip`
  (39 MB, confirmed reachable, `Content-Type: application/zip`). Listed as the
  latest entry on <https://wgms.ch/data_databaseversions/>.
- Inside the zip, `data/mass_balance.csv` (3.5 MB) has the header:
  `country,glacier_name,glacier_id,outline_id,year,time_system,begin_date,begin_date_unc,midseason_date,midseason_date_unc,end_date,end_date_unc,winter_balance,winter_balance_unc,summer_balance,summer_balance_unc,annual_balance,annual_balance_unc,ela_position,ela,ela_unc,aar,area,investigators,agencies,references,remarks`
  — `country`, `glacier_name`, `glacier_id`, `year`, `annual_balance` are all
  present exactly as the user described, and are the first five meaningful
  columns (no join against `glacier.csv` needed).
- **Unit confirmed as metres water equivalent.** The zip's `datapackage.json`
  (a Frictionless Data package descriptor shipped alongside the CSVs) tags the
  `annual_balance` field explicitly: `"units": "m w.e."`, `"example": "-0.87"`.
  Sample rows from the CSV itself (`0.02`, `-0.02`, `0.03`, ...) are consistent
  with metres, not millimetres — confirms the user's "usually measured in m
  w.e." is correct for this file specifically, not just generically.
- Some later columns (`investigators`, `agencies`, `references`, `remarks`)
  contain quoted free text with embedded commas (e.g.
  `"[data from] Dyurgerov, M. (2002) | ..."`). They come *after*
  `annual_balance` in column order, but a transformer should still use a
  quote-aware CSV reader rather than a naive `Split(',')`, in case any
  `glacier_name` ever contains an unquoted comma — correctness here is cheap
  (see "CsvHelper" below).
- 8,944 total data rows; 8,444 have a non-blank `annual_balance`, across 512
  distinct `glacier_id`s.
- Year range: 1885–2025.

### Applying the Benchmark rule to today's data

Interpreting the user's quoted rule as: **more than 10 years of annual-balance
records, with at most 1 missing year in the most recent 10 calendar years of
the dataset** (2016–2025, since the dataset's own latest year is 2025 — using
the dataset's own max year, not wall-clock "now", keeps the transformer pure
and its output reproducible from the input file alone):

- **138 glaciers** qualify. Geographically well spread: Switzerland (16),
  USA (14), Austria (13), Italy (10), Norway (10), Svalbard/SJ (10),
  Iceland (9), Canada (8), Kyrgyzstan (7), France (5), China (4),
  Greenland (4), Nepal (4), Sweden (4), Antarctica (3), and others — this is
  the same order of magnitude as WGMS's own reference-glacier network, which
  is the expected sanity check.
- Longest series: Claridenfirn (CH, 111 years), Silvretta (CH, 107 years),
  Storglaciären (SE, 80), Taku (US, 80).
- A stricter "relative to each glacier's own last-reported year" reading of
  the rule was tried first and rejected: it let long-dormant glaciers (e.g.
  one last reported in 2013) count as "ongoing", which contradicts the rule's
  intent. The dataset-wide recent-decade window avoids that.

### Sanity-checking the anomaly-average approach

For the 138 qualifying glaciers, computed each glacier's anomaly (its own
`annual_balance` values minus its own all-time mean) and simple-averaged the
anomalies per year:

| Year | Contributing glaciers | Mean anomaly (m w.e.) |
|---|---|---|
| 1985 | 56 | +0.00 |
| 2000 | 83 | +0.40 |
| 2010 | 115 | −0.06 |
| 2020 | 137 | +0.02 |
| 2022 | 138 | **−0.91** |
| 2023 | 138 | **−0.92** |
| 2024 | 138 | −0.63 |
| 2025 | 128 | −0.61 |

The sharp negative swing in 2022–2023 matches the well-documented global
glacier mass loss record years — a good real-world sanity check that the
anomaly-averaging approach produces a sensible signal, not an artifact.

Early years are thin (n=1 for most years before 1946) and correspondingly
noisy — see "Minimum contributing glaciers" below for how the transformer
handles that.

## Design

### 1. New enum members (`ClimateExplorer.Core/Enums.cs`)

```csharp
public enum DataType
{
    ...
    CO2Deseasoned,
    GlacierMassBalance,   // new
}

public enum UnitOfMeasure
{
    ...
    Ph,
    MetresWaterEquivalent,   // new
}
```

Update all three switch statements that are keyed off `UnitOfMeasure`:

- `UnitOfMeasureLabelShort`: `UnitOfMeasure.MetresWaterEquivalent => "m w.e."`
- `UnitOfMeasureLabel` (private one): `UnitOfMeasure.MetresWaterEquivalent => "Metres water equivalent (m w.e.)"`
- `UnitOfMeasureRounding`: `UnitOfMeasure.MetresWaterEquivalent => 2` (values are
  typically in the ±0.01–2 range; the default fallback of `1` decimal place
  would be too coarse — e.g. the 2022/2023 anomalies above would round to the
  same `-0.9`).

Only one new unit is added. The codebase's `DegreesCelsius` /
`DegreesCelsiusAnomaly` split exists because both an absolute per-station unit
*and* a providers'-precomputed-anomaly product (NOAAGlobalTemp) coexist for
temperature. This plan ships only the computed global anomaly-index series (no
raw per-glacier series — see "Out of scope"), so a single unit is sufficient
for now; if a raw per-glacier series is ever added, revisit whether it needs
its own unit or can reuse this one (an absolute annual balance is still
correctly expressed in "m w.e.").

### 2. New `Region` ("Glaciers") (`ClimateExplorer.Core/Model/Region.cs`)

Add alongside `Greenland`/`Arctic`/`Antarctic`:

```csharp
public const string Glaciers = "Glaciers";
```

- `RegionId(string)`: `Glaciers => new Guid("B91B639B-5625-461B-BCB7-5E0783EA7FFD")`
- `GetRegions()`: add `new Region { Id = RegionId(Glaciers), Name = Glaciers }`

A dedicated region (rather than folding into `Region.Earth`) matches how
`Greenland`, `Ocean`, and `Atmosphere` are each their own region even though
they could conceptually nest under Earth — keeps `SuggestedPresetLists`
lookups (`Region.GetRegion(Region.Glaciers)`) and the `DataFileMapping` file
self-contained.

### 3. Download + transform pipeline

**A plain `DirectHttpDataSetDownloader` pointed at the zip is not enough on
its own.** The user's expectation of reusing the existing direct-HTTP
primitive still holds, but the benchmark-glacier filter and the per-glacier
anomaly averaging both require a full pass over every row before any output
row can be written — the same reason `OceanAcidity`/`SeaLevel`/`Ozone` use
`TransformingDataSetDownloader` (which itself performs a plain HTTP GET via
`DataSetHttpFileDownloader` — the same primitive `DirectHttpDataSetDownloader`
uses) plus a `IDataSetSourceFileTransformer`, rather than a bespoke
`IDataSetDownloader`. This dataset follows that exact precedent — no new
downloader class, no custom API client, just a new transformer.

**`DataDownloadUrl` is a dated, versioned filename** (`DOI-WGMS-FoG-2026-02-10.zip`)
and WGMS publishes a new dated zip for each release rather than updating one
stable URL. This matches the existing precedent of `TSI`'s dated filename
(`tsi-ssi_v03r00_..._c20250917.txt`, `DataSetDefinitionsBuilder.cs`) — accept
that the URL will need a manual bump when a maintainer notices a newer release
(same operational model as TSI today), rather than building a scraper for
`https://wgms.ch/data_databaseversions/`. Not worth the added complexity for a
dataset that updates roughly annually.

### 4. New transformer: `WgmsGlacierMassBalanceSourceFileTransformer`

New file: `ClimateExplorer.Data.Downloading/Transformers/WgmsGlacierMassBalanceSourceFileTransformer.cs`,
implementing `IDataSetSourceFileTransformer`.

```csharp
public sealed class WgmsGlacierMassBalanceSourceFileTransformer : IDataSetSourceFileTransformer
{
    private const int MinimumYearsOfRecords = 10;       // "more than 10 years"
    private const int RecentDecadeWindowYears = 10;
    private const int MaximumRecentGapYears = 1;         // "max gap of one year in the past decade"
    private const int MinimumContributingGlaciers = 5;   // suppress noisy early single/double-glacier years

    public async Task TransformAsync(string rawFilePath, string outputFilePath, CancellationToken cancellationToken)
    {
        // 1. Open rawFilePath (the downloaded zip) with System.IO.Compression.ZipArchive
        //    (already a transitive BCL dependency elsewhere in the codebase, e.g.
        //    ClimateExplorer.Core/InputOutput/DataReaderFunctions.cs) and locate the
        //    "data/mass_balance.csv" entry. Throw InvalidDataException if missing.
        // 2. Parse with CsvHelper (quote-aware — see below), reading
        //    glacier_id, year, annual_balance for every row where annual_balance is present.
        // 3. Group rows by glacier_id.
        // 4. maxYear = the maximum year across ALL rows (not wall-clock "now" — keeps the
        //    transform pure and reproducible from the input file alone).
        //    decadeWindow = [maxYear - 9, maxYear].
        // 5. A glacier qualifies ("benchmark rule") when:
        //      - it has more than MinimumYearsOfRecords distinct years of annual_balance, AND
        //      - decadeWindow has at most MaximumRecentGapYears years with no record for that glacier.
        // 6. For each qualifying glacier: meanBalance = average of ALL its annual_balance values
        //    (its own full history, not just the decade); anomaly(year) = annual_balance(year) - meanBalance.
        // 7. Group anomalies by year across all qualifying glaciers; drop any year with fewer than
        //    MinimumContributingGlaciers contributing glaciers (avoids single-glacier years, e.g.
        //    1885, dominating the early record with unrepresentative noise).
        // 8. globalIndex(year) = simple mean of that year's anomalies (equal weight per glacier —
        //    deliberately NOT weighted by glacier area/count, so one heavily-instrumented region
        //    doesn't dominate; matches the user's "avoid biasing toward whichever glaciers happen
        //    to have data" requirement).
        // 9. Write "Year,Value" header + one "{year},{value:0.###}" line per remaining year, sorted.
        //10. Throw InvalidDataException if zero qualifying glaciers or zero output years (mirrors
        //    OceanAcidity/Ozone's "contained no usable measurements" guard).
    }
}
```

Add `CsvHelper` (already used by `ClimateExplorer.Data.Ghcnd`, version
`33.1.0`) as a `PackageReference` to `ClimateExplorer.Data.Downloading` — a
hand-rolled quote-aware CSV splitter is avoidable complexity when a
well-tested one is already a dependency elsewhere in the solution, and the
existing transformers (`OceanAciditySourceFileTransformer`,
`OzoneSourceFileTransformer`) only got away with `string.Split` because their
source formats are tab/simple-comma-delimited with no embedded punctuation —
`mass_balance.csv` genuinely has quoted commas, so this isn't over-engineering.

### 5. New `DataSetDefinition` (`DataSetDefinitionsBuilder.cs`, `BuildOtherDataSetDefinitions()`)

Added alongside the Arctic/Antarctic sea ice extent and Greenland ice melt
entries (same "single global region, computed index" shape):

```csharp
new()
{
    Id = Guid.Parse("E970C6DA-564E-4768-8FC4-3E46B4B8776F"),
    Name = "Glacier mass balance",
    ShortName = "Glacier mass balance",
    Description = "A global glacier mass balance index, built from the World Glacier Monitoring Service's Fluctuations of Glaciers database. Includes every 'Benchmark' glacier — more than 10 years of ongoing glaciological mass-balance measurements, with at most one year's gap in the past decade. Each glacier's annual balance is expressed as a deviation from its own long-term mean (in metres water equivalent) before averaging, so glaciers with longer or more complete records don't dominate the global signal.",
    Publisher = "World Glacier Monitoring Service (WGMS)",
    PublisherUrl = "https://wgms.ch/",
    MoreInformationUrl = "https://wgms.ch/products_ref_glaciers/",
    DataDownloadUrl = "https://wgms.ch/downloads/DOI-WGMS-FoG-2026-02-10.zip",
    DataDownloaderKey = "wgms-glacier-mass-balance",
    MeasurementDefinitions =
    [
        new()
        {
            DataType = DataType.GlacierMassBalance,
            UnitOfMeasure = UnitOfMeasure.MetresWaterEquivalent,
            DataResolution = DataResolution.Yearly,
            DataAdjustment = null,
            DataRowRegEx = @"^(?<year>\d{4}),(?<value>-?\d+\.\d+)$",
            DataFileSource = LooseSource(@"Glaciers\wgms-glacier-mass-balance-index.csv"),
        },
    ],
},
```

`DataResolution.Yearly` already has a working end-to-end precedent
(`CO2Emissions`, `Ozone`/ODGI in `DataSetDefinitionsBuilder.Atmosphere.cs`) and
a 28-day freshness refresh interval in `DataSetFreshnessPolicy` — no changes
needed there.

### 6. `DataFileMapping_Glaciers.json` (`ClimateExplorer.WebApi/MetaData/DataFileMapping/`)

Mirrors `DataFileMapping_Greenland.json`'s shape exactly (one region, no
`[station]` token used in the path):

```json
{
  "DataSetDefinitionId": "E970C6DA-564E-4768-8FC4-3E46B4B8776F",
  "LocationIdToDataFileMappings": {
    "B91B639B-5625-461B-BCB7-5E0783EA7FFD": [
      { "Id": "Glaciers" }
    ]
  }
}
```

### 7. DI wiring (`ClimateExplorer.WebApi/Program.cs`)

```csharp
builder.Services.AddSingleton<IDataSetDownloader>(
    services => new TransformingDataSetDownloader(
        "wgms-glacier-mass-balance",
        services.GetRequiredService<DataSetHttpFileDownloader>(),
        new WgmsGlacierMassBalanceSourceFileTransformer()));
```

Add next to the `ocean-acidity`/`sea-level`/`ozone` registrations.

### 8. Source data mirror (`ClimateExplorer.SourceData/Glaciers/wgms-glacier-mass-balance-index.csv`)

Per the CO2-domain-work precedent (see
[[project_co2_domain_plan_implemented]]): `DataPackageDefinitionTests`/
`DataSetDownloadMetadataTests` validate a `DataFileSource`'s `DataRowRegEx`
against a real sample file at `Folders.SourceDataFolder`, not against
`ClimateExplorer.WebApi/Datasets/`. Add a small hand-written sample CSV here
(a handful of `Year,Value` rows in the transformer's output shape) — mirrors
`ClimateExplorer.SourceData/Greenland/greenland-melt-area.csv`.

### 9. `ClimateExplorer.WebApi/Datasets/Glaciers/wgms-glacier-mass-balance-index.csv`

The actual runtime asset. Seed it once by running the transformer offline
against a freshly downloaded zip (or a small script), same bootstrap step
every other `TransformingDataSetDownloader`-backed dataset needed at
introduction — the automated-download pipeline keeps it fresh afterwards
(see [2026-07-13-01-automated-dataset-downloads-plan.md](2026-07-13-01-automated-dataset-downloads-plan.md)).

### 10. `SuggestedPresetLists.Global.cs`

Add a lookup alongside the other region/measurement lookups:

```csharp
var glacierMassBalance = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(dataSetDefinitions, Region.RegionId(Region.Glaciers), DataType.GlacierMassBalance, null, throwIfNoMatch: true);
```

Insert a new top-level preset **after** the "Sea ice extent" preset (whose
"Greenland ice melt area" variant is the anchor point the user named) and
before "Solar irradiation + sunspots":

```csharp
suggestedPresets.Add(
    new SuggestedChartPresetModelWithVariants()
    {
        Title = "Global glacier mass balance",
        Description = "Bar chart of the global glacier mass balance anomaly, averaged across WGMS benchmark glaciers, each expressed as a deviation from its own long-term mean",
        ChartSeriesList =
        [
            new ChartSeriesDefinition()
            {
                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(Region.GetRegion(Region.Glaciers), glacierMassBalance!),
                Aggregation = SeriesAggregationOptions.Mean,
                BinGranularity = BinGranularities.ByYear,
                Smoothing = SeriesSmoothingOptions.None,
                SmoothingWindow = 10,
                Value = SeriesValueOptions.Value,
                DisplayStyle = SeriesDisplayStyle.Bar,
                GroupingThreshold = 0.05f,
                RequestedColour = UiLogic.Colours.Blue,
            },
        ],
    });
```

Modelled on the existing "Global temperature anomaly" bar-chart preset — same
shape (a single precomputed anomaly-index series, bar display). No `Variants`
for the first cut; a smoothed line variant (`MovingAverage`, window 10) can be
added the same way `Sea ice extent` has variants, if wanted later.

## Testing

- `Enums`-level: extend any exhaustive `UnitOfMeasure`/`DataType` switch
  coverage tests if they exist (check for a test that asserts every enum
  value has a label/rounding case — the existing `NotImplementedException`
  fallback in each switch means a missing case fails loudly at first use
  either way).
- `WgmsGlacierMassBalanceSourceFileTransformerTests.cs`: benchmark-rule
  filtering (glacier with exactly 10 years excluded, 11 included; glacier with
  a 2-year gap in the recent decade excluded, 1-year gap included); anomaly
  computation (a glacier's own mean, not the global mean); the
  minimum-contributing-glaciers cutoff drops sparse years; malformed/empty
  `annual_balance` rows are skipped, not treated as zero; missing
  `data/mass_balance.csv` entry in the zip throws `InvalidDataException`.
- `DataSetDefinitionsBuilderTests.cs`: the new definition resolves via
  `Region.RegionId(Region.Glaciers)` / `DataType.GlacierMassBalance`.
- `DataSetDownloadMetadataTests.cs`: add `"wgms-glacier-mass-balance"` to the
  downloader-key list at line 27 (`DataSetDownloadMetadataTests.cs`); confirm
  the new asset's relative path and downloader key.
- `DataPackageDefinitionTests.cs`: will exercise `DataRowRegEx` against the
  `ClimateExplorer.SourceData/Glaciers/` sample automatically once it's added
  — confirm it passes (this is exactly the check the CO2 domain work's memory
  note flags as easy to miss).
- `SuggestedPresetLists`-level: whatever existing test (if any) walks every
  suggested preset's `SourceSeriesSpecifications` and asserts they resolve
  against `DataSetDefinitionsBuilder.BuildDataSetDefinitions()` output will
  cover the new preset for free.

## Open questions to resolve during implementation, not here

- Exact `MinimumContributingGlaciers` cutoff (5 recommended above, based on
  live data: pushes the effective series start to 1946; a threshold of 3 gives
  the same start year, 10 pushes it to 1953) — a judgement call, not a
  correctness question.
- Whether to expose the per-glacier contributing count anywhere in the UI
  (e.g. a tooltip caveat for early, thin years) — no existing precedent for
  this in another dataset, so probably not worth inventing new UI for a first
  cut.
- Whether `ZipArchive` entry lookup should tolerate a different top-level
  folder name in a future WGMS release (this release nests everything under
  `data/`) — match by filename suffix (`EndsWith("mass_balance.csv")`) rather
  than the exact full entry path, to be a little more resilient to that.

## Out of scope

- **Per-glacier locations/stations.** The user's "ideally" framing — treating
  each glacier as its own `Location` with its own raw (non-anomaly) `m w.e.`
  series, the way GHCNd/ECA&D treat individual weather stations — is a
  substantially larger undertaking: ~500 new `Location` entities (needing
  coordinates from `glacier.csv`), a `DataFileMapping` with ~500 entries
  instead of 1, ~500 individual per-glacier CSV files, and a
  `StationMetadataFileName` glacier-metadata file. This plan ships the single
  computed global index instead, which is enough to chart the headline signal
  the user described and matches the "may just need to be done as a custom
  Transformer" fallback the user proposed. Revisit as a new stage (not a new
  doc) if per-glacier browsing turns out to be wanted.
- **Auto-discovering the latest WGMS release URL.** Accepted as a manually-bumped
  dated URL (see "Download + transform pipeline" above), matching the existing
  TSI precedent.
- **Winter/summer balance, ELA, AAR, or any other `mass_balance.csv` column.**
  Only `annual_balance` is in scope, per the user's column list.
- **`mass_balance_point.csv` / `mass_balance_band.csv`** (glacier-flagged
  point/elevation-band level detail) — out of scope; `mass_balance.csv`'s
  whole-glacier annual figure is sufficient for a global index.

## Addendum — implementation notes (2026-08-27)

Implemented per the user's direction on three points, each a deviation from
the original proposal above:

1. **Reused the existing `"Land"` region** (`6FA62EA0-F9EC-46CB-A9E5-F610EB6BAC5E`)
   instead of adding a new `Region.Glaciers`. `Model/Region.cs` was not
   touched at all. `DataFileMapping_WgmsGlacierMassBalance.json` (named after
   the dataset, not the shared region — matching `OceanAcidity`/`OzoneHoleArea`'s
   precedent of a dataset-named file mapped onto a shared region) keys its one
   entry on the `Land` region's GUID.
2. **`MinimumContributingGlaciers = 5`** shipped as proposed.
3. **No DI wiring in `ClimateExplorer.WebApi/Program.cs`.** The
   `TransformingDataSetDownloader` for `"wgms-glacier-mass-balance"` is
   registered only in `ClimateExplorer.Data.Misc/Program.cs`'s downloader
   list, alongside `ocean-acidity`/`sea-level`/`ozone`. The zip URL is not
   duplicated there — it's read once from `DataSetDefinitionsBuilder.cs`'s
   `DataDownloadUrl` (the shared metadata both WebApi's and Data.Misc's
   `DataSetSourceAssetResolver` read from), so bumping it on a future WGMS
   release only means editing that one field. Confirmed by reading
   `DataSetSourceUpdateCoordinator.RefreshAsync`: when a `DownloaderKey`
   isn't found in a host's registered downloader list, it logs a warning and
   returns `null`, which the caller treats as "refresh failed" and falls back
   to the already-published static file — so WebApi safely serves the
   committed CSV without ever attempting a live 39MB download, and only a
   manual run of `ClimateExplorer.Data.Misc` (intended ~annually) refreshes it.

**Also discovered while implementing** (not anticipated in the plan above):
`ClimateExplorer.SourceData/<Dataset>/...` mirrors are full, real duplicates
of `ClimateExplorer.WebApi/Datasets/<Dataset>/...` (confirmed byte-identical
for `Greenland` and `OceanAcidity`), not small hand-written samples as
originally assumed — `DataSetDownloadMetadataTests.ValidateAsync_StageOnePackagedSources_AllMatchTheirConfiguredReaders`
validates every asset's `DataRowRegEx` against `Folders.SourceDataFolder`
specifically. Both copies were bootstrapped with genuine output: the
transformer's algorithm was run by hand (Python, matching the C# logic
exactly) against the real 2026-02-10 WGMS release, producing 78 years
(1946–2025, from 138 Benchmark-qualifying glaciers) written identically to
both `ClimateExplorer.WebApi/Datasets/Glaciers/wgms-glacier-mass-balance-index.csv`
and `ClimateExplorer.SourceData/Glaciers/wgms-glacier-mass-balance-index.csv`.

**Files touched:**
- `ClimateExplorer.Core/Enums.cs` — `DataType.GlacierMassBalance`,
  `UnitOfMeasure.MetresWaterEquivalent` (+ its three label/rounding switches).
- `ClimateExplorer.Core/DataSetDefinitionsBuilder/DataSetDefinitionsBuilder.cs` —
  new `DataSetDefinition` in `BuildOtherDataSetDefinitions()`, inserted after
  "Greenland ice melt area".
- `ClimateExplorer.Data.Downloading/Transformers/WgmsGlacierMassBalanceSourceFileTransformer.cs` —
  new, as designed above (`CsvHelper`-based, quote-safe).
- `ClimateExplorer.Data.Downloading/ClimateExplorer.Data.Downloading.csproj` —
  added explicit `CsvHelper` `PackageReference` (was previously only a
  transitive dependency via the `ClimateExplorer.Data.Ghcnd` project
  reference — made explicit rather than relied upon).
- `ClimateExplorer.WebApi/MetaData/DataFileMapping/DataFileMapping_WgmsGlacierMassBalance.json` — new.
- `ClimateExplorer.Data.Misc/Program.cs` — downloader registration (see point 3 above).
- `ClimateExplorer.WebApi/Datasets/Glaciers/wgms-glacier-mass-balance-index.csv`
  and `ClimateExplorer.SourceData/Glaciers/wgms-glacier-mass-balance-index.csv` — new, real data.
- `ClimateExplorer.Web.Client/UiModel/SuggestedPresetLists.Global.cs` — new
  "Global glacier mass balance" preset, inserted after "Sea ice extent"
  (whose "Greenland ice melt area" variant was the user's anchor point) and
  before "Solar irradiation + sunspots".
- `ClimateExplorer.UnitTests/DataSetSourceFileTransformerTests.cs` — 8 new
  test methods for the transformer (benchmark-rule qualification/gap/year-count
  exclusion, minimum-contributing-glaciers cutoff, malformed-row skipping, the
  three `InvalidDataException` paths), added to this existing shared file
  rather than a new per-transformer file, matching the file's established
  convention of covering every `IDataSetSourceFileTransformer` in one place.
- `ClimateExplorer.UnitTests/DataSetDownloadMetadataTests.cs` — asset count
  2002 → 2003, `"wgms-glacier-mass-balance"` added to the downloader-key list.

**Verification:** `dotnet build ClimateExplorer.sln` clean (no new warnings).
Full `ClimateExplorer.UnitTests` suite: 558 passed, 0 failed, 0 skipped. Per
[[feedback_no_playwright_or_dev_servers]], the site itself was not run - the
new preset's UI rendering is unverified.

**Follow-ups not done:** none required by this plan; the "Out of scope" items
above (per-glacier locations, auto-discovered release URL, other
`mass_balance.csv` columns) remain deliberately unbuilt.
