# `ChartView` responsibility refactor plan

- **Date:** 2026-06-20
- **Status:** Proposed
- **Author:** Patrick Lea (with Codex)
- **Scope:** `ChartView`, `ChartablePage`, location and regional/global chart pages, chart URL state, default chart selection, chart data preparation, chart rendering, and chart UI event flow.
- **Builds on:** [Investigation and refactor plan: `ChartView.razor`](2026-06-20-01-investigation-and-refactor-plan-ChartView.md)
- **Branch context:** `development`

## Summary

`ChartView` currently renders the chart, but it also acts as the chart page coordinator. The component:

- reads and writes chart state in the URL;
- chooses default charts for both location pages and the regional/global page;
- infers page kind from `Location is null`;
- resolves location and region IDs from serialized chart state;
- mutates existing chart series when the selected location changes;
- fetches datasets through `IDataService`;
- owns chart data preparation and Chart.js rendering;
- emits page-level snackbar and modal/download events.

The intended split should be:

- parent pages and page services own page context, URL state, defaults, navigation, modals, and notifications;
- chart state services parse/serialize URL state and validate it against available data;
- default chart services build explicit defaults for explicit page contexts;
- chart data services fetch and prepare chart data;
- `ChartView` renders prepared chart state, handles chart-specific UI interactions, and raises typed user-intent events.

This plan deliberately avoids a big rewrite. It first locks down current behavior, then extracts the highest-risk responsibilities while preserving existing routes, query strings, default charts, and empty-series behavior.

## Current Component Shape

`ChartView` accepts page-aware inputs:

```csharp
PageName
DataSetDefinitions
ChartAllData
Location
LocationDictionary
Regions
DownloadDataEvent
ShowAddDataSetModalEvent
SnackbarMessageEvent
```

It injects:

```csharp
IDataService
ICurrentDeviceService
IJSRuntime
ILogger<ChartView>
NavigationManager
```

Its internal state mixes several concerns:

- render handles: `chart`, `chartTrendline`, `chartWrapper`, `chartOptionsInfoPanel`;
- render flags: `ChartLoadingIndicatorVisible`, `ChartLoadingErrored`, `NoChartDataAvailable`, `haveCalledResizeAtLeastOnce`, `disposed`;
- orchestration guards: `updateUiStateInProcess`, `buildDataSetsInProcess`;
- chart selection state: `ChartSeriesList`, `ChartAllData`, `SelectedBinGranularity`, `SelectedStartYear`, `SelectedEndYear`, `SelectedGroupingDays`, `InternalGroupingThreshold`, `UserOverridePresetAggregationSettings`, `AxesScaleToZero`;
- prepared data: `ChartSeriesWithData`, `ChartBins`, `chartStartBin`, `chartEndBin`, `StartYears`;
- page/location state: `InternalLocationId`, `Location`, `LocationDictionary`, `Regions`, `PageName`;
- rendering state: `Colours`, `InternalChartType`, `CurrentAxes`, `IsMobileDevice`.

## Current `OnAfterRenderAsync` Flow

Setup currently happens in `OnAfterRenderAsync` because both parent pages load data during render lifecycle and the child `Chart<T>`/Chart.js instance may not be ready immediately in Blazor Server. The method is not limited to `firstRender`; it runs after renders but is gated by loading flags, in-process flags, and the availability of `DataSetDefinitions` and `Regions`.

Current pseudo-code:

