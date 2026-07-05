# AboutData Station Metadata Plan

- **Date:** 2026-07-05
- **Status:** Proposed
- **Author:** Codex
- **Scope:** `ClimateExplorer.WebApi` `PostDataSets`, shared dataset metadata models, chart data preparation, and `ClimateExplorer.Web.Client/Components/ChartSeries/AboutData`
- **Builds on:** N/A
- **Branch context:** `development`

## Summary

`AboutData.razor` currently displays dataset definition metadata from `ChartSeriesDefinition.SourceSeriesSpecifications`: dataset name, publisher, optional more-information link, and description. It does not receive the `DataSet` returned by `POST /dataset`, so it cannot display station/source metadata that is only known after the API resolves a chart request.

The likely current flow is:

1. `ChartDataBuilder` turns each `ChartSeriesDefinition` into a `PostDataSetsRequestBody`.
2. `DataSetEndpoints.PostDataSets` calls `DataSetBuilder.BuildDataSet`.
3. `DataSetBuilder` calls `SeriesProvider.GetSeriesDataRecordsForRequest`.
4. `SeriesProvider` resolves `DataSetDefinition.DataLocationMapping.LocationIdToDataFileMappings` for each requested source series, then reads one or more files identified by `DataFileFilterAndAdjustment.Id`.
5. `PostDataSets` returns a shared `DataSet` containing geography, measurement metadata, binned records, and optional raw records.
6. The Blazor client wraps that `DataSet` in `SeriesWithData`, then creates `PreProcessedDataSet` and `ProcessedDataSet` variants for charting.
7. `ChartView` renders `ChartSeriesListView`, which renders `ChartSeriesView`, which renders `AboutData`, but only the editable `ChartSeriesDefinition` reaches `AboutData`.

The existing metadata pieces are close to what is needed:

- `DataSetDefinition` already has source-level fields such as `ShortName`, `Name`, `Publisher`, `PublisherUrl`, `MoreInformationUrl`, `StationInfoUrl`, `LocationInfoUrl`, and `DataDownloadUrl`.
- `DataFileMapping.LocationIdToDataFileMappings` already models one location backed by one or more station/file identifiers.
- `DataFileFilterAndAdjustment` already carries station/file `Id`, `StartDate`, and `EndDate`.
- `Station` already carries `Id`, `Name`, `CountryCode`, coordinates, `FirstYear`, and `LastYear`.
- `RecentObservationSourceMetadata` has similar source fields, but it is single-station and includes `RetrievedAtUtc`, which is not appropriate for chart datasets.

## Stage 1: API Changes

### Proposed Shared Models

Add new chart dataset metadata models in `ClimateExplorer.Core.Model` rather than extending `RecentObservationSourceMetadata`.

Recommended shape:

```csharp
public sealed record DataSetSourceMetadata
{
    public Guid? DataSetDefinitionId { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? SourceCode { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceUrlLabel { get; set; }
    public List<DataSetStationMetadata> Stations { get; set; } = [];
}

public sealed record DataSetStationMetadata
{
    public string? StationId { get; set; }
    public string? StationName { get; set; }
    public DateOnly? StationStartDate { get; set; }
    public DateOnly? StationEndDate { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceUrlLabel { get; set; }
}
```

Then extend `DataSet` with:

```csharp
public List<DataSetSourceMetadata>? SourceMetadata { get; set; }
```

Why `DataSet` is the right owner:

- The metadata is resolved from the same request as the chart data.
- `ChartSeriesDefinition` is UI configuration and can outlive or differ from the resolved API result.
- `DataSetDefinitionViewModel` is source catalog metadata and does not know which stations backed a specific location request.
- A derived chart dataset can use more than one source specification, so the API response should preserve metadata for every input source in request order.

Keep `RetrievedAtUtc` out of these models. Chart datasets are served from bundled/cached dataset files, not the recent-observations download endpoint.

### Source and Station Resolution

Add a small metadata builder used by `DataSetEndpoints.PostDataSets`, for example `DataSetSourceMetadataBuilder`.

Responsibilities:

1. Iterate every `SeriesSpecification` in `PostDataSetsRequestBody.SeriesSpecifications`, not only index `0`.
2. For each specification:
   - Resolve `DataSetDefinition` by `DataSetDefinitionId`.
   - Resolve `GeographicalEntity` by `LocationId`.
   - Read `DataSetDefinition.DataLocationMapping.LocationIdToDataFileMappings[LocationId]`.
   - Create one `DataSetStationMetadata` row per `DataFileFilterAndAdjustment`.
3. Populate source fields:
   - `SourceCode`: prefer `DataSetDefinition.ShortName`.
   - `SourceName`: prefer `DataSetDefinition.Name`.
   - `SourceUrl`: prefer `MoreInformationUrl`, then `PublisherUrl`, then a non-station-specific `DataDownloadUrl`.
   - `SourceUrlLabel`: prefer `DataSetDefinition.ShortName` or `DataSetDefinition.Name`.
