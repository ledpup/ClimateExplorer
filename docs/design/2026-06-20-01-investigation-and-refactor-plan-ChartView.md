# Investigation and refactor plan: `ChartView.razor`

## Task

Investigate `ChartView.razor` and produce a refactor plan. Do **not** start the refactor.

The component currently appears to have too many responsibilities. The C# code-behind is around 1,647 lines long.

The intended responsibility of `ChartView` should be:

> Receive data, apply chart-related processing, and render the chart UI.

However, it currently appears to also handle:

* URL chart-state loading;
* default chart selection;
* location-page logic;
* regional/global-page logic;
* page fallback behaviour;
* location and region lookup;
* assumptions about which page it is on.

This task is to review the component, classify its responsibilities, and propose a staged plan to separate concerns while preserving current behaviour.

## Current parameters

```csharp
[Parameter]
public string? PageName { get; set; }

[Parameter]
public IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

[Parameter]
public bool ChartAllData { get; set; }

[Parameter]
public Location? Location { get; set; }

[Parameter]
public Dictionary<Guid, Location>? LocationDictionary { get; set; }

[Parameter]
public IEnumerable<Region>? Regions { get; set; }

[Parameter]
public EventCallback<DataDownloadPackage> DownloadDataEvent { get; set; }

[Parameter]
public EventCallback ShowAddDataSetModalEvent { get; set; }

[Parameter]
public EventCallback<SnackbarMessage> SnackbarMessageEvent { get; set; }
```

Some of these are likely signs that `ChartView` is doing page-level orchestration rather than just chart rendering.

## Known concerns

`ChartView` currently has a complex `OnAfterRenderAsync`.

From current understanding:

* It loads chart state from information in the URL.
* If URL chart state does not exist, it calls `SetUpLocalDefaultCharts`.
* If `Location` is null, it assumes it is on the regional/global page.
* It then tries to get chart data from the URL again.
* If that fails, it calls `AddDefaultChart`, which adds the default chart for the regional/global page.

This is problematic because `ChartView` should not know that the regional/global page exists. It also should not infer page type from `Location == null`.

## Investigation requirements

### 1. Create a method inventory

Review every method and event handler in `ChartView.razor` and its code-behind.

For each method, document:

* method name;
* purpose;
* whether it is lifecycle, event handler, rendering, chart processing, URL handling, default setup, download/export, validation, notification, or page orchestration;
* fields/properties it reads;
* fields/properties it mutates;
* parameters it depends on;
* whether it should remain in `ChartView`;
* where it should move if it does not belong in `ChartView`.

Use categories like:

```text
Keep in ChartView:
- chart rendering
- chart display state
- chart-specific UI interactions
- chart-specific transformations
- chart-specific validation

Move out of ChartView:
- URL parsing
- URL serialization
- default chart selection
- location-page defaults
- regional/global-page defaults
- page identity checks
- location lookup
- region lookup
- cross-page orchestration
```

### 2. Analyse `OnAfterRenderAsync`

Document the current `OnAfterRenderAsync` flow clearly.

Include:

* why setup is happening in `OnAfterRenderAsync`;
* whether it only runs on first render;
* what data it waits for;
* whether it behaves differently in Server vs WASM;
* whether it attempts URL chart loading more than once;
* where it calls `SetUpLocalDefaultCharts`;
* where it calls `AddDefaultChart`;
* where it assumes `Location == null` means regional/global page.

Provide pseudo-code for the current flow.

Then propose the desired flow.

Expected direction:

```text
Parent page:
- determines page context;
- loads required location/region/dataset information;
- parses chart state from URL;
- chooses default chart state if no valid URL state exists;
- passes prepared chart state into ChartView.

ChartView:
- receives chart state;
- renders chart UI;
- handles chart-specific interactions;
- emits events when the user changes chart state.
```

### 3. Review each parameter

For every current parameter, recommend one of:

* keep;
* remove;
* replace with narrower input;
* move responsibility upstream to parent page;
* move responsibility into a service.

