# Retire `/recent-observations` in favor of `/climate-record`

- **Date:** 2026-07-14
- **Status:** Proposed
- **Author:** Claude
- **Scope:** `ClimateExplorer.WebApi` (`RecentObservationsEndpoints`, `RecentObservations/*`, `ClimateRecordsEndpoints`, `DataSetMetadataBuilder`), `ClimateExplorer.Data.Downloading` (`DataSetSourceAssetResolver`, `DataSetFreshnessPolicy`), `ClimateExplorer.Core.Model` (`ClimateRecordsResponse`), `ClimateExplorer.WebApiClient`, `ClimateExplorer.Web.Client/Services/RecentObservations`
- **Builds on:** [Automated dataset downloads for historical API data](2026-07-13-01-automated-dataset-downloads-plan.md), [AboutData Station Metadata Plan](2026-07-05-01-about-data-station-metadata-plan.md), [Location Dataset Metadata Plan](2026-07-07-01-location-dataset-metadata-plan.md)
- **Branch context:** `issues/file-rework`

## Summary

[docs/notes/2026-07-14-01-climate-records-vs-recent-obs.md](../notes/2026-07-14-01-climate-records-vs-recent-obs.md) investigated whether `/recent-observations` can be retired now that `/climate-record` can trigger the same downloads, and found four gaps that were load-bearing in `/recent-observations`: content-aware freshness, cross-type (BOM vs GHCNd) source consistency, a flat per-series `SourceMetadata` shape, and a cheap "is this location supported" probe.

This plan re-examines those four gaps from the UI's point of view (`RecentObservationsDataProvider`), using one new fact: **GHCNd locations always map to exactly one station, and BOM locations always map to exactly one *currently open* station** (older stations are closed with a fixed end date, never reopened). That fact was verified against the live mapping data, not assumed — see "Verified facts" below.

Given that fact, three of the four gaps turn out to already have free or near-free answers in the general pipeline:

- **Gap 2 (SourceMetadata shape)** is closed almost entirely by wiring: `DataSetEndpoints.PostDataSets` already builds `DataSet.SourceMetadata` (`List<DataSetMetadata>`) for every request, `ClimateRecordsEndpoints.GetClimateRecords` already receives it, and just never copies it onto `ClimateRecordsResponse`. Because there is always exactly one open station, "the active station" is an unambiguous `Stations.SingleOrDefault(x => x.StationEndDate is null)` — no reconciliation logic needed.
- **Gap 4 (cheap support probe)** is already answered by whether `ClimateRecordsEndpoints` found a `matchingDsd`/`md` at all (`DataResolution.HasValue` on the response), which is metadata-only and happens before any network I/O.
- **Gap 3 (cross-type consistency)** already holds today, as an accident of declaration order in `DataSetDefinitionsBuilder` (BOM is declared, and therefore matched, before GHCNd for temperature *and* precipitation) plus BOM bundling all its measurements into one physical station archive. It needs a regression test to stop being an accident, not new production code.
- **Gap 1 (freshness)** is the one real gap. The general pipeline is time-since-fetch (24h for daily); `/recent-observations` retried every ≥6h until the response actually contained yesterday's row. `bom-station` and `ghcnd-station` back the large majority of location traffic and both publish daily, so this plan ports true content-aware freshness for those two downloader keys specifically, rather than approximating it with a shorter time window (see Stage 3).

Separately, this investigation surfaced a **live bug, independent of this retirement**: `DataSetSourceAssetResolver` resolves an automatic-retrieval asset for *every* historical BOM station mapped to a location, not just the currently open one, and does this on ordinary `/climate-record`/`/dataset` requests today — not only in the nightly batch. 82 of 113 BOM (unadjusted) locations have 2+ historical stations. This directly matches what was flagged when re-reading `BomDailyDataClient.cs`: it should not be possible for the automatic pipeline to keep re-fetching a closed station. Fix this first (Stage 1); it stands on its own regardless of whether `/recent-observations` is retired.

## Verified facts