```text
OnAfterRenderAsync(firstRender):
  IsMobileDevice ??= await CurrentDeviceService.Mobile()

  if buildDataSetsInProcess or updateUiStateInProcess:
    return

  if DataSetDefinitions is null or Regions is null:
    return

  if Location is not null:
    if InternalLocationId is set and differs from Location.Id:
      InternalLocationId = Location.Id
      await ChangedLocation()

    if ChartLoadingIndicatorVisible:
      uri = current URL

      if query contains "csd=":
        await UpdateUiStateBasedOnQueryString(uri)
      else if InternalLocationId is null:
        InternalLocationId = Location.Id
        await SetUpLocalDefaultCharts(Location.Id)
      else if query contains "chartAllData=":
        await RenderChart()

  else:
    if ChartLoadingIndicatorVisible:
      uri = current URL

      if query contains "csd=":
        await UpdateUiStateBasedOnQueryString(uri)
      else if query does not contain "chartAllData=":
        await AddDefaultChart()
      else:
        await RenderChart()
```

Important current behaviors:

- URL state is loaded in both the location and non-location branches.
- `Location is null` is used as the page-kind signal for regional/global behavior.
- Missing `csd` plus missing `chartAllData` means "create default chart".
- Missing `csd` plus present `chartAllData` means "do not recreate defaults"; this preserves the empty/no-series state after a user removes series.
- `BuildDataSets` serializes current chart state back to the URL and calls `NavigateTo`. It then renders directly instead of relying on navigation to re-trigger setup.
- In Blazor Server, default chart setup waits for `waitForChartReady` before building datasets; in browser/WASM it does not.

Desired flow:

```text
Parent page:
  load data definitions, regions, and any required locations
  create explicit ChartPageContext
  parse chart URL state
  if URL state is valid:
    use it
  else if URL explicitly says empty chart state:
    use empty state
  else:
    ask default chart provider for page-context default
  pass chart state/options into ChartView

ChartView:
  receive chart state and display options
  render controls and chart output
  fetch/prepare/render through chart-specific services during transition
  raise ChartStateChanged, DownloadRequested, AddDataSetRequested, ChartMessageRaised

Parent page:
  serialize ChartStateChanged to URL
  own page navigation, page modals, and snackbars
```

## Method Inventory

Line numbers refer to `ClimateExplorer.Web.Client/Components/Chart/ChartView.razor.cs` when this document was written.