#### `PageName`

Investigate whether this is only used for page identity, URL handling, or default chart selection.

Likely recommendation: remove from `ChartView`.

#### `DataSetDefinitions`

Investigate whether this is genuinely chart input or whether `ChartView` is doing too much preprocessing.

Possible recommendation: remove.

#### `ChartAllData`

Investigate whether this is a chart display option or a page-level mode.

Possible recommendation: keep.

#### `Location`

Investigate whether the full `Location` is needed.

Questions:

* Is it used only for labels?
* Is it used for default selection?
* Is it used to infer page type?
* Is it used to resolve chart state?

Likely recommendation: remove from `ChartView`.

#### `LocationDictionary`

Investigate whether this is only used for URL state resolution or ID lookup.

Likely recommendation: remove from `ChartView`.

#### `Regions`

Investigate whether this is only used for regional chart setup or URL state resolution.

Likely recommendation: remove from `ChartView`.

#### `DownloadDataEvent`

Likely recommendation: keep.

#### `ShowAddDataSetModalEvent`

Likely acceptable as a user-intent event if the parent owns the modal.

Consider renaming to something like `AddDataSetRequested`.

#### `SnackbarMessageEvent`

Investigate whether `ChartView` should directly emit snackbar messages or whether it should emit structured chart messages/results.

### 4. Identify hidden page responsibilities

Search for logic related to:

* regional/global page;
* location page;
* default charts;
* route state;
* URL query state;
* page name;
* location fallback;
* region fallback;
* `Location == null`.

For each occurrence, document:

* where it is;
* what it currently does;
* why it does or does not belong in `ChartView`;
* where it should move.

Likely destinations:

* location page component;
* regional/global page component;
* `ChartStateUrlService`;
* `DefaultChartProvider`;
* `ChartPageStateFactory`;
* `ChartConfigurationService`;
* `ChartDataPreparationService`.

### 5. Propose a target responsibility split

#### Parent page responsibilities

The parent page should:

* know whether it is a location page or regional/global page;
* load `Location`, `LocationDictionary`, `Regions`, and dataset definitions;
* parse chart state from the URL;
* choose default chart state if URL chart state is missing or invalid;
* pass prepared chart state/options into `ChartView`;
* handle page-level navigation and URL updates;
* own page-level modals and notifications.

#### Chart state URL service responsibilities

A chart state URL service should:

* parse chart state from URL/query parameters;
* validate parsed state against available datasets, locations, and regions;
* return a valid chart state or a failure result;
* serialize chart state back to the URL if required.

#### Default chart provider responsibilities

A default chart provider should:

* provide default chart state for a location page;
* provide default chart state for a regional/global page;
* use explicit page context;
* never rely on `Location == null` to determine page type.

#### `ChartView` responsibilities

`ChartView` should:

* receive chart state and display options;
* render chart controls and chart output;
* apply chart-specific transformations;
* handle local chart UI interactions;
* raise events such as:

  * `ChartStateChanged`;
  * `DownloadRequested`;
  * `AddDataSetRequested`;
  * `ChartMessageRaised`.

`ChartView` should not:

* inspect the URL directly;
* infer which page it is on;
* know regional/global page defaults;
* create page-specific default charts;
* resolve location IDs or region IDs from URL state;
* contain page fallback logic.

### 6. Consider possible new abstractions

Do not introduce abstractions for their own sake. Prefer small, obvious seams.

Possible types or services to consider:

```csharp
public sealed class ChartViewModel
{
    public IReadOnlyList<ChartSeriesViewModel> Series { get; init; } = [];
    public ChartDisplayOptions DisplayOptions { get; init; } = new();
    public ChartSelectionState SelectionState { get; init; } = new();
}
```

```csharp
public interface IChartStateUrlService
{
    ChartStateParseResult TryReadFromUrl(...);
    string BuildUrl(...);
}
```

```csharp
public interface IDefaultChartProvider
{
    ChartState GetDefaultChartState(ChartPageContext context);
}
```

