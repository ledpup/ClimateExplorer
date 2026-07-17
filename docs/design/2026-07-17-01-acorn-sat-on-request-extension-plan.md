# Extend ACORN-SAT with recent CDO data on `/climate-record`

- **Date:** 2026-07-17
- **Status:** Proposed
- **Author:** Codex
- **Scope:** `/climate-record`, ACORN-SAT/CDO record composition, dataset source coordination, extension decision/cache state, source metadata, Web API dependency injection, unit tests, and removal of `ClimateExplorer.Data.Bom.AcornSatUpdateFromRaw`
- **Builds on:** [Automated dataset downloads for historical API data](2026-07-13-01-automated-dataset-downloads-plan.md) and [Retire the recent-observations endpoint](2026-07-14-01-retire-recent-observations-plan.md)
- **Branch context:** `issues/file-rework`

## Goal

When `/climate-record` serves an adjusted ACORN-SAT daily temperature series,
extend it through the current calendar year with unadjusted Bureau of
Meteorology Climate Data Online (CDO) observations only when the most recent
complete ACORN-SAT year demonstrates that the adjusted and CDO series are not
materially different.

For a request made on 2026-07-10, the successful shape is:

1. read the packaged annual ACORN-SAT series through 2025-12-31;
2. resolve and, if stale, refresh the open CDO station archive for the requested
   location using the existing `bom-station` downloader;
3. compare ACORN-SAT and CDO over 2025;
4. if the comparison proves that the series is eligible, append CDO values from
   2026-01-01 through 2026-07-10;
5. cache the small eligibility decision/current-year overlay and return the
   composed recordset without altering `ACORN-SAT.zip` or its build-time source
   files.

This is an explicit ACORN-SAT exception, not a new convention for HadCET,
GHCNd, or every manually maintained dataset. ACORN-SAT must remain an
annual/manual source: do not add a downloader key, discover a newer ACORN-SAT
release, or publish a generated ACORN-SAT archive from a web request.

When this flow is proven, remove
`ClimateExplorer.Data.Bom.AcornSatUpdateFromRaw` from the solution and delete
the project.

## Investigation findings

### The old executable is no longer the right integration point

`ClimateExplorer.Data.Bom.AcornSatUpdateFromRaw/Program.cs` currently:

- chooses `DateTime.Now.Year - 1` as the year to create and compares the year
  before that;
- repeats the work independently for mean, maximum, and minimum temperature;
- assumes the adjusted and unadjusted files use the same station ID;
- treats the absence of a difference greater than 0.1 degrees as permission to
  update, even when the comparison has no meaningful overlap;
- appends a complete calendar year, including future blank rows;
- rewrites loose files below `ClimateExplorer.SourceData` and therefore needs a
  later data-package rebuild before the Web API can see the change;
- still refers to the retired `ClimateExplorer.SourceData/Temperature_BOM`
  layout, while CDO sources are now station ZIPs below `BOM`;
- contains two parser classes and unused transfer-function reporting code that
  have no callers outside this project.

The executable predates the dataset-owned packaging and automatic CDO refresh
work. Moving its algorithm unchanged into a downloader would preserve its
station assumption, weak no-overlap behavior, and source mutation.

### Current request, download, and cache behavior

`ClimateRecordsEndpoints.GetClimateRecords` selects a dataset/measurement,
builds a single-series `PostDataSetsRequestBody`, and calls
`DataSetEndpoints.PostDataSets(..., permitSourceUpdate: true)`. Direct
`/dataset` requests always pass `permitSourceUpdate: false`.

For CDO, `DataSetSourceAssetResolver` resolves the one open mapped station and
the `bom-station` downloader refreshes one station ZIP containing maximum,
minimum, derived mean temperature, precipitation, and solar radiation. The
coordinator already supplies keyed concurrency, validation, atomic source
publication, content-aware freshness, retrieval state, and last-known-good
fallback. The new feature should reuse this path rather than call
`BomDailyDataClient` itself.

ACORN-SAT deliberately has no `DataDownloaderKey`. Its source is the annual
`Datasets/ACORN-SAT.zip`, so the generic source resolver returns no managed
asset and never attempts an ACORN-SAT download.

The normal response cache is not sufficient for this feature:

- `DataSetEndpoints` deliberately does not cache `ByYearAndDay` responses;
- an unmanaged cached ACORN-SAT response has no CDO dependency for the source
  coordinator to freshness-check and could otherwise be returned indefinitely;
