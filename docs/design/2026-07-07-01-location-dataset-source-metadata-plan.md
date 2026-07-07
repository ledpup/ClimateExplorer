# Location Dataset Metadata Plan

- **Date:** 2026-07-07
- **Status:** Proposed (API and WebApiClient stages implemented 2026-07-07; see addenda)
- **Author:** Codex
- **Scope:** `ClimateExplorer.WebApi` dataset/location metadata endpoints, `ClimateExplorer.WebApiClient`, `ClimateExplorer.Web.Client` location table and dashboard source metadata UI
- **Builds on:** [AboutData Station Metadata Plan](2026-07-05-01-about-data-station-metadata-plan.md)
- **Branch context:** `development`

## Summary

Add a location-level dataset metadata API and a shared Blazor side panel so users can inspect the sources behind a location without first opening a chart series.

The API should return all dataset sources available to one location as `DataSetMetadata` entries. The UI should open a side panel from both `Locations.razor` and `LocationDashboard`, fetch that metadata, render one tab per dataset source, and reuse the existing `AboutData` metadata rendering instead of creating a second station/source display path.

## Current-State Investigation

### Dataset Metadata

`DataSetMetadata` is a shared model in `ClimateExplorer.Core.Model`. It currently carries:

- `DataSetDefinitionId`, `LocationId`, and `LocationName`.
- `SourceCode`, `SourceName`, `SourceUrl`, and `SourceUrlLabel`.
- A list of `DataSetStationMetadata` rows.

Relevant files:

- `ClimateExplorer.Core/Model/DataSetMetadata.cs`
- `ClimateExplorer.Core/Model/DataSetMetadata.cs`
- `ClimateExplorer.Core/Model/DataSet.cs`, where `SourceMetadata` is attached to returned chart datasets.

`DataSetStationMetadata` already supports one or more station/file mappings through station id, station name, station start/end dates, source URL, and source URL label.

### Metadata Builder

`DataSetMetadataBuilder` currently lives in `ClimateExplorer.WebApi/DataSetMetadata/DataSetMetadataBuilder.cs`.

Current behavior:

- `BuildAsync(PostDataSetsRequestBody, IReadOnlyList<DataSetDefinition>?)` iterates every chart request `SeriesSpecification`.
- For each specification it resolves the matching `DataSetDefinition`, geographical entity, and `DataLocationMapping.LocationIdToDataFileMappings`.
- It creates one `DataSetMetadata` per source specification.
- It creates one `DataSetStationMetadata` per mapping when the dataset is station-backed.
- It already handles multiple mapped stations and missing station details.

Important limitation for the new endpoint:

- The public builder entry point is chart-request shaped. A location-level endpoint does not naturally have `DataType`, `DataAdjustment`, binning, or aggregation details. The builder should not be fed fake `PostDataSetsRequestBody` values just to reuse its internals.

Recommended direction:

- Keep the existing chart-shaped `BuildAsync` as an adapter for `PostDataSets`.
- Extract or add a second internal builder method that accepts already-resolved source inputs, such as dataset definition plus location id.
- Add a small location-level service/wrapper that finds all definitions available to a location, then asks the builder to build metadata for those definitions.

### AboutData Usage

`AboutData` is in `ClimateExplorer.Web.Client/Components/ChartSeries`.

Current behavior:

- It is a modal component.
- It receives `ChartSeriesDefinition? ChartSeries`.
- It receives `IReadOnlyList<DataSetMetadata>? SourceMetadata`.
- It renders dataset name, description, and publisher from `SourceSeriesSpecification.DataSetDefinition`.
- It finds source/station metadata by matching `DataSetDefinitionId` and `LocationId`.
- It renders single-station details compactly and multiple stations in a table.

Important limitation for side-panel reuse:

- The useful rendering logic is inside a modal shell. Rendering the current `AboutData` component inside a side panel would nest a hidden modal rather than show inline panel content.

Recommended direction:

- Refactor `AboutData` so the existing metadata body is extracted to an inline child component, for example `AboutDataSourceDetails`.
- Keep `AboutData` as the chart modal wrapper and have it call the extracted component.
- Use the extracted component inside the new location side panel.
- This avoids duplicated rendering logic while avoiding a modal inside a side panel.

