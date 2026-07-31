# Add European Climate Assessment & Dataset (ECA&D) as a preferred daily source

- **Date:** 2026-07-30
- **Status:** Proposed
- **Author:** Patrick Lea (with Claude)
- **Scope:** `ClimateExplorer.Core` (`Station`, `DataSetDefinitionsBuilder`), a new `ClimateExplorer.Data.Ecad` offline build tool, `ClimateExplorer.Data.Downloading` (new `EcadDataSetDownloader`), `ClimateExplorer.WebApi` (DI wiring), `ClimateExplorer.Data.Ghcnd` (WMO ID extraction, for the future blended path)
- **Branch context:** `issues/eca-and-d`

## Goal

Add ECA&D (`https://www.ecad.eu/`) as a daily temperature/precipitation source for
locations whose station is registered with ECA&D, and prefer it over GHCNd for
those locations, because ECA&D updates more frequently than GHCNd does for
European stations. ECA&D publishes two parallel editions of every series —
**blended** (gap-filled/homogenized) and **non-blended** (raw, as submitted by the
national participant) — which map onto this codebase's existing
`DataAdjustment.Adjusted` / `DataAdjustment.Unadjusted` distinction. A user can
request either.

**Current scope: non-blended (`DataAdjustment.Unadjusted`) only.** See "What's
actually available today" below — the blended API isn't shipped yet, so this plan
implements the half that's real and leaves blended as an explicitly-flagged
fast-follow.

## What's actually available today (verified against the live APIs, not assumed)

This required two rounds of research because the first round found a plausible
but wrong answer. Recorded here so the next reader doesn't repeat the same dead
end.

