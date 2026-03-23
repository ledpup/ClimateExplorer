# ClimateExplorer — Context Pack

**Purpose:** New chat session handoff and developer documentation
**Repo:** https://github.com/ledpup/ClimateExplorer · branch `development`
**Stack:** Blazor WebAssembly · Blazor Server · .NET 10 · C# 14 · Blazorise (Bootstrap 5) · Font Awesome 6

---

## Solution Structure

| Project | Role |
|---|---|
| `ClimateExplorer.Web.Client` | **Blazor WASM frontend** — all pages, components, UI logic, CSS |
| `ClimateExplorer.Web` | ASP.NET Core host for the WASM app; `wwwroot/css/app.css` global styles; blog static content |
| `ClimateExplorer.WebApi` | Minimal API backend; serves dataset, location, region, and climate-record data; file-backed two-layer cache |
| `ClimateExplorer.WebApiClient` | `IDataService` / `DataService` / `DataServiceCache` — typed HTTP client used by the WASM app |
| `ClimateExplorer.Core` | All shared domain models, enums, data-prep pipeline, math helpers (no UI dependencies) |
| `ClimateExplorer.SourceData` | Raw data file infrastructure |
| `ClimateExplorer.DataPipeline` | Orchestrates data ingestion tools — **avoid building frequently**: its build steps create zip files that are slow to create |
| `ClimateExplorer.Data.*` (6 projects) | Data importers: BOM, GHCND, GHCNM, ISD, CO2/misc |
| `ClimateExplorer.CachingTool` | CLI tool to pre-warm the API cache |
| `ClimateExplorer.UnitTests` | xUnit tests for Core maths, bin identifiers, data reading |
| `ClimateExplorer.Web.UiTests` | UI / integration tests |
| ~~Archived/*~~ | **Excluded** — two deprecated projects, not in active use |

---

## Pages

Both pages inherit `ChartablePage` (abstract `ComponentBase`).

### `Index` — `/` · `/location/` · `/location/{locationString}`
- Shows a leaflet map (`MapContainer`) + `LocationInfo` side panel
- `LocationInfo` is wrapped in a `Collapsible` (XL collapser) showing warming anomaly, heating score, record high, climate stripes
- Below: `ChartView` with full chart controls
- Has `SuggestedCharts` (full) and `SuggestedChartsMini` (collapsed header) preset selector
- Uses `Blazored.LocalStorage` for site-overview panel dismissal tracking

### `RegionalAndGlobal` — `/regionalandglobal`
- No map, no `LocationInfo`
- `SuggestedCharts` (full, in a `Collapsible`) + `ChartView`

### `ChartablePage` (abstract base)
Provides: `DataService`, `NavManager`, `Exporter`, `JsRuntime`, `Logger`, `DataSetDefinitions`, `LocationDictionary`, `Regions`, `ChartView` ref, `AddDataSetModal`, `Snackbar`, `PageName`, navigation-changed event handling, and the shared `OnInitializedAsync` data-loading sequence.

---

## Component Hierarchy

```
ChartablePage (Index / RegionalAndGlobal)
├── SuggestedCharts / SuggestedChartsMini
├── MapContainer              (Index only)
├── LocationInfo              (Index only)
│   ├── OverviewField (×n)
│   ├── ClimateStripe (×2)
│   └── ExtremeYears (×2)
└── ChartView
    ├── Collapsible ("Chart", AllowCollapse=false)
    ├── ChartSeriesListView
    │   └── ChartSeriesView (×n)
    ├── Chart<double?> + ChartTrendline<double?>  (Blazorise.Charts)
    ├── chart-controls bar
    │   ├── "Chart all data" anchor
    │   ├── "Clear filter" anchor (conditional)
    │   ├── Dropdown ("Grouping")  — Yearly / Monthly / Daily
    │   ├── ChartAxisListView      — per-axis "Scale from 0" toggles
    │   ├── "Aggregation options" anchor → OptionsModal
    │   ├── info-icon (fa-circle-info)
    │   └── "Download" anchor     — margin-left:auto, flush right
    ├── OptionsModal (Blazorise Modal)
    ├── InfoPanel (chartOptionsInfoPanel)
    └── InfoPanel (aggregationOptionsInfoPanel)
```

---

## Key Components

### `ChartView` (`ChartView.razor` + `.razor.cs` + `.razor.css`)
The central component. All chart state lives here.

**State properties:**

| Property | Type | Default | Purpose |
|---|---|---|---|
| `ChartSeriesList` | `List<ChartSeriesDefinition>` | `[]` | Active series |
| `ChartSeriesWithData` | `List<SeriesWithData>` | null | Fetched + processed data |
| `ChartBins` | `BinIdentifier[]` | null | X-axis bin labels |
| `SelectedBinGranularity` | `BinGranularities` | `ByYear` | Active granularity |
| `CurrentAxes` | `List<AxisInfo>` | `[]` | Active y-axes (id + label) |
| `AxesScaleToZero` | `Dictionary<string, bool>` | `{}` | Per-axis zero-floor toggle |
| `SelectedStartYear` / `SelectedEndYear` | `string?` | null | Year range filter |
| `SelectedGroupingDays` | `short` | 14 | Grouping bin size |
| `InternalGroupingThreshold` | `float` | 0.7 | Data completeness threshold |
| `ChartAllData` | `bool` | (parameter) | Show full date range |
| `IsMobileDevice` | `bool?` | null (lazy) | From `ICurrentDeviceService` |

**URL state** — all chart state is persisted in the query string:
```
?chartAllData=false
&startYear=1950&endYear=2024
&groupingDays=14&groupingThreshold=70
&axisScaleToZero=y1,y2
&csd=<ChartSeriesListSerializer encoded string>
```
- `GetGlobalQueryStringSettings()` → builds URL
- `UpdateUiStateBasedOnQueryString(uri)` → parses it
- `ChartSeriesListSerializer` → encodes/decodes `csd`

**Rendering pipeline:**
```
BuildDataSets()
  → if URL changed: NavigateTo() (triggers re-entry via OnAfterRenderAsync)
  → else: RetrieveDataSets()
           → BuildProcessedDataSets()
           → RenderChart()
                → AddDataSetsToChart()
                → BuildChartScales()
```

---

### `ChartSeriesView` (`.razor` + `.razor.cs` + `.razor.css`)
Collapsible "lozenge" card per series. Title bar shows short title + description + trash icon + chevron. Expanded panel is `position: absolute`, `z-index: 100`, floats over content, min-width 640px. Two-column edit grid with `Select` controls for: Aggregation, Secondary Calculation, Smoothing, Smoothing Window, Display (Value/Anomaly), Transformation, Display Style (Line/Bar). Series controls row: Lock, Duplicate, Show Trendline.

---

### `Collapsible` (`.razor` + `.razor.css`)
Custom collapsible panel with Open Iconic chevron. Parameters:

| Parameter | Type | Notes |
|---|---|---|
| `Title` / `FullTitle` | `string?` | FullTitle shown on hover tooltip |
| `AllowCollapse` | `bool` | false = always open |
| `InitiallyShown` | `bool` | — |
| `ShowTitleWhenExpanded` | `bool` | — |
| `ContentLayoutType` | `CollapserContentLayoutTypes` | Block / FlexboxColumns / FlexboxRow |
| `CollapserSize` | `CollapserSizes` | Normal / Large / ExtraLarge |
| `ShadeBackground` | `bool` | Adds `#425f59` left-border accent |
| `NoBottomMargin` | `bool` | Suppresses bottom margin |
| `CollapsedContent` | `RenderFragment?` | Shown only when collapsed |

---

### `InfoPanel` (`.razor` + `.razor.cs` + `.razor.css`)
Slide-up overlay panel. Fixed position, bottom-anchored, dark overlay backdrop. Animated in/out with `slide-in`/`slide-out` CSS classes (`transform: translateY`). `PanelName` + `Version` used by `IInfoPanelDismissalService` to track "don't show again". `Height` overrides panel height on desktop only.

---

### `SuggestedCharts` / `SuggestedChartsMini`
Flex-wrap card grid of preset chart configurations. Each card: title, description, optional chevron for variants dropdown (positioned absolutely, `z-index: 6`, `modal-underlay` behind it). On mobile `Index` uses `SuggestedChartsMini`. Presets defined in `SuggestedPresetLists.cs`.

---

### `OverviewField` (`.razor` + `.razor.css`)
Inline-block data field: small grey label above bold large value. Optional `PopupContent` renders a Blazorise `Modal` (with `custom-modal-header`) on click.

---

### `ChartAxisListView` (`.razor` + `.razor.css`)
Dropdown in `chart-controls` for per-axis "Scale from 0" toggles.
Parameters: `List<AxisInfo>? Axes`, `Dictionary<string, bool>? ScaleToZero`, `EventCallback OnChanged`.
Uses `display: contents` wrapper pattern for CSS isolation (see CSS Standards below).

---

## UI Design Standards

### Colour Palette

| Role | Hex | Usage |
|---|---|---|
| Primary green | `#425f59` | Modal headers, `Collapsible` left-border, collapser active, button text |
| Secondary text | `#595959` | Body text, descriptions, labels |
| Data value text | `#494949` | `OverviewField` values |
| Light background | `#f8f8f8` | Button bg, expanded-series config bg, suggestion cards |
| **Hover green** | `#cfc` | Hover state for **all** interactive elements |
| Info blue | `#79c6f4` | `fa-circle-info` info icons |
| Link blue | `#7DB0DF` | Links in location header |
| Danger red | `#a00000` | Trash / delete icons |
| White | `#ffffff` | Card backgrounds, modal body |
| Additional info | `#50c880` | `OverviewField .additional-info` |

### Typography
- Font family: `'Helvetica Neue', Helvetica, Arial, sans-serif` (global, `app.css`)
- Body: browser default
- Small / secondary: `0.75rem`, `0.8rem`, `0.85rem`
- Overview values: `1.25rem`, bold
- Dropdown section labels: `0.75rem`, `font-weight: 600`, uppercase, `letter-spacing: 0.04em`, `color: #6c757d`

### Spacing & Shape

| Token | Value |
|---|---|
| Border radius — controls / buttons | `8px` |
| Border radius — cards / suggestion tiles | `12px` |
| Border radius — modals / InfoPanel | `16px` |
| Shadow — controls | `rgb(0 0 0 / 20%) 0px 0px 4px` |
| Shadow — series cards | `rgb(0 0 0 / 20%) 0px 0px 6px` |
| Shadow — expanded series config | `rgba(0,0,0,0.3) 4px 6px 16px` |
| Hover transition | `background-color 0.25s` |
| Minimum touch target height | `44px` |

### `chart-controls` Bar

Flexbox row, `flex-wrap: wrap`, `margin: 20px 0 -16px 0` (negative bottom compensates for last-row child margin).

- **`<a class="chart-control">`** — `padding: 8px`, `#f8f8f8` bg, `#425f59` text, `8px` radius, `margin-right: 16px`, `margin-bottom: 16px`, light shadow. Icons inside get `margin-right: 8px`.
- **Blazorise `<Dropdown>`** — styled via `::deep .chart-controls .dropdown` to match `chart-control` exactly: same padding, bg, colour, radius, shadow. Icon inside `.btn` gets `margin-right: 8px`.
- **Download button** — `chart-control chart-control-download`: `margin-left: auto`, `margin-right: 0` → always flush to right edge. Rule must appear **after** `.chart-control` in the stylesheet to win the cascade.
- **Info icon** — `color: #79c6f4`, `font-size: larger`, inline, `margin-left: 8px`.

### Modal Headers
All modals use `Class="custom-modal-header"` on `<ModalHeader>`:
- Background: `#425f59`, text: white
- Close button: `filter: invert(1) grayscale(100%) brightness(200%)`

### CSS Isolation (Blazor)
- Every component has a co-located `.razor.css` file.
- Use `::deep` to reach into Blazorise child component DOM.
- **Root-element isolation problem:** If a component's root is a Blazorise component (not a plain HTML element), Blazor cannot stamp the CSS scope attribute, so `::deep` rules silently do nothing.
- **Fix:** Wrap in a plain `<div>` with `display: contents` as the new outer root. This is layout-transparent (invisible to flexbox/grid) but gives Blazor a native element to stamp. Example: `ChartAxisListView` uses `<div class="axes-dropdown-wrapper">` with `display: contents`.

### Responsive Breakpoints
- **Mobile** (`max-width: 1024px`): map 250px tall, stacks vertically, chart height 30vh min 300px.
- **Desktop** (`min-width: 1025px`): map positioned absolute right of location info, chart height 50vh, collapsible panel `min-height: 600px`.

---

## Data Layer

### Core Domain Models

| Type | Key Properties | Notes |
|---|---|---|
| `Location` | `Id`, `Name`, `FullTitle`, `HeatingScore`, `RecordHigh` | Geographical measurement station |
| `Region` | `Id`, `Name` | Non-station entity (Atmosphere, Ocean, Arctic…) |
| `DataSetDefinition` | `Id`, `MeasurementDefinitions` | What data exists for a source |
| `MeasurementDefinition` | `DataType`, `DataAdjustment`, `DataResolution`, `UnitOfMeasure` | One measurement within a dataset |
| `DataSet` | `GeographicalEntity`, `MeasurementDefinition`, `DataRecords` | API response |
| `BinnedRecord` | `BinId`, `Value?` | Atomic chart data point |
| `BinIdentifier` | `Id`, `Label` | Identifies one x-axis bin; subtypes: `YearBinIdentifier`, `MonthOnlyBinIdentifier`, etc. |
| `AxisInfo` | `Id`, `Label` | Record type in `Web.Client.UiModel`; represents one y-axis |

### Key Enums (`ClimateExplorer.Core.Enums`)

| Enum | Values (selected) |
|---|---|
| `DataType` | TempMax, TempMin, TempMean, Precipitation, CO2, CH4, N2O, SeaIceExtent, SeaLevel, … (30+) |
| `UnitOfMeasure` | DegreesCelsius, DegreesCelsiusAnomaly, Millimetres, PartsPerMillion, MillionSqKm, … |
| `DataAdjustment` | Unadjusted, Adjusted, Difference |
| `DataResolution` | Yearly, Monthly, Weekly, Daily |
| `BinGranularities` | ByYear, ByYearAndMonth, ByYearAndWeek, ByYearAndDay, ByMonthOnly, ByDayOnly, BySouthernHemisphere*Season* |
| `SeriesAggregationOptions` | Mean, Maximum, Minimum, Sum, Median |
| `SeriesValueOptions` | Value, Anomaly |
| `SeriesTransformations` | Identity, DayOfYearIfFrost, Custom, … |
| `SeriesDerivationTypes` | ReturnSingleSeries, … |

**Linear vs modular granularities:**
- **Linear** (ByYear, ByYearAndMonth, …): gapless x-axis; supports moving-average smoothing.
- **Modular** (ByMonthOnly, ByDayOnly, BySouthernHemisphere*): cyclic/wrapped x-axis; smoothing skipped.

## Services (`Web.Client`)

| Interface | Implementation | Lifetime | Purpose |
|---|---|---|---|
| `IDataService` | `DataService` | Scoped (`HttpClient`) | All API calls |
| `IDataServiceCache` | `DataServiceCache` | Singleton | In-memory API response cache |
| `IExporter` | `Exporter` | Transient | CSV download |
| `IInfoPanelDismissalService` | `InfoPanelDismissalService` | Scoped | "Don't show again" tracking for `InfoPanel` |
| `ISiteOverviewService` | `SiteOverviewService` | Scoped | Triggers site-overview panel |
| `ICurrentDeviceService` | (CurrentDevice lib) | Scoped | Mobile/desktop detection |
| `Blazored.LocalStorage` | — | Scoped | Persists user preferences across sessions |

---

## Patterns & Conventions

| Pattern | Detail |
|---|---|
| **No `async void`** | All event handlers return `Task` |
| **`LogAugmenter`** | Wraps `ILogger` with method-name prefix; used in all significant methods |
| **Re-entrancy guards** | `buildDataSetsInProcess` and `updateUiStateInProcess` bool fields prevent concurrent execution of their respective flows |
| **`@key` on foreach** | `ChartSeriesDefinition.Id` (Guid) used as `@key` for stable component identity across re-renders |
| **`CreateNewListWithoutDuplicates()`** | Extension method called after mutations to `ChartSeriesList` |
| **`DataSubstitute` chains** | Defines fallback matching for data lookup: `StandardTemperatureDataMatches()` tries TempMax adjusted → TempMean → TempMax unadjusted |
| **`SeriesWithData` pipeline** | Three dataset stages: `SourceDataSet` (raw from API) → `PreProcessedDataSet` (after moving average) → `ProcessedDataSet` (gap-filled, display-range filtered) |
| **C# 14 collection expressions** | Used throughout: `List<T> x = []`, `T[] arr = [a, b, c]` |
| **Tuple returns over `out` params** | Preferred for methods returning multiple values |
| **`::deep` always required** | For styling Blazorise child elements from a parent component's `.razor.css` |
| **`display: contents` isolation wrapper** | Required when a component's root is a Blazorise component — see CSS Isolation section |