- caching every full 100-plus-year daily station series would be much larger
  than caching only the current-year overlay and eligibility state.

Use a focused ACORN-SAT extension cache. Continue reading the annual ACORN-SAT
series through the existing data reader, as the endpoint already does, and
cache only the small exceptional result.

### Mapping and packaged-data facts

- The adjusted and unadjusted Australian mappings both contain 113 location
  entries, one of which is an all-null placeholder. There are 112 real
  ACORN-SAT stations and 336 archive entries (mean/max/min).
- 82 of the 113 CDO locations stitch two or more physical stations across
  history. The extension must compare the CDO series for the requested
  **location**, respecting those date ranges, rather than read one inferred
  filename.
- At 112 real locations, 111 currently have the same adjusted station ID and
  open CDO station ID. Location `70a07bb0-2220-402b-be83-f2c35edfdd12` is the
  important exception: ACORN-SAT uses `023000`, while the open CDO mapping is
  `023090` after a 1977 transition. The old filename assumption cannot handle
  it.
- The current packaged ACORN-SAT archive has every maximum-temperature file
  through 2025-12-31. Mean and minimum are also through that date except for
  station `046012`, whose mean and minimum files stop at 2024-12-31. The new
  flow must not bridge the missing 2025 year for those two measurements.
- A comparison of the current packaged sources found no differences greater
  than the executable's 0.1-degree tolerance in 2025 for 110 maximum, 110
  minimum, and 109 mean series with usable overlap. The remaining cases were
  missing/all-null overlap, the `023000` to `023090` transition, and the two
  `046012` series that end in 2024. This supports on-request extension while
  also demonstrating why eligibility must fail closed when evidence is absent.

## Recommended architecture

### Use a recordset extender, not an existing source-file transformer

The `IDataSetSourceFileTransformer` implementations in
`ClimateExplorer.Data.Downloading` transform a newly downloaded candidate file
before `DataSetSourceFileStore` publishes it. ACORN-SAT has no candidate
download, and publishing the composed result is specifically out of scope.

Add a focused recordset component, suggested name
`AcornSatRecordExtender`, below an `Extenders` folder in
`ClimateExplorer.Data.Downloading`. It should be a pure, deterministic
algorithm over ACORN-SAT/CDO daily records plus a supplied current date. It
should return a structured result containing:

- the eligibility decision and reason;
- the comparison year;
- the latest ACORN-SAT date and a stable signature of all eligibility inputs
  from the ACORN-SAT series;
- the adjusted and open CDO station IDs used for diagnostics/cache validation;
- the current-year CDO overlay records, if eligible;
- whether any CDO values were actually contributed.

Keep Web API concerns outside this component. An
`AcornSatClimateRecordService` in `ClimateExplorer.WebApi` should detect the
ACORN-SAT request, coordinate CDO freshness, use the extension cache, invoke
the pure extender, and supply the composed daily series to the ordinary
binning/response pipeline.

This split gives the suggested transformer behavior without pretending the
result is a downloadable source asset or creating a general plugin model that
no other dataset needs.

### Compose before binning and endpoint filtering

The overlay must be applied to daily `DataRecord` values before
`DataSetBuilder` runs filters and binning. This makes all existing
`/climate-record` shapes consistent:

- daily results receive individual current-year days;
- `monthly=true` aggregates those same days using the existing monthly path;
- month/day filters, ascending/descending ordering, `take`/`skip`, `TotalCount`,
  `StartYear`, and `EndYear` all see one composed series;
- top/bottom record queries can include a recent CDO-backed value naturally.

Refactor the current build path just enough to accept an already prepared
`Series`/daily record array and share the existing `DataSet` response
construction. Do not duplicate binning, metadata construction, pagination, or
rounding inside the ACORN-SAT service.

One reasonable shape is:

1. extract a reusable response builder from `DataSetEndpoints.PostDataSets`;
2. allow `DataSetBuilder` to build from either its normal `SeriesProvider`
   result or a supplied `Series`;
3. have the ordinary dataset path continue using the default provider;
4. have only the ACORN-SAT `/climate-record` path supply the merged daily
   series and bypass the ordinary full-response cache.

The exact class split may follow existing naming conventions during
implementation, but the composition boundary must remain before binning.

### Eligibility rules