| Method | Category | Reads and parameter dependencies | Mutates or side effects | Recommendation |
| --- | --- | --- | --- | --- |
| `DisposeAsync` (133) | Lifecycle/render cleanup | `chart` | Sets `disposed`; destroys Chart.js wrapper | Keep in `ChartView`. |
| `OnAddDataSet` (148) | Event handler, page command, series factory | `ChartSeriesList`, `SelectedBinGranularity`; method args `DataSetLibraryEntry`, `dataSetDefinitions` | Adds a `ChartSeriesDefinition`; calls `BuildDataSets` | Move add-series orchestration to parent or `ChartSeriesFactory`; keep only a typed chart-state change during transition. |
| `OnChartPresetSelected` (171) | Event handler, preset/default setup | `chartPresetModel`, `SnackbarMessageEvent` | Mutates `ChartAllData`, selected years, granularity, `ChartSeriesList`; may show snackbar; calls `BuildDataSets` | Move upstream. Parent should translate presets to chart state and pass state into `ChartView`. |
| `HandleOnYearFilterChange` (190) | Event handler, chart interaction, location-page fallback | `SelectedBinGranularity`, `ChartSeriesWithData`, `ChartSeriesList`, `Location`, `DataSetDefinitions` | Changes granularity, filters series, adds a year-filtered series, calls `BuildDataSets` | Split. Keep chart click intent in `ChartView`; move data-definition lookup and location fallback to a chart-state/page service. |
| `OnInitialized` (276) | Lifecycle/default UI state | None external | Initializes loading flags, selected years, grouping defaults | Keep during transition. Later move defaults into `ChartState` initializers. |
| `OnAfterRenderAsync` (288) | Lifecycle, URL handling, default setup, page orchestration | `CurrentDeviceService`, `DataSetDefinitions`, `Regions`, `Location`, `InternalLocationId`, `NavManager`, guard flags | Sets `IsMobileDevice` and `InternalLocationId`; calls URL loading, default setup, location-change flow, rendering | Move orchestration out. Keep only device/chart readiness work that truly belongs to rendering. |
| `BuildDataSets` (354) | URL serialization, navigation, data retrieval, render orchestration | `NavManager`, `PageName`, `ChartSeriesList`, `ChartAllData`, filters, grouping, axes | Sets loading flags; updates URL; fetches data; sets `ChartSeriesWithData`; calls `RenderChart`; raises snackbar on error | Split into `ChartStateChanged` event, URL service, chart data builder, and render call. |
| `RenderChart` (463) | Rendering | `chart`, `chartTrendline`, `ChartSeriesWithData`, `ChartAllData`, selected grouping/filter state, `IsMobileDevice`, `ChartLoadingErrored` | Clears/adds chart datasets, options, trendlines, labels; sets flags; JS interop; updates axes/type | Keep core render workflow in `ChartView`; extract pure option/data helpers as useful. |
| `LoadingChart` (610) | UI state | `ChartSeriesList` for logging | Sets loading/error/no-data flags | Keep or fold into render coordinator. |
| `ChangedLocation` (618) | Location-page orchestration, location lookup, series mutation | `Location`, `Regions`, `DataSetDefinitions`, `ChartSeriesList`, `SnackbarMessageEvent` | Rewrites source specs to new location, duplicates locked series, marks availability, calls `BuildDataSets` | Move to location page state coordinator or `ChartSeriesLocationSubstitutionService`. |
| `MapSeriesAggregationOptionToBinAggregationFunction` (792) | Pure chart data mapping | Method arg only | None | Move to chart data service/helper, or keep static until extraction. |
| `IsCompatibleUnitOfMeasure` (805) | Pure validation/filtering | Method args only | None | Move to chart series selection helper. |
| `GetChartLabel` (816) | Pure rendering label | Method args only | None | Move to `ChartLogic` or keep near rendering. |
| `HasRenderableChartData` (826) | Rendering validation | `ChartBins`, `ChartSeriesWithData`, `Logger` | Logs warnings | Keep in rendering/data-prep boundary; could move to data builder result validation. |
| `CreateChartOptions` (853) | Rendering options | `IsMobileDevice`; method args | Creates options object | Keep in `ChartView` or extract `ChartOptionsFactory`. |
| `GetGlobalQueryStringSettings` (898) | URL serialization | `PageName`, `ChartAllData`, selected years, grouping, `AxesScaleToZero`, override flag | Builds URL string | Move to `ChartStateUrlService`; remove `PageName` from `ChartView`. |
| `UpdateUiStateBasedOnQueryString` (943) | URL parsing, validation, page lookup, render orchestration | `DataSetDefinitions`, `Regions`, `LocationDictionary`/`Location`, `Logger`, query params | Mutates chart settings and `ChartSeriesList`; calls `BuildDataSets` or `RenderChart`; raises snackbar | Move to `ChartStateUrlService` plus parent coordinator. |
| `RetrieveDataSets` (1015) | Data retrieval | `DataService`, selected granularity/grouping, chart series | Calls API; returns `SeriesWithData` | Move to chart data builder/service. |
| `SetUpLocalDefaultCharts` (1070) | Location-page default setup | `DataSetDefinitions`, `LocationDictionary`/`Location`, `IsMobileDevice`, `JsRuntime` | Adds temperature and maybe precipitation series; waits for chart readiness; calls `BuildDataSets` | Move default series creation to `DefaultChartProvider`; keep chart readiness in view if still needed. |
| `AddDefaultChart` (1127) | Regional/global default setup | `DataSetDefinitions`, `JsRuntime` | Adds CO2 annual-change series; waits for chart readiness; calls `BuildDataSets` | Move to `DefaultChartProvider` using explicit page kind. |
| `GetKnownLocation` (1160) | Location lookup | `Location`, `LocationDictionary` | None | Move upstream or into URL/default validation service. |
| `GetKnownLocationDictionary` (1172) | Location lookup | `Location`, `LocationDictionary` | Allocates one-item dictionary when needed | Move upstream. Chart title should use resolved entity display data. |
| `LogChartSeriesList` (1184) | Diagnostics | `ChartSeriesList`, `SelectedBinGranularity`, `Logger` | Logs | Keep while useful; make null-safe if retained. |
| `ChartAllDataToggle` (1194) | UI event | `ChartAllData` | Toggles parameter value; calls `BuildDataSets` | Replace with chart state update event; parent owns state. |
| `ShowChartOptionsInfo` (1200) | UI event | `chartOptionsInfoPanel` | Opens panel | Keep in `ChartView`. |
| `BuildSourceSeriesSpecification` (1205) | Series factory | Method args, dataset definitions | Creates source spec | Move to `ChartSeriesFactory` used by parent/add-dataset flow. |
| `BuildDataPrepSeriesSpecification` (1221) | Data retrieval mapping | Source spec arg | Creates API spec | Move to chart data service. |
| `GetGroupingThreshold` (1233) | Data-prep option resolution | `UserOverridePresetAggregationSettings`, `InternalGroupingThreshold`; method args | None | Move with chart data preparation. |
| `AddDataSetsToChart` (1244) | Rendering | `ChartSeriesWithData`, `IsMobileDevice`, `chart`, `Colours` | Adds datasets to Chart.js; sets chart series colors; returns trendlines | Keep in rendering; consider moving label/color details to `ChartLogic`. |
| `BuildProcessedDataSets` (1285) | Chart data processing, validation, notification | `SelectedBinGranularity`, selected years, grouping, `SnackbarMessageEvent`, `ChartSeriesWithData` | Mutates `SeriesWithData`, `ChartBins`, start/end bins, `StartYears`; may show snackbar | Move to testable chart data preparation service after URL/default work. |
| `BuildChartScales` (1457) | Rendering options | `SelectedBinGranularity` | Creates scale object; calls `CreateYAxes` | Keep or extract `ChartOptionsFactory`. |
| `CreateYAxes` (1482) | Rendering options | `ChartSeriesList`, `AxesScaleToZero`, processed data via `CreateAxesMinMax` | Mutates `CurrentAxes`; adds dynamic y axes | Keep or extract with chart options. |
| `CreateAxesMinMax` (1524) | Rendering/data calculation | `ChartSeriesWithData` | None | Move to chart data/render helper; test independently if extracted. |
| `OnSelectedBinGranularityChanged` (1568) | UI event/state mutation | `ChartSeriesList`; method args | Mutates selected granularity and every series; removes duplicates; optionally calls `BuildDataSets` | Keep as chart UI intent during transition; later emit chart state change. |
| `GetGroupingThresholdText` (1585) | Rendering label | `ChartSeriesList`, override/internal threshold | None | Keep or move to display options helper. |
| `OnLineChartClicked` (1596) | Chart interaction | `SelectedBinGranularity`, `ChartAllData`, `StartYears`, `ChartSeriesWithData` | Calls `HandleOnYearFilterChange` | Keep click handling; emit `YearFilterRequested` or chart state change instead of building series directly. |
| `SetStartAndEndYears` (1613) | Data-prep/display state | `chartSeriesWithData` | Sets `StartYears` | Move with data preparation or expose as result metadata. |
| `OnClearFilter` (1622) | UI event | None external | Clears selected years; calls `BuildDataSets` | Keep UI intent; later emit state change. |
| `OnDownloadDataClicked` (1630) | UI event | `ChartSeriesWithData`, `ChartBins`, `SelectedBinGranularity` | Invokes `DownloadDataEvent` | Keep, rename to `DownloadRequested`. |
| `ShowAddDataSetModal` (1635) | UI event | `ShowAddDataSetModalEvent` | Invokes parent modal event | Keep as user-intent event, rename to `AddDataSetRequested`. |
| `OnAggregationSettingsChanged` (1640) | UI event/state mutation | Method arg | Mutates grouping threshold/days/override; calls `BuildDataSets` | Keep UI intent; later emit chart state change. |