### Location Dataset Availability

Locations currently resolve available datasets through dataset definitions:

- `MetadataEndpoints.GetDataSetDefinitions` projects `DataSetDefinition.DataLocationMapping.LocationIdToDataFileMappings.Keys` into `DataSetDefinitionViewModel.LocationIds`.
- `DataSetDefinitionViewModel.GetMeasurementsForLocation` filters definitions by `LocationIds.Contains(locationId)`.
- `DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement` uses the same `LocationIds` check for chart and dashboard data selection.

The new API should use the authoritative server-side source:

- Load `DataSetDefinition.GetDataSetDefinitions()`.
- Select definitions where `DataLocationMapping.LocationIdToDataFileMappings` contains the requested location id.
- Return one source metadata entry per matching dataset definition, not one per measurement definition.

This means a dataset such as one source containing temp min, temp max, and temp mean still appears as one tab/source.

### Locations.razor Data Sources Column

`ClimateExplorer.Web.Client/Pages/Locations.razor` currently:

- Loads all locations and dataset definitions in `OnInitializedAsync`.
- Builds `locationDataTypes` and `locationDataSources` dictionaries from `DataSetDefinitionViewModel.LocationIds`.
- Displays data sources as plain text in the table: `GetDataSourcesDisplay(location.Id)`.
- Already has a `SidePanel` for climate records.

Recommended direction:

- Keep the precomputed source-short-name dictionary for table display.
- Replace the plain source text with an accessible link-style button when sources exist.
- Open the new shared data source side panel with the selected location.

### LocationDashboard Side Panels and Options

`ClimateExplorer.Web.Client/Components/Location/LocationDashboard.razor` currently:

- Has a cog `DropdownButton` for location dashboard options.
- Contains a precipitation stripe toggle in the menu.
- Opens `SidePanel` instances for climate records and recent observations.
- Receives `Location` and `DataSetDefinitions` as parameters.

Recommended direction:

- Add a new option-menu action that opens the same shared data source side panel.
- Pass the current `Location` to the shared component at click time.
- Reuse the existing `DataSetDefinitions` parameter if the inline AboutData renderer needs dataset definition details.

## Stage 1: API Design

### Endpoint

Recommended route, matching the existing flat query-style endpoint conventions:

- `GET /location-dataset-metadata?locationId={guid}`

Alternative REST-style route:

- `GET /location/{locationId:guid}/dataset-source-metadata`

Recommendation: use the flat route unless the project is intentionally moving endpoint naming toward route parameters. Existing routes such as `/location-by-id`, `/nearby-locations`, `/climate-record`, and `/recent-observations` all use query parameters.

### Request

Required query parameter:

- `locationId`: location `Guid`.

No body.

### Response

Success:

- `200 OK`
- JSON array of `DataSetMetadata`.
- One entry per dataset definition available to the location.
- Stable ordering by `SourceCode`, then `SourceName`, then `DataSetDefinitionId`.

Example shape:

```json
[
  {
    "dataSetDefinitionId": "00000000-0000-0000-0000-000000000000",
    "locationId": "00000000-0000-0000-0000-000000000000",
    "locationName": "Hobart",
    "sourceCode": "ACORN-SAT",
    "sourceName": "Australian Climate Observations Reference Network - Surface Air Temperature",
    "sourceUrl": "https://example.test/source",
    "sourceUrlLabel": "ACORN-SAT",
    "stations": [
      {
        "stationId": "094029",
        "stationName": "Hobart",
        "stationStartDate": "1910-01-01",
        "stationEndDate": "2024-12-31",
        "sourceUrl": "https://example.test/station/094029",
        "sourceUrlLabel": "Station 094029"
      }
    ]
  }
]
```

### Response Completeness

The existing `AboutData` body uses both dataset definition details and `DataSetMetadata` details:

- Dataset name, description, publisher, publisher URL, and more-information URL come from `DataSetDefinitionViewModel`.
- Resolved source URL, source label, station ids, station names, station date ranges, and station URLs come from `DataSetMetadata`.

There are two viable designs:

1. Keep `DataSetMetadata` focused on resolved source/station metadata and have the UI join against already-loaded `DataSetDefinitions`.
2. Extend `DataSetMetadata` with nullable catalog display fields such as `Description`, `Publisher`, `PublisherUrl`, and `MoreInformationUrl`.

Recommendation: use option 1 for the first implementation because both target UI callers already have `DataSetDefinitions`. If later callers need a standalone endpoint response, add optional catalog fields to `DataSetMetadata` without changing the route or response collection type.

### Location-Level Metadata Service

Add a small API-side service/wrapper, for example `LocationDataSetMetadataService`.

Responsibilities:

- Resolve the requested location from existing location metadata.
- Load dataset definitions from `DataSetDefinition.GetDataSetDefinitions()`.
- Select definitions whose `DataLocationMapping.LocationIdToDataFileMappings` contains the location id.
- Delegate source/station construction to `DataSetMetadataBuilder`.
- Return an ordered `List<DataSetMetadata>`.

This keeps the endpoint thin and keeps dataset availability logic out of the Blazor client.

### Builder Changes

Refactor `DataSetMetadataBuilder` around a reusable core operation:

- Build one metadata row from `DataSetDefinition` plus `locationId`.
- Existing `BuildAsync(PostDataSetsRequestBody, definitions)` becomes an adapter that resolves definitions from source specs and calls the reusable core.
- New location service calls the reusable core for each available definition.

Why not call the current method directly:

- The current method requires a full `PostDataSetsRequestBody`.
- The location endpoint should not invent unused `DataType`, `DataAdjustment`, binning, or aggregation values.
- Fake chart requests would hide the true contract and make tests harder to read.

### Endpoint Handler

Add a handler in the most fitting existing API class:

- Preferred: `LocationEndpoints.GetLocationDataSetMetadata(...)`, because the endpoint is location-scoped.
- Acceptable: `DataSetEndpoints.GetLocationDataSetMetadata(...)`, because the payload is dataset metadata.

Map it in `ClimateExplorerEndpointRouteBuilderExtensions`.

### Error Handling

Unknown location id:

- Return `404 NotFound`.
- Keep the response body simple, for example a problem/details message containing the unknown `locationId`.

Valid location with no datasets:

- Return `200 OK` with an empty array.
- This distinguishes "unsupported location" from "unknown location".

Invalid or missing GUID:

- Let ASP.NET Core minimal API binding return a bad request for the required `Guid` parameter.

Station metadata file missing or station id not found:

- Do not fail the whole endpoint.
- Return station ids and mapping dates when available, with null station names/date fallbacks as the builder already does.

Dataset definition mapping inconsistency:

- Prefer not to throw from the endpoint for one bad source if the location has other valid sources.
- Log and omit only the invalid source if the failure is isolated.
- Add a test before choosing to swallow isolated source failures, because silent omissions can make provenance look cleaner than it is.

### Caching

First implementation can rely on the existing cheap metadata reads and static station metadata cache in `StationMetadataLookup`.

Client-side caching should be added in `DataService` through `DataServiceCache`, keyed by the full endpoint URL, matching the existing `/datasetdefinition`, `/location`, and `/nearby-locations` patterns.

Server-side cache can be added later if profiling shows repeated file reads matter:

- Suggested key: `LocationDataSetMetadata_v1_{locationId}`.
- Prefer `LongtermCache`, because bundled metadata is static within a running deployment.

## Stage 2: API Client Design

Add to `IDataService`:

- `Task<IReadOnlyList<DataSetMetadata>> GetLocationDataSetMetadata(Guid locationId);`

Add to `DataService`:

- Build `/location-dataset-metadata`.
- Add `locationId` via `QueryHelpers.AddQueryString`.
- Read `DataSetMetadata[]` with the existing web JSON options.
- Cache by URL in `DataServiceCache`.
- Throw a clear exception on non-success status codes so the side panel can show an error state.

Do not fetch this metadata eagerly for every location in the table. Fetch only when the user opens the side panel.

## Stage 3: Shared UI Component Design

### Component

Add a shared component under `ClimateExplorer.Web.Client/Components/Location`, for example:

- `LocationDataSourcesSidePanel.razor`
- `LocationDataSourcesSidePanel.razor.cs`
- optional scoped CSS file if needed

This component should wrap the existing `SidePanel` so both callers use one loading and rendering path.

Suggested public API:

- `IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions`
- `string Width`, default `85%`
- public `Task ShowAsync(Location location)`

Internal state:

- `SidePanel? sidePanel`
- `Location? selectedLocation`
- `IReadOnlyList<DataSetMetadata>? sourceMetadata`
- `bool isLoading`
- `string? errorMessage`
- `string? selectedTab`
- a monotonically increasing request id or cancellation token source to ignore stale responses if the user opens another location before the previous request completes.

Injected services:

- `IDataService`
- `ILogger<LocationDataSourcesSidePanel>`

### Loading Flow

`ShowAsync(Location location)` should:

- Set `selectedLocation`.
- Reset `sourceMetadata`, `errorMessage`, and `selectedTab`.
- Show the side panel immediately.
- Fetch metadata for `location.Id`.
- Set the active tab to the first returned source.
- Ignore stale responses if a newer `ShowAsync` call has started.

### Loading, Empty, and Error States

Loading:

- Use existing loading indicator conventions, preferably `DelayedLoadingIndicator`.
- Keep the panel title stable, for example `Data sources for Hobart`.

Empty:

- Show a concise empty state when the endpoint returns an empty array.
- Keep the side panel open so the user understands the click worked.

Error:

- Show a concise error message and a `Retry` `ClimateButton`.
- Log the exception.
- Retry should call the same endpoint for `selectedLocation`.

Accessibility:

- The close button is already named by `SidePanel`.
- Tab labels must be visible text.
- Retry and source table buttons/links must have accessible names.

### Tabs

Render one tab per `DataSetMetadata`.

Tab label:

- Use `SourceCode` first.
- Fall back to `SourceName`.
- Fall back to `Dataset {index}`.

Tab identity:

- Use `DataSetDefinitionId` when available.
- Fall back to a generated stable index key.

Duplicate short names:

- The requirement says to use the dataset short name as the tab label, so keep the visible label as `SourceCode`.
- Use unique internal tab names even if two visible labels match.

### Reusing AboutData

Do not duplicate the current `AboutData` station/source rendering.

Recommended refactor:

- Extract the body used for one source specification into an inline component, for example `AboutDataSourceDetails`.
- Parameters for the extracted component:
  - `DataSetDefinitionViewModel? DataSetDefinition`
  - `DataSetMetadata? SourceMetadata`
  - optional heading/title parameter if chart-derived series still need section headings.
- Move helper methods such as station/source formatting with the component or into a small internal helper class if both the modal and panel need them.
- Update `AboutData` to keep the modal shell and loop over chart sources, rendering `AboutDataSourceDetails` for each source.
- Render `AboutDataSourceDetails` from the new side panel once per selected tab.

If the team wants the literal `AboutData` component to support inline use instead, add an explicit `DisplayMode` parameter rather than relying on modal internals. The extraction approach is cleaner because it keeps modal behavior out of the side panel.

### Matching Metadata to Dataset Definitions

The side panel should match endpoint metadata to existing `DataSetDefinitions` by `DataSetDefinitionId`.

If a matching definition is not found:

- Still render the source/station metadata from the endpoint.
- Use `SourceName` as the dataset name fallback.
- Omit missing description and publisher fields.

## Stage 4: Locations.razor Integration

### Data Sources Column Behavior

Current plain text:

- `@GetDataSourcesDisplay(location.Id)`

Replace with:

- A link-style `ClimateButton` when the location has one or more source names.
- A dash when no sources are known.

Button behavior:

- Text remains the comma-separated source short-name list.
- Click calls `ShowDataSourcesAsync(location)`.
- `AriaLabel` should include the location name, for example `View data sources for Hobart`.

This preserves the current scannable table display while making the cell actionable.

### Selected Location Flow

Add:

- `LocationDataSourcesSidePanel? dataSourcesSidePanel`
- `Task ShowDataSourcesAsync(Location location)`

The method should call:

- `await (dataSourcesSidePanel?.ShowAsync(location) ?? Task.CompletedTask);`

