# Automated dataset downloads for historical API data

- **Date:** 2026-07-13
- **Status:** Proposed
- **Author:** Codex
- **Scope:** dataset download metadata, `/dataset` and `/climate-record` request flow, dataset file loading/storage, Web API dependency injection, `ClimateExplorer.Data.Misc`, and related unit tests
- **Branch context:** `development`

## Goal

Move the regional/global download work currently performed by
`ClimateExplorer.Data.Misc` into the Web API data paths used by `/dataset` and
`/climate-record`. The shared monthly Mauna Loa atmospheric CO₂ source used by
both `CO2` and `CO2Deseasoned` is also in scope even though the current
`globalDataDefinitions` predicate skips it. Historical unadjusted daily BOM and
GHCNd station data are also in scope once their deployed storage is reorganised
into independently replaceable station archives.

For datasets that have opted into automatic retrieval, a request should:

1. use `ICachedData.RetrievedDate` to decide whether its cached response and
   source assets are fresh;
2. refresh daily data after 24 hours and monthly data after 28 days, based on
   the requested `MeasurementDefinition.DataResolution`;
3. download into an isolated temporary directory;
4. validate the downloaded and, where applicable, transformed data;
5. publish a last-known-good source asset without exposing a partial write;
6. update retrieval state, rebuild/cache the response, and return it;
7. return the existing cached response if download, transformation, validation,
   publication, or rebuilding fails; or, when no response has yet been cached,
   read the existing source file, cache that response, and return it.

`/recent-observations` endpoint behavior, caching, and response construction are
explicitly out of scope. Its BOM/GHCNd clients and parsers are useful lower-level
building blocks for the historical downloader, but the endpoint should not be
routed through the new coordinator or changed to use the historical cache.

The end state for `ClimateExplorer.Data.Misc/Program.cs` should ideally be:

```csharp
await BuildStaticContent.GenerateSiteMap();
GenerateMapMarkers();
```

Deployment continues to include a source-data package. For automatically managed
datasets, the deployed file and the subsequently downloaded file are the same
physical source file: a successful refresh atomically replaces it in place.
There is no separate packaged fallback copy or runtime-override tree.

## Current flow and constraints

`DataSetEndpoints.PostDataSets` currently checks a file-backed response cache
before it resolves or reads any source data. A cache hit is returned without a
freshness check. New responses do not set `DataSet.RetrievedDate`.

`ClimateRecordsEndpoints.GetClimateRecords` resolves a dataset and measurement,
then calls `DataSetEndpoints.PostDataSets`. It does not have a separate response
cache and does not copy the underlying dataset's retrieval time into
`ClimateRecordsResponse.RetrievedDate`.

`DataSetBuilder` and `SeriesProvider` ultimately read data through
`DataReaderFunctions` or `TwelveMonthPerLineDataReader`. The current file cascade
is:

1. an entry in a dataset-wide archive such as `Datasets/Ocean.zip`;
2. a single-entry archive such as
   `Datasets/GHCNd/Temperature/[station].zip`;
3. an uncompressed relative file.

An automatically managed file cannot remain inside a data-type-wide ZIP without
rewriting unrelated datasets on every refresh. It also must not exist both loose
and inside a ZIP, because that creates two candidate sources. Reorganise the
deployed data by owning dataset rather than temperature/precipitation/solar data
type. Use one replaceable station ZIP for daily BOM and GHCNd, and loose files
for the small non-station datasets.

Adjusted ACORN-SAT and GHCNm remain annual/manual and are out of automatic-
download scope. They can be repackaged under dataset-owned archives as a storage
migration, without changing their update cadence or request behavior.

The current archive inventory illustrates the ownership problem:

- `Temperature_BOM.zip` contains 205 stations for each of max, min, and derived
  mean temperature;
- `Precipitation.zip` mixes 205 BOM station files, 1 Met file, and 1,690 GHCNm
  files;
- `Temperature.zip` mixes 336 ACORN-SAT files, 3,802 GHCNm files, 3 Met files,
  and 14 NOAAGlobalTemp files;
- `Solar.zip` mixes 205 BOM station files with the two global solar files.

Dataset ownership makes the refresh unit explicit and prevents one provider's
update from rewriting an archive owned jointly with unrelated providers.

`FileBackedTwoLayerCache` persists endpoint results but does not expire them or
write them atomically. `DataSet` and `ClimateRecordsResponse` already implement
`ICachedData`; their `RetrievedDate` properties are currently unused.

`ClimateExplorer.Data.Misc` currently performs four kinds of work:

- direct HTTP file downloads selected by `globalDataDefinitions`;
- post-download transforms for ocean acidity, sea level, and two ozone files;
- multi-file or dynamically named downloads for ODGI and NOAAGlobalTemp;
- multi-year API aggregation for Greenland ice melt.