## Parameter Responsibility Analysis

| Parameter | Current use | Recommendation |
| --- | --- | --- |
| `PageName` | Base route for query-string serialization in `GetGlobalQueryStringSettings` | Remove from `ChartView`. Parent or URL service should know route/page identity. |
| `DataSetDefinitions` | URL parsing, default chart creation, add dataset conversion, location-change substitution, data availability checks | Move most use upstream or into services. `ChartView` should not need the full dataset catalog except temporarily while it owns add-series/state mutation. |
| `ChartAllData` | Display/data-range option, URL state, click-to-filter math, mutable UI state | Keep concept, but replace mutable parameter with `ChartState.ChartAllData` plus `ChartStateChanged`/two-way binding. |
| `Location` | Page kind inference, location defaults, title lookup, location-change substitution, year-filter fallback | Remove from `ChartView`. Parent should pass explicit context or resolved display metadata. |
| `LocationDictionary` | URL state resolution, title lookup, location-change support, export in parent | Remove from `ChartView`. Keep in parent/URL parsing/default services. |
| `Regions` | Blocks setup until loaded, URL state resolution, distinguishes region IDs from location IDs during location substitution | Remove from `ChartView`. Move to URL/default/location-substitution services. |
| `DownloadDataEvent` | Emits current rendered data for CSV export | Keep as user intent, rename to `DownloadRequested`; consider passing `ChartDownloadRequest`. |
| `ShowAddDataSetModalEvent` | Parent owns modal; chart button asks to show it | Keep as user intent, rename to `AddDataSetRequested`. |
| `SnackbarMessageEvent` | Chart raises user-facing messages directly | Replace with `ChartMessageRaised` or structured operation results. Parent decides snackbar presentation. |