Add the component near the existing climate records side panel:

- Pass `DataSetDefinitions="@DataSetDefinitions"`.

### Table View Model Changes

No major table view model change is required.

Keep:

- `locationDataSources` for display labels.
- `locationDataTypes` for current data type display.

Possible small improvement:

- Sort source labels when building `locationDataSources`, or sort in `GetDataSourcesDisplay`, so the visible order matches the side panel tab order.

Do not preload the new endpoint for each table row.

## Stage 5: LocationDashboard Integration

### Options Menu Change

Add a menu action inside the existing `DropdownButton` menu:

- Text: `Data sources`
- Icon: use an existing icon style consistent with the app, for example `fas fa-circle-info` or `fas fa-database`.
- Click: `ShowDataSourcesAsync`.
- Disabled when `Location` is null.
- Accessible name includes the current location when available.

Keep the existing precipitation checkbox behavior unchanged.

### Reuse Shared Side Panel

Add the same `LocationDataSourcesSidePanel` component to `LocationDashboard.razor`.

Pass:

- `DataSetDefinitions="@DataSetDefinitions"`
- optional `Width="85%"` if the source/station table needs room.

Code-behind:

- Add `LocationDataSourcesSidePanel? dataSourcesSidePanel`.
- Add `Task ShowDataSourcesAsync()`.
- The method should no-op when `Location` is null, otherwise call `dataSourcesSidePanel.ShowAsync(Location)`.

### Dashboard-Specific State

No dashboard-specific metadata cache is needed.

Important behavior:

- If the dashboard location changes, the next menu click should load metadata for the new `Location`.
- If a request is in flight and a different location is opened, the shared panel's stale-response guard should prevent old metadata from replacing new metadata.

## Stage 6: Testing Plan

### API Unit Tests

Extend or add tests in `ClimateExplorer.UnitTests`.

Follow the repo convention:

- `MethodName_StateUnderTest_ExpectedBehavior`

Recommended builder tests:

- `BuildAsync_LocationWithMultipleDataSetDefinitions_ReturnsOneMetadataRowPerDefinition`
- `BuildAsync_LocationWithSingleStationDataset_ReturnsStationMetadata`
- `BuildAsync_LocationWithMultiStationDataset_ReturnsAllStations`
- `BuildAsync_NonStationBackedDataset_ReturnsSourceWithoutStations`
- `BuildAsync_MissingStationDetails_ReturnsStationIdsWithoutNames`

Recommended service tests:

- `GetMetadataAsync_UnknownLocation_ReturnsNotFoundResult` or equivalent handler-level test.
- `GetMetadataAsync_KnownLocationWithNoDatasets_ReturnsEmptyList`
- `GetMetadataAsync_KnownLocationWithMultipleDatasets_ReturnsOrderedMetadata`

Existing useful coverage:

- `DataSetMetadataBuilderTests` already covers single station, multiple stations, missing station details, and derived chart requests. Keep these tests and add location-level cases rather than replacing them.

### API Client Tests

If adding tests around `DataService` is practical with the existing test setup:

- Verify `GetLocationDataSetMetadata` calls `/location-dataset-metadata?locationId=...`.
- Verify successful JSON deserialization.
- Verify non-success status throws a useful exception.
- Verify repeated calls hit `DataServiceCache`.

If there is no established `DataService` test pattern, rely on compile coverage plus focused API-side tests.

### UI and Component Tests

There does not appear to be a bUnit setup in the current unit tests. Do not add a large new UI test framework just for this feature unless the project decides to standardize on it.

Recommended low-cost checks:

- Razor compile through `dotnet build`.
- Unit-test any extracted non-visual state helper if tab selection or metadata matching becomes non-trivial.
- Keep Playwright/browser tests out of Codex verification because `AGENTS.md` says not to run the website, Playwright, Lighthouse, or browser tests.

### Manual Test Cases

Manual QA cases for a human developer:

- Location with one dataset source.
- Location with multiple dataset sources.
- Dataset with one station.
- Dataset with multiple stations.
- Missing or unsupported location.
- Location table source button opens the panel and uses the selected row.
- Dashboard option menu source action opens the same panel for the current location.
- Error state is visible if the API request fails.
- Empty state is visible for a known location with no datasets.
- Tabs remain usable on mobile, tablet, and fullscreen widths.

