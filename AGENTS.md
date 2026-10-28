# Agent Notes

- A plan or analysis document goes in docs/design. See [readme](docs/design/README.md)
- Do not run the website, Playwright, Lighthouse, or browser tests. This includes starting
  `dotnet run`/dev servers for the Web or WebApi projects for manual or automated UI verification.
  Verify with `dotnet build` and the unit test projects only (`ClimateExplorer.Core.UnitTests`,
  `ClimateExplorer.WebApi.UnitTests`, `ClimateExplorer.Web.UnitTests`).
- If a build fails because IIS Express (or another dev server the user started) has locked files in a `bin`
  folder, you may stop that process and rebuild.
- Don't run `ClimateExplorer.DataPipeline` unless asked. It rebuilds `ClimateExplorer.WebApi/Datasets` from
  `ClimateExplorer.SourceData`. Building it is fine (`ClimateExplorer.WebApi.UnitTests` references it).
- When estimating effort, don't give time estimates (hours/days). Report how many files would
  change and by roughly how much (e.g. "3 files, ~40 lines; 1 new file").

## Solution

| Project | Role |
|---|---|
| `ClimateExplorer.Web.Client` | Blazor WebAssembly app: pages, components, UI services, scoped CSS |
| `ClimateExplorer.Web` | Blazor Server host. Global CSS in `wwwroot/css` (`base.css`, `app.css`, `table.css`, `utilities.css`); blog content |
| `ClimateExplorer.WebApi` | Minimal API serving datasets, locations, regions and climate records from `Datasets`; file-backed cache |
| `ClimateExplorer.WebApiClient` | `IDataService`/`DataService`, the typed HTTP client; caches responses in `IMemoryCache` |
| `ClimateExplorer.Core` | Domain models, enums, data preparation (binning, aggregation), stats. No UI dependencies |
| `ClimateExplorer.SourceData` | Raw source data files |
| `ClimateExplorer.DataPipeline` | Packages `SourceData` into `WebApi/Datasets` (see above) |
| `ClimateExplorer.Data.*` | Downloaders and importers (BOM, GHCNd, GHCNm, misc global series) |
| `ClimateExplorer.CachingTool` | Pre-warms the API cache |
| `ClimateExplorer.*.UnitTests` | MSTest. `Web.UnitTests` also scans source (unit text, CSS class usage) |
| `ClimateExplorer.Web.UiTests` | Playwright tests (don't run) |

## Front end

Pages: `Index` (`/`, `/location/{locationString}`: map, `LocationDashboard`, chart) and `Global` (`/global`: chart
only) both inherit `ChartablePage`. Also `Locations`, `About` and `Blog`. Components are grouped by feature under
`Components/` (`Chart`, `Location`, `RecentObservations`, `ClimateRecords`, `Common`, ...).

Chart:
- `ChartablePage` owns the current `ChartState` and the built data. `ChartView` only renders what it's given and raises
  `ChartStateChanged`; it doesn't fetch.
- `ApplyChartStateAsync` writes the state to the URL, then builds it with `ChartDataBuilder.BuildAsync`. A build-version
  counter drops results from superseded builds. Navigating to the URL doesn't trigger the render (same-path `replace`
  navigation is unreliable when deployed).
- The query string holds the whole chart: `chartAllData`, `startYear`, `endYear`, `groupingDays`, `groupingThreshold`,
  `axisScaleToZero` and `csd` (the series list, via `ChartSeriesListSerializer`). `ChartStateUrlService` parses and
  builds it; back/forward re-applies it (`SyncChartStateFromUrlAsync`).
- `SeriesWithData` holds three stages: `SourceDataSet` (from the API) → `PreProcessedDataSet` (smoothed) →
  `ProcessedDataSet` (gap-filled, cut to the display range).
- Data is built in metric. `ChartView` converts for display through `UnitPreference`.
- Bin granularities are linear (`IsLinear()`: by year, year and month, ...; smoothing applies) or modular (month only,
  day only, seasons; a wrapped x-axis, no smoothing).
- On a location change, `ChartSeriesLocationSubstitutionService` moves unlocked series to the new location and adds
  an unlocked copy of each locked series. Call `CreateNewListWithoutDuplicates()` after changing a series list.
- `DataSubstitute.StandardTemperatureDataMatches()` is the temperature fallback: TempMean adjusted → TempMax adjusted →
  TempMax unadjusted.

## C#

- Unit test names use `MethodName_StateUnderTest_ExpectedBehavior`
- No `async void`; event handlers return `Task`
- Use collection expressions (`List<T> x = []`), and return tuples rather than use `out` parameters

## Blazor

- Dependency injection for Blazor components go in Blazor Server `ClimateExplorer.Web` and Blazor WebAssembly `ClimateExplorer.Web.Client` projects
  (both `Program.cs` files register the same list)