## Hidden Page Responsibilities

| Location | Current behavior | Why it should move |
| --- | --- | --- |
| `OnAfterRenderAsync`, `Location is not null` branch | Treats current page as a location page, loads URL state, sets local defaults, handles changed location | The page already knows it is the location page; `ChartView` should not infer it from nullable data. |
| `OnAfterRenderAsync`, `Location is null` branch | Treats current page as regional/global and adds CO2 default | This hard-codes a page-level default inside the chart component. |
| `GetGlobalQueryStringSettings` | Builds route plus query from `PageName` and chart settings | Navigation belongs to parent/page state layer. |
| `UpdateUiStateBasedOnQueryString` | Parses URL, resolves dataset/location/region IDs, mutates UI state | URL parsing and validation should be testable without a rendered chart. |
| `SetUpLocalDefaultCharts` | Chooses temperature and optional precipitation defaults for a location | Location-page default selection belongs to explicit page context/default provider. |
| `AddDefaultChart` | Chooses regional/global CO2 annual-change default | Regional/global default selection belongs to explicit page context/default provider. |
| `ChangedLocation` | Rewrites chart series when map/modal changes selected location | This is location-page behavior and should live with location navigation/state. |
| `GetKnownLocation` / `GetKnownLocationDictionary` | Resolves location display data for parsing/title logic | Entity lookup belongs to parent or URL/default validation service. |
| `ChartablePage.GetLocationFromCsd` | Parent already parses URL CSD to choose initial location | This duplicates the need for a dedicated chart URL state service shared by pages. |

## Target Responsibility Split

### Parent pages

Parent pages should:

- declare explicit page kind (`Location` or `RegionalAndGlobal`);
- load `DataSetDefinitions`, `Regions`, and required location data;
- decide whether a full `LocationDictionary` is required for URL state;
- parse URL state through a chart URL service;
- select default chart state through a default chart provider;
- preserve empty chart state when `chartAllData` exists without `csd`;
- pass chart state/options into `ChartView`;
- update navigation when chart state changes;
- own add-dataset modal, downloads, and snackbar rendering.