HadCET and HadCEP are a fifth case: four measurements share one
`DataSetDefinition`, but each measurement has a different direct download URL.
The program currently handles this by mutating `DataSetDefinition.DataDownloadUrl`
before every call.

The NOAAGlobalTemp station and file-mapping JSON already exist under
`ClimateExplorer.WebApi/MetaData`. Runtime data retrieval should consume that
metadata; it should not regenerate metadata during a client request.

## Dataset inventory

The existing `globalDataDefinitions` predicate is useful as a starting migration
inventory, but it must not become the runtime selection rule. It accidentally
selects station-templated GHCNd precipitation, while excluding GHCNd temperature,
BOM, and the shared Mauna Loa CO₂ file. The new metadata should explicitly opt
in complete source assets, including all measurements stored in a station ZIP.

| Group | Current handling | Target handler | Refresh cadence |
| --- | --- | --- | --- |
| Mauna Loa CO₂/seasonally adjusted CO₂, Niño 3.4, methane, nitrous oxide, IOD, Arctic/Antarctic sea ice, sunspots, total solar irradiance, Mauna Loa transmission, and AMO | shared direct source or `globalDataDefinitions` plus `DownloadDataSetData` | generic direct-file handler | measurement resolution: 24 hours daily, 28 days monthly |
| Ocean acidity | direct download plus `OceanAcidityReducer` | ocean-acidity transforming handler | 28 days |
| Sea level and the two ozone datasets | direct download plus reducers | focused transforming handlers | 24 hours |
| HadCET mean/max/min and HadCEP precipitation | per-measurement mutation followed by `DownloadDataSetData` | generic direct-file handler using measurement URLs | mean 28 days; max/min/precipitation 24 hours |
| GHCNd temperature and precipitation | separate station ZIPs below data-type folders; recent observations already download one provider CSV per station | GHCNd station handler producing one dataset-owned station ZIP | 24 hours |
| BOM unadjusted mean/max/min temperature, precipitation, and solar radiation | temperature-wide and precipitation/solar aggregate ZIPs | BOM station handler producing one dataset-owned station ZIP | 24 hours |
| ACORN-SAT and GHCNm adjusted data | annual pipeline downloads in data-type archives | dataset-owned archives, no automatic handler | out of scope; retain annual process |
| ODGI | two manually substituted table URLs | ODGI handler | yearly data; use 28 days unless a separate yearly policy is approved |
| NOAAGlobalTemp | manually selected release year/month and one download per mapped area | NOAAGlobalTemp handler | 28 days |
| Greenland ice melt | one API request per year followed by CSV aggregation | Greenland handler | 24 hours |

Mauna Loa carbon dioxide has two monthly measurement definitions (`CO2` and
`CO2Deseasoned`) sharing `Atmosphere/GHGs/co2_mm_mlo.txt`. It is in scope and is
scheduled for stage 2 after the single-measurement direct path. Both
measurements must resolve to one asset key, one freshness record, and one
download.

The separate yearly **CO₂ emissions** dataset is not the shared Mauna Loa source:
its definition currently has no `DataDownloadUrl` and its release filename
changes. It remains a manual source unless a stable discovery mechanism is added
to scope separately.

## Metadata decisions

### Add `DataDownloadUrl` to `MeasurementDefinition`

Add an optional `MeasurementDefinition.DataDownloadUrl`. Resolve the effective
URL as:

```text
measurement.DataDownloadUrl ?? dataSet.DataDownloadUrl
```

This is necessary for HadCET/HadCEP, where the four files have different URLs
and different refresh cadences. Put the direct file URLs on the corresponding
measurements and stop mutating the shared dataset definition. The Hadley landing
page belongs in `MoreInformationUrl`; it should not remain as a fallback data
file URL once measurement URLs are configured.

Keep `DataSetDefinition.DataDownloadUrl` for sources shared by every measurement
or for URL templates whose variable is supplied by the location-to-file mapping.
Do not copy download URLs into view models unless a client-facing requirement is
identified separately.

### Make automatic retrieval opt-in

Add an optional downloader key at dataset level, with a measurement-level
override only if a mixed dataset eventually needs it. Suggested values are
stable strings such as `direct-http`, `ocean-acidity`, `odgi`,
`noaa-global-temperature`, and `greenland-melt`.

A missing key means "read the deployed source without automatic refresh." This
avoids accidentally enabling every definition with a non-null URL, permits
incremental deployment, and keeps the old `globalDataDefinitions` predicate out
of request handling.

The key selects a DI-registered implementation; it is not an enum switch. New
dataset types require a new handler registration and metadata assignment, not a
change to the coordinator.

