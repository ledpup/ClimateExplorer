# `/climate-record` vs `/recent-observations`: investigation

## Summary

The two download paths overlap in *what* they fetch (same GHCNd CSV endpoint, same BOM zip endpoints) but differ in *scope* and *purpose*. The general `/climate-record` pipeline is now capable of triggering the same downloads (via `DataSetSourceUpdateCoordinator` + `GhcndDataSetDownloader`/`BomDataSetDownloader`), so the download step itself is genuinely duplicated. But `/recent-observations` isn't just "download + call climate-record 3 times" — it does several things the general pipeline doesn't.

## Differences: GHCNd

| | `RecentObservationsGhcndDataSource.cs` | `GhcndDataSetDownloader.cs` |
|---|---|---|
| Scope | Reads a **~2 year window** (`today.Year-1` to `today.Year` Dec 31) of raw daily rows, no binning | Downloads the **full station history** and writes it into the persistent archive used by the general pipeline |
| Output | In-memory `DataRecord` list for immediate response | A file artifact on disk (source-of-truth cache) |
| Sharing across series | Explicit `parsedRowsByStation` dictionary so TempMax/TempMin/Precip parse the CSV **once per request** ([RecentObservationsGhcndDataSource.cs:45-65](../../ClimateExplorer.WebApi/RecentObservations/RecentObservationsGhcndDataSource.cs#L45-L65)) | N/A — one archive build call already receives all requested measurements at once ([GhcndDataSetDownloader.cs:19](../../ClimateExplorer.Data.Downloading/Downloaders/GhcndDataSetDownloader.cs#L19)) because `DataSetSourceAssetResolver` expands a single-DataType request to all measurements sharing the same asset key |
| Station selection | "As-at" lookup — picks whichever station was **operating on a specific date** ([RecentObservationsDataSourceHelpers.cs:135-147](../../ClimateExplorer.WebApi/RecentObservations/RecentObservationsDataSourceHelpers.cs#L135-L147)) | Just uses whatever `FileFilter.Id` the resolver hands it (based on `DataLocationMapping`, no "as-at" concept beyond static date ranges in the mapping) |
| Freshness | Content-aware: retries until the response actually **contains today's/yesterday's row**, else re-tries every ≥6h ([RecentObservationsEndpoints.cs:33-34](../../ClimateExplorer.WebApi/RecentObservationsEndpoints.cs#L33-L34)) | Time-based only: `DataSetFreshnessPolicy` treats data as fresh for 24h since last **retrieval**, regardless of whether it contains yesterday's row |

## Differences: BOM

| | `RecentObservationsBomDataSource.cs` | `BomDataSetDownloader.cs` |
|---|---|---|
| What's downloaded | Only the obs codes actually requested (max/min/precip) | Always downloads **all four** codes (max, min, precip, solar) plus derives `TempMean`, every time ([BomDataSetDownloader.cs:31-36](../../ClimateExplorer.Data.Downloading/Downloaders/BomDataSetDownloader.cs#L31-L36)) |
| Output | In-memory records for a 2-year window | A zip archive with one entry per measurement, written to the persistent store |
| Error handling | Per-series: a failed obs-code download just nulls that one series, others still return | All-or-nothing: `CreateMeanTemperature` throws if max/min don't overlap, failing the whole asset |
| Temp files | Explicit temp dir per call, cleaned up in a `finally` | Writes directly into the managed workspace/candidate path |

## Reasons to retain `/recent-observations` (or gaps to close first)

1. **Freshness semantics are different and arguably more correct for "recent."** The general policy is *time-since-fetch*, not *does-the-data-contain-yesterday*. If an on-demand refresh happens to run at 1am before BOM/GHCNd publish yesterday's final reading, the general pipeline will consider that data "fresh" for a full 24h and never notice it's missing the row the UI actually wants to show. `/recent-observations`'s `HasRecordForDate`/`WasDataRetrievedInLastHours(6)` check exists specifically to solve that. This would need to be ported into `DataSetFreshnessPolicy` (or a daily-resolution-specific variant) before retiring the bespoke path.

2. **`RecentObservationSourceMetadata` isn't currently reachable from `/climate-record` at all.** `ClimateRecordsResponse` has no `SourceMetadata`/station/URL fields today — only `DataSet` (internal) carries `List<DataSetMetadata>`. You'd need to add that to `ClimateRecordsResponse`, and reconcile the shapes: `DataSetMetadata` is dataset/location-level with a `Stations` list (a location can map to *several* stations across time ranges) and no `RetrievedAtUtc`, whereas the client's `RecentObservationSourceMetadata` is one flat record per series (single `StationId`, single `SourceUrl`, `RetrievedAtUtc`) representing the *one* station actually used for that fetch "as at now." These aren't a 1:1 mapping — you'd be picking one station out of `Stations` and bolting on a retrieval timestamp.

3. **Cross-type single-source consistency.** `/recent-observations` explicitly does `BOM ?? GHCNd` once per location, so TempMax/TempMin/Precipitation are guaranteed to come from the *same* station/source in one response ([RecentObservationsEndpoints.cs:86-87](../../ClimateExplorer.WebApi/RecentObservationsEndpoints.cs#L86-L87)). `/climate-record`'s dsd selection ([ClimateRecordsEndpoints.cs:33-53](../../ClimateExplorer.WebApi/ClimateRecordsEndpoints.cs#L33-L53)) picks per-`DataType` independently by resolution match, with no explicit BOM-over-GHCNd preference — it just takes the first matching dsd in definition order. Calling it 3 times isn't guaranteed to land on the same source for all three unless that ordering is verified/aligned.

4. **"Is this location supported" is a distinct cheap capability check.** `isLocationSupported=true` on `/recent-observations` answers "does this location have *both* max and min available" without doing any download work — a concept `/climate-record` doesn't expose (it just returns empty records per data type, so the client would need to reconstruct the "supported" boolean itself from three separate empty/non-empty results).

5. **GHCNd/BOM download work is no longer actually duplicated at the network layer** — this part of the original rationale is now stale. `DataSetSourceAssetResolver.ResolveAsync` already expands a single-DataType request to cover every measurement sharing the same physical asset, and `DataSetSourceUpdateCoordinator`'s per-asset lock + state store means three concurrent `/climate-record` calls (max/min/precip) for GHCNd or BOM will only trigger one real download, not three. So "why not call it 3 times" is largely fine on the network/download side already — the remaining friction is in points 1–4 (freshness semantics, metadata shape, source consistency, and the cheap-support-check), not in the downloader classes themselves.

## Bottom line

The downloader duplication (`GhcndDataSetDownloader`/`BomDataSetDownloader` vs the `RecentObservations*DataSource` download code) is real but already mostly superseded by the general pipeline — that part can likely be deleted. What's actually load-bearing in `/recent-observations` is the endpoint-level logic: content-aware freshness, single-source-across-series consistency, the flat per-series metadata shape, and the cheap support probe. Retiring the endpoint means re-homing those four behaviors into `/climate-record` (or a thin wrapper), not just switching the client to three calls.

## Revisit (2026-07-14): station cardinality closes gap #2

Checked the claim that GHCNd is always single-station and BOM always has exactly one *currently open* station per location, against the actual mapping data:

- `DataFileMapping_ghcnd_temperature.json`: 1734/1734 locations map to exactly one station. No multi-station GHCNd locations exist.
- `DataFileMapping_Australia_unadjusted.json`: 82/113 locations have multiple time-boxed station entries (closed stations stitched together), but **every** location's last entry has `EndDate: null` — there is always exactly one open station per location, never zero, never more than one. `DataFileMapping_Australia_adjusted.json` (ACORN-SAT) has zero multi-station locations.
- `RecentObservationsDataSourceHelpers.GetMostRecentOperatingStationId` — the "as-at" picker that looked like `/recent-observations`-only capability — is only ever invoked with `asAt = today` ([RecentObservationsEndpoints.cs:22](../../ClimateExplorer.WebApi/RecentObservationsEndpoints.cs#L22)), so it's already degenerate in practice: it always resolves to whichever mapping entry has `EndDate == null`.

This means gap #2 (`RecentObservationSourceMetadata` vs `DataSetMetadata.Stations` shape mismatch) isn't a real reconciliation problem. There is never more than one candidate "active" station to pick from — `SourceMetadata` on `ClimateRecordsResponse` can be defined as the `Stations` entry with `StationEndDate == null`, paired with the `RetrievedDate` the endpoint already computes ([ClimateRecordsEndpoints.cs:169](../../ClimateExplorer.WebApi/ClimateRecordsEndpoints.cs#L169)). Mechanical, not a design problem.

Gaps #1 (content-aware freshness), #3 (cross-type source consistency), and #4 (cheap support probe) are untouched by this — they don't depend on station cardinality and still need to be closed before `/recent-observations` can be retired.

Separately: `DataSetSourceAssetResolver` still treats each *closed* BOM station as its own perpetually-refreshed asset — nothing marks a closed station (`EndDate` in the past) as "fetch once, then freeze," so the background pipeline will keep re-downloading dead stations' archives forever. `BomDataSetDownloader` already hard-fails if a request ever resolves to more than one station id, so this is a resolver-level fix (skip/freeze closed-station assets after their first successful fetch), not a downloader-level one.