### Chart URL state service

Responsibilities:

- parse supported query params: `chartAllData`, `startYear`, `endYear`, `groupingDays`, `groupingThreshold`, `userOverride`, `axisScaleToZero`, and `csd`;
- call `ChartSeriesListSerializer` or replace it behind a stable API;
- validate parsed series against dataset definitions, known locations, and regions;
- return a result that distinguishes:
  - valid chart state;
  - explicit empty chart state;
  - missing chart state;
  - invalid chart state;
- serialize chart state back to the same query-string shape.

### Default chart provider

Responsibilities:

- take explicit `ChartPageContext`;
- produce location defaults: temperature plus precipitation on non-mobile when available;
- produce regional/global defaults: CO2 annual change;
- never infer page kind from `Location is null`.

### Chart data builder

Responsibilities:

- map chart series definitions to API `SeriesSpecification`;
- call `IDataService.PostDataSet`;
- apply grouping threshold rules;
- apply secondary calculations, smoothing, bin selection, gap filling, and axis-range metadata;
- return a result object with data, bins, start/end bins, start years, warnings, and validation status.

This can be later than URL/default extraction because it touches render behavior more deeply.

### `ChartView`

Responsibilities:

- render chart series list, controls, empty state, Chart.js chart, and info panel;
- maintain only view-local render state and transient interaction state;
- apply chart options and Chart.js interop;
- raise typed events:
  - `ChartStateChanged`;
  - `DownloadRequested`;
  - `AddDataSetRequested`;
  - `ChartMessageRaised`;
  - possibly `YearFilterRequested`.

`ChartView` should not:

- inspect `NavigationManager.Uri`;
- call `NavigateTo`;
- know the current route;
- choose page defaults;
- infer page kind;
- resolve location or region IDs from URL state;
- load the location dictionary;
- mutate location-specific chart state after page navigation without an explicit state update.

## Proposed Types

Introduce only what is needed for the next extraction. The likely end state is:

```csharp
public enum ChartPageKind
{
    Location,
    RegionalAndGlobal,
}

public sealed record ChartPageContext
{
    public required ChartPageKind PageKind { get; init; }
    public Location? Location { get; init; }
    public IReadOnlyDictionary<Guid, Location>? Locations { get; init; }
    public required IReadOnlyList<Region> Regions { get; init; }
    public required IReadOnlyList<DataSetDefinitionViewModel> DataSetDefinitions { get; init; }
    public bool IsMobileDevice { get; init; }
}

public sealed record ChartState
{
    public bool ChartAllData { get; init; }
    public string? StartYear { get; init; }
    public string? EndYear { get; init; }
    public short GroupingDays { get; init; } = 14;
    public string GroupingThresholdText { get; init; } = "70";
    public bool UserOverrideAggregationSettings { get; init; }
    public IReadOnlyDictionary<string, bool> AxesScaleToZero { get; init; } = new Dictionary<string, bool>();
    public IReadOnlyList<ChartSeriesDefinition> Series { get; init; } = [];
}

public enum ChartUrlStateKind
{
    Missing,
    Valid,
    ExplicitEmpty,
    Invalid,
}

public sealed record ChartUrlStateResult(
    ChartUrlStateKind Kind,
    ChartState? State,
    string? ErrorMessage);
```

Service shapes:

```csharp
public interface IChartStateUrlService
{
    ChartUrlStateResult Parse(Uri uri, ChartPageContext context);
    string BuildRelativeUrl(string pagePath, ChartState state);
}

public interface IDefaultChartProvider
{
    ChartState CreateDefault(ChartPageContext context);
}

public interface IChartDataBuilder
{
    Task<ChartDataBuildResult> BuildAsync(ChartState state, CancellationToken cancellationToken = default);
}
```

