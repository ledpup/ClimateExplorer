# Automated dataset downloads for historical API data

- **Date:** 2026-07-13
- **Status:** Proposed
- **Author:** Codex
- **Scope:** dataset download metadata, `/dataset` and `/climate-record` request flow, dataset file loading/storage, Web API dependency injection, `ClimateExplorer.Data.Misc`, and related unit tests
- **Branch context:** `development`

## Goal

Move the regional/global download work currently performed by
`ClimateExplorer.Data.Misc` into the Web API data paths used by `/dataset` and
`/climate-record`.

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
   read the packaged dataset, cache that response, and return it.

`/recent-observations` and its BOM/GHCNd download implementations are explicitly
out of scope. They are useful examples for temporary-file cleanup and defensive
download handling, but should not be routed through the new historical-dataset
coordinator.

The end state for `ClimateExplorer.Data.Misc/Program.cs` should ideally be:

```csharp
await BuildStaticContent.GenerateSiteMap();
GenerateMapMarkers();
```

The packaged ZIP files remain a deployable bootstrap and last-resort fallback;
automatic downloads do not eliminate them.

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

The first-match behavior means an automatically downloaded override cannot
simply be placed beside the source files: a packaged dataset-wide archive would
still win. File lookup needs an explicit runtime-override tier ahead of the
immutable package tiers.

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

The existing `globalDataDefinitions` predicate is useful as a migration
inventory, but it must not become the runtime selection rule. In particular, it
also selects a station-templated GHCNd precipitation definition, for which a
request's mapped station ID is required. Automatic retrieval should be explicitly
enabled in metadata and should operate only on the assets required by the
current request.

| Group | Current handling | Target handler | Refresh cadence |
| --- | --- | --- | --- |
| Niño 3.4, methane, nitrous oxide, IOD, Arctic/Antarctic sea ice, sunspots, total solar irradiance, Mauna Loa transmission, AMO, and eligible mapped GHCNd precipitation assets | `globalDataDefinitions` plus `DownloadDataSetData` | generic direct-file handler | measurement resolution: 24 hours daily, 28 days monthly |
| Ocean acidity | direct download plus `OceanAcidityReducer` | ocean-acidity transforming handler | 28 days |
| Sea level and the two ozone datasets | direct download plus reducers | focused transforming handlers | 24 hours |
| HadCET mean/max/min and HadCEP precipitation | per-measurement mutation followed by `DownloadDataSetData` | generic direct-file handler using measurement URLs | mean 28 days; max/min/precipitation 24 hours |
| ODGI | two manually substituted table URLs | ODGI handler | yearly data; use 28 days unless a separate yearly policy is approved |
| NOAAGlobalTemp | manually selected release year/month and one download per mapped area | NOAAGlobalTemp handler | 28 days |
| Greenland ice melt | one API request per year followed by CSV aggregation | Greenland handler | 24 hours |

Carbon dioxide has two measurement definitions sharing one downloaded file, so
it is not currently selected by `globalDataDefinitions`. It is required for
the first migration.

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

A missing key means "packaged data only." This avoids accidentally enabling
every definition with a non-null URL, permits incremental deployment, and keeps
the old `globalDataDefinitions` predicate out of request handling.

The key selects a DI-registered implementation; it is not an enum switch. New
dataset types require a new handler registration and metadata assignment, not a
change to the coordinator.

Remove `AlterDownloadedFile` after every current use has migrated to an explicit
transforming handler. During the staged rollout it can remain as legacy metadata,
but it should not control the new routing.

### Separate local identity from remote naming

Build the local asset path from `FolderName`, `FileNameFormat`, and mapped file
ID. Resolve only supported request variables such as `[station]` in the generic
handler. Reject unresolved variables rather than issuing a malformed request.

Remote release identifiers must not force manual local filename changes. This
is especially important for NOAAGlobalTemp and total solar irradiance. A
specialised handler may discover a dated remote file but should publish it under
a stable local asset identity. Introduce the stable filename and matching
packaged fallback in the same deployment so cold fallback continues to work.

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
- an optional content hash/length for diagnostics;
- optional source/release information needed by specialised handlers.

Treat state as usable only when both the state entry and its published asset
exist. Write the asset first, then write state. If the state write fails after a
successful publish, a later request may download again, which is safer than
claiming a missing file is current.

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
packaged-only fallback response can be cached with a null retrieval time, while
the source state remains absent. If repeated remote failures become noisy, add a
separate short `NextAttemptAfter` backoff later rather than overloading
`RetrievedDate`.

### File store and packaged fallback

Introduce a dataset file-store abstraction used by both the download coordinator
and the two existing line readers. Its read order should be:

1. a validated runtime override in a configurable writable data root;
2. the existing single-entry package ZIP;
3. the existing dataset-wide package ZIP;
4. the legacy uncompressed source path, where still supported.

Keep the runtime root outside the checked-in/package content and configure it to
a persistent writable location in deployed environments. Verify this deployment
assumption before enabling the first handler.

Single-entry ZIPs are straightforward with the existing reader and preserve
disk space, so prefer them for the runtime override: the archive contains one
entry whose name is the stable local filename. The design should still permit an
uncompressed override if a specialised source makes ZIP publication awkward.
Do not rewrite a large dataset-wide ZIP for one refreshed file.

Every attempt creates a uniquely named directory below `Path.GetTempPath()`, as
the recent BOM observations path does. Download and transform only inside that
directory. In a `finally` block, delete temporary files and the directory.

After validation, create the final ZIP or file in the runtime data root under a
sibling temporary name, flush/close it, and atomically rename it over the prior
runtime override. This same-volume final step prevents readers from observing a
partial file. The immutable packaged artifact is never overwritten and remains
available if no valid runtime override exists.

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

### Endpoint orchestration and failure behavior

Place refresh orchestration in `DataSetEndpoints.PostDataSets`, before a cached
`DataSet` is returned. `/climate-record` already delegates to this path, so it
should not duplicate download logic.

For each request:

1. Resolve the definitions, measurements, locations, and mapped source assets
   for every `SeriesSpecification`.
2. Read the existing cached `DataSet` using the current request cache key.
3. If the response is fresh for all managed contributing resolutions, return it.
4. Otherwise ask the coordinator to ensure each managed asset is current. The
   coordinator itself checks source state so another response shape can reuse a
   recent download.
5. If any required refresh fails and a cached response exists, log the fallback
   and return that response unchanged.
6. If all managed assets are usable, rebuild from runtime overrides/package
   cascade, set `RetrievedDate`, cache, and return the new response.
7. If there is no cached response and retrieval fails, build through the file
   cascade. This reads an older validated runtime override when one exists, or
   the packaged file otherwise. Cache and return that response without claiming
   a successful new retrieval.
8. If rebuilding unexpectedly fails after a source publish, return the prior
   cached response. With no cached response, retry against packaged fallback and
   only propagate an error if the package is also missing or invalid.

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
by the existing reader. A first successful runtime snapshot may be built from
all required years. Later daily refreshes should reuse immutable completed years
and request only the current year, plus the previous year around a year boundary
if the provider revises it.

Do not emit future dates as zero. Distinguish a reported zero melt area from a
missing API observation, and preserve the existing packaged aggregate when the
current-year response is missing, malformed, or implausibly incomplete. Publish
the combined CSV only after all changed years and the final aggregate validate.

This is intentionally the last specialised stage because it needs incremental
multi-file state and stronger merge tests than the direct downloads.

## Staged implementation plan

### Stage 0: Contract tests and file-store seam

1. Add tests that capture current packaged reads for one dataset-wide ZIP and
   one single-entry ZIP.
2. Extract file lookup behind the runtime-override/package cascade without
   changing the returned records.
3. Add the writable runtime store, atomic publish behavior, temporary cleanup,
   and per-asset locking.
4. Add `ICachedData` freshness tests for null, daily, monthly, boundary, and UTC
   timestamps.
5. Add endpoint tests for fresh cache, stale cache, successful refresh, stale
   fallback, cold packaged fallback, and multi-series oldest-retrieval behavior.

No dataset is opted in during this stage.

### Stage 1: Generic direct downloads

1. Add downloader metadata and the DI-registered `direct-http` handler.
2. Resolve mapped `[station]` values from the request rather than downloading
   every known station.
3. Opt in the non-transforming datasets currently handled by
   `globalDataDefinitions`, in small batches grouped by source provider.
4. For each dataset, add parser-validation fixtures and a packaged-fallback test.
5. Measure automatically managed unaggregated daily response sizes and decide
   whether to retain or remove the current no-cache early return.
6. Confirm `/recent-observations` tests and call paths are unchanged.

Do not mechanically opt in a definition merely because the old LINQ predicate
selected it. Verify URL tokens, local filenames, mappings, and packaged fallback
for every entry first.

### Stage 2: Transforming global downloads

1. Move ocean-acidity transformation into its handler and add golden tests.
2. Move sea-level and ozone transformations into focused handlers and add golden
   tests.
3. Opt in these datasets only after raw and transformed validation is in place.
4. CO2 is in scope for this stage.
5. Remove their calls from `ClimateExplorer.Data.Misc` after the Web API path is
   deployed and observed successfully.