Remove `AlterDownloadedFile` after every current use has migrated to an explicit
transforming handler. During the staged rollout it can remain as legacy metadata,
but it should not control the new routing.

### Separate local identity from remote naming

Do not build the physical asset path only from `FolderName` and
`FileNameFormat`. That model assumes one file per measurement and cannot express
one BOM/GHCNd station ZIP shared by several measurements.

Add storage metadata that can resolve:

- a dataset-owned storage folder, such as `BOM`, `GHCNd`, `Met`, or
  `NOAAGlobalTemp`;
- an optional archive path format, such as `[station].zip`;
- each measurement's entry/file path format within that asset;
- the mapped station/area ID used to replace `[station]`.

Keep GHCNd and GHCNm as separate storage owners rather than a single ambiguous
`GHCN` folder: their resolution, adjustment, archive granularity, and update
cadence are different. Similarly, keep `BOM` separate from `ACORN-SAT` even
though both are published by the Bureau of Meteorology.

Resolve only supported request variables such as `[station]`. Reject unresolved
variables before issuing a request or constructing a path.

Remote release identifiers must not force manual local filename changes. This
is especially important for NOAAGlobalTemp and total solar irradiance. A
specialised handler may discover a dated remote file but should publish it under
a stable local asset identity. Introduce the stable filename and deploy an
initial source file at that path in the same release so a cold request can still
be served when the remote source is unavailable.

## Proposed architecture

### Request and result models

Introduce a request model containing the resolved dataset definition,
measurement definition, location, mapped file ID, effective URL, local asset
path, and resolution. Do not pass the full HTTP request body into individual
downloaders.

An asset key should include the dataset ID, measurement/file identity, and mapped
file ID, but not chart aggregation options. Measurements that use the same local
source file should deliberately produce the same key. This lets one download
serve multiple `/dataset` response shapes and prevents the two CO2 measurements
from downloading the same file twice.

Return a structured result such as `Fresh`, `Downloaded`, `Unavailable`, or
`Failed`, plus the successful retrieval time. Do not use exceptions for expected
remote unavailability; unexpected exceptions should still be caught at the
coordinator boundary and converted to a failed result after logging.

### Downloader abstraction

Use a small handler contract, for example:

```csharp
internal interface IDataSetDownloader
{
    string Key { get; }

    Task<DataSetDownloadArtifact> DownloadAsync(
        DataSetDownloadRequest request,
        string temporaryDirectory,
        CancellationToken cancellationToken);
}
```

The coordinator resolves exactly one registered handler for the effective key.
It should fail configuration validation at startup, or fail the request safely,
when no handler or duplicate handlers exist. There is no dataset-ID switch and
no analogue of `DownloadAndReadRecentObservationsData`.

Use a generic `direct-http` implementation for one remote resource that can be
published under one local filename. Specialised handlers own multi-request,
release-discovery, or transformation behavior. Shared helpers may perform HTTP
status checks, bounded stream copies, hashing, and cleanup, but helpers should
not decide which dataset algorithm to run.

Register the coordinator, file store, freshness policy, `TimeProvider`, generic
HTTP client, and all handlers through Web API dependency injection. Extend
`ClimateExplorerApiServices` only as a transition if replacing that service bag
would broaden the change too far. Do not add a Web API reference to the
`ClimateExplorer.Data.Misc` executable; move the focused transformer logic into
Web API-owned classes and unit-test it there.

### Retrieval state and freshness

Persist one small source state per asset key in the long-lived cache. It should
implement `ICachedData` and contain at least:

- `RetrievedDate` in UTC;
- the asset key and local path;
- the source file's content hash and length;
- optional source/release information needed by specialised handlers.

Treat state as usable only when the source file exists and its hash/length match
the state entry. This detects a deployment replacing an automatically updated
file with the newly deployed package version. Write the source file first, then
write state. If the state write fails after a successful replacement, a later
request may download again, which is safer than claiming a different file is
current.

Use one shared freshness policy that accepts `ICachedData`, the underlying
measurement resolution, and `TimeProvider`. The initial policy is:

- `Daily`: stale at 24 hours;
- `Monthly`: stale at 28 days;
- `Yearly`: stale at 28 days for ODGI, because no yearly interval was specified
  and checking monthly is inexpensive and conservative;
- any newly enabled resolution: require an explicit policy rather than silently
  choosing one.

The yearly choice should be confirmed while implementing the ODGI stage. It is
kept out of a generic `default` branch so a future weekly dataset cannot inherit
the wrong behavior.

Set a rebuilt `DataSet.RetrievedDate` to the oldest successful retrieval time of
the automatically managed assets contributing to it. For a multi-series request,
this makes the response stale as soon as any contributing source is stale. Use
each source's own resolution when deciding which assets actually require a
download.