Names are illustrative. The first implementation can be internal classes in the client project, using existing `ChartSeriesListSerializer` and current model types to avoid a large data-model rewrite.

## Staged Refactor Plan

### Phase 1: Lock current behavior and extract URL state shape

- Add tests around `ChartSeriesListSerializer` and new URL state behavior before changing call sites.
- Capture current query-string semantics:
  - no `csd` and no `chartAllData` means default chart;
  - no `csd` but `chartAllData` present means explicit empty chart;
  - `csd` means parse chart state;
  - malformed `csd` behavior is documented before it is improved.
- Introduce `ChartState`/`ChartUrlStateResult` or similarly small records.
- Move `GetGlobalQueryStringSettings` and the parse portion of `UpdateUiStateBasedOnQueryString` into a service/helper.
- Keep `ChartView` calling the service at first to reduce blast radius.

### Phase 2: Extract default chart creation

- Move the series-construction logic from `SetUpLocalDefaultCharts` and `AddDefaultChart` into `DefaultChartProvider`.
- Add unit tests for:
  - location default adds temperature;
  - location default adds precipitation only when available and not mobile;
  - regional/global default is CO2 annual change with existing smoothing/color/display settings.
- Keep `ChartView` invoking the provider initially, but require an explicit `ChartPageKind` from parent so `Location is null` is no longer the default selector.

### Phase 3: Move chart state orchestration to parent pages

- Add a coordinator on `ChartablePage` or separate `ChartPageStateCoordinator`.
- Parent flow:
  - load page dependencies;
  - create `ChartPageContext`;
  - parse URL state;
  - choose valid, explicit-empty, or default state;
  - pass state to `ChartView`.
- `ChartView` raises `ChartStateChanged` whenever controls alter chart state.
- Parent serializes state to URL and chooses `replace` semantics.
- Remove `PageName` and `NavigationManager` from `ChartView`.

### Phase 4: Move location-change substitution out of `ChartView`

- Extract current `ChangedLocation` logic into a location-page service.
- Parent location page applies the substitution when `Location` changes, then passes the updated chart state to `ChartView`.
- Preserve locked-series duplication behavior and unavailable-data snackbar warnings.
- Remove `Location`, `LocationDictionary`, and `Regions` from `ChartView` once URL parsing, defaults, and location substitution no longer need them there.

### Phase 5: Extract chart data building

- Move `RetrieveDataSets`, `BuildDataPrepSeriesSpecification`, `GetGroupingThreshold`, `BuildProcessedDataSets`, `SetStartAndEndYears`, and possibly `CreateAxesMinMax` into a testable chart data builder.
- Have the builder return:
  - `SeriesWithData`;
  - `ChartBins`;
  - start/end bins;
  - start years;
  - warnings/messages;
  - renderability status.
- `ChartView` then renders a build result instead of mutating raw data throughout the component.

### Phase 6: Simplify `ChartView` API and split rendering helpers

Target public surface:

```csharp
[Parameter]
public ChartState State { get; set; } = new();

[Parameter]
public ChartDataBuildResult? Data { get; set; }

[Parameter]
public EventCallback<ChartState> ChartStateChanged { get; set; }

[Parameter]
public EventCallback<ChartDownloadRequest> DownloadRequested { get; set; }

[Parameter]
public EventCallback AddDataSetRequested { get; set; }

[Parameter]
public EventCallback<ChartMessage> ChartMessageRaised { get; set; }
```

This shape may change as phases 1-5 expose better names. The important part is that the final API is chart-state-based rather than page-context-based.

## Testing Plan

Add tests in layers:

- Unit tests for URL state parse/build:
  - valid location URL state with `csd`;
  - valid regional/global URL state with `csd`;
  - no `csd` and no `chartAllData` returns missing/default-needed;
  - no `csd` and present `chartAllData` returns explicit empty;
  - `startYear`, `endYear`, `groupingDays`, `groupingThreshold`, `userOverride`, and `axisScaleToZero` round-trip;
  - invalid `csd` behavior matches current behavior first, then can be improved deliberately.
