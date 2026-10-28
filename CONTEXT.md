# ClimateExplorer — Context Pack

**Purpose:** New chat session handoff and developer documentation
**Repo:** https://github.com/ledpup/ClimateExplorer · branch `development`
**Stack:** Blazor Server · Blazor WebAssembly · .NET 10 · C# 14 · Chart.js 4 (vendored) · Font Awesome 6 (vendored, solid) · no UI component library

---

## Solution Structure

| Project | Role |
|---|---|
| `ClimateExplorer.Web.Client` | **Blazor WASM frontend** — all pages, components, UI logic, CSS |
| `ClimateExplorer.Web` | Blazor Server; ASP.NET Core host for the WASM app; `wwwroot/css/app.css` global styles; blog static content |
| `ClimateExplorer.WebApi` | Minimal API backend; serves dataset, location, region, and climate-record data; file-backed two-layer cache |
| `ClimateExplorer.WebApiClient` | `IDataService` / `DataService` (caches responses via injected `IMemoryCache`) — typed HTTP client used by the WASM app |
| `ClimateExplorer.Core` | All shared domain models, enums, data-prep pipeline, math helpers (no UI dependencies) |
| `ClimateExplorer.SourceData` | Raw data file infrastructure |
| `ClimateExplorer.DataPipeline` | Console app that packages `ClimateExplorer.SourceData` into the zips and files the API serves, and replaces `ClimateExplorer.WebApi/Datasets` with them. It copies the BOM and GHCNd zips, builds the ACORN-SAT and GHCNm archives, and validates the layout in a staging folder before swapping it in |
| `ClimateExplorer.Data.*` (6 projects) | Data importers: BOM, GHCND, GHCNM, ISD, CO2/misc |
| `ClimateExplorer.CachingTool` | CLI tool to pre-warm the API cache |
| `ClimateExplorer.Core.UnitTests` | MSTest tests for Core: maths, bin identifiers, data reading, smoothing, trends |
| `ClimateExplorer.WebApi.UnitTests` | MSTest tests for WebApi and the data projects (downloaders, AcornSat, endpoints) |
| `ClimateExplorer.Web.UnitTests` | MSTest tests for Web.Client, plus source-scanning checks (unit text, CSS class usage) |
| `ClimateExplorer.Web.UiTests` | UI / integration tests |

---

## Pages

Both pages inherit `ChartablePage` (abstract `ComponentBase`).

### `Index` — `/` · `/location/` · `/location/{locationString}`
- Shows a leaflet map (`MapContainer`) + `LocationInfo` side panel
- `LocationInfo` is wrapped in a `Collapsible` (XL collapser) showing warming anomaly, heating score, record high, climate stripes
- Below: `ChartView` with full chart controls
- Has `SuggestedCharts` (full) and `SuggestedChartsMini` (collapsed header) preset selector
- Uses `Blazored.LocalStorage` for site-overview panel dismissal tracking

### `Global` — `/global`
- No map, no `LocationInfo`
- `SuggestedCharts` (full, in a `Collapsible`) + `ChartView`

### `ChartablePage` (abstract base)
Provides: `DataService`, `NavManager`, `Exporter`, `JsRuntime`, `Logger`, `DataSetDefinitions`, `LocationDictionary`, `Regions`, `ChartView` ref, `AddDataSetModal`, `PageName`, navigation-changed event handling, and the shared `OnInitializedAsync` data-loading sequence.

---

## Component Hierarchy

```
ChartablePage (Index / Global)
├── SuggestedCharts / SuggestedChartsMini
├── MapContainer              (Index only)
├── LocationInfo              (Index only)
│   ├── OverviewField (×n)
│   ├── ClimateStripe (×2)
│   ├── ExtremeYears (×2)
│   └── ClimateRecords
└── ChartView
    ├── Collapsible ("Chart", AllowCollapse=false)
    ├── ChartSeriesListView
    │   └── ChartSeriesView (×n)
    ├── ChartJsChart                (Chart.js; interop in ChartJsChart.razor.js)
    ├── chart-controls bar
    │   ├── "Chart all data" Button
    │   ├── "Clear filter" Button (conditional)
    │   ├── DropdownButton ("Grouping") — Year / Year and month / Month / Day
    │   ├── ChartAxisListView      — per-axis "Scale from 0" toggles
    │   ├── AggregationOptions     — Button opening a panel: grouping days, threshold %, override, reset
    │   ├── info-icon (fa-circle-info)
    │   └── "Download" Button      — margin-left:auto, flush right (not on mobile)
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
Collapsible "lozenge" card per series. Title bar shows short title + description + info icon (opens `AboutData`) + trash icon + chevron. Expanded panel is `position: absolute`, `z-index: 100`, floats over content, min-width 640px. Two-column edit grid with `Select` controls for: Aggregation, Secondary Calculation, Smoothing, Smoothing Window, Display (Value/Anomaly), Transformation, Display Style (Line/Bar). Series controls row: Lock, Duplicate, Show Trendline.

---

### `Collapsible` (`.razor` + `.razor.css`)
Custom collapsible panel with a Font Awesome chevron. Parameters:

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
Inline-block data field: small grey label above bold large value. Optional `PopupContent` opens in a `Modal` on click.

---

### `ChartAxisListView` (`.razor` + `.razor.css`)
Dropdown in `chart-controls` for per-axis "Scale from 0" toggles.
Parameters: `List<AxisInfo>? Axes`, `Dictionary<string, bool>? ScaleToZero`, `EventCallback OnChanged`.
A `DropdownButton` whose menu holds one `Checkbox` per axis.

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
- Dropdown section labels (`.dropdown-header`): `0.875rem`, `#595959`

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

