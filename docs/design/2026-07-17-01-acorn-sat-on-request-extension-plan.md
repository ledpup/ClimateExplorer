# Extend ACORN-SAT with recent CDO data on `/climate-record`

- **Date:** 2026-07-17
- **Status:** In progress — Stages 0, 1, 2, and 3 complete and verified
  end-to-end against the running Web API; Stage 4 (remove the obsolete
  executable) next
- **Author:** Codex
- **Scope:** `/climate-record`, ACORN-SAT/CDO record composition, dataset source coordination, extension decision/cache state, source metadata, Web API dependency injection, unit tests, and removal of `ClimateExplorer.Data.Bom.AcornSatUpdateFromRaw`
- **Builds on:** [Automated dataset downloads for historical API data](2026-07-13-01-automated-dataset-downloads-plan.md) and [Retire the recent-observations endpoint](2026-07-14-01-retire-recent-observations-plan.md)
- **Branch context:** `issues/file-rework`

## Goal

When `/climate-record` serves an adjusted ACORN-SAT daily temperature series,
extend it through today with unadjusted Bureau of Meteorology Climate Data
Online (CDO) observations only when the most recent complete ACORN-SAT year
demonstrates that the adjusted and CDO series are not materially different.
ACORN-SAT can be more than one year stale for a given data type (see
`046012`'s mean/minimum below), so the appended range can span more than one
calendar year: it always runs from the day after ACORN-SAT's latest date
through today, not just from 1 January of the request year.

For a request made on 2026-07-10, the ordinary successful shape (ACORN-SAT
complete through the prior year) is:

1. read the packaged annual ACORN-SAT series through 2025-12-31;
2. resolve and, if stale, refresh the open CDO station archive for the requested
   location using the existing `bom-station` downloader;
3. compare ACORN-SAT and CDO over 2025;
4. if the comparison proves that the series is eligible, append CDO values from
   2026-01-01 through 2026-07-10;
5. cache the small eligibility decision/overlay and return the composed
   recordset without altering `ACORN-SAT.zip` or its build-time source files.

When ACORN-SAT is stale by more than one year for a data type (e.g. `046012`'s
mean/minimum, complete only through 2024-12-31), the same request instead
compares over 2024 and, if eligible, appends CDO values from 2025-01-01
through 2026-07-10 — a two-calendar-year overlay from one comparison.

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
  than caching only the (at most few-year) overlay and eligibility state.

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
- At all 112 real locations, the adjusted station ID and the *open* CDO
  station ID (the mapping entry with a null `EndDate`) now match. Location
  `70a07bb0-2220-402b-be83-f2c35edfdd12` (Adelaide) is the interesting case: its
  CDO mapping closes `023000` in 1977, opens `023090` from 1977-02-01 to
  2018-06-30, then reopens `023000` from 2018-07-01 onward per
  `primarysites.txt`. `DataFileMapping_Australia_unadjusted.json` has been
  corrected to this three-entry mapping.
  `DataReaderFunctions.GetDataRecords` reads each mapping entry independently
  (own station file, own start/end filter) and merges by date, so the
  close/reopen pattern composes correctly with no duplicate-date collision;
  `DataSetSourceAssetResolver.IsAutomaticallyRetrievable` selects only the
  null-`EndDate` entry as the automatically refreshed station, so CDO
  correctly treats `023000` as open. The old filename assumption in the
  retired executable could not have handled this three-entry history, but the
  location-series reader used by this plan already does. The extension still
  only needs the currently open station for its comparison/append work; the
  historical `023090` segment matters for full-history reads, not for
  eligibility.
- The current packaged ACORN-SAT archive has every maximum-temperature file
  through 2025-12-31. Mean and minimum are also through that date except for
  station `046012`, whose mean and minimum files stop at 2024-12-31. Because
  CDO extension must now be able to cover more than one calendar year (see
  below), `046012`'s mean/minimum is the motivating real case for a
  multi-year append: given a matching 2024 comparison and no adjustments
  detected, a request in 2026 should be eligible to fill 2025-01-01 through
  today from CDO, not just the current calendar year.
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
- the CDO overlay records for the append range, if eligible (may span more
  than one calendar year when ACORN-SAT is more than one year stale);
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

- daily results receive individual overlay days across the whole append range;
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
3. ACORN-SAT reaches 31 December of some year `C` (the comparison year), where
   `C` is the latest year for which ACORN-SAT has complete 31-December
   coverage and `C < Y`. `C` is not required to be `Y - 1`: a data type such as
   station `046012`'s mean/minimum, which currently stops at 2024-12-31, has
   `C = 2024` even in a `Y = 2026` request, and the append range below then
   spans both 2025 and 2026. Do not append when ACORN-SAT already contains any
   date in year `Y`, and fail closed (`AcornNotThroughPreviousYear`) if
   ACORN-SAT has no complete prior year at all.
4. The non-null date sets match over comparison year `C`. A value present only
   in ACORN-SAT or only in CDO for that year is insufficient evidence that raw
   values from `C + 1` onward can stand in for the adjusted series.
5. Every paired value in year `C` is within the existing 0.1-degree tolerance.
   Preserve the executable's intended one-decimal comparison semantics and
   cover the exact boundary in tests; do not rely on untested binary
   floating-point equality.

If those rules pass, select only non-null CDO records whose dates are after
the latest ACORN-SAT date and no later than `today`, inclusive — that is, from
`C + 1`-01-01 through `today`, which may span more than one calendar year when
ACORN-SAT is more than one year stale. Never replace an ACORN-SAT date and
never append a date on or before the latest ACORN-SAT date. It is valid for
the eligible overlay to be empty when CDO has not yet published any value
after that date; return base ACORN-SAT in that case while retaining the
eligibility decision.

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
- the overlay records for the append range (from the day after the latest
  ACORN-SAT date through `today`; this may span more than one calendar year);
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
read from `ACORN-SAT.zip`; the cache contains only the append-range values
(bounded by how stale ACORN-SAT is for that data type — ordinarily one
calendar year, occasionally a few) plus the small decision record.

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

### Stage 0: Lock down current contracts and extract reusable build seams — done

1. **Done.** Added `ClimateExplorer.WebApi/AcornSat/AcornSatDatasetIds.cs` (the
   ACORN-SAT and CDO dataset definition GUIDs in one named location) plus
   `AcornSatDatasetIdsTests.cs`, which resolves each ID through
   `DataSetDefinition.GetDataSetDefinitions()` and asserts `ShortName`,
   measurement adjustment, and that ACORN-SAT still has no
   `DataDownloaderKey`.
2. **Done.** `IDataSetSourceUpdateCoordinator.PrepareAsync` and
   `DataSetSourceUpdateCoordinator` now accept `ICachedData?` instead of
   `DataSet?` (mechanical change — `DataSetFreshnessPolicy.IsFresh` and
   `DataSetRetrievalDate.OldestFor` already operated on `ICachedData`). Updated
   the `IDataSetSourceUpdateCoordinator` test stubs in
   `ClimateRecordsEndpointsTests.cs` and `DataSetEndpointSourceUpdateTests.cs`
   to match.
3. **Done.** Added `DataSetBuilder.BuildDataSetFromSeries(request, series)`,
   which runs the same validation/filter/binning pipeline as `BuildDataSet`
   but starts from a caller-supplied `SeriesProvider.Series` instead of
   reading through `SeriesProvider`. Extracted
   `DataSetEndpoints.BuildResponseDataSet(body, series, retrievedDate)` from
   `PostDataSets`, so a future caller can build a `DataSet` response
   (geographical entity, source metadata, binned records) from an
   already-prepared series without duplicating that logic. `PostDataSets`
   itself is behaviourally unchanged (verified by the existing test suite).
4. **Done.** `ClimateRecordsEndpoints.GetClimateRecords` now takes a
   `CancellationToken` parameter (bound automatically by minimal APIs) and
   passes it to `DataSetEndpoints.PostDataSets`. Added
   `GetClimateRecords_CallerCancelsRequest_PropagatesOperationCanceledException`,
   which proves a cancelled token throws `OperationCanceledException` through
   the endpoint rather than being swallowed.
5. **Done.** Full existing test suite (413 tests) passes unchanged after all
   of the above.

### Stage 1: Implement and test the pure ACORN-SAT record extender — done

1. **Done.** Added `ClimateExplorer.Data.Downloading/Extenders/` with
   `AcornSatExtensionDecision` (the six-value enum) and
   `AcornSatRecordExtensionResult` (decision, comparison year, latest
   ACORN-SAT date, signature, station IDs, overlay records,
   `CdoContributed`).
2. **Done.** `AcornSatRecordExtender.Extend(...)` computes the comparison year
   as the latest year for which ACORN-SAT reaches 31 December (not
   necessarily `today`'s year minus one), checks the non-null date sets match
   over that year, and reproduces the retired executable's exact
   `Math.Round(Math.Abs(diff), 1) > 0.1` tolerance semantics.
3. **Done.** The overlay is every non-null CDO date strictly after the latest
   ACORN-SAT date through `today`, which the implementation and tests confirm
   can span more than one calendar year.
4. **Done.** The comparison signature is a SHA-256 hash over the latest
   ACORN-SAT date plus the ordered comparison-year date/value pairs; the
   component takes no `HttpClient`, file path, or cache dependency.
5. **Done.** `AcornSatRecordExtenderTests.cs` (18 tests) covers the ordinary
   single prior-year case, the `046012`-style two-calendar-year bridge, the
   Adelaide `023000`/`023090`/`023000` close-reopen history, the exact
   0.1-degree boundary (accepted) versus an above-tolerance difference
   (rejected), zero/one-sided/all-null comparison overlap, missing station,
   missing/empty ACORN-SAT or CDO input, and comparison-signature
   stability/invalidation.

### Stage 2: Add the focused extension cache and CDO orchestration — done

0. **Done, ahead of schedule.** Added `ClimateExplorer.WebApi/AcornSat/AcornSatStationResolver.cs`
   (`AcornSatStationResolution`) implementing eligibility rule 2: resolves the
   single non-blank adjusted ACORN-SAT station and the single open
   (null-`EndDate`), non-blank CDO station for a location from
   `DataSetDefinition.DataLocationMapping`. `AcornSatStationResolverTests.cs`
   covers the Adelaide close/reopen history (both sides resolve to `023000`),
   the all-null placeholder location
   (`143983a0-240e-447f-8578-8daf2c0a246a`), an unknown location, and asserts
   every real ACORN-SAT location resolves to exactly one open CDO station
   (a regression guard for the mapping fix above).
1. **Done.** Added `AcornSatExtensionCacheEntry`
   (`ClimateExplorer.Data.Downloading/Extenders/AcornSatExtensionCacheEntry.cs`,
   implements `ICachedData`, includes an `IsConclusive` helper for the
   Eligible/AdjustmentsDetected-only reuse rule) and
   `AcornSatExtensionCache` (`ClimateExplorer.WebApi/AcornSat/AcornSatExtensionCache.cs`),
   a thin wrapper over `ICache` keyed by location + data type so callers never
   see the underlying cache key format.
   `AcornSatExtensionCacheTests.cs` covers the empty/round-trip/no-collision
   cases against a fake `ICache`.
2. **Done.** Added `AcornSatClimateRecordService.ResolveAsync`
   (`ClimateExplorer.WebApi/AcornSat/AcornSatClimateRecordService.cs`), which
   ties `AcornSatStationResolver`, `AcornSatRecordExtender`, and
   `AcornSatExtensionCache` together. **Deliberate simplification versus the
   original plan text:** rather than trusting a `UseCached` coordinator
   outcome to skip the comparison, the service always re-reads ACORN-SAT/CDO
   from local disk and re-runs `AcornSatRecordExtender.Extend` (a cheap local
   read plus an O(365) loop - no network, no full-archive hashing). The
   extension cache's `RetrievedDate` is still given to
   `IDataSetSourceUpdateCoordinator.PrepareAsync` so it continues to gate how
   often CDO is actually refreshed over the network; the cache otherwise
   exists to detect conclusive decisions and to serve last-known-good
   overlays. This is simpler than the originally sketched skip-on-fresh path
   and produces the same output, at the cost of always paying the (small)
   comparison cost even when nothing changed.
3. **Done.** The CDO dependency is resolved by giving `IDataSetSourceUpdateCoordinator.PrepareAsync`
   a `PostDataSetsRequestBody` targeting the CDO dataset/location/data
   type/`Unadjusted`; `BomDataSetDownloader` is never called directly.
4. **Done.** A keyed `DataSetAssetLockProvider` lease
   (`acorn-sat-extension:{locationId}:{dataType}`) wraps cache read through
   cache write, distinct from the coordinator's own `BOM/[station].zip`
   asset lock. On `RefreshFailed` with a conclusive cached entry, that
   entry's overlay is returned as-is (last-known-good); on `RefreshFailed`
   with no usable cache, the service falls through to a cold comparison
   against whatever CDO archive is already published, with a null retrieval
   time so the result remains retryable. Cancellation propagates through the
   lock acquisition and the coordinator call rather than being caught.
5. **Done, lighter than sketched.** `LogDebug`/`LogWarning`/`LogInformation`
   calls cover no-open-station, refresh-failure fallback, and the final
   decision (with comparison year and overlay count, never the full
   recordset). No separate annual-signature-invalidation log line was added;
   the signature comparison itself isn't currently logged distinctly from
   the decision.
   `AcornSatClimateRecordServiceTests.cs` covers: no-open-station short-circuiting
   before the coordinator is ever called; `RefreshFailed` with a conclusive
   cached entry returning that overlay without rereading; `RefreshFailed`
   with no cache falling back to a cold comparison with a null retrieval
   date; `Rebuild` using the coordinator's retrieval date and writing a
   conclusive decision to the cache; and cancellation propagation.

### Stage 3: Wire `/climate-record` and provenance — done

1. **Done.** `ClimateRecordsEndpoints.GetClimateRecords` takes an optional
   `[FromServices] AcornSatClimateRecordService?` parameter (optional so
   every pre-existing direct-call test site keeps compiling unchanged) and
   routes to `AcornSatClimateRecordService.BuildComposedDataSetAsync` only
   when the resolved dataset is ACORN-SAT, the measurement is `Adjusted`,
   and the data type is `TempMean`/`TempMax`/`TempMin`. Every other request -
   including CDO's own `Unadjusted` measurement for the same location, and
   direct `/dataset` access - takes the unchanged `DataSetEndpoints.PostDataSets`
   path.
2. **Done.** `AcornSatClimateRecordService.BuildComposedDataSetAsync` reads
   the adjusted ACORN-SAT series, concatenates the extension's overlay
   records (already guaranteed to be strictly after ACORN-SAT's latest
   date), sorts by date, and builds the response via the Stage 0
   `DataSetBuilder.BuildDataSetFromSeries` / `DataSetEndpoints.BuildResponseDataSet`
   seam - the same binning/filtering/pagination pipeline every other
   `/climate-record` request uses, so daily, `monthly=true`, month/day
   filters, ordering, `take`/`skip`, `TotalCount`, and `StartYear`/`EndYear`
   all see one composed series without any bespoke logic in
   `ClimateRecordsEndpoints`.
3. **Done.** `RetrievedDate` is taken from the coordinator's CDO retrieval
   only when the overlay actually contributed a value
   (`AcornSatRecordExtensionResult.CdoContributed`); otherwise it stays null.
   CDO's own `DataSetMetadata` entry is appended to the ACORN-SAT entry only
   in that same case.
4. **Done**, via reuse of the shared pipeline rather than bespoke coverage of
   each acceptance-criteria bullet individually.
   `ClimateRecordsEndpointsAcornSatTests.cs` covers: a daily adjusted request
   returning ACORN-SAT metadata with CDO metadata/`RetrievedDate` present iff
   contributed; a `monthly=true` request aggregating the composed series;
   an unadjusted request (which resolves to CDO, not ACORN-SAT) not routing
   through the extension; and the service being absent falling back to the
   ordinary `DataSetEndpoints` path. These deliberately assert only the
   invariants the design guarantees, not the exact eligibility outcome or
   overlay content of today's packaged data (which can legitimately change
   as CDO/ACORN-SAT are refreshed).
5. **Done.** `Program.cs` registers `AcornSatExtensionCache` (over the
   existing `longtermCache` instance) and `AcornSatClimateRecordService` as
   singletons, reusing the already-registered `IDataSetSourceUpdateCoordinator`,
   `DataSetAssetLockProvider`, and `TimeProvider`. `ClimateExplorer.Data.Misc`
   and `DataSetBatchRefresher` are untouched.

   **Verified against the running Web API** (not just unit tests): started
   `ClimateExplorer.WebApi` and requested
   `/climate-record?locationId=70a07bb0-2220-402b-be83-f2c35edfdd12&dataType=TempMax&dataAdjustment=Adjusted`.
   It refreshed the real `BOM/023000.zip` over the network, logged
   `ACORN-SAT extension decision ... Eligible (comparison year 2025, 197
   overlay records)`, and returned a 200 response whose records include
   `2026-01-26` at 44.7°C - a CDO-sourced value beyond the packaged
   `ACORN-SAT.zip`'s 2025-12-31 end date. That live run's on-disk BOM refresh
   left `ClimateExplorer.WebApi/Datasets/BOM/023000.zip` modified in the
   working tree (802,385 → 808,045 bytes) as an incidental side effect of
   verification, alongside the `086338.zip`/`094029.zip` changes already
   present before this work began.

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

- an eligible request on 2026-07-10 with ACORN-SAT complete through 2025
  appending only 2026-01-01 through 2026-07-10;
- an eligible request on 2026-07-10 with ACORN-SAT complete only through 2024
  (as with `046012` mean/minimum) appending 2025-01-01 through 2026-07-10 —
  a two-calendar-year overlay from a single 2024 comparison;
- ACORN-SAT already containing a 2026 date, or having no complete prior year
  at all, producing no overlay;
- zero comparison overlap, one-sided non-null dates, and all-null comparison
  years failing closed;
- exact equality, an accepted 0.1-degree boundary, and a rejected difference
  above tolerance for max/min and derived mean;
- overlay-range nulls being omitted and an empty CDO contribution leaving
  ACORN-SAT unchanged;
- the CDO location series respecting historical mapping dates across a
  closed/reopened station history (`023000` → `023090` → `023000`) while
  still resolving the same open station ID as the adjusted station;
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
  compare only ACORN-SAT's latest complete year (which may be more than one
  year prior to `today`), and let the new year produce a new cache
  decision/overlay key state.
- **A multi-year append range means more can go wrong between the comparison
  year and today.** The overlay still only depends on the single
  comparison-year signature; do not require every intervening year to be
  independently verified, since CDO's own validation already covers its whole
  series.
- **CDO is a bundled station asset.** Preserve the existing whole-ZIP refresh
  and validation semantics; do not publish a temperature-only partial archive.
- **Concurrent requests can duplicate CPU work even when the download is
  coalesced.** Take a separate keyed extension lock around comparison/cache
  replacement.
- **Persistent cache corruption or an old schema must not block records.**
  Version the extension cache key/model and treat unreadable entries as misses,
  falling back to base ACORN-SAT.
