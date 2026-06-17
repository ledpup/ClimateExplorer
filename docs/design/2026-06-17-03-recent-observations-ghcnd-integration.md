# Recent Observations: GHCNd station integration

- **Date:** 2026-06-17
- **Status:** Implemented 2026-06-17 (see addendum)
- **Author:** Patrick Lea (with Codex)
- **Scope:** `RecentObservationsEndpoints`, WebApi service wiring, GHCNd data library integration, focused recent-observations tests
- **Builds on:** [2026-06-17-01-recent-observations-metric-architecture.md](2026-06-17-01-recent-observations-metric-architecture.md)
- **Branch context:** `issues/recent-ghcnd`

## Goal

Extend `/recent-observations` so a location GUID can resolve to either a BOM
station or a GHCNd station, retrieve the latest daily observations for the
requested data type, cache them, clean up temporary download files, and return the
same `RecentObservationsResponse` shape the BOM path returns today.

## Current shape

The endpoint is currently BOM-first:

- `GetRecentObservations` calls `GetBomRecentObservationsContext`.
- The BOM context checks whether the `DataType` maps to a daily BOM obs code,
  loads locations, finds the BOM dataset definition and mapping, chooses the most
  recent operating station, and returns the matching measurement definition.
- The download/read path fetches a BOM zip, extracts the one CSV inside it, parses
  through `DataReaderFunctions.ProcessDataFile`, applies `ValueAdjustment`, caches
  the response, and deletes the temporary zip/directory.

Recent GHCNd prep already exists in WebApi:

- `ClimateExplorerApiConstants` contains GHCNd temperature and precipitation
  dataset definition IDs.
- `ClimateExplorerApiServices` has a `GhcndHttpClient` property.
- `ClimateExplorer.Data.Ghcnd` exposes CSV download, read, temperature processor,
  and precipitation processor helpers.

## Design

Keep the endpoint simple and close to the BOM pattern. Introduce a small internal
source context for recent observations:

```csharp
private enum RecentObservationStationSource
{
    Bom,
    Ghcnd,
}

private sealed record RecentObservationsContext(
    RecentObservationStationSource Source,
    string StationId,
    MeasurementDefinition MeasurementDefinition);
```

The lookup flow becomes:

1. Resolve the location once through `LocationEndpoints.GetCachedLocations`.
2. Try the BOM context for supported BOM data types and dataset mapping.
3. If BOM does not support the location/data type, try GHCNd:
   - temperature requests (`TempMax`, `TempMin`, and any existing daily mean
     temperature type if supported by the app model) use the GHCNd temperature
     dataset definition;
   - precipitation requests use the GHCNd precipitation dataset definition;
   - solar radiation and unrelated data types are unsupported.
4. Return unsupported only if no source context is found.

This is intentionally just conditional branching. Central England datasets can be
added later as a third branch with the same context shape, using HadCET for
temperature and HadCEP for precipitation, without committing to a broader strategy
framework now.

## GHCNd Retrieval

Follow the same logical stages as BOM:

1. Use the existing recent-observations cache key shape: location, data type, and
   recent observation year range. This preserves existing BOM cache entries and
   keeps GHCNd consistent with the endpoint contract.
2. Download the station CSV using `GhcndStationCsvDownloader.DownloadCsvAsync`.
3. Write it to a temporary `*.csv` file under a per-request temp directory.
4. Read it with `GhcndCsvReader`, then remove rows with no relevant data.
5. For temperature, use `GhcndTemperatureProcessor` to create and validate
   records. Do **not** apply the bulk pipeline's full-year sufficiency filter in
   the recent-observations endpoint, because the current year is intentionally
   partial. Convert `Tmax`/`Tmin` tenths of degrees Celsius into the same
   `DataRecord` value units as the existing GHCNd measurement definitions.
6. For precipitation, use `GhcndPrecipitationProcessor` to create and validate
   records. Do **not** apply the bulk pipeline's full-year sufficiency filter here.
   Convert tenths of millimetres into the same value units as the existing
   precipitation measurement definition.
7. Apply the requested date range and return records ordered by date.
8. Cache the `RecentObservationsResponse`.
9. Delete the temporary CSV and directory in `finally`.

GHCNd temperature and precipitation live in the same CSV. If sharing one download
for both logical data types falls out naturally from a helper, use it. Otherwise,
download per requested data type just like BOM does; the endpoint contract only
asks for one data type at a time.

## BOM Preservation

The BOM branch should remain behaviorally unchanged:

- use the existing obs-code mapping and BOM `p_c` lookup;
- keep the zip download/extract/read path;
- keep existing `DataReaderFunctions.ProcessDataFile` parsing and value
  adjustment;