Set `ClimateRecordsResponse.RetrievedDate` from the `DataSet` used to build it.
Do not use `DataSet.Resolution` for freshness: it is currently set to yearly for
aggregated responses and is not the source measurement resolution.

Existing cache records with a null `RetrievedDate` are stale. A failure must not
advance `RetrievedDate`; doing so would disguise an unsuccessful refresh. A
response built from a deployed source with no successful automatic retrieval can
be cached with a null retrieval time, while the source state remains absent. If
repeated remote failures become noisy, add a separate short `NextAttemptAfter`
backoff later rather than overloading `RetrievedDate`.

### One mutable source-data store

There must be one physical source for each logical asset. For automatically
managed datasets, deploy the file or station archive at the same writable path
that the downloader will later replace. Readers do not choose between deployed
and downloaded versions; they are successive versions of the same source asset.

The target deployed structure is:

```text
Datasets/
  BOM/
    001019.zip
    001021.zip
    ...
  GHCNd/
    AE000041196.zip
    ...
  ACORN-SAT.zip
  GHCNm.zip
  Met/
    meantemp_monthly_totals.txt
    maxtemp_daily_totals.txt
    mintemp_daily_totals.txt
    HadCEP_daily_totals.txt
  NOAAGlobalTemp/
    <stable area files>
  CO2/
    co2_mm_mlo.txt
  Methane/
    ch4_mm_gl.txt
  NitrousOxide/
    n2o_mm_gl.txt
  Nino34/
    nino34.long.anom.data.txt
  IOD/
    dmi.had.long.data.txt
  AMO/
    ersst.v5.amo.dat
  OceanAcidity/
    HOT_surface_CO2_reduced.csv
  SeaLevel/
    slr_sla_gbl_free_ref_90_reduced.csv
  Greenland/
    greenland-melt-area.csv
  ArcticSeaIce/
    N_seaice_extent_daily_v4.0.csv
  AntarcticSeaIce/
    S_seaice_extent_daily_v4.0.csv
  OzoneHoleArea/
    cams_ozone_monitoring_sh_ozone_area_reduced.csv
  OzoneHoleColumn/
    cams_ozone_monitoring_sh_ozone_minimum_reduced.csv
  ODGI/
    odgi_table1.csv
    odgi_table2.csv
  AtmosphericTransmission/
    mauna_loa_transmission.dat
  CO2Emissions/
    GCB2025v15_MtCO2_flat.csv
  Sunspots/
    SN_d_tot_V2.0.txt
  TSI/
    tsi-ssi_v02r01_observed-tsi-composite.txt
```

The exact storage names should be stable identifiers rather than mutable display
names, but the ownership rule is the important part: top-level paths represent
dataset/provider families, never a measurement type such as temperature,
precipitation, atmosphere, ocean, ice, or solar.

Each `BOM/<station>.zip` is one source asset and contains that station's
temperature maximum, temperature minimum, derived mean temperature,
precipitation, and solar-radiation files. All are daily and share the same
24-hour freshness boundary. The handler may make several provider requests, but
it validates and replaces the station archive as one unit.

Each `GHCNd/<station>.zip` is one source asset built from the provider's one
station CSV. It contains separate temperature and precipitation entries when
those series are available/mapped. A request for either measurement refreshes
the shared station archive once.

`ACORN-SAT.zip` and `GHCNm.zip` are dataset-owned annual archives and remain
outside automatic retrieval. This storage-only move separates adjusted annual
data from unadjusted daily BOM/GHCNd data without changing the annual update
process.

Remove `Temperature_BOM.zip`, `Temperature.zip`, `Precipitation.zip`, and
`Solar.zip` after their entries have moved to the target owners. Remove
`Atmosphere.zip`, `Ocean.zip`, and `Ice.zip` after deploying each entry loose
under its dataset-owned folder.
No migrated logical file may remain in both the old archive and its new path.

Update `ClimateExplorer.DataPipeline` to emit this structure and add a packaging
test that fails if one logical path is emitted twice. Update the file loader to
resolve an explicit source descriptor rather than infer storage solely from the
first `FolderName` segment.

The descriptor needs to distinguish the physical asset from the entry consumed
by a measurement. For example, GHCNd temperature and precipitation have the
same `GHCNd/[station].zip` asset but different entry paths. Prefer dataset-level
storage metadata (dataset folder/archive format) plus a measurement-level entry
path over continuing to overload `MeasurementDefinition.FolderName`.

The source root must be writable in the deployed Web API. It may be populated by
the deployment package on each release, but after startup there is still only
one copy of each managed asset. Retrieval state hashes detect whether deployment
has changed that copy and prevent a cache timestamp for an older file from being
treated as current.