**Round 1** looked at `https://api.meteogate.eu/eu-eumetnet-climate-observations/v1/collections/eu-daily`
and found a single collection with WIGOS-style station ids (`0-20000-0-{WMOID}`,
zero-padded 5 digits — confirmed `0-20000-0-06260` is De Bilt, NL, matching
`NLM00006260`'s WMO number 06260) and no trace of a blend/non-blend selector
anywhere in its parameter catalog or query parameters (a guessed `souid=non-blend`
query param was silently accepted and ignored — a strong sign it isn't a real,
validated parameter for this collection). Its own collection description text
("...in the Netherlands... more than 50 automatic weather stations...") turned
out to be copy-pasted boilerplate from a sibling KNMI-only collection and
contradicts the verified data (stations from NL, HU, and NO were all present) —
so the *data* looks genuinely pan-European even though the *metadata* is
untrustworthy. **Whether `eu-daily` is, or will become, the blended ECA&D
collection is unconfirmed** — don't assume it without re-checking when blended
actually ships.

**Round 2**, prompted by the user pointing at a new announcement, found the real
non-blended collection: **`ecad-nonblended`** at the same base URL
(`https://api.meteogate.eu/eu-eumetnet-climate-observations/v1`), demonstrated in
[github.com/ECA-D/ecad_rodeo_demo](https://github.com/ECA-D/ecad_rodeo_demo)
(MeteoGate/RODEO project). Verified directly:

- Collection title is literally **`[PRE-RELEASE] Daily in-situ ground-based
  meteorological surface observations focused on Europe from ECA&D`** — treat
  this integration as depending on a beta API that can still change shape.
- Description: *"The non-blended series are the series as provided by the
  participants."* — matches ECA&D's own FAQ definition exactly.
- Temporal extent starts 1756-01-02 (older than `eu-daily`'s 1829, consistent
  with non-blended being the un-truncated raw feed).
- **Station ids are `ecad_{staid:07d}`, not WIGOS/WMO-based** — e.g. De Bilt is
  `ecad_0000162` here (confirmed same station: name "De Bilt", coordinates
  `[5.17944, 52.09889]`, matching `0-20000-0-06260`'s coordinates from round 1).
  A sample `/locations?bbox=...` feature's `properties` has `station_name`,
  `country_code`, `height_above_mean_sea_level`, and a data-provider attribution
  object — **no `wmoId` field**. This means **the WMO crosswalk this plan
  originally built (`Station.WmoId`, `ghcnd-stations.txt`) does not link
  non-blended stations at all** — a different linking mechanism is needed (below).
- **Parameter ids are ECA&D's native short codes with a large per-convention
  fan-out**, not the self-describing `air_temperature:1.5:mean:-P1D` style from
  `eu-daily`: `tg1`–`tg24` (24 mean-temperature variants), `tn1`–`tn19` (19
  minimum-temperature variants), `tx1`–`tx19` (19 maximum-temperature variants),
  `rr1`–`rr23` (23 precipitation variants). The numbered variants encode each
  contributing country's own daily-accumulation-period convention (00-00 UTC,
  06-06 UTC, 12-12 UTC, 18-18 UTC, local civil day, "unknown", etc.) — exactly the
  kind of inhomogeneity blending exists to remove, so a raw/non-blended station
  will only ever populate one or two of these ~20 variants for a given
  measurement type, and *which* one varies by country. There's a hint that a
  station's feature properties may list which parameter codes it actually reports
  (an example bbox query showed stations paired with codes like `rr2`, `sd3`,
  `dd1`, `fg1`) — verify the exact property name/shape during implementation; if
  it holds, use it to pick the right variant per station instead of guessing.
- Any unbounded request (no `datetime`/`parameter-name` filter) returns **HTTP
  413 Payload Too Large** — same lesson as round 1: always bound `datetime`, and
  bound `parameter-name` to the relevant candidate variants rather than
  requesting everything.
- Spatial extent bbox is much wider than Europe (`[-179.37, 22.80, 179.32,
  83.66]`) — ECA&D's participant network extends beyond Europe in this
  collection; not a problem, just don't assume "European-looking longitude" is a
  useful filter.

**Net effect on this plan:** there are two structurally different linking
problems, one per adjustment, and only one of them is buildable right now.

| | Non-blended (`Unadjusted`) — **build now** | Blended (`Adjusted`) — **fast-follow, not yet available** |
|---|---|---|
| Collection | `ecad-nonblended` (confirmed, pre-release) | unconfirmed — re-verify against `eu-daily` or whatever EUMETNET ships |
| Station id | `ecad_{staid:07d}` | `0-20000-0-{wmoId}` (WIGOS) |
| Link to our GHCN station | No WMO id exposed — **name + coordinate matching** | WMO id crosswalk (`Station.WmoId`) |
| Parameter selection | Superset of `tg#`/`tn#`/`tx#`/`rr#` candidates, take whichever is populated | Superset of `1.5`/`2.0` height variants, prefer `1.5` |

## How this fits the existing architecture

(Unchanged from the original research — still holds regardless of which
adjustment is in play.)

1. **Every dataset's location coverage is a static, offline-built, checked-in
   JSON file** (`DataFileMapping_*.json` under `MetaData/DataFileMapping`),
   produced by a standalone console project a maintainer runs manually. There is
   no live per-request "does this location have source X" call anywhere today.
   This plan adds an **offline discovery step**, matching every existing source,
   rather than a live check in the request path.
2. **Source preference between two datasets covering the same location is
   decided purely by declaration order** in
   `DataSetDefinitionsBuilder.BuildDataSetDefinitions()` — see
   [2026-07-14-01-retire-recent-observations-plan.md](2026-07-14-01-retire-recent-observations-plan.md).
   `ClimateRecordsEndpoints.GetClimateRecords` walks datasets in list order and
   takes the first matching `(DataType, DataAdjustment, DataResolution)`. BOM is
   declared before GHCNd today for exactly this reason, with a regression test
   locking it in. Declaring ECA&D before GHCNd, with a matching `DataAdjustment`
   on each measurement, gets the preference for free — no endpoint changes.
3. **A downloader that needs "only what's new since last time" reads the
   already-published file itself.** `GreenlandDataSetDownloader` is the existing
   precedent: it takes `DataSetSourceFileStore`, reads whatever was last
   published, fetches only what's missing, writes a merged candidate file. The
   ECA&D downloader follows this pattern (keyed by date) rather than changing
   `IDataSetDownloader` or the state store.

## Design

### 1. Station linking

**Non-blended (build now):** a new offline step, in the new
`ClimateExplorer.Data.Ecad` console tool, fetches
`.../collections/ecad-nonblended/locations` once (the bulk GeoJSON listing — a
real `HttpClient` call, not bound by whatever size limit tripped up ad hoc
exploration here) and matches each feature to a GHCN station by:

1. Coordinate proximity (round to ~2 decimal places, same tolerance style already
   used by `ClimateExplorer.Data.Ghcnm/Program.cs`'s `RemoveDuplicateLocations`
   for reconciling pre-existing locations against newly-clustered ones), **and**
2. Name similarity (case/underscore-normalized equality, same normalization
   `RemoveDuplicateLocations` already applies) as a corroborating signal, not a
   sole key (station names differ enough between GHCN and ECA&D's own naming that
   name-only matching will miss real matches).

Treat this as a genuine reconciliation step, not a guaranteed 1:1 join: log and
skip ambiguous cases (multiple candidates within tolerance) the same way
`StationFile.Load` already logs and drops rows it can't confidently resolve,
rather than silently picking one. Persist the confirmed matches to
`MetaData/EcadNonBlendedStationIds.json` (`Dictionary<string,string>`, GHCN id →
`ecad_XXXXXXX`), mirroring the `GhcnIdToLocationIds.json` pattern.

**Blended (fast-follow, not yet buildable):** once the real blended collection is
confirmed, this reuses `Station.WmoId` (add the field to
`ClimateExplorer.Core/Model/Station.cs` now, since it's cheap and useful
regardless) populated from NOAA's `ghcnd-stations.txt` — its exact fixed-width
layout is already documented in this repo's own
`ClimateExplorer.Data.Ghcnd/readme.txt` (`WMO ID` at columns 81-85) — giving a
clean, deterministic `location_id = "0-20000-0-" + wmoId.PadLeft(5, '0')` with no
fuzzy matching needed. Don't build the rest of this path yet (no
`DataFileMapping_ecad_adjusted.json`, no blended downloader) until the collection
is confirmed — building against an unconfirmed, possibly-still-unshipped
collection risks throwaway work.

### 2. New offline tool: `ClimateExplorer.Data.Ecad`

Console project, sibling to `ClimateExplorer.Data.Ghcnd`, run manually and
periodically (same operational model as the existing GHCN tooling). For the
non-blended path:

1. Load the GHCN station list (`Folders.SelectedStationsFile`) and the GHCN
   id → location id mapping already produced by `ClimateExplorer.Data.Ghcnm`.
2. Fetch `ecad-nonblended`'s `/locations` bulk listing; run the coordinate+name
   match from step 1 above to build `EcadNonBlendedStationIds.json`.
3. For each matched station, bootstrap its full history now (a one-off wide
   `datetime` range is fine for the build tool; never do this in the runtime
   downloader — the 413s found above confirm why) across the parameter-variant
   superset for each of TempMean/TempMax/TempMin/Precipitation, picking whichever
   variant is actually populated per date. Write
   `ClimateExplorer.WebApi/Datasets/Ecad/Unadjusted/{ghcnId}.csv` in the
   `Date,TempMean,TempMax,TempMin,Precipitation` shape the runtime downloader
   will maintain incrementally. Also copy at least one sample station's file into
   `ClimateExplorer.SourceData/Ecad/Unadjusted/` — `DataPackageDefinitionTests`/
   `DataSetDownloadMetadataTests` validate against that folder specifically (see
   the CO2 domain work's precedent: missing that second copy fails with
   "contained no finite measurements").
4. Write `MetaData/DataFileMapping/DataFileMapping_ecad_unadjusted.json`
   (`DataSetDefinitionId` = the new ECA&D `DataSetDefinition.Id`, mapping only
   matched locations to their GHCN station id). **Do not touch the GHCNd mapping
   files** — ECA&D wins by declaration order alone (see below), so this stays
   purely additive/reversible.

The `Unadjusted` subfolder in both dataset paths is deliberate even though there's
only one adjustment today — it means adding blended later is a pure addition
(`Ecad\Adjusted\{ghcnId}.csv` + new measurement definitions), not a rename of
already-shipped paths.

### 3. `DataSetDefinitionsBuilder.Ecad.cs` (new partial file)

One `DataSetDefinition` today, all four measurements bundled together (same
reasoning as BOM: keeps cross-type consistency structural, not an accident of
declaration order — this is exactly the smell the retirement plan flagged in the
old split GHCNd/GHCNdp definitions):

```csharp
new()
{
    Id = Guid.Parse("265289F3-D375-437C-A642-A5EC49C8B5F7"),
    Name = "European Climate Assessment & Dataset (ECA&D)",
    ShortName = "ECA&D",
    Publisher = "Royal Netherlands Meteorological Institute (KNMI)",
    PublisherUrl = "https://www.ecad.eu/",
    MoreInformationUrl = "https://www.ecad.eu/dailydata/index.php",
    DataDownloaderKey = "ecad-station",
    MeasurementDefinitions =
    [
        new() { DataType = DataType.TempMean, DataAdjustment = DataAdjustment.Unadjusted, DataResolution = DataResolution.Daily,
                DataFileSource = LooseSource(@"Ecad\Unadjusted\[station].csv"), DataRowRegEx = @"...group 2..." },
        new() { DataType = DataType.TempMax, DataAdjustment = DataAdjustment.Unadjusted, ... DataRowRegEx = @"...group 3..." },
        new() { DataType = DataType.TempMin, DataAdjustment = DataAdjustment.Unadjusted, ... DataRowRegEx = @"...group 4..." },
        new() { DataType = DataType.Precipitation, DataAdjustment = null, ... DataRowRegEx = @"...group 5..." },
    ],
    // StationMetadataFileName intentionally omitted for now — see the station-metadata note below.
},
```

Wire it into `BuildDataSetDefinitions()` **before** `BuildGhcnDataSetDefinitions()`
(order relative to BOM doesn't matter — no geographic overlap):

```csharp
dataSetDefinitions.AddRange(BuildBomDataSetDefinitions());
dataSetDefinitions.AddRange(BuildEcadDataSetDefinitions());
dataSetDefinitions.AddRange(BuildGhcnDataSetDefinitions());
```

**`DataAdjustment` alignment with GHCNd:** in this codebase, `DataAdjustment`
(Adjusted/Unadjusted) is a temperature-only concept — precipitation and every
other data type always carry `DataAdjustment = null` (confirmed by the user;
matches GHCNd's and GHCNm's own precipitation definitions, both `null`). ECA&D
follows the same rule: `TempMean`/`TempMax`/`TempMin` are tagged
`DataAdjustment.Unadjusted` (matching GHCNd's `TempMax`/`TempMin`, so ECA&D
correctly preempts GHCNd for those by declaration order alone; GHCNd has no daily
`TempMean` at all, so ECA&D's is net-new rather than a replacement), and
`Precipitation` is tagged `null` (matching GHCNd's `Precipitation`, so it
preempts the same way, no gap). There is no future "blended precipitation" tier
to design around: only temperature ever splits by blended/non-blended, so
precipitation stays a single canonical series (sourced from non-blended today)
indefinitely, not a second competing measurement definition.

**Station metadata display:** the existing `StationMetadataFileName` mechanism
(`DataSetMetadataBuilder.BuildStationsAsync` → `StationMetadataLookup`) looks a
station up by the exact `Id` used in the mapping, in a JSON file keyed the same
way. Since non-blended's `Id` is `ecad_XXXXXXX` (not a GHCN id — unlike every
other GHCN-family dataset, which is why the earlier draft of this plan assumed it
could reuse `Stations_ghcnm_adjusted.json` for free), reusing that shared file
won't work unmodified. Options to resolve during implementation: keep the mapped
`Id` as the GHCN id (translate to `ecad_XXXXXXX` only inside the downloader, via
the crosswalk file) so the existing shared station file keeps working for free —
preferred, consistent with how GHCNd/GHCNm do it — versus introducing a new
`Stations_ecad.json` keyed by `ecad_XXXXXXX`. Lean toward the former unless it
turns out `DataSetMetadataBuilder`/`DataSetSourceAssetResolver` need the mapped
`Id` to literally be what's substituted into `DataFileSource`/`DataDownloadUrl`
templates (in which case the downloader-side translation still works the same
way — `[station]` stays the GHCN id for file paths, and the downloader
separately looks up the `ecad_XXXXXXX` id for the API call, exactly as originally
planned for the WMO crosswalk).

### 4. `EcadDataSetDownloader` (new, in `ClimateExplorer.Data.Downloading`)

```csharp
public sealed class EcadDataSetDownloader(
    HttpClient httpClient,
    DataSetSourceFileStore sourceFileStore,
    IReadOnlyDictionary<string, string> ghcnIdToEcadStationId) : IDataSetDownloader
{
    public string Key => "ecad-station";
    // ...
}
```

Following the `GreenlandDataSetDownloader` precedent:

1. Resolve `ecad_XXXXXXX` for the request's station id (`[station]` = GHCN id)
   from the crosswalk.
2. Read the already-published CSV via
   `sourceFileStore.ResolvePath(request.RelativePath)`; find the last saved date.
3. Call `.../collections/ecad-nonblended/locations/{ecadStationId}` with
   `parameter-name` set to the full candidate list for whichever measurements are
   requested (e.g. all of `tg1..tg24` for TempMean) and
   `datetime={lastSavedDate.AddDays(1)}T00:00:00Z/..`.
4. Per date, take the first populated variant per measurement family (log if more
   than one variant is ever simultaneously populated for the same
   date/measurement — that would mean the "one convention per station" assumption
   doesn't hold and needs a real tie-break rule, not a silent pick).
5. Merge new rows onto existing ones; write the merged CSV as the candidate file.
6. Throw `InvalidDataException` if the station id 404s or returns no coverage —
   the offline tool already confirmed the match, so treat it as a hard failure.

Register in `ClimateExplorer.WebApi/Program.cs` next to the other downloaders,
and add `"ecad-station"` to `DataSetFreshnessPolicy.ContentAwareDownloaderKeys`
(alongside `bom-station`/`ghcnd-station`) — `DataSetDownloadValidator`'s existing
generic latest-record-date tracking applies for free, no change needed there.

## Testing

- `DataSetDefinitionOrdering_EcadPrecedesGhcndForTempMaxTempMinAndUnadjustedTempMean`
  in `DataSetDefinitionsBuilderTests.cs` — same shape as the existing
  BOM-before-GHCNd test.
- A station-linking test for the coordinate+name matcher: exact match, ambiguous
  match (skipped/logged, not guessed), no match.
- `EcadDataSetDownloaderTests.cs`: incremental fetch only requests dates after the
  last published record; parameter-variant fallback (only the 12-12 UTC variant
  populated still yields a value); merge preserves previously-published rows;
  unmatched/missing station id fails loudly.
- Extend `DataSetDownloadMetadataTests.cs`'s downloader-key list; check whether it
  needs the same carve-out `ghcnd-station` gets from the generic "every asset
  matches its configured reader" loop before assuming it does.

## Open questions to resolve during implementation, not here

- Exact property name/shape for a station's reported parameter codes on
  `ecad-nonblended` (the `rr2`/`sd3`/`dd1`/`fg1` hint from a bbox query) — if it
  exists, use it instead of requesting the full ~20-variant superset per station.
- Whether `ecad-nonblended`'s `/locations` bulk listing paginates.
- Exact CoverageJSON null representation for missing days (not exercised here).

## Out of scope

- **Blended (`DataAdjustment.Adjusted`) data** — the API isn't shipped yet per
  EUMETNET; `Station.WmoId` is added now because it's cheap, but the mapping
  file, `DataSetDefinition` measurements, and downloader path for blended are
  deferred until the collection is confirmed. Revisit this doc (new stage, not a
  new doc) once it ships. When it does, only `TempMean`/`TempMax`/`TempMin` gain
  an `Adjusted` variant (`Ecad\Adjusted\{ghcnId}.csv`) — per the alignment note
  above, `Precipitation` has no blended counterpart to add; it keeps reading from
  the non-blended file it already uses.
- Monthly ECA&D data — GHCNm remains the monthly source for all locations,
  unconditionally.
- Any change to `ClimateRecordsEndpoints`, `RecentObservationsService`, or any
  other consumer — they get ECA&D for free once it's declared before GHCNd and
  its mapping is populated (modulo the `Precipitation`/`DataAdjustment` UI gap
  noted above).
- Pruning GHCNd's own mapping/refresh for now-ECA&D-covered locations — left
  running as-is.
