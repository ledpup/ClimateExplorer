# Remove `DataType` from the recent-observations endpoint

- **Date:** 2026-06-19
- **Status:** Implemented 2026-06-19 (see addendum)
- **Author:** Patrick Lea (with Claude)
- **Scope:** `/recent-observations` Web API endpoint, `RecentObservationsResponse`,
  the BOM/GHCNd recent-observation data sources, `IDataService`/`DataService`,
  `RecentObservationsDataProvider`, `LocationDashboard` support detection, and the
  associated unit tests.
- **Builds on:** [GHCNd integration](2026-06-17-03-recent-observations-ghcnd-integration.md)
- **Branch context:** `development`

## Problem

The `/recent-observations` endpoint takes a `DataType` query parameter
(`TempMax`, `TempMin`, `Precipitation`) and returns the records for that one
data type. This leaks source-specific shape into the public API and causes
redundant work:

- **Duplicate downloads.** GHCNd serves max temp, min temp and precipitation
  from a single station CSV (`GhcndInputRow` carries `Prcp`, `Tmax`, `Tmin` on
  every row). But the temperature tab calls the endpoint **twice** (once per
  `TempMax`/`TempMin`), and precipitation a third time. Each call re-downloads
  and re-parses the same CSV. The server cache key includes `dataType`
  (`RecentObservationsEndpoints.cs:33`), so the three calls never share work.
- **Leaky abstraction.** `DataType` only matters because BOM uses a separate
  obs-code file per data type. That is a BOM implementation detail; the generic
  endpoint should not carry it.
- **Vestigial response metadata.** `RecentObservationsResponse.DataType`,
  `UnitOfMeasure`, `DataAdjustment` and `DataResolution` are populated by the
  server but the Blazor client never reads them — only `Records`, `IsSupported`
  and `SourceMetadata` are consumed (`RecentObservationsDataProvider.cs`). They
  are asserted only in `RecentObservationsEndpointTests`.

## Goal

Replace the per-`DataType` request with a single request per location that
returns all three required series together, with provenance metadata, and let
each source decide internally how to fetch them. Remove `DataType` from the
public endpoint and the response.

## Proposed API shape

Request: `GET /recent-observations?locationId={guid}[&isLocationSupported=true]`
— no `dataType`.

Response — one series per required observation, each self-describing and
carrying its own provenance (BOM provenance differs per series; GHCNd shares
one source URL across all three):

```csharp
public sealed record RecentObservationsResponse
{
    public bool IsSupported { get; set; }
    public DateTimeOffset? RetrievedDate { get; set; }
    public RecentObservationSeries? TempMax { get; set; }
    public RecentObservationSeries? TempMin { get; set; }
    public RecentObservationSeries? Precipitation { get; set; }
}

public sealed record RecentObservationSeries
{
    public List<DataRecord> Records { get; set; } = [];
    public DataAdjustment? DataAdjustment { get; set; }
    public DataResolution? DataResolution { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public RecentObservationSourceMetadata? SourceMetadata { get; set; }
}
```

A series is `null` when that observation is unavailable for the location.
`IsSupported` becomes the temperature-support signal (see below); precipitation
support is implied by `Precipitation != null`.

### Support semantics

> A location is supported for Recent Observations **temperature** if **both**
> `TempMax` and `TempMin` are available.

So `IsSupported = (TempMax is not null && TempMin is not null)`. This fixes the
current probe, which checks only `TempMax`
(`LocationDashboard.razor.cs:192`). The `isLocationSupported=true` fast path
resolves contexts for both temperature series without downloading records, as it
does today for one.

## Source-specific behaviour

**GHCNd** — resolve the station once, download/parse the station CSV **once**,
and extract `TempMax`, `TempMin` and `Precipitation` from the same parsed rows.
No more one-request-per-type. Note GHCNd splits temperature and precipitation
across two dataset definitions (`GhcndTemperatureDataSetDefinitionId`,
`GhcndPrecipitationDataSetDefinitionId`) with different measurement defs/units
and potentially different station mappings; resolve both, but if both resolve to
the same station id, download the CSV only once and reuse the parsed rows.

**BOM** — keep obs-code handling entirely inside the BOM source. Internally
fetch the obs-code files needed for the required series (TempMax=122,
TempMin=123, Rainfall=136) and assemble the three series. Obs codes never appear
in the endpoint or response.

## Files / classes likely to change