4. Populate station fields:
   - `StationId`: `DataFileFilterAndAdjustment.Id`.
   - `StationName`: station lookup `Name` when available.
   - `StationStartDate` and `StationEndDate`: mapping `StartDate` and `EndDate`.
   - If mapping dates are missing and station metadata has `FirstYear` or `LastYear`, optionally derive `YYYY-01-01` / `YYYY-12-31` as a fallback only if the UI copy makes that distinction clear.
5. Populate station links:
   - If `StationInfoUrl` is present, replace `[station]` with the station ID.
   - If `LocationInfoUrl` is present and contains `[primaryStation]`, use the first station ID for a source-level link.
   - If `DataDownloadUrl` contains `[station]`, expand it per station.
   - Use a label such as `Station {stationId}` when no better label is available.

Do not silently discard stations when the station metadata file has no matching row. Return the station ID and mapping dates, with `StationName = null`.

### Station Metadata Lookup

There is no general station metadata service today. Add one deliberately rather than embedding file reads inside `PostDataSets`.

Recommended approach:

- Add an optional station metadata hint to `DataSetDefinition`, such as `StationMetadataFileName`.
- Set it for known station-backed datasets where metadata files exist under `ClimateExplorer.WebApi/MetaData/Station`.
- Let the metadata builder load and cache `Station` rows by dataset definition.
- If a definition has no station metadata file, return station IDs from the data mapping without names.

This avoids guessing across all station files and avoids accidental ID collisions between unrelated sources.

Known complication: some metadata files are sparse. For example, BOM station rows may have station IDs but no station names or first/last years. The API should still return the mapping dates because those are often the most accurate dates for composite chart data.

### Composite and Derived Datasets

Composite locations are already represented by multiple `DataFileFilterAndAdjustment` rows for one location. The API should return all of those rows in `DataSetSourceMetadata.Stations`.

For series derivation:

- `ReturnSingleSeries`: one source metadata group.
- `AverageOfAnomaliesInRegion`: one source metadata group if the request contains one source specification, matching current validation.
- `DifferenceBetweenTwoSeries`: two source metadata groups in request order.
- `AverageOfMultipleSeries`: one source metadata group per input specification.

`PostDataSets` currently builds returned geography and measurement metadata from `body.SeriesSpecifications[0]`. Keep that behavior unless a broader derived-series API refactor is planned, but do not limit source metadata to the first specification.

### Cache Behavior

`PostDataSets` caches `DataSet` responses by serialized request body. Existing cache entries will not have the new metadata property. The implementation should either:

- version the cache key, for example `DataSet_v2_`, or
- hydrate missing `SourceMetadata` on cache hit before returning and then write the updated value back.

The versioned key is simpler and avoids stale responses.

### API Tests

Add focused MSTest tests using the existing naming style `MethodName_StateUnderTest_ExpectedBehavior`.

Recommended tests:

- `BuildSourceMetadata_SingleMappedStation_ReturnsSourceAndStationMetadata`
- `BuildSourceMetadata_MultipleMappedStations_ReturnsAllStationsWithMappingDates`
- `BuildSourceMetadata_MissingStationDetails_ReturnsStationIdsWithoutNames`
- `BuildSourceMetadata_DerivedSeries_ReturnsMetadataForEachSourceSpecification`
- `PostDataSets_CachedResponseWithoutMetadata_DoesNotReturnStaleMetadata` if choosing hydration instead of a versioned cache key

Prefer unit tests around the metadata builder with in-memory definitions, mappings, and station rows. Add one `PostDataSets` integration-style test only if the helper wiring is otherwise untested.

## Stage 2: Blazor App Changes

### Data Flow

Update the chart component parameter chain so `AboutData` can see the metadata returned by the API:

1. `ChartView` already receives `ChartDataBuildResult Data`.
2. Preserve a list containing both `Data.SeriesWithData` and `Data.NonRenderedSeriesWithData`.
3. Pass that list into `ChartSeriesListView`.
4. `ChartSeriesListView` matches each `ChartSeriesDefinition` to its `SeriesWithData` by `ChartSeries.Id`.
5. Pass the matching source metadata into `ChartSeriesView`.
6. Pass it into `AboutData`.

Recommended component API:

```csharp
// ChartSeriesView
[Parameter]
public IReadOnlyList<DataSetSourceMetadata>? SourceMetadata { get; set; }

// AboutData
[Parameter]
public IReadOnlyList<DataSetSourceMetadata>? SourceMetadata { get; set; }
```

Passing only the metadata keeps `AboutData` focused. Passing the full `SeriesWithData` would also work, but it would couple the modal to chart rendering state it does not need.