```csharp
public sealed class ChartPageContext
{
    public ChartPageKind PageKind { get; init; }
    public Location? Location { get; init; }
    public IReadOnlyDictionary<Guid, Location>? Locations { get; init; }
    public IReadOnlyList<Region>? Regions { get; init; }
}
```

These are illustrative only. The investigation should recommend the actual shape based on current code.

### 7. Propose a staged refactor plan

The plan should minimise risk and preserve behaviour.

#### Phase 1: Document current behaviour

* Create full method inventory.
* Document current lifecycle/setup flow.
* Identify page-specific logic inside `ChartView`.
* Identify URL/default chart logic inside `ChartView`.
* Do not make large production changes.

#### Phase 2: Extract URL chart state logic

* Move URL parsing and serialization into a dedicated service/helper.
* Preserve existing behaviour.
* Add tests around current URL parsing and fallback behaviour if practical.

#### Phase 3: Extract default chart creation

* Move `SetUpLocalDefaultCharts`, `AddDefaultChart`, and related default-selection logic out of `ChartView`.
* Parent pages should explicitly request the correct default chart state.
* Remove `Location == null` page-type inference.

#### Phase 4: Move page orchestration to parent pages

* Location page prepares local chart state.
* Regional/global page prepares regional chart state.
* `ChartView` receives prepared chart state.
* Reduce or remove `PageName`, `Location`, `LocationDictionary`, and `Regions` parameters where possible.

#### Phase 5: Simplify `ChartView` public API

Replace broad page-aware parameters with narrower chart-specific inputs.

Possible end-state shape:

```csharp
[Parameter]
public ChartViewModel? Chart { get; set; }

[Parameter]
public IReadOnlyList<DataSetDefinitionViewModel> AvailableDataSets { get; set; } = [];

[Parameter]
public ChartDisplayOptions DisplayOptions { get; set; } = new();

[Parameter]
public EventCallback<ChartState> ChartStateChanged { get; set; }

[Parameter]
public EventCallback<ChartDownloadRequest> DownloadRequested { get; set; }

[Parameter]
public EventCallback AddDataSetRequested { get; set; }

[Parameter]
public EventCallback<ChartMessage> MessageRaised { get; set; }
```

This is illustrative. Recommend the actual final API after reviewing current usage.

#### Phase 6: Reduce code-behind size

After responsibilities are extracted:

* keep rendering-related logic in `ChartView`;
* move chart calculations into testable helpers/services;
* move URL/default/page logic out;
* consider splitting child components if the Razor markup is also too large.

### 8. Testing requirements

Identify existing behaviours that must be preserved.

At minimum, cover:

* location page loads chart from URL when valid URL chart state exists;
* location page falls back to local default chart when URL chart state is missing;
* regional/global page loads chart from URL when valid URL chart state exists;
* regional/global page falls back to regional default chart when URL chart state is missing;
* invalid URL chart state behaves the same as today;
* `ChartAllData` still works;
* add dataset modal still works;
* download data still works;
* snackbar/user messages still appear where currently expected;
* Blazor Server and WASM behaviour remain consistent.

### 9. Deliverables

Produce a short design document containing:

1. Summary of current problems.
2. Method inventory table.
3. Current `OnAfterRenderAsync` flow.
4. Parameter responsibility analysis.
5. List of responsibilities that should move out of `ChartView`.
6. Proposed target architecture.
7. Proposed new or changed services/types/components.
8. Staged refactor plan.
9. Testing plan.
10. Risks and open questions.

### Acceptance criteria

This task is complete when:

* every method and event handler in `ChartView` has been reviewed and classified;
* every parameter has a recommendation: keep, remove, replace, or move upstream;
* the current lifecycle/default chart setup flow is documented;
* regional/global page coupling is explicitly identified;
* `Location == null` page inference is addressed;
* a staged refactor plan exists;
* current behaviour is preserved;
* no large refactor is performed before the investigation is complete.