| File | Change |
|---|---|
| `ClimateExplorer.Core/Model/RecentObservationsResponse.cs` | Reshape to series-per-observation; add `RecentObservationSeries`; drop top-level `DataType`/`UnitOfMeasure`/`DataAdjustment`/`DataResolution`/`Records`. |
| `ClimateExplorer.WebApi/RecentObservationsEndpoints.cs` | Drop `dataType` param; resolve source once; fetch all series; cache key keyed by location+years only; build combined response. |
| `ClimateExplorer.WebApi/RecentObservations/RecentObservationsGhcndDataSource.cs` | Download/parse CSV once; return all three series from one parse. |
| `ClimateExplorer.WebApi/RecentObservations/RecentObservationsBomDataSource.cs` | Fetch required obs-code files internally; return all available series. |
| `RecentObservationsContext` / `RecentObservationsDownloadResult` | Carry per-series measurement defs + records + provenance instead of a single `DataType` slice. |
| `ClimateExplorer.WebApiClient/Services/IDataService.cs` & `DataService.cs` | `GetRecentObservations(Guid locationId, bool isLocationSupported = false)` — drop `dataType`, drop the `dataType` query string. |
| `ClimateExplorer.Web.Client/Services/RecentObservationsDataProvider.cs` | Fetch combined response once per location (cache by location, not by tab); temperature tab reads `TempMax`/`TempMin`, precip tab reads `Precipitation` from the same response. |
| `ClimateExplorer.Web.Client/Components/Location/LocationDashboard.razor.cs` | Support probe drops `DataType.TempMax`; `IsSupported` now already means "both temp series present". |
| `ClimateExplorer.WebApi/MetadataEndpoints.cs` | Update endpoint doc text. |

## Downstream impact

- **API contract:** breaking change to the `/recent-observations` shape and query
  string. Both server and the only client ship together, so no external
  versioning concern — but confirm no other consumer hits the raw endpoint.
- **Caching/dedup:** server cache becomes one entry per location (per year
  range) instead of three; GHCNd downloads the CSV once instead of three times.
  Client cache key changes from `(location, tab)` to `(location)`.
- **Lazy precip loading:** with a combined response the precipitation tab no
  longer makes its own network call — it reads the already-fetched response.
  See trade-off below.
- **Loading/error states:** a single fetch now backs both tabs. A partial
  failure (e.g. BOM rainfall file missing) must surface as "precip unavailable"
  while temperature still renders, so error handling moves from
  whole-response to per-series.
- **Provenance notes:** unchanged data, but now attached per series. Provenance
  MUST stay per-series in the API/source layer — do **not** dedup server-side.
  Deduplication happens only at display time, in
  `RecentObservationRetrievalMetadataSelector.Select`, which groups by
  `SourceUrl`. This yields the required notes-section behaviour:
  - BOM temperature tab → **two** URLs (TempMax obs 122 and TempMin obs 123 have
    distinct zip URLs, so grouping keeps both).
  - BOM precipitation tab → **one** URL (rainfall obs 136).
  - GHCNd temperature tab → **one** URL (TempMax + TempMin share the station CSV
    URL, so grouping collapses to one).
  - GHCNd precipitation tab → the **same one** URL (same station CSV).

  The client must therefore feed each tab the relevant series' metadata —
  temperature tab gets `TempMax.SourceMetadata` + `TempMin.SourceMetadata`;
  precipitation tab gets `Precipitation.SourceMetadata` — and let the existing
  selector group them.

## Risks

1. **BOM over-fetch — accepted.** Returning all three series eagerly means
   viewing only the temperature tab also downloads the BOM rainfall file. This is
   explicitly acceptable (decision 2026-06-19): downloading BOM precipitation
   alongside temperature is fine. The combined response is cached server-side
   (~6h) and client-side, so it is fetched at most once. The **group**
   alternative below is therefore not needed.
2. **"One request = one data type" assumption** is baked into the client cache
   keying, the data provider's two temperature calls, and every endpoint test.
   All must move together.
3. **GHCNd dual dataset definitions** could resolve temperature and precipitation
   to different station ids; handle the "same station → one download" and
   "different station → separate downloads" cases explicitly.
4. **Partial support** (temperature available but not precipitation, or vice
   versa) must be representable — hence nullable per-series rather than a single
   `IsSupported` flag governing everything.
5. **Missing-data / no-records** handling: distinguish "series unavailable for
   location" (`null`) from "available source but no recent records yet" (empty
   `Records`) so tiles don't mislabel support.

### Alternative considered (coarser grouping)

Replace `dataType` with a `group` parameter (`temperature` | `precipitation`)
rather than removing it entirely. Temperature group returns max+min from one
GHCNd parse (fixing the main duplicate-download pain) while precipitation stays
lazily loaded, avoiding BOM over-fetch. This preserves lazy precip loading but
keeps a (smaller) source detail in the API and doesn't fully meet the "all
series together" goal. Recommended only if BOM over-fetch proves significant.