### Preserve Metadata During Client Processing

`ChartDataBuilder.BuildProcessedDataSets` creates fresh `DataSet` instances for annual change, moving-average preprocessing, and gap-filled processed datasets. Copy `SourceMetadata` whenever creating a replacement `DataSet`.

This matters immediately for annual-change series, because that path replaces `SourceDataSet` before `AboutData` would read it.

### AboutData Display Logic

Keep the existing dataset definition content and add source/station content below it.

For each `SourceSeriesSpecification` currently rendered by `AboutData`:

1. Find matching `DataSetSourceMetadata` by `DataSetDefinitionId` and `LocationId`.
2. Render source code/name when present.
3. Render a source link when `SourceUrl` is present.
4. Render station details according to station count.

Single station:

- Prefer a compact summary using the existing `.entry` layout rather than a one-row table.
- Suggested fields:
  - `Source`: `SourceCode` and `SourceName`
  - `Station`: station name plus station ID, or station ID alone
  - `Station dates`: formatted start/end range, with missing values handled
  - `Source link`: station link if present, otherwise source link if present

Multiple stations:

- Use existing table styling, not a new visual treatment.
- Use the shared `.table-card`, `.table-header`, and `.table` classes, with no pagination.
- Suggested columns:
  - Station ID
  - Station name
  - Start date
  - End date
  - Source
- If every station link is missing but a source-level link exists, show the source link once above or below the table rather than repeating an empty column.

Missing values:

- Missing station name: show an em dash entity (`&#x2014;`) or omit the name beside the ID.
- Missing start date: show `Unknown` or `&#x2014;`.
- Missing end date: show `Present` only when the mapping explicitly has no end date and the source is an ongoing station series; otherwise show `&#x2014;`.
- Missing source URL: omit the link or show `&#x2014;` in a table cell.
- Missing source URL label: fall back to `Source`, `Station {stationId}`, or the URL host if a helper already exists.

Avoid rendering a station section when there is no station metadata and no mapping-backed station ID.

### Accessibility

While touching this area, ensure the "About" control has an accessible name. The existing clickable `div.series-control` should be converted to an accessible button-like component or given keyboard handling plus `role="button"`, `tabindex="0"`, and an `aria-label`.

The modal content should keep normal semantic headings and table headers. Source links should keep `target="_blank"` and add `rel="noopener noreferrer"`.

### Blazor Tests and Checks

There does not appear to be a bUnit setup in `ClimateExplorer.UnitTests`. Avoid introducing bUnit just for this unless component testing is becoming a broader project standard.

Recommended checks:

- Unit-test formatting/mapping helpers in `AboutData.razor.cs` if display logic is extracted into methods.
- Add `ChartDataBuilder` tests proving `SourceMetadata` is preserved through:
  - annual change
  - moving average
  - processed dataset creation
- Build the solution or relevant projects to catch Razor compile errors.
- Do not run the website, Playwright, Lighthouse, or browser tests.

## Risks and Open Questions

- `DataFileFilterAndAdjustment.Id` is usually a station ID, but some datasets use it as a file/source token. The UI should not label non-station global index tokens as stations unless the dataset definition is marked as station-backed.
- There is no current `StationMetadataFileName` on `DataSetDefinition`. Adding one is explicit and safe, but it requires updating definitions for known sources.
- Some station files lack station names and first/last years. The UI must still look intentional with IDs and mapping dates only.
- `SourceName` could mean dataset name or publisher name. Recommendation: use `DataSetDefinition.Name` as source name because `AboutData` already displays publisher separately.
- Station-specific source links differ by source. URL template expansion should be centralized and unit-tested.
- Old cached `DataSet` responses can miss metadata unless the cache key is versioned or metadata is hydrated on cache hit.

## Recommended Implementation Order

1. Add `DataSetSourceMetadata` and `DataSetStationMetadata` models and a nullable `DataSet.SourceMetadata` property.
2. Add a metadata builder with in-memory unit tests for single-station, multi-station, missing-details, and derived-series cases.
3. Add `StationMetadataFileName` or equivalent explicit lookup configuration to `DataSetDefinition` for known station-backed datasets.
4. Wire the metadata builder into `PostDataSets`, iterate every source specification, and version the dataset cache key.
5. Update `ChartDataBuilder` to preserve `SourceMetadata` when it creates replacement datasets.
6. Pass metadata from `ChartView` through `ChartSeriesListView` and `ChartSeriesView` to `AboutData`.
7. Update `AboutData` rendering for source summary, single-station compact layout, and multi-station table using existing table classes.
8. Add focused helper and chart-data-builder tests.
9. Run `dotnet test` or at least `dotnet build` for compile and Razor validation, without running the website or browser tests.
