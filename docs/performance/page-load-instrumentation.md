# Page-load performance instrumentation

Issue #647 adds lightweight diagnostics rather than a telemetry provider. Timings appear in two places:

- Browser DevTools console and Performance entries with the `ClimateExplorer:` prefix.
- Application logs with `PerfStart`, `PerfEnd`, and `PerfApiClient` entries.

## What is instrumented

- Initial location page lifecycle (`Index.OnInitializedAsync`, `Index.OnAfterRenderAsync`).
- Reference data loading for dataset definitions, location dictionary, and regions.
- Location dashboard data loading, including anomaly, climate stripe, and recent-observation support paths.
- Chart dataset preparation and rendering.
- API client GET calls, including endpoint, cached/uncached state, elapsed milliseconds, response type, and record counts where available.

## How to compare timings locally

1. Run the Web and WebApi projects locally.
2. Open DevTools and keep the Console and Performance tabs open.
3. Load `/`, `/location/{id}`, and a regional/global page once with cache disabled, then again with normal cache enabled.
4. Compare `ClimateExplorer:measure` browser timings and `PerfApiClient` log lines between cold and warm loads.

## Suspicious paths to watch first

- The first `Index.LoadReferenceData` block, because it downloads the location dictionary and all dataset definitions before the default location dashboard can be fully selected.
- `LocationDashboard.OnAfterRenderAsync`, because it intentionally defers anomaly/stripe work until after first render but still controls perceived dashboard completion.
- Chart `PostDataSet` requests triggered from `ChartView.BuildDataSets`, because chart rendering waits on all selected series data.
- Repeated `PerfApiClient cached=false` lines for the same endpoint during one navigation, which would indicate duplicated or unexpectedly early API calls.