Every attempt creates a uniquely named directory below `Path.GetTempPath()`, as
the recent BOM observations path does. Download and transform only inside that
directory. In a `finally` block, delete temporary files and the directory.

After validation, copy the candidate into the source directory under a sibling
temporary name, flush/close it, and atomically rename it over the existing source
file. This same-volume final step prevents readers from observing a partial file.
If any earlier step fails, the existing deployed or previously downloaded source
file is untouched.

Use a per-asset asynchronous lock in the singleton coordinator. Recheck source
state after taking the lock so simultaneous requests coalesce into one download.
Different assets may refresh concurrently. Sanitize mapped file IDs before
using them in paths, even though the mappings are server-controlled.

### Validation

Validation must occur before publication and must exercise the same format
assumptions as normal reads.

All handlers should verify:

- a successful HTTP response;
- non-empty content and a configured maximum size;
- a readable archive when the remote response is a ZIP;
- the expected number/name/type of entries;
- that the final candidate can be parsed by the applicable one-value-per-row or
  twelve-months-per-row reader;
- at least one valid record and at least one finite, non-null value;
- valid dates for the declared resolution;
- any source-specific identity, range, or completeness invariant.

Do not reject an otherwise valid source only because it contains headers or
provider comments. Conversely, do not accept an HTTP 200 HTML error page merely
because it is non-empty.

Transforming handlers validate both the raw structure needed by the transform
and the transformed candidate consumed by Climate Explorer. Add golden input and
output fixtures for ocean acidity, sea level, ozone, and Greenland so that moving
the current reducers does not silently change the public series. Corrections to
known reducer behavior should be explicit test-backed changes, not accidental
side effects of the move.

When several measurement definitions share one source asset, validation must run
the candidate against every consuming definition before replacement. In
particular, `co2_mm_mlo.txt` must satisfy both the `CO2` and `CO2Deseasoned`
parsers even when the current request asks for only one of them.

### Endpoint orchestration and failure behavior

Place refresh orchestration in `DataSetEndpoints.PostDataSets`, before a cached
`DataSet` is returned. `/climate-record` already delegates to this path, so it
should not duplicate download logic.

For each request:

1. Resolve the definitions, measurements, locations, and mapped source assets
   for every `SeriesSpecification`.
2. Read the existing cached `DataSet` using the current request cache key.
3. If the response is fresh for all managed contributing resolutions and every
   contributing source-state hash still matches its source file, return it.
4. Otherwise ask the coordinator to ensure each managed asset is current. The
   coordinator itself checks source state so another response shape can reuse a
   recent download.
5. If any required refresh fails and a cached response exists, log the fallback
   and return that response unchanged.
6. If all managed assets are usable, rebuild from the current source files, set
   `RetrievedDate`, cache, and return the new response.
7. If there is no cached response and retrieval fails, build from the existing
   source file. On a new deployment this is the file supplied by the package; on
   a running deployment it may be the last successfully downloaded version.
   Cache and return that response without claiming a successful new retrieval.
8. If rebuilding unexpectedly fails after a source publish, return the prior
   cached response. With no cached response, propagate an error only when the
   single source file is also missing or invalid.

Do not invalidate every cached chart variation eagerly. Each cached result has a
retrieval time; it will rebuild lazily on its next request. A source refresh from
one request updates source state, so other stale response shapes rebuild without
redownloading.

Preserve the existing optimization that avoids caching certain unaggregated
daily responses only if the source-level last-known-good state is sufficient to
meet the failure fallback contract. Otherwise cache automatically managed daily
responses as well. Decide this with a size measurement in stage 1 rather than
assuming the current early return is still appropriate.

Logging should include dataset ID, measurement, asset key, resolution,
fresh/stale decision, download duration, validation record count, publication
result, and fallback source. Do not log response bodies or untrusted path text.

## Specialised handler notes

### Ocean acidity, sea level, and ozone

Move each reducer behind its own handler or pure transformer. The handler should
download the provider file, transform it to the stable file format already
consumed by the measurement definition, validate that transformed file with the
normal reader, then publish it.

Ocean acidity is monthly. Sea level and ozone are daily. The focused ocean
acidity parser will not continue using `CsvHelper` in the Web API. It is to be rewritten as
a small robust tabular parser.

### HadCET and HadCEP

Put these direct URLs on their measurements:

- mean temperature: `meantemp_monthly_totals.txt`;
- maximum temperature: `maxtemp_daily_totals.txt`;
- minimum temperature: `mintemp_daily_totals.txt`;
- precipitation: `HadCEP_daily_totals.txt`.

The generic direct handler can then apply the correct cadence independently for
each measurement. No special Hadley dispatcher is needed.

### GHCNd station archives

Use the existing GHCNd HTTP client and processors as shared lower-level code,
without changing `/recent-observations`. One provider station CSV contains the
daily temperature and precipitation inputs, so the historical handler should:

1. download that CSV once for the mapped station;
2. build the temperature and precipitation entry formats consumed by the
   historical readers;
3. validate every entry that the station's mappings expose;
4. create `Datasets/GHCNd/<station>.zip` in the temporary directory; and
5. atomically replace that station archive and update its one retrieval-state
   cache entry.

This replaces the current parallel `GHCNd/Temperature/<station>.zip` and
`GHCNd/Precipitation/<station>.zip` paths. If a station supports only one mapped
series, the archive may contain only that valid entry; it must always contain
the entry requested by the current call.

### BOM station archives

The BOM provider supplies separate downloads per observation code, but all the
historical BOM measurements are daily and should share one station-level source
asset. On refresh, the handler should download every supported mapped series for
that station, validate them, derive mean temperature from maximum/minimum using
the existing `ClimateExplorer.Data.Bom.CreateTempMean` rules, and build one
`Datasets/BOM/<station>.zip` containing:

- maximum temperature;
- minimum temperature;
- mean temperature;
- precipitation; and
- solar radiation.

Replace the archive only when every expected entry is valid. Do not patch one
entry inside the deployed archive or give its entries different retrieval
dates. The remote ZIP parsing and observation-code lookup can be extracted from
recent observations into shared source-client code, but `/recent-observations`
must retain its current endpoint cache, date window, and response behavior.

### ODGI

The current program downloads `table1` and `table2`, while the checked-in
`DataFileMapping_ODGI.json` maps its only exposed location to `table1`. Before
retaining the two-file behavior, add a test/inventory check that establishes
whether `table2` is consumed. Download only requested/mapped assets unless both
tables are needed to construct one output.

The ODGI handler owns table URL expansion and any all-files validation. Treat its
yearly source as eligible for refresh every 28 days until a yearly cadence is
explicitly chosen.

### NOAAGlobalTemp

Replace the hard-coded `2025`/`12` release and the matching hard-coded local
filename with a release-discovery strategy. Prefer a provider manifest or
directory listing if stable; otherwise probe a bounded sequence of recent
monthly release candidates. Unit-test discovery with captured listings or fake
HTTP responses rather than the live provider.

Download only the station/area files required by the current request. Publish
remote dated files under stable local identities and record the selected remote
release in retrieval state. Validate the complete mapped-area inventory as a
metadata test, but do not regenerate station or mapping JSON during requests.

### Greenland ice melt

The handler must turn multiple yearly JSON responses into the one CSV consumed
by the existing reader. A first successful refreshed snapshot may be built from
all required years. Later daily refreshes should reuse immutable completed years
and request only the current year, plus the previous year around a year boundary
if the provider revises it.

Do not emit future dates as zero. Distinguish a reported zero melt area from a
missing API observation, and preserve the existing source aggregate when the
current-year response is missing, malformed, or implausibly incomplete. Publish
the combined CSV only after all changed years and the final aggregate validate.

This is intentionally the last specialised stage because it needs incremental
multi-file state and stronger merge tests than the direct downloads.

## Staged implementation plan

### Stage 0: Contract tests and file-store seam — completed 2026-07-13

1. Add record-level contract tests for every storage family before paths move.
2. Add dataset-level storage metadata and teach the file loader to distinguish a
   physical archive from its measurement entry.
3. Change `ClimateExplorer.DataPipeline` to emit the target dataset-owned
   structure: BOM/GHCNd station ZIPs, annual ACORN-SAT/GHCNm archives, loose
   Met/NOAAGlobalTemp files, and one dataset-owned folder for every small loose
   global source formerly held in Atmosphere/Ocean/Ice/Solar archives.
4. Update measurement paths/mappings and prove that the same records are read
   after migration. Do not enable network retrieval yet.
5. Delete the obsolete data-type archives only after parity tests pass, and add
   a packaging assertion that one logical asset never exists twice.
6. Add atomic in-place source replacement, temporary cleanup, and per-asset
   locking.
7. Add `ICachedData` freshness tests for null, daily, monthly, boundary, and UTC
   timestamps.
8. Add endpoint tests for fresh cache, stale cache, successful refresh, stale
   cache fallback, cold deployed-source fallback, and multi-series
   oldest-retrieval behavior.

No dataset is opted in during this stage.

#### Stage 0 implementation progress

Completed on 2026-07-13:

- `ClimateExplorer.DataPipeline` now deterministically emits the dataset-owned
  layout. The current package contains 10,426 logical source files in 2,098
  physical assets.
- BOM is packaged as 205 station archives containing temperature maximum,
  temperature minimum, derived mean temperature, precipitation, and solar
  radiation. GHCNd temperature and precipitation are combined into one archive
  per station.