## Suggested implementation order

1. Reshape `RecentObservationsResponse` + add `RecentObservationSeries` (Core).
2. Rework GHCNd source to download/parse once and emit all three series.
3. Rework BOM source to fetch required obs files internally and emit available
   series.
4. Rewrite the endpoint: resolve source once, assemble combined response,
   re-key cache, set `IsSupported` from both temperature series; update the
   `isLocationSupported` fast path.
5. Update `IDataService`/`DataService` signature and query string.
6. Update `RecentObservationsDataProvider` to one fetch per location feeding both
   tabs; update `LocationDashboard` support probe.
7. Update `MetadataEndpoints` doc text.
8. Update/extend tests.

## Tests to update / add

- **`RecentObservationsEndpointTests`** — rewrite the three existing tests to call
  the no-`dataType` endpoint and assert on `response.TempMax/TempMin/Precipitation`
  series. Add: GHCNd downloads the station CSV **once** for a full response
  (assert request count == 1); BOM fetches the expected obs-code files;
  per-series provenance is correct; partial availability yields a null series;
  `IsSupported` is false when only one temperature series resolves.
- **`RecentObservationsServiceTests`** — update the `GetRecentObservations(..., DataType, false)`
  mock setups/verifications (~lines 759, 793, 824, 874, 1709–1919) to the new
  single-call-per-location signature; add a test that the temperature and
  precipitation tabs share one fetch.
- Add a regression test asserting no duplicate GHCNd download across the temp tab.

## Decisions (2026-06-19)

- **Shape:** combined **all-three** response (the **group** alternative is not
  needed).
- **BOM over-fetch:** accepted — downloading BOM precipitation alongside
  temperature is fine.
- **Provenance:** stays per-series in the API; the notes section must still show
  two URLs for the BOM temperature tab and one for every other case, via the
  existing display-time grouping by `SourceUrl`.

This is a breaking API change, not a small/safe edit, so implementation is a
follow-up once this plan is accepted.

## Addendum — implementation notes (2026-06-19)

Shipped as planned. Highlights and deviations:

- **Core model:** `RecentObservationsResponse` reshaped to `IsSupported` +
  `RetrievedDate` + nullable `TempMax`/`TempMin`/`Precipitation`, each a new
  `RecentObservationSeries` (records + measurement metadata + per-series
  `SourceMetadata`). The old top-level `Records`/`DataType`/`UnitOfMeasure`/
  `DataAdjustment`/`DataResolution` are gone.
- **Internal WebApi types:** `RecentObservationsContext` now holds three nullable
  `RecentObservationSeriesContext` (station + measurement definition) per source;
  `RecentObservationsDownloadResult` holds three nullable
  `RecentObservationSeriesDownload`. `RecentObservationsDataSourceHelpers.GetContext`
  was split into `GetStationId` + `CreateSeriesContext`. (The WebApi project has
  nullable reference types off and StyleCop SA1402, so each record lives in its
  own file with non-annotated reference types.)
- **GHCNd:** downloads/parses each station CSV once via an in-method
  `parsedRowsByStation` cache and extracts all available series from it. When
  temperature and precipitation resolve to the same station (the normal case),
  that's a single download — verified by a test asserting request count == 1.
- **BOM:** `DownloadSeries` fetches each obs-code file internally; obs codes never
  leave the BOM source. TempMax/TempMin keep distinct zip URLs.
- **Endpoint:** resolves the source once (BOM then GHCNd), cache key is now
  `RecentObservations_{locationId}_{startYear}_{endYear}` (no data type),
  `IsSupported = context.IsTemperatureSupported`. Cache-freshness/record-date
  checks now scan across all series.
- **Client:** `IDataService.GetRecentObservations(locationId, isLocationSupported)`
  drops the `dataType` parameter and query string. `RecentObservationsDataProvider`
  fetches the combined response **once per location** (new `recentResponseCache`)
  and feeds both tabs — temperature reads `TempMax`/`TempMin`, precipitation reads
  `Precipitation`. Provenance is left per-series; the existing
  `RecentObservationRetrievalMetadataSelector` still groups by `SourceUrl` at
  display time, giving two notes for BOM temperature and one elsewhere.
- **Support probe:** `LocationDashboard` drops `DataType.TempMax`; `IsSupported`
  now already means "both temperature series present".
- **Tests:** endpoint tests rewritten to assert per-series shape, GHCNd
  single-download (request count == 1), and distinct-vs-shared source URLs;
  service-test mocks updated to the single-call combined response. Full suite:
  216 passed, 0 failed; solution builds with 0 warnings.

No follow-ups outstanding.