Use `TimeProvider` and derive one deterministic `today` for both the comparison
and append range. For an ordinary request during year `Y`, extension is allowed
for one data type/location only when all of the following hold:

1. The request selected the ACORN-SAT dataset ID, an adjusted daily
   `TempMean`, `TempMax`, or `TempMin` measurement, and came through
   `/climate-record`. Do not change direct `/dataset` behavior.
2. The adjusted mapping contains a non-blank station and the CDO mapping can
   produce a location series with exactly one open non-blank station.
3. The ACORN-SAT source reaches 31 December of `Y - 1`. Do not bridge multiple
   missing years, and do not append when ACORN-SAT already contains any dates
   in year `Y`.
4. The comparison year is exactly `Y - 1` and contains at least one non-null
   adjusted/CDO pair.
5. The non-null date sets match. A value present only in ACORN-SAT or only in
   CDO is insufficient evidence that raw current-year values can stand in for
   the adjusted series.
6. Every paired value is within the existing 0.1-degree tolerance. Preserve
   the executable's intended one-decimal comparison semantics and cover the
   exact boundary in tests; do not rely on untested binary floating-point
   equality.

If those rules pass, select only non-null CDO records whose dates are between
1 January of `Y` and `today`, inclusive. Never replace an ACORN-SAT date and
never append a prior/future year. It is valid for the eligible overlay to be
empty when CDO has not yet published a current-year value; return base
ACORN-SAT in that case while retaining the eligibility decision.

Use a decision enum/reason rather than a bare boolean so logs and retry rules
can distinguish:

- `Eligible`;
- `AdjustmentsDetected`;
- `AcornNotThroughPreviousYear`;
- `InsufficientComparisonData`;
- `NoOpenCdoStation`;
- `SourceUnavailable` or invalid input.

Only `Eligible` and `AdjustmentsDetected` are conclusive for a matching annual
ACORN-SAT comparison signature. Missing data and source failures are transient
and must be eligible for a later retry.

### Extension cache and invalidation

Store one small `AcornSatExtensionCacheEntry` per location and temperature data
type in the existing long-term cache (behind a focused cache wrapper so tests
do not depend on filename details). The entry should implement `ICachedData`
and contain at least:

- location ID and data type;
- adjusted station ID and open CDO station ID;
- comparison year and decision/reason;
- the latest adjusted date and a stable signature of the ACORN-SAT eligibility
  inputs (coverage plus comparison-year dates/values);
- the current-year overlay records;
- the successful CDO `RetrievedDate` and latest source record date, when known.

The signature should be derived from the adjusted comparison-year records and
coverage bounds that are already read for the response. This is stronger than
relying only on ZIP length/timestamp and avoids hashing the full 47 MB archive
on every request. A new annual release that adds a year, introduces current-year
records, or changes the comparison values therefore invalidates the decision
automatically. Historical ACORN-SAT revisions outside the eligibility inputs do
not need to invalidate the decision because the base series is read fresh and
is never stored in this cache.

Store the compared year and conclusive outcome rather than scanning the entire
history to calculate a "last adjustment year." The request-time decision only
depends on the latest complete year; a full historical scan adds cost without
changing whether the current year may be extended.

Generalize `IDataSetSourceUpdateCoordinator.PrepareAsync` from accepting a
`DataSet?` cached value to accepting `ICachedData?`; it only needs nullability
and `RetrievedDate` today. The ACORN-SAT service can then give its matching
extension entry to the coordinator while resolving the **CDO** request:

- `UseCached`: the CDO source state and overlay are fresh; reuse the overlay
  without repeating the comparison;
- `Rebuild`: CDO was refreshed or the overlay is stale; read the current CDO
  series, recalculate the decision/overlay, and update the extension cache;
- `RefreshFailed`: reuse a matching last-known-good overlay, or return base
  ACORN-SAT when there is no usable cached overlay.

On a cold refresh failure, it is acceptable to attempt the comparison against
the already published/package CDO archive, matching `DataSetEndpoints`' current
cold-source fallback. Such a result has a null retrieval time and must remain
retryable. Cancellation requested by the caller must still propagate rather
than being converted into base-data fallback.

Acquire the existing keyed `DataSetAssetLockProvider` with a distinct extension
key (location plus data type) while recalculating and writing an entry. The
source coordinator continues to own the separate `BOM/[station].zip` download
lock. This coalesces concurrent cold requests without coupling unrelated
locations or measurements.