- ACORN-SAT and GHCNm are stored in their own annual archives. Met,
  NOAAGlobalTemp, and the small global datasets are loose files in
  dataset-owned folders. The corresponding loose `ClimateExplorer.SourceData`
  inputs were moved to the same folder structure.
- Every `MeasurementDefinition` now has a required explicit
  `DataFileSourceDefinition`. `FolderName`, `FileNameFormat`, and the inferred
  legacy archive cascade were removed rather than retained as compatibility
  paths.
- The old Atmosphere, Ice, Ocean, Precipitation, Solar, Temperature, and
  Temperature_BOM archives were removed from the deployable package. Packaging
  fails for duplicate source files, duplicate logical destinations, duplicate
  physical assets, or omitted source files.
- Real-package contracts resolve every measurement definition against the new
  package, including archive entries, and verify that the two Mauna Loa CO₂
  measurements share one physical source.
- The Web API now has DI-registered freshness policy, uniquely named temporary
  workspaces, per-asset asynchronous locks, and same-directory atomic source
  publication. Source state records length, SHA-256, path, asset key, and the
  successful retrieval time.
- `/dataset` now contains the refresh/fallback orchestration seam and propagates
  successful retrieval time. `/climate-record` propagates that time from the
  underlying dataset response. Tests cover fresh cache, failed refresh with a
  cached response, cold packaged-source fallback, and successful rebuild.
- No downloader key is assigned to a dataset, so this stage does not perform
  network retrieval. `/recent-observations` remains unchanged.

Verification at completion: 323 unit tests passed and the complete solution
built with zero warnings and errors.

### Stage 1: Generic direct downloads

1. Add downloader metadata and the DI-registered `direct-http` handler.
2. Opt in the small, non-transforming datasets currently handled by
   `globalDataDefinitions`, in small batches grouped by source provider. Leave
   GHCNd for its station-archive stage rather than treating precipitation as a
   one-measurement direct file.
3. For each dataset, add parser-validation fixtures and test cold fallback to
   the same deployed loose source file.
4. Measure automatically managed unaggregated daily response sizes and decide
   whether to retain or remove the current no-cache early return.
5. Confirm `/recent-observations` tests and call paths are unchanged.

Do not mechanically opt in a definition merely because the old LINQ predicate
selected it. Verify URL tokens, local filenames, mappings, and the deployed loose
source for every entry first.

### Stage 2: Shared CO₂ and transforming global downloads

1. Opt in the shared Mauna Loa CO₂ file and prove that `CO2` and
   `CO2Deseasoned` share one download and retrieval state.
2. Move ocean-acidity transformation into its handler and add golden tests.
3. Move sea-level and ozone transformations into focused handlers and add golden
   tests.
4. Opt in the transforming datasets only after raw and transformed validation
   is in place.
5. Remove their calls from `ClimateExplorer.Data.Misc` after the Web API path is
   deployed and observed successfully.

This completes the small `globalDataDefinitions`/reducer portion while station
archives and the explicitly specialised datasets remain deferred.

### Stage 3: HadCET and HadCEP

1. Add measurement-level download URLs and make the generic handler prefer them.
2. Opt in each Hadley measurement with its own resolution-derived cadence.
3. Add tests proving that selecting one measurement downloads only its file and
   never mutates shared dataset metadata.

### Stage 4: GHCNd station archives

1. Extract/reuse the GHCNd station download and processing helpers without
   changing `/recent-observations`.
2. Implement one refresh operation for `GHCNd/<station>.zip`, containing all
   mapped temperature/precipitation entries produced from one provider CSV.
3. Add tests for temperature-only, precipitation-only, shared-series,
   corruption, cold source fallback, and one-download concurrency behavior.
4. Opt in both GHCNd definitions at the daily cadence.

### Stage 5: BOM station archives

1. Extract/reuse BOM observation-code download helpers without changing
   `/recent-observations`.
2. Implement an all-expected-series refresh of `BOM/<station>.zip`, including
   derived mean temperature and solar radiation.
3. Add tests for partial provider failure, derived mean parity, missing expected
   entries, archive validation, cold source fallback, and concurrency.
4. Opt in the historical BOM definition at the daily cadence.

### Stage 6: ODGI

1. Resolve the `table1`/`table2` inventory discrepancy.
2. Implement and register the ODGI handler with its explicit yearly freshness
   policy.
3. Test mapped-table selection, malformed table fallback, and last-known-good
   behavior.

### Stage 7: NOAAGlobalTemp

1. Implement bounded latest-release discovery and stable local filenames.
2. Test all existing mapped station/area IDs and on-demand single-area downloads.
3. Use the stable loose source names established in stage 0.
4. Record remote release metadata and test fallback when a newer release is
   absent or malformed.

