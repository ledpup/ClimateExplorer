# `ChartView` initial render regression investigation

- **Date:** 2026-06-21
- **Status:** Fixed, UI regression tests added but not run
- **Context:** Follow-up to [`2026-06-20-01-investigation-and-refactor-plan-ChartView.md`](2026-06-20-01-investigation-and-refactor-plan-ChartView.md) and the staged `ChartView` responsibility refactor.

## Reported Symptoms

Two regressions appeared after the staged `ChartView` refactor:

1. On the first Blazor Server page load, the default location chart rendered duplicate Chart.js datasets. The location default state contains two series, temperature and rainfall, but the rendered chart showed four datasets. Changing charts, datasets, presets, pages, or series after the first load behaved normally. WASM did not appear to show the same issue.
2. The left-hand color strip in `ChartSeriesView` was incorrect on initial load. The expected default colors are red for the first series and blue for the second series, but the second strip could appear green.

## Investigation

The duplicate dataset problem was isolated to `ChartView.OnAfterRenderAsync`.

`ChartView` now receives already-built `ChartDataBuildResult` from the parent. To avoid rerendering the same build result, it tracked `renderedData`, but it only assigned `renderedData = Data` after `await RenderChart()`.

On Blazor Server, `RenderChart()` awaits multiple JS interop calls and calls `StateHasChanged()`. That gives the renderer a chance to enter `OnAfterRenderAsync` again before `renderedData` has been updated. The second entry sees the same `Data` as still unrendered and starts another `RenderChart()` call for the same build result.

The color issue is consistent with the same race. `AddDataSetsToChart()` mutates each `ChartSeriesDefinition.Colour` as Chart.js datasets are added. If two render passes interleave, the shared `ColourServer` field can be reset and consumed by another pass while the first pass is mid-loop. That can shift the second auto-assigned color from blue to green.

`ChartSeriesView` also cached `StyleForTitleBar` and `StyleForOuterDiv` in `OnAfterRender`. Those styles are derived from `ChartSeries.Colour`, which is assigned during chart rendering rather than when the series list first renders. Caching the style after render made the component more sensitive to stale or mid-render color state.

## Fix

`ChartView.OnAfterRenderAsync` now claims a build result before awaiting any chart-rendering work:

- it sets `renderedData = Data` and `renderedLoadingErrored = LoadingErrored` before `waitForChartReady`/`RenderChart`;
- it uses `renderChartInProcess` so the same component cannot start a concurrent render for the same data;
- it clears `renderedData` if rendering throws, allowing a later retry.

`ChartSeriesView` no longer caches color-derived styles in `OnAfterRender`. `StyleForTitleBar` and `StyleForOuterDiv` are computed from the current `ChartSeries.Colour` during normal rendering. The existing `StateHasChanged()` at the end of `RenderChart()` is therefore enough for the series list to pick up the assigned red/blue colors.

## Regression Tests Added

Added Playwright tests in `ClimateExplorer.Web.UiTests/ChartTests.cs`:

- `InitialDefaultChartRendersEachSeriesOnceInChartJs`
  - loads the default page;
  - waits for the default chart;
  - asserts the UI has two chart series controls;
  - asserts the Chart.js instance has exactly two datasets.
- `InitialDefaultChartSeriesTitleColoursMatchRenderedSeriesColours`
  - loads the default page;
  - waits for the default chart;
  - asserts the first two chart-series title bars have red and blue left borders.

These tests target the Blazor Server UI-test host (`http://localhost:5298`) and are intended to catch this initial-render race in future runs.

## Verification

The UI tests were intentionally not run, per request.