## Proposed Implementation Order

1. Refactor `DataSetMetadataBuilder` to expose a source-definition-plus-location core method while preserving the existing chart request adapter.
2. Add the location-level metadata service/wrapper and unit tests for dataset availability selection.
3. Add the new endpoint and route mapping.
4. Add `IDataService.GetLocationDataSetMetadata` and `DataService` implementation with client-side caching.
5. Extract the reusable inline `AboutData` source details renderer and update the existing modal to use it.
6. Add `LocationDataSourcesSidePanel` with loading, empty, error, and tab states.
7. Update `Locations.razor` to turn data source text into an accessible link-style button and wire the side panel.
8. Update `LocationDashboard` options menu and wire the same side panel.
9. Run `dotnet build`.
10. Run focused non-browser unit tests for the metadata builder/service/client work.

## Risks and Open Questions

- `DataSetMetadata` does not currently include publisher or description fields. The recommended UI design joins against existing `DataSetDefinitions`; if future callers need a standalone response, extend the model with optional catalog fields.
- Some dataset mappings use file/source identifiers rather than true station identifiers. The existing builder only creates station rows when the definition looks station-backed; preserve that behavior.
- Some station metadata files are sparse. The UI should look intentional with station ids and dates even when names are missing.
- Source short names may not be globally unique. Use short names as visible tab labels as required, but use dataset ids as internal tab keys.
- The existing `SidePanel` has a private dismiss method and no public close callback. The new shared side panel can work without changes, but future polish may require a close event or focus management pass.
- `LocationEndpoints.GetLocationById` currently returns null rather than a 404-style result. The new endpoint should explicitly return 404 for unknown ids because the caller needs a clear error state.

## Addendum - API Implementation Notes

Implemented the API stage on 2026-07-07:

- Added `DataSetMetadataBuilder.BuildAsync(DataSetDefinition, Guid)` so location-level metadata can reuse the existing source/station resolution without constructing fake chart request bodies.
- Added `LocationDataSetMetadataService`, which validates a location, selects dataset definitions mapped to that location, orders them by short name/name/id, and delegates metadata construction to the builder.
- Added `LocationDataSetMetadataResult` to distinguish unknown locations from known locations with no datasets.
- Added `GET /location-dataset-metadata?locationId={guid}` via `LocationEndpoints.GetLocationDataSetMetadata`.
- Added unit coverage for the direct builder path and the location-level service.

Verification:

- `dotnet build ClimateExplorer.sln` passed with the existing MSTest parallelization warning.
- `dotnet test ClimateExplorer.UnitTests\ClimateExplorer.UnitTests.csproj --no-build --filter "FullyQualifiedName~DataSetMetadataBuilderTests|FullyQualifiedName~LocationDataSetMetadataServiceTests"` passed: 9 tests.

Not implemented in this stage:

- Blazor side panel/component work.
- `Locations.razor` and `LocationDashboard` integration.

## Addendum - WebApiClient Implementation Notes

Implemented the WebApiClient stage on 2026-07-07:

- Added `IDataService.GetLocationDataSetMetadata(Guid locationId)`.
- Added `DataService.GetLocationDataSetMetadata`, calling `GET /location-dataset-metadata?locationId={guid}`.
- Added client-side caching through `DataServiceCache` using the full URL as the cache key.
- Added explicit non-success response handling that includes the status code and response body in the thrown exception.
- Added focused `DataServiceTests` coverage for endpoint URL construction, response deserialization, caching, and non-success error behavior.

Verification:

- `dotnet build ClimateExplorer.sln` passed with the existing MSTest parallelization warning.
- `dotnet test ClimateExplorer.UnitTests\ClimateExplorer.UnitTests.csproj --no-build --filter "FullyQualifiedName~DataServiceTests|FullyQualifiedName~DataSetMetadataBuilderTests|FullyQualifiedName~LocationDataSetMetadataServiceTests"` passed: 12 tests.

Not implemented in this stage:

- Blazor side panel/component work.
- `Locations.razor` and `LocationDashboard` integration.