- `DataFileMapping_ghcnd_temperature.json`: 1734/1734 locations map to exactly one station. GHCNd never has a multi-station location.
- `DataFileMapping_Australia_unadjusted.json`: 82/113 locations have 2 or 3 time-boxed station entries (older stations closed with a fixed `EndDate`), but in **every** location the *last* entry has `EndDate: null` — there is always exactly one open station, never zero, never more than one. `DataFileMapping_Australia_adjusted.json` (ACORN-SAT) has zero multi-station locations.
- `RecentObservationsDataSourceHelpers.GetMostRecentOperatingStationId`, the "as-at" station picker that looked like `/recent-observations`-only capability, is only ever called with `asAt = today` ([RecentObservationsEndpoints.cs:22](../../ClimateExplorer.WebApi/RecentObservationsEndpoints.cs#L22)), so it is already degenerate: it always resolves to whichever entry has `EndDate == null`.
- Every station id referenced anywhere in `DataFileMapping_Australia_unadjusted.json` (open or closed, 205 real ids) already has a packaged archive under `ClimateExplorer.WebApi/Datasets/BOM/*.zip`. There is no closed station missing a deployed source file, so narrowing automatic refresh to the open station only (Stage 1) does not strand any location without a readable file for its closed-station history.
- `DataSetEndpoints.PostDataSets` ([DataSetEndpoints.cs:41,63](../../ClimateExplorer.WebApi/DataSetEndpoints.cs#L41)) already builds and attaches `List<DataSetMetadata>? SourceMetadata` for every request. `ClimateRecordsEndpoints.GetClimateRecords` ([ClimateRecordsEndpoints.cs:69-94](../../ClimateExplorer.WebApi/ClimateRecordsEndpoints.cs#L69)) already receives that `dataSet` back and copies `dataSet.RetrievedDate` (line 169) but not `dataSet.SourceMetadata`.
- `DataSetDefinitionsBuilder` declares the BOM `DataSetDefinition` (id `E5EEA4D6…`, containing TempMax/TempMin/TempMean/Precipitation/SolarRadiation together) before GHCNd (`87C65C34…`) and GHCNdp (`5BBEAF4C…`). `ClimateRecordsEndpoints.GetClimateRecords`'s dsd selection (lines 28-53) picks the first dsd in list order whose measurement definitions match, so BOM is already preferred over GHCNd for all three measurement types whenever a location is mapped in both — matching `/recent-observations`'s explicit `BOM ?? GHCNd` context selection ([RecentObservationsEndpoints.cs:81-82](../../ClimateExplorer.WebApi/RecentObservationsEndpoints.cs#L81)).
- `DataSetSourceAssetResolver.ResolveAsync` ([DataSetSourceAssetResolver.cs:36](../../ClimateExplorer.Data.Downloading/Orchestration/DataSetSourceAssetResolver.cs#L36)) and `GetAllResolutionInputs` (lines 121-125) both take every filter in `LocationIdToDataFileMappings` unfiltered by `EndDate`, so a multi-station BOM location resolves to one `DataSetDownloadRequest` per historical station. `DataSetSourceUpdateCoordinator.EnsureCurrentAsync` ([DataSetSourceUpdateCoordinator.cs:94-118](../../ClimateExplorer.Data.Downloading/Orchestration/DataSetSourceUpdateCoordinator.cs#L94)) then freshness-checks and, once >24h stale, re-downloads *each* of them independently — including closed stations whose content can never change again.
- `SeriesProvider` reads `DataSetDefinition.DataLocationMapping.LocationIdToDataFileMappings` directly ([SeriesProvider.cs:198](../../ClimateExplorer.Core/DataPreparation/SeriesProvider.cs#L198)), independently of `DataSetSourceAssetResolver`. Excluding closed stations from the resolver does not stop `SeriesProvider` from reading their already-downloaded/packaged files for full historical stitching.

## Stage 1: Stop the automatic pipeline from re-fetching closed BOM stations

This is a live latency/bandwidth bug today, not a retirement prerequisite — ship it independently and first.

Exclude filters that are not the currently open station from automatic-retrieval resolution, at both call sites in `DataSetSourceAssetResolver`:

- `ResolveAsync` (per-request path used by `/climate-record` and `/dataset`): line 36, change `mappedFilters!.Where(x => !string.IsNullOrWhiteSpace(x.Id))` to also require `x.EndDate is null`.
- `GetAllResolutionInputs` (batch path used by `ClimateExplorer.Data.Misc`'s `DataSetBatchRefresher`): lines 121-125, same additional predicate.

Do **not** touch `SeriesProvider`'s traversal — it must keep reading every mapped filter's file (open and closed) so full multi-century history keeps stitching together from the closed stations' already-downloaded archives; only the *refresh* path should stop considering them.

This is safe for GHCNd (every location already has exactly one filter with `EndDate == null`, so the added predicate changes nothing there) and only changes behavior for the 82 multi-station BOM locations.

Edge case: if a location's only filter ever had a non-null `EndDate` (fully decommissioned station with no replacement), it would resolve to zero refreshable assets for that location — same as any other unmanaged dataset (`assets.Count == 0` in `DataSetSourceUpdateCoordinator.PrepareAsync`, which falls back to reading the existing cached/deployed file rather than throwing). No location currently has this shape, but add a packaging-time or startup contract test asserting "every mapped location has exactly one filter with `EndDate == null`" so a future mapping edit that violates it fails loudly instead of silently losing automatic refresh for that location.

### Tests

- `ResolveAsync_MultiStationBomLocation_ResolvesOnlyTheOpenStation` — a 3-filter BOM location produces exactly one `DataSetDownloadRequest`, keyed to the filter with `EndDate == null`.
- `ResolveAllAsync_MultiStationBomLocation_ExcludesClosedStations` — same assertion through the batch path.
- `ResolveAsync_SingleStationGhcndLocation_UnaffectedByEndDateFilter` — regression guard that the GHCNd single-station path is unchanged.
- Contract test: every `LocationIdToDataFileMappings` entry across every checked-in `DataFileMapping_*.json` has exactly one filter with `EndDate == null`.

## Stage 2: Add `SourceMetadata` and confirm the structural "supported" signal on `ClimateRecordsResponse`

Mechanical, low-risk — this mostly reuses machinery already shipped for `AboutData`/`LocationDataSetMetadataSidePanel`.

1. Add `public List<DataSetMetadata>? SourceMetadata { get; set; }` to `ClimateRecordsResponse` ([ClimateRecordsResponse.cs](../../ClimateExplorer.Core/Model/ClimateRecordsResponse.cs)).
2. In `ClimateRecordsEndpoints.GetClimateRecords`, set `SourceMetadata = dataSet.SourceMetadata` next to the existing `RetrievedDate = dataSet.RetrievedDate` (around line 169).
3. No new builder, no new endpoint, no server-side "pick the active station" logic — `DataSetMetadata.Stations` keeps being the full historical list already used elsewhere. Do the "active station" pick client-side (Stage 5), so `DataSetMetadata` itself stays the general-purpose shape.

"Active station" derivation, given Stage 1's invariant: `stations.SingleOrDefault(x => x.StationEndDate is null)`.

"Structural support" (replacing `RecentObservationsResponse.IsSupported`): `ClimateRecordsEndpoints.GetClimateRecords` already returns a response with `DataResolution == null` when no `matchingDsd`/`md` was found (lines 55-62, before any network I/O), versus `DataResolution` set (even if `Records` ends up empty from a transient fetch failure) when a structural match exists. `DataResolution.HasValue` (equivalently `SourceMetadata is { Count: > 0 }`) is the same signal `RecentObservationsBomDataSource.GetContext`/`RecentObservationsGhcndDataSource.GetContext` compute today, already available with zero new server code.

### Tests

- `GetClimateRecords_KnownLocation_ReturnsSourceMetadataFromDataSet`.
- `GetClimateRecords_MultiStationBomLocation_SourceMetadataStationsContainsExactlyOneOpenStation`.
- `GetClimateRecords_UnmatchedDataTypeForLocation_ReturnsNullDataResolutionAndNoSourceMetadata` — locks in the "supported" signal.

## Stage 3: Freshness — content-aware for `bom-station`/`ghcnd-station`

**Decision: content-aware, not a shorter time window.** `bom-station` and `ghcnd-station` back the large majority of locations, and both providers publish daily (BOM every day; GHCNd daily in many networks, notably the US, even though not universal). That combination — high traffic and a source that genuinely updates daily — is exactly the case worth the extra mechanism. Most other automatically-managed datasets (Mauna Loa CO₂, ODGI, HadCET, Greenland, sea level, ozone, …) are both lower-traffic (one location or a handful of global series each) and don't publish same-day/next-day, so a blanket content-aware policy would just make them retry forever without ever catching up — the existing 24h/28-day time-based policy stays their default.

This does **not** repeat the pattern rejected for NOAAGlobalTemp. That rejection was about adding a field to shared `DataSetSourceState` with exactly one consumer for a niche, low-traffic dataset. Here, the mechanism being added — "the latest date actually found in a downloaded daily source" — is generic and cheap for *any* daily, one-value-per-row downloader, because `DataSetDownloadValidator.ValidateAsync` already parses every measurement's records to validate them ([DataSetDownloadValidator.cs:15-36](../../ClimateExplorer.Data.Downloading/Orchestration/DataSetDownloadValidator.cs#L15)). Recording the max date it already saw costs nothing extra to compute. Only the *freshness policy's interpretation* of that field — "treat it as enough to skip a retry" — is scoped to the two downloader keys that need it; every other downloader can populate the same field for free and simply won't have it consulted differently.

### Behavior

Match `/recent-observations`'s semantics exactly: if the source already has a record for yesterday (or today), it's fresh regardless of how long ago it was fetched — don't touch the network again until tomorrow. If it doesn't have yesterday's row yet, retry at most once every 6 hours.

### Mechanism

1. **`DataSetDownloadValidator.ValidateAsync`** changes from `Task` to `Task<DateOnly?>`. For each measurement it already parses (`RowDataType.OneValuePerRow` via `DataReaderFunctions.GetDataRecords`), when `MeasurementDefinition.DataResolution == DataResolution.Daily`, track the maximum `record.Date` seen. Return the **minimum** of that maximum across all of the request's measurements (not the overall maximum) — a BOM/GHCNd asset bundles several measurements (temp max/min/mean/precip[/solar]) into one archive, and freshness should reflect the *worst-covered* bundled measurement, not the best, so a lagging precipitation feed can't be masked by an up-to-date temperature feed sharing the same download. Non-daily/`TwelveMonthsPerRow` measurements don't contribute and leave the result `null` for datasets that don't use this at all.
2. **`DataSetSourceState`** ([DataSetSourceState.cs](../../ClimateExplorer.Core/Model/DataSetSourceState.cs)) gains `public DateOnly? LatestRecordDate { get; set; }`.
3. **`DataSetSourceUpdateCoordinator.RefreshAsync`** ([DataSetSourceUpdateCoordinator.cs:169-222](../../ClimateExplorer.Data.Downloading/Orchestration/DataSetSourceUpdateCoordinator.cs#L169)) captures the validator's return value (`var latestRecordDate = await validator.ValidateAsync(...)`) and sets it on the constructed `DataSetSourceState`. This only updates on an actual (re)download attempt, which is correct: it's the last time the source was genuinely checked.
4. **`DataSetFreshnessPolicy`** gets a new overload used only for source-state checks: `IsFresh(DataSetSourceState? state, DataResolution dataResolution, string downloaderKey)`. When `downloaderKey` is `"bom-station"` or `"ghcnd-station"` (a short, explicit, named allowlist — not a new metadata field) and `dataResolution == DataResolution.Daily`:
   ```csharp
   var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
   var hasYesterday = state.LatestRecordDate is { } latest && latest >= today.AddDays(-1) && latest <= today;
   return hasYesterday || IsWithinHours(state.RetrievedDate, contentAwareRetryWindowHours: 6);
   ```
   A `LatestRecordDate` in the future (bad parse/timezone skew) fails `latest <= today` and falls through to the 6-hour retry window rather than being trusted blindly, consistent with how the existing policy already treats a future `RetrievedDate` as untrustworthy. Every other downloader key keeps calling the existing `IsFresh(ICachedData?, DataResolution)` overload unchanged.
5. **`DataSetSourceUpdateCoordinator.GetCurrentStateAsync`** ([DataSetSourceUpdateCoordinator.cs:139-167](../../ClimateExplorer.Data.Downloading/Orchestration/DataSetSourceUpdateCoordinator.cs#L139)) is the only call site that needs the new overload — it already has `asset.DownloaderKey` in scope. The response-level check in `PrepareAsync` (line 75, against the cached `DataSet`) stays on the plain `ICachedData` overload: it's a secondary fast-path that only matters once the source state is *already* confirmed fresh (see the outcome-combination logic at lines 64-91), so it doesn't need content-awareness of its own.

`today` is computed from `TimeProvider.GetUtcNow()` (UTC), not server-local time. The old `/recent-observations` code used `DateOnly.FromDateTime(DateTime.Today)` (server-local). Neither is truly station-timezone-correct (BOM stations are AEST/AEDT, GHCNd stations span every timezone); UTC is a deliberate, testable normalization consistent with the rest of `DataSetFreshnessPolicy`, not a regression — the old local-time check was never rigorously correct either.

### What this still doesn't guarantee

If BOM or GHCNd is down or simply hasn't published yet, the 6-hour retry keeps trying indefinitely — same as `/recent-observations` did. This is a real network cost class that's new to the general pipeline for these two keys specifically (every popular multi-station-adjacent location can trigger a real download attempt up to 4 times/day if a source is lagging), but it's bounded and matches what already ran in production under `/recent-observations`, just relocated.

### Tests

- `ValidateAsync_DailyOneValuePerRowMeasurement_ReturnsLatestRecordDate`.
- `ValidateAsync_MultipleMeasurementsWithDifferingCoverage_ReturnsMinimumOfTheirLatestDates`.
- `ValidateAsync_NonDailyOrTwelveMonthsPerRow_ReturnsNull`.
- `IsFresh_BomOrGhcndStationWithYesterdaysRecord_IsFreshRegardlessOfRetrievedDateAge`.
- `IsFresh_BomOrGhcndStationMissingYesterdaysRecord_IsFreshOnlyWithinSixHoursOfRetrieval`.
- `IsFresh_BomOrGhcndStationWithFutureLatestRecordDate_TreatedAsMissingAndFallsBackToRetryWindow`.
- `IsFresh_OtherDownloaderKeyWithDailyResolution_StillUsesTwentyFourHourWindow` — regression guard that every other daily dataset is unaffected.
- `IsFresh_NonDailyResolutionForBomOrGhcnd_StillUsesTimeBasedWindow` — guards the resolution gate even though no current BOM/GHCNd measurement is non-daily.

## Stage 4: Lock in BOM-before-GHCNd consistency

No production change — the ordering already produces single-source-per-location results for TempMax/TempMin/Precipitation (see "Verified facts"). Add a regression test so it stops being an accident of declaration order:

- `DataSetDefinitionOrdering_BomPrecedesGhcndForTempMaxTempMinAndPrecipitation` — asserts `DataSetDefinitionsBuilder.BuildDataSetDefinitions()` places the BOM definition before both GHCNd definitions, so a future reordering (alphabetizing, inserting a new AU source) fails a test instead of silently splitting a location's max/min/precip across two providers.

## Stage 5: Migrate `RecentObservationsDataProvider` onto `/climate-record` only

Once Stages 2-4 land, `RecentObservationsDataProvider.FetchTemperatureData`/`FetchPrecipitationData` ([RecentObservationsDataProvider.cs:74-128](../../ClimateExplorer.Web.Client/Services/RecentObservations/RecentObservationsDataProvider.cs#L74)) already do most of the needed merge shape — they just also merge in a second, now-redundant, fetch:

- Delete `GetRecentObservations`, `recentResponseCache`, `AwaitAndEvictOnFailure`, and the `recentTask`/`recentResponse` half of both fetch methods.
- `FetchTemperatureData`: keep the existing `historicalMaxTask`/`historicalMinTask` calls, drop `MergeDailyDataRecords(historical, recent)` in favor of using `historicalMaxResponse.Records`/`historicalMinResponse.Records` directly — freshness now lives server-side (Stage 3). `hasHistoricalMaxMin`/mean-fallback logic is unchanged.
- Replace the `!recentResponse.IsSupported` unsupported check with `!historicalMaxResponse.DataResolution.HasValue && !historicalMinResponse.DataResolution.HasValue` (Stage 2's structural signal).
- `CreateSourceMetadata`: replace reading `recentResponse.TempMax?.SourceMetadata`/`TempMin?.SourceMetadata` with a small client-local mapper from `historicalMaxResponse.SourceMetadata`/`historicalMinResponse.SourceMetadata` (now `List<DataSetMetadata>`) to the existing `RecentObservationSourceMetadata` shape: pick `Stations.SingleOrDefault(x => x.StationEndDate is null)` per Stage 2, `RetrievedAtUtc` from `ClimateRecordsResponse.RetrievedDate`. Keep constructing `RecentObservationSourceMetadata` itself — do not touch `RecentObservationsPanel.razor`, `RecentObservationRetrievalMetadataSelector`, or `RecentObservationsTabResult`, which all consume that exact shape today and have no reason to change.
- `FetchPrecipitationData`: same pattern against `historicalResponse.Records`/`historicalResponse.SourceMetadata`.
- Check whether `MergeDailyDataRecords` has any other caller before deleting it; if not, delete it along with the two now-unused adjustment/merge helpers it exists for.
- Re-examine whether `recentResponseCache`'s original rationale (comment at lines 17-20: sharing one fetch across both tabs to avoid re-downloading the same GHCNd/BOM file) still needs its own cache layer once there is only one endpoint per data type, or whether `DataService`'s own response caching is sufficient — decide during implementation rather than presupposing here.

### Tests

- Existing `RecentObservationsDataProvider`-adjacent tests (calculator/view-model tests) should be unaffected since they operate on `RecentObservationsDataSet`, not the removed endpoint.
- Add/port: temperature and precipitation fetch build a merged, deduplicated-by-date record list purely from `/climate-record`; unsupported location produces `UnsupportedTemperature()`/`UnsupportedPrecipitation()`; source metadata maps the open-station entry correctly for a multi-station BOM location and the single-station entry for GHCNd.

## Stage 6: Delete `/recent-observations`

Once Stage 5 ships and has been running long enough to be confident in the freshness tradeoff from Stage 3:

- `ClimateExplorer.WebApi/RecentObservationsEndpoints.cs`
- `ClimateExplorer.WebApi/RecentObservations/` (`RecentObservationsBomDataSource.cs`, `RecentObservationsGhcndDataSource.cs`, `RecentObservationsDataSourceHelpers.cs`)
- The route mapping in [ClimateExplorerEndpointRouteBuilderExtensions.cs:24](../../ClimateExplorer.WebApi/ClimateExplorerEndpointRouteBuilderExtensions.cs#L24)
- `IDataService.GetRecentObservations` / `DataService.GetRecentObservations` ([IDataService.cs:36](../../ClimateExplorer.WebApiClient/Services/IDataService.cs#L36), [DataService.cs:278-280](../../ClimateExplorer.WebApiClient/Services/DataService.cs#L278))
- `ClimateExplorer.Core/Model/RecentObservationsResponse.cs` and `RecentObservationSeries.cs` (wire-response types with no other purpose)
- **Keep** `ClimateExplorer.Core/Model/RecentObservationSourceMetadata.cs` — Stage 5 keeps constructing it client-side as a plain view-shaping record; only its role as a wire-response type disappears.
- `ClimateExplorer.UnitTests/RecentObservationsEndpointTests.cs` and `RecentObservationsServiceTests.cs` — delete or port any assertion not already covered by the shared `BomDailyDataClient`/`bom-station`/`ghcnd-station` downloader tests (check for genuinely duplicated coverage before deleting; the automated-downloads plan's Stage 5 already consolidated BOM CSV download/parsing into the shared downloader, but the recent-observations sources may still have independent 2-year-window slicing logic worth preserving as a ported test rather than losing coverage).
- Leave `RecentObservationComparisonTests.cs`, `RecentObservationRecordDetectionTests.cs`, `RecentObservationRetrievalMetadataSelectorTests.cs`, and `RecentObservationTileExpansionStateTests.cs` untouched — they test client-side calculator/view-model logic, not the endpoint.
- Update [2026-07-13-01-automated-dataset-downloads-plan.md](2026-07-13-01-automated-dataset-downloads-plan.md), which repeatedly states "`/recent-observations` remains unchanged" as an explicit out-of-scope/acceptance-criteria item across every stage — add a short pointer to this doc so the two don't read as contradictory to a future reader.

## Risks

- **Content-aware freshness moves validator parsing onto the request path's failure surface.** `DataSetDownloadValidator.ValidateAsync` changing from `Task` to `Task<DateOnly?>` touches every downloader's call site; get the "minimum across bundled measurements" rule (Stage 3) right in tests before relying on it, since an inverted min/max would silently mask a lagging measurement as fresh.
- **Retry-storm shape**: content-aware freshness means a source that's genuinely behind (BOM/GHCNd publishing late) gets re-attempted up to 4 times/day (every 6h) for every affected location, rather than settling into the 24h cadence. This matches what `/recent-observations` already did in production, just relocated into the general pipeline — not a new risk, but worth watching in logs after Stage 3 ships given `bom-station`/`ghcnd-station` now carry more request volume than `/recent-observations` alone did.
- **Latency is not a new risk**: `RecentObservationsDataProvider.FetchTemperatureData` already calls `/climate-record` with refresh permitted today (`GetHistoricalRecords`/`PostDataSets(..., permitSourceUpdate: true)`), so Stage 5 removes a *second* concurrent fetch rather than introducing new synchronous download-on-request behavior.
- **Stage 1 changes live behavior for 82 BOM locations.** Verified every closed station referenced in the mapping already has a packaged archive, so no location loses readable history — but confirm this on the deployed environment's actual data directory, not just the checked-in `ClimateExplorer.SourceData`/`ClimateExplorer.WebApi/Datasets` copies, before shipping.
- **Sequencing**: Stage 1 is independent and should ship regardless of the rest of this plan. Stages 2-4 are additive and low-risk individually. Stage 5 is the only stage that changes user-visible behavior (freshness cadence); Stage 6 is cleanup that should trail Stage 5 by enough time to be confident nothing regressed.

## Acceptance criteria

- `/climate-record` responses include `SourceMetadata` and an unambiguous open-station entry for every station-backed dataset.
- A location's temperature "supported" status is derivable from `/climate-record` alone, with no dedicated probe endpoint.
- TempMax/TempMin/Precipitation for one location are provably sourced from the same provider (BOM or GHCNd) via a regression test, not an accident of declaration order.
- No `/climate-record` or `/dataset` request triggers a refresh attempt for a closed BOM station.
- `RecentObservationsDataProvider` makes one request per data type (no `/recent-observations` call), and `RecentObservationsPanel` renders unchanged.
- `/recent-observations`, its WebApi source classes, and its client plumbing are deleted with no loss of currently-tested behavior that isn't already covered elsewhere.