- Unit tests for default chart provider:
  - location default temperature;
  - location default precipitation availability and mobile exclusion;
  - regional/global CO2 annual-change default;
  - explicit page context, no `Location is null` inference.
- Unit tests for location substitution:
  - unlocked single-location series move to new location;
  - region source specs are not rewritten;
  - locked series are duplicated for the new location;
  - unavailable measurements mark series unavailable and raise warning.
- Unit tests for data builder after extraction:
  - moving average fallback warning;
  - annual-change secondary calculation;
  - `ChartAllData` range selection;
  - modular granularity bins;
  - empty/no-renderable data result.
- UI tests:
  - location page loads chart from valid URL state;
  - location page falls back to local default when URL state is absent;
  - regional/global page loads chart from valid URL state;
  - regional/global page falls back to CO2 default when URL state is absent;
  - removing a series keeps `chartAllData` URL behavior and does not recreate defaults;
  - add dataset modal still opens and adds a series;
  - download still produces a CSV;
  - snackbar messages still appear;
  - Blazor Server `waitForChartReady` behavior remains covered by a smoke test if practical.

Existing `ClimateExplorer.Web.UiTests/ChartTests.cs` already covers part of the empty-series URL behavior. Expand rather than replace those tests.

## Risks

- URL behavior is user-visible and shareable; even small query-string changes can break bookmarked charts. **Not important**
- `ChartView` currently mutates `[Parameter] ChartAllData`; fixing ownership may expose assumptions in parent pages.
- `OnAfterRenderAsync` order differs between parent pages and child chart setup. Moving initialization too early may reintroduce Chart.js readiness issues, especially in Blazor Server.
- Location dictionary loading is intentionally deferred unless URL CSD parsing needs it. Moving URL parsing upstream must preserve that performance behavior.
- Invalid `csd` handling may currently leave awkward UI states. Treat that as a documented behavior first, then improve with explicit acceptance criteria.
- `ChangedLocation` contains subtle locked-series duplication and region-skipping rules. Extract it with tests before changing behavior.
- `BuildProcessedDataSets` mutates data objects in place. Extraction should either preserve mutation semantics carefully or switch to immutable results in a separate phase.

## Open Questions

- Should `ChartView` continue to fetch data after URL/default extraction, or should parent pages pass fully built `ChartDataBuildResult`?
  - Answer: No, it should not fetch data. Create a `ChartDataBuilder` service and have parent pages call it after URL parsing and default selection. This keeps all orchestration out of `ChartView` and allows for better testing of data preparation logic.
- Should chart state be immutable records from the start, or should phase 1 wrap existing mutable `ChartSeriesDefinition` lists to limit churn?
  -  Answer: I don't understand the ramifications. 
- Should snackbar messages from chart data preparation become structured warning codes so parent pages can choose presentation?
  - Answer: no, this is not important.
- Should `ChartSeriesListSerializer` remain the long-term URL format, or should a versioned serializer be introduced before more chart state is added?
  - Answer: I don't understand the ramifications.
- Should the location page keep using `ChartablePage.GetLocationFromCsd`, or should initial-location selection use the same `ChartStateUrlService.Parse` result as chart setup?
  - Answer: the trick is that the csd can acually have more than one location in it. There is a mismatch where only one LocationDashboard is shown, but multiple locations can appear in the chart. This might need more investigation. Maybe we should be able to slide through the locations from the csd, displaying the different LocationDashboards as we do. That means we don't have a Location object but a Locations list. The map would have to change as the user cycles through the locations. This is a bigger change.

## Immediate Next Step

Start with Phase 1. Add a `ChartStateUrlService` around the current query parameters and serializer, with tests for current behavior. Then replace only `GetGlobalQueryStringSettings` and query parsing in `ChartView` with calls to that service, leaving rendering and default creation untouched until behavior is locked.