This completes the `globalDataDefinitions`/reducer portion while keeping the
three explicitly specialised datasets deferred.

### Stage 3: HadCET and HadCEP

1. Add measurement-level download URLs and make the generic handler prefer them.
2. Opt in each Hadley measurement with its own resolution-derived cadence.
3. Add tests proving that selecting one measurement downloads only its file and
   never mutates shared dataset metadata.
4. Remove the HadCET/HadCEP block from `ClimateExplorer.Data.Misc`.

### Stage 4: ODGI

1. Resolve the `table1`/`table2` inventory discrepancy.
2. Implement and register the ODGI handler with its explicit yearly freshness
   policy.
3. Test mapped-table selection, malformed table fallback, and last-known-good
   behavior.
4. Remove the ODGI block from `ClimateExplorer.Data.Misc`.

### Stage 5: NOAAGlobalTemp

1. Implement bounded latest-release discovery and stable local filenames.
2. Test all existing mapped station/area IDs and on-demand single-area downloads.
3. Migrate the packaged fallback to the stable local name in the same release.
4. Record remote release metadata and test fallback when a newer release is
   absent or malformed.
5. Remove hard-coded release selection and metadata generation from
   `ClimateExplorer.Data.Misc`.

### Stage 6: Greenland ice melt

1. Implement yearly API download/validation and aggregate generation.
2. Add first-snapshot, incremental current-year, year-boundary, missing-date,
   reported-zero, and partial-failure tests.
3. Opt in the dataset at the daily cadence.
4. Remove `GreenlandApiClient` invocation from `ClimateExplorer.Data.Misc`.

### Stage 7: Cleanup and operations

1. Reduce `ClimateExplorer.Data.Misc/Program.cs` to site-map and map-marker
   generation, then remove unused HTTP setup, download helpers, reducer classes,
   packages, and folders.
2. Keep `ClimateExplorer.DataPipeline` capable of creating packaged fallback
   snapshots; it is no longer the normal freshness mechanism for migrated
   datasets.
3. Document the runtime data path, persistence/permissions requirement, cache
   keys, operational logs, and how to force a safe refresh without deleting the
   packaged fallback.
4. After an observation period, decide whether any migrated source data can be
   removed from `ClimateExplorer.SourceData`; do not combine that deployment
   decision with the first downloader release.

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
  data, corrupt ZIP, wrong ZIP entry, regex mismatch, invalid dates, and all-null
  values;
- failed refresh leaving the previously published asset and cached response
  byte-for-byte/semantically unchanged;
- cold failure loading and caching the packaged file;
- measurement URL overriding the dataset URL;
- unresolved URL tokens being rejected before HTTP;
- multi-series responses using the oldest contributing retrieval time and
  refreshing only stale assets;
- `/climate-record` receiving and returning the underlying retrieval time;
- runtime override precedence followed by single-entry ZIP, dataset ZIP, and
  uncompressed fallback precedence;
- each specialised transform/discovery/merge behavior described above;
- all existing recent-observations endpoint and service tests continuing to pass
  without modification to production recent-observations code.

The rollout is complete when:

- every migrated request either returns fresh validated data or a known-good
  cached/packaged response;
- failed attempts never replace a known-good file or advance `RetrievedDate`;
- new downloader types can be added by implementing/registering a handler and
  assigning metadata, without editing a central dispatcher;
- the Web API no longer relies on `ClimateExplorer.Data.Misc` for the listed
  datasets;
- `ClimateExplorer.Data.Misc/Program.cs` contains only the site-map and map-marker
  calls shown in the goal;
- `/recent-observations` remains unchanged.

## Risks and rollout safeguards

- **Writable storage may not persist in hosting.** Verify and configure a
  persistent runtime data root before stage 1; otherwise every restart will
  discard overrides and source state will be invalid.
- **Provider formats can drift.** Parse-based validation and packaged fallback
  are required for every source; content length alone is insufficient.
- **Old response caches have no retrieval date.** Treat them as stale but retain
  them as fallback until a validated refresh succeeds.
- **Source and response caches have different granularity.** Keep asset retrieval
  state separate from chart-response cache entries and derive response time from
  contributing assets.
- **Multi-asset refresh can partially succeed.** Publish atomically per asset and
  rebuild/cache a response only when all required assets are usable. A failed
  request returns its prior cached response even if another asset was safely
  updated for a future request.
- **Cold fallback may retry frequently during an outage.** Initially favor truth
  by leaving `RetrievedDate` null. Add a separate bounded retry marker only if
  operational logs show a need.
- **Remote URLs may be dated.** Separate remote release discovery from stable
  local identity and ship a matching packaged fallback whenever that identity
  changes.