- Razor components keep C# in `.razor.cs` code-behind files
- Supported screen sizes:
  - Mobile: max-width = 767px
  - Tablet: min-width = 768px and max-width = 1024px
  - Fullscreen: min-width = 1025px
- UI controls must have an accessible name
- Smallest allowed `font-size` for any label/text is `0.75rem` (12px). Don't use keyword sizes like
  `x-small`/`smaller`/`xx-small`, or px/rem/em values below that, even for secondary/caption text.
- Utility classes are limited to those in `utilities.css`; prefer component-scoped CSS, and don't add new
  utilities without a reason.
- `CssClassUsageTests` fails on a CSS class selector that no markup or code uses, and on a scoped `.razor.css`
  selector that styles a child component's markup (including a class passed through its `Class` parameter)
  without `::deep`. Delete the rule or fix the selector; a class built at runtime goes in its allow-list.

Use the following existing common components, when appriopriate:
- Autocomplete (text box with suggestions; needs an `AriaLabel`, or a `<label for>` pointing at its `Id`)
- Button (`<Button>` is the component and `<button>` the HTML element; Razor matches tags case-sensitively, so
  don't "fix" one into the other)
- Checkbox
- Collapsible
- DropdownButton / DropdownItem (the menu is a native `popover`)
- InfoPanel
- LoadingIndicator
  - Overlay (default): wraps existing content that stays visible underneath while it refreshes (chart, tables,
    dashboard fields).
  - Inline (`Variant="LoadingIndicatorVariant.Inline"`): sits where content will go when there's nothing to show yet
    (first load of a panel or list).
- Modal (`Title` or `TitleContent`; open with `ShowAsync()` or `@bind-Visible`; the body only renders while open)
- OverviewField
- PaginationControl
- Select / SelectItem (needs a `<label for>` pointing at its `Id`, or an `aria-label`)
- SidePanel
- Tabs / Tab (needs an `AriaLabel`; it renders the tab strip only, and the parent renders the selected content)
- TextInput (fires on change, not per keystroke; the parent validates and passes `Invalid`/`ErrorMessage`)
- Tooltip

Tables are plain `<table class="table">`, styled globally in `table.css` (modifiers `table--striped`, `table--compact`).

The common components are built on native browser features, not a component library:
- `Modal` is a `<dialog>` opened with `showModal()` (`Modal.razor.js`). The page behind doesn't scroll
  (`html:has(dialog.modal[open])` in `app.css`).
- `DropdownButton` (`popover="auto"`) and `Tooltip` (manual) are popovers, so they sit in the top layer: never clipped
  by `overflow: hidden`, and shown above an open `<dialog>`.
- `wwwroot/js/floating.js` positions `DropdownButton`, `Autocomplete` and tooltip popups next to their anchor, flipping
  when there's no room.
- `wwwroot/js/tooltip.js` is one shared tooltip for every `[data-tooltip]` element, by event delegation. It's loaded
  once from `App.razor`, so it works during prerender.

Common inputs (`Select`, `TextInput`, `Checkbox`) bind with `Value`/`ValueChanged` (not `Checked`, `Text`, `SelectedValue`, etc.).
Style a child component's markup with `::deep`. Blazor only stamps the scope attribute on elements in the component's
own markup, so if a component's root is another component, wrap it in a `<div>` with `display: contents` for the
`::deep` rule to anchor to.

### Colours

Use the `--climate-*` variables in `app.css` where one exists.

| Role | Hex |
|---|---|
| Primary green (text, modal headers, accents) | `#425f59` |
| Hover (all interactive elements) | `#cfc` |
| Info icons (`fa-circle-info`) | `#79c6f4` |
| Danger (trash/delete) | `#a00000` |
| Control background | `#f8f8f8` |
| Secondary text | `#595959` |

Border radius: controls 8px, cards 12px, modals/InfoPanel 16px.

Target size: 44px minimum height for touch (`@media (pointer: coarse)`, WCAG 2.5.5 AAA). With a mouse/trackpad,
controls may be compact, but never below 24px (WCAG 2.5.8 AA). `Select` and `TextInput` already do this via
`.control` in `app.css`.

## Chart.js

Chart.js is vendored in `ClimateExplorer.Web.Client/wwwroot/lib/chartjs` (4.6.1). The `seriesNote` plugin in
`ChartJsChart.razor.js` wraps the private `Legend._fitRows`; when upgrading Chart.js, check it still exists and
behaves the same.

## Font Awesome

Font Awesome Free 6.7.2 is vendored in `ClimateExplorer.Web.Client/wwwroot/lib/fontawesome`, solid style only
(`fas`/`fa-solid`). Using a `far`/`fab` icon means vendoring that style's CSS and font too. It is the only icon font;
don't add Open Iconic or other icon sets.