Do not cache the full composed century-scale daily series. The annual base is
read from `ACORN-SAT.zip`; the cache contains at most one current calendar year
of values plus the small decision record.

### Retrieval time and source provenance

When at least one CDO value contributes to the composed series:

- set `ClimateRecordsResponse.RetrievedDate` from the successful CDO retrieval
  represented by the extension entry;
- retain the ACORN-SAT `DataSetMetadata` entry and append the CDO metadata for
  the requested location, including its open station;
- leave the requested `DataAdjustment` as `Adjusted`, because eligibility is
  the rule that treats the current raw values as equivalent, while the second
  source-metadata entry makes the provisional source explicit.

When no CDO value contributes, preserve the base ACORN-SAT retrieval time
(currently null) and source metadata only. A comparison attempt by itself does
not make annual ACORN-SAT newly retrieved.

No response schema change is required. If a future UI requirement needs to
label individual records as provisional, scope that separately rather than
embedding a second adjustment state into this feature.

### Exceptional routing, not metadata-driven automatic retrieval

Use well-known ACORN-SAT and CDO dataset IDs in one named location rather than
scattered GUID literals. Detection belongs at the `/climate-record` service
boundary after normal dataset selection. Do not set ACORN-SAT's
`DataDownloaderKey` to `bom-station`: that would resolve the wrong physical
asset, make the annual archive look automatically managed, and include
ACORN-SAT in `DataSetBatchRefresher`.

The CDO dependency request should use the selected location, matching
temperature data type, `DataAdjustment.Unadjusted`, and daily resolution. The
existing CDO metadata/resolver will select the open station and group all five
archive entries for validation/publication. Do not copy BOM URL construction or
station-selection rules into the ACORN-SAT component.

## Implementation stages

### Stage 0: Lock down current contracts and extract reusable build seams

1. Add well-known ACORN-SAT/CDO dataset IDs and metadata contract tests.
2. Change `IDataSetSourceUpdateCoordinator.PrepareAsync` and its implementation
   to accept `ICachedData?`, keeping every existing `DataSet` call unchanged.
3. Extract the dataset response-building seam needed to build from a prepared
   daily series without duplicating `DataSetEndpoints` behavior.
4. Add a `CancellationToken` to the `/climate-record` endpoint flow and pass it
   through the new service and existing coordinator.
5. Prove with existing endpoint tests that non-ACORN behavior and direct
   `/dataset` download prohibition are unchanged.

### Stage 1: Implement and test the pure ACORN-SAT record extender

1. Add the comparison/merge result models and decision enum.
2. Implement the previous-calendar-year completeness, overlap, date-set, and
   0.1-degree comparison rules with injected `TimeProvider`/date.
3. Generate only current-year, non-null, non-overlapping overlay records.
4. Generate the deterministic comparison signature and keep the component free
   of HTTP, filesystem publication, endpoint, and cache concerns.
5. Use real-shape fixtures for multi-station mappings, `023000`/`023090`,
   all-null `002079`, and the `046012` 2024 cutoff.

### Stage 2: Add the focused extension cache and CDO orchestration

1. Add `AcornSatExtensionCacheEntry` and a cache wrapper over
   `ClimateExplorerApiServices.LongtermCache`.
2. Implement signature/station/data-type validation and conclusive versus
   transient decision reuse.
3. Resolve/prepare the CDO dependency through the existing source coordinator;
   never call the BOM client directly.
4. Coalesce extension recalculation by location/data type and implement
   last-known-good overlay fallback.
5. Add structured logs for cache reuse, comparison decisions, CDO contribution
   range/count, annual-signature invalidation, and fallback. Do not log the
   complete recordset.

### Stage 3: Wire `/climate-record` and provenance

1. Route only selected ACORN-SAT adjusted temperature requests through
   `AcornSatClimateRecordService`.
2. Compose ACORN-SAT plus the daily overlay before standard filtering/binning.
3. Set retrieval time and append CDO source metadata only when CDO contributes.
4. Cover daily and monthly responses, filters, ordering, pagination, totals,
   and end-year behavior.
5. Register the service/cache/extender with Web API dependency injection. Keep
   `ClimateExplorer.Data.Misc` and the batch refresher unchanged; they still
   refresh CDO normally and never manufacture ACORN-SAT.

### Stage 4: Remove the obsolete executable

1. Delete `ClimateExplorer.Data.Bom.AcornSatUpdateFromRaw`, including its two
   unused parser classes and package references.