- keep existing fallback behavior: return a fresh supported empty response or the
  cached response on download/read failure.

Only rename or factor BOM helpers if doing so clarifies the new source branch and
does not change behavior.

## Implementation Steps

1. Add the design doc before code changes.
2. Wire `GhcndHttpClientFactory.CreateHttpClient()` into `Program.cs` so
   `ClimateExplorerApiServices` is constructed consistently with its current
   constructor.
3. Replace the direct BOM context call in `GetRecentObservations` with a
   `GetRecentObservationsContext` lookup that tries BOM, then GHCNd.
4. Add GHCNd context resolution using the existing GHCNd dataset definition IDs
   and location-to-data-file mappings.
5. Add a GHCNd download/read helper that mirrors the BOM helper structure and
   returns `List<DataRecord>?`.
6. Build the response from the selected source context, preserving the existing
   response fields.
7. Update logging so failures name the selected source rather than always saying
   BOM.
8. Add focused tests if there is an existing WebApi endpoint test pattern.
9. Run the relevant build/test commands and record results in an implementation
   addendum.

## Testing Approach

- Unit or endpoint-level tests for source selection:
  - BOM location still routes to BOM.
  - GHCNd temperature location routes to GHCNd temperature.
  - GHCNd precipitation location routes to GHCNd precipitation.
  - unsupported data types remain unsupported.
- GHCNd conversion tests:
  - `TempMax` and `TempMin` return Celsius values from tenths-of-degree records.
  - precipitation returns millimetres from tenths-of-millimetre records.
  - quality-flagged or null GHCNd values are omitted.
- Cache behavior:
  - cached recent/yesterday/hour-old responses are reused for GHCNd just like BOM.
- Cleanup behavior:
  - temporary GHCNd CSV files/directories are deleted after success and failure,
    where the current test style can observe it without excessive plumbing.

## Out of Scope

- Central England implementation. The branch shape should make HadCET/HadCEP easy
  to add later, but no HadCET/HadCEP download or parsing work is included here.
- A generalized plugin-style data-source framework.
- Changing the public response contract.
- Changing the UI or recent-observation metric rendering.

## Addendum — implementation notes (2026-06-17)

Implemented in the WebApi endpoint:

- `GetRecentObservations` now resolves a source context and tries BOM first, then
  GHCNd. Unsupported locations/data types still return the same
  `RecentObservationsResponse { IsSupported = false }` shape.
- `Program.cs` now wires `GhcndHttpClientFactory.CreateHttpClient()` into
  `ClimateExplorerApiServices`.
- GHCNd temperature and precipitation use the existing GHCNd dataset definition
  IDs and metadata mappings to resolve station IDs.
- GHCNd retrieval downloads the station CSV, writes it to a per-request temporary
  CSV file, reads it with `GhcndCsvReader`, processes through the temperature or
  precipitation processor, applies the measurement definition's `ValueAdjustment`,
  caches the response, and deletes the temporary CSV/directory in `finally`.
- The BOM path remains on the existing zip/`DataReaderFunctions` path and keeps
  the existing cache-key shape.
- The GHCNd bulk pipeline's full-year sufficiency filter is intentionally not used
  here, because recent observations need the partial current year.
- `GhcndHttpClientFactory` now sets a 60-second timeout, uses a valid
  browser-style `User-Agent`, and connects to GHCNd over IPv4. Local diagnostics
  showed `curl` reaching NOAA quickly over IPv4 while .NET `HttpClient` and
  PowerShell's .NET-backed `Invoke-WebRequest` timed out against the same URL,
  with DNS returning both IPv6 and IPv4 addresses. The downloader requests
  response headers first and calls `EnsureSuccessStatusCode` so slow or rejected
  downloads fail visibly.
- Added focused endpoint tests for GHCNd temperature and precipitation conversion,
  branch routing through metadata mappings, and quality-flag omission.

Verification:

- `dotnet build ClimateExplorer.WebApi\ClimateExplorer.WebApi.csproj` passes.
- `dotnet test ClimateExplorer.UnitTests\ClimateExplorer.UnitTests.csproj --filter RecentObservationsEndpointTests` passes: 2/2.
- Full `dotnet test ClimateExplorer.UnitTests\ClimateExplorer.UnitTests.csproj`
  currently fails in two existing client-side expanded-tile tests:
  `ExpandedTilesExposePeriodAndDailyExtremesMetricGroups` and
  `ExpandedTilesOmitRecordsWhenNoHistoryIsAvailable`. The failures are label/order
  expectations in `RecentObservationsServiceTests` and are outside the WebApi
  files changed for this integration.