### Stage 8: Greenland ice melt

1. Implement yearly API download/validation and aggregate generation.
2. Add first-snapshot, incremental current-year, year-boundary, missing-date,
   reported-zero, and partial-failure tests.
3. Opt in the dataset at the daily cadence.

### Stage 9: Cleanup and operations

1. Keep `ClimateExplorer.DataPipeline` capable of producing one deployable data
   package: loose mutable files under dataset-owned folders, BOM/GHCNd station
   archives, and annual ACORN-SAT/GHCNm dataset archives.
2. Document source-data write permissions, deployment replacement semantics,
   cache keys, operational logs, and how to force a safe refresh without
   deleting the current source file.
3. Keep `ClimateExplorer.SourceData` as the build-time input to the deployable
   data package. The pipeline may copy a managed file loose rather than archive
   it, but the running Web API receives only one source copy.

## Tests and acceptance criteria

Use `MethodName_StateUnderTest_ExpectedBehavior` for all new C# tests.

At minimum, cover:

- daily data at just under/over 24 hours and monthly data at just under/over 28
  days;
- null and future `RetrievedDate` handling with a controllable `TimeProvider`;
- a fresh cached response causing no HTTP request;
- a stale response causing one request and receiving a new retrieval time;
- concurrent requests for one asset causing one download;
- different assets being able to download independently;
- non-success HTTP, timeout, cancellation, empty content, HTML masquerading as
  data, regex mismatch, invalid dates, all-null values, corrupt station ZIPs,
  missing station entries, and wrong archive entries;
- failed refresh leaving the previously published asset and cached response
  byte-for-byte/semantically unchanged;
- cold failure loading and caching the deployed source file;
- measurement URL overriding the dataset URL;
- unresolved URL tokens being rejected before HTTP;
- multi-series responses using the oldest contributing retrieval time and
  refreshing only stale assets;
- `/climate-record` receiving and returning the underlying retrieval time;
- storage-migration parity for BOM, GHCNd, ACORN-SAT, GHCNm, Met,
  NOAAGlobalTemp, and every small dataset extracted from the former
  Atmosphere/Ocean/Ice/Solar archives;
- packaging validation that every logical asset exists exactly once, whether it
  is a loose file, station archive, or annual dataset archive;
- one GHCNd provider download producing every mapped entry in one station ZIP;
- one BOM station refresh producing max/min/derived-mean temperature,
  precipitation, and solar entries in one station ZIP;
- each specialised transform/discovery/merge behavior described above;
- all existing recent-observations endpoint and service tests continuing to pass
  without modification to production recent-observations code.

The rollout is complete when:

- every migrated request either returns fresh validated data or a known-good
  cached/current-source response;
- failed attempts never replace a known-good file or advance `RetrievedDate`;
- the data package contains no temperature-, precipitation-, or solar-wide
  archive that mixes independently owned datasets;
- GHCNd and BOM unadjusted station archives refresh daily, while GHCNm and
  ACORN-SAT retain their annual/manual process;
- new downloader types can be added by implementing/registering a handler and
  assigning metadata, without editing a central dispatcher;
- the Web API no longer relies on `ClimateExplorer.Data.Misc` for the listed
  datasets;
- `ClimateExplorer.Data.Misc/Program.cs` contains only the site-map and map-marker
  calls shown in the goal;
- `/recent-observations` remains unchanged.

## Risks and rollout safeguards

- **The deployed source directory may be read-only.** Verify write permissions
  before stage 1. Automatically managed files must be atomically replaceable in
  the same directory from which readers load them.
- **Deployment can replace an updated source file.** Store a required file hash
  with retrieval state. A mismatch makes the state stale and triggers refresh;
  it must not create a second source copy.
- **Provider formats can drift.** Parse-based validation and preservation of the
  current source file are required for every source; content length alone is
  insufficient.
- **Old response caches have no retrieval date.** Assume they will be deleted.
- **Source and response caches have different granularity.** Keep asset retrieval
  state separate from chart-response cache entries and derive response time from
  contributing assets.
- **A station archive couples several measurements.** Treat the whole BOM or
  GHCNd station ZIP as the freshness and replacement unit. Never partially
  update archive entries or assign per-entry retrieval dates.
- **Multi-asset refresh can partially succeed.** Publish atomically per asset and
  rebuild/cache a response only when all required assets are usable. A failed
  request returns its prior cached response even if another asset was safely
  updated for a future request.
- **Cold fallback may retry frequently during an outage.** Initially favor truth
  by leaving `RetrievedDate` null. Add a separate bounded retry marker only if
  operational logs show a need.
- **Remote URLs may be dated.** Separate remote release discovery from stable
  local identity and deploy an initial source asset whenever that identity
  changes.