2. Remove its project/configuration/nesting entries from
   `ClimateExplorer.sln`, preserving unrelated solution changes.
3. Confirm no scripts, documentation, or build jobs reference the project.
4. Keep `ClimateExplorer.DataPipeline.BuildAcornSatArchive` unchanged so a
   manually supplied annual ACORN-SAT release still produces the deployed
   `ACORN-SAT.zip`.

## Tests and acceptance criteria

Use `MethodName_StateUnderTest_ExpectedBehavior` for every new C# test.

At minimum, cover:

- an eligible request on 2026-07-10 appending only 2026-01-01 through
  2026-07-10;
- ACORN-SAT ending in 2024, already containing a 2026 date, or not reaching the
  prior 31 December producing no overlay;
- zero comparison overlap, one-sided non-null dates, and all-null comparison
  years failing closed;
- exact equality, an accepted 0.1-degree boundary, and a rejected difference
  above tolerance for max/min and derived mean;
- current-year nulls being omitted and an empty current-year CDO series leaving
  ACORN-SAT unchanged;
- the CDO location series respecting historical mapping dates and resolving a
  different open station ID from the adjusted station;
- a matching annual signature reusing a conclusive decision, and a changed
  comparison year/value invalidating it;
- a fresh CDO source reusing the cached overlay, a stale source refreshing and
  replacing the overlay, and concurrent requests causing one download/merge;
- refresh failure returning a matching cached overlay, while a cold failure or
  invalid comparison returns base ACORN-SAT without failing the endpoint;
- caller cancellation propagating;
- daily and monthly binning plus month/day filters, ordering, `take`/`skip`,
  `TotalCount`, `StartYear`, and `EndYear` seeing the composed series;
- `RetrievedDate` and both ACORN-SAT/CDO source metadata appearing only when
  CDO contributes;
- unadjusted CDO, GHCNd, HadCET, non-temperature, non-ACORN, and direct
  `/dataset` requests retaining current behavior;
- resolver/batch tests continuing to prove that ACORN-SAT has no automatic
  source asset or downloader;
- solution/build tests passing after the obsolete project is removed.

The rollout is complete when:

- an eligible adjusted ACORN-SAT `/climate-record` request can return current-
  year CDO values after the normal on-demand CDO refresh;
- no request downloads or overwrites ACORN-SAT source data;
- inconclusive or adjusted comparison years never receive a raw overlay;
- a new annual ACORN-SAT package invalidates prior eligibility without manual
  cache deletion;
- failed CDO work never removes a known-good overlay or prevents base
  ACORN-SAT from being served;
- direct `/dataset` and batch-refresh behavior remain unchanged; and
- `ClimateExplorer.Data.Bom.AcornSatUpdateFromRaw` no longer exists in the
  repository or solution.

Verification should use the unit test suite and relevant project/solution
builds. Do not run the website, Playwright, Lighthouse, or browser tests.

## Risks and safeguards

- **The response remains labelled adjusted while its newest values originate
  from CDO.** Require the conservative eligibility rules and include both
  sources in metadata whenever an overlay contributes.
- **No observed difference is not proof when data is absent.** Require matching
  non-null date sets and at least one pair; cache missing/failed cases only as
  transient outcomes.
- **An annual ACORN-SAT release can revise history as well as add a year.** Read
  the annual base on every response as today and key only the eligibility
  decision to the comparison-year signature; never cache the base recordset.
- **A station can change independently between ACORN-SAT and CDO.** Resolve CDO
  by location and mapping dates, and store both station IDs in cache/logs.
- **The two sources can overlap unexpectedly.** Never overwrite an ACORN-SAT
  date. Treat current-year ACORN-SAT content as a reason not to extend.
- **A year boundary can make yesterday's overlay stale.** Use `TimeProvider`,
  compare only the immediately previous complete year, and let the new year
  produce a new cache decision/overlay key state.
- **CDO is a bundled station asset.** Preserve the existing whole-ZIP refresh
  and validation semantics; do not publish a temperature-only partial archive.
- **Concurrent requests can duplicate CPU work even when the download is
  coalesced.** Take a separate keyed extension lock around comparison/cache
  replacement.
- **Persistent cache corruption or an old schema must not block records.**
  Version the extension cache key/model and treat unreadable entries as misses,
  falling back to base ACORN-SAT.