Flexbox row, `flex-wrap: wrap`, `gap: 16px`, `margin: 20px 0 4px`.

- **Controls** — `Button` and `DropdownButton` common components in their default look (`#f8f8f8` bg, `#425f59` text,
  `8px` radius, light shadow); the bar adds no styling of its own beyond layout.
- **Download button** — wrapped in `<span class="chart-download-control">` with `margin-left: auto`, so it sits flush
  right. On mobile the margin is `0` and it wraps with the rest.
- **Info icon** — `color: #79c6f4`, `font-size: larger`, `display: flex`, `align-items: center`.

### Modals
All modals use the common `Modal` component (a native `<dialog>` opened with `showModal()`), which draws the header:
- Background: `#425f59`, title (`<h2>`, `1.25rem`) and close button white; the close button's hover is `#cfc`
- Tablet and desktop: the whole modal scrolls in the overlay. Mobile: the modal fits the screen and only its body scrolls
- The page behind doesn't scroll (`html:has(dialog.modal[open])` in `app.css`)

### CSS Isolation (Blazor)
- Every component has a co-located `.razor.css` file.
- Use `::deep` to style a child component's markup, including a class passed through its `Class` parameter
  (`CssClassUsageTests` enforces this).
- **Root-element problem:** Blazor stamps the scope attribute only on elements in the component's own markup. If the
  root is another component, there's no stamped ancestor, so `::deep` rules silently do nothing.
- **Fix:** Wrap in a plain `<div>` with `display: contents` as the new outer root. It's layout-transparent but gives
  Blazor an element to stamp. Examples: `AggregationOptions` (`aggregation-options-wrapper`) and `Top100`
  (`top100-download-dropdown-wrapper`).

### Responsive Layout
Screen-size breakpoints are defined in [AGENTS.md](AGENTS.md).
- **Mobile and tablet**: map 250px tall, stacks vertically, chart height 30vh min 300px.
- **Fullscreen**: map positioned absolute right of location info, chart height 50vh, collapsible panel `min-height: 600px`.

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
| `IDataService` | `DataService` | Scoped (`HttpClient`) | All API calls; caches responses via injected `IMemoryCache` (registered singleton, size-limited to `DataService.CacheSizeLimit`) |
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
| **`SeriesWithData` pipeline** | Three dataset stages: `SourceDataSet` (raw from API) → `PreProcessedDataSet` (after smoothing) → `ProcessedDataSet` (gap-filled, display-range filtered) |
| **C# 14 collection expressions** | Used throughout: `List<T> x = []`, `T[] arr = [a, b, c]` |
| **Tuple returns over `out` params** | Preferred for methods returning multiple values |
| **`::deep` always required** | For styling a child component's markup from a parent component's `.razor.css` |
| **`display: contents` isolation wrapper** | Required when a component's root is another component — see CSS Isolation section |

---

## Common Components

The UI uses no component library. The shared components live in `Components/Common` (listed in
[AGENTS.md](AGENTS.md)) and are built on native browser features:

| Primitive | Used by | Notes |
|---|---|---|
| `<dialog>` + `showModal()` | `Modal` | Top layer, inert page behind, Escape to close. `Modal.razor.js` opens and closes it |
| `popover` | `DropdownButton` (`popover="auto"`), `Tooltip` (manual) | Top layer, so never clipped by `overflow: hidden` and shown above an open `<dialog>` |
| `wwwroot/js/floating.js` | `DropdownButton`, `Autocomplete`, `tooltip.js` | Positions a floating element next to its anchor and flips it when there's no room |
| `wwwroot/js/tooltip.js` | `Tooltip` | One shared tooltip for every `[data-tooltip]` element, by event delegation. Loaded once from `App.razor`, so it works during prerender |

Inputs (`Select`, `TextInput`, `Checkbox`) bind with `Value` / `ValueChanged`, not `Checked`, `Text` or
`SelectedValue`.