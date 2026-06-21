# Location change chart-state regression investigation

- **Date:** 2026-06-21
- **Status:** Fixed, UI tests updated but not run
- **Author:** Codex
- **Scope:** map/change-location flows, `/location/{guid}` compatibility route, chart-state refresh, canonical chart-state URL

## What Changed Previously

The previous routing fix moved route location resolution into `Index.OnParametersSetAsync` and made `ResolveLocationAsync` the single route-to-location entry point. That fixed `/locations` table clicks because name routes such as `/location/hobart` are now resolved before `ChartablePage.EnsureInitialChartStateAsync` creates the first chart state.

That same fix also stopped rewriting name URLs to GUID URLs during route resolution. This preserved the working `/locations` flow and avoided re-entrant navigation while asynchronous name lookup was still in flight.

## Why `/locations` Works

The `/locations` page links to `/location/{location-name}`. On a fresh `Index` instance, `initialChartStateResolved` is still false. After `ResolveLocationAsync` finds the location and the data prerequisites are loaded, `EnsureInitialChartStateAsync` creates the default location chart state, builds chart data, and calls `ReflectChartStateInUrl`.

The final browser URL is therefore the canonical chart-state URL: `/location?chartAllData=...&csd=...`.

## Why Other Location Changes Broke

Map selection, near me, random, nearby locations, and the modal search all call `SelectedLocationChanged`, which navigates to `/location/{location-guid}`. That route is still valid as a compatibility and intermediate route, but it is not the desired final chart-state URL.

When the component has already initialized a chart, `ResolveLocationAsync` updates `Location`, so the dashboard and map receive the new location. However, `ChartablePage.EnsureInitialChartStateAsync` is guarded by `initialChartStateResolved`. It no-ops after the first chart initialization, so the existing `CurrentChartState` and `CurrentChartData` stay pointed at the old location.

The result was a split state:

- dashboard: updated from `Location`;
- map: updated from `Location`;
- chart state: reused from the previous location;
- chart data: reused from the previous location until another chart action occurred;
- URL: left at `/location/{guid}` because no chart-state apply occurred to reflect the canonical URL.

## Responsibilities

`Index.ResolveLocationAsync` is responsible for converting the current route segment into a `Location`, using `GetLocationById` for GUID routes and `GetLocationByPath` for name routes. This avoids eager loading of the full location dictionary for normal name/GUID route resolution.

`ChartSeriesLocationSubstitutionService` is responsible for converting an existing chart state from the previous selected location to the newly selected location. It rewrites unlocked single-location series, leaves region series alone, and duplicates locked series for the new location.

`ChartStateUrlService` is responsible for parsing and building the canonical chart-state query string, including `csd`.

`ChartablePage.ApplyChartStateAsync` is responsible for setting `CurrentChartState`, rebuilding chart data, and reflecting the canonical chart-state URL.

## Route Semantics

`/location/{location-guid}` is a supported intermediate/compatibility route for UI-driven location changes. It should not remain the final browser URL after chart state is available.

The canonical hydrated local chart URL is `/location?...&csd=...`. Name URLs such as `/location/hobart` are valid entry URLs, especially for SEO and the locations table, but once the chart state is applied the app settles on the query-string chart-state route.

## Root Cause

The previous routing fix centralized route resolution but did not centralize the post-resolution location-change behavior. The old direct mutation path in `SelectedLocationChanged` had already been replaced by navigation to `/location/{guid}`, but the chart-state substitution step was left disconnected.

Because `initialChartStateResolved` is intentionally one-shot, route reuse after the first chart render updated `Location` without recreating, substituting, or notifying chart state. The fix accidentally covered the initial `/locations` navigation path while leaving already-hydrated location changes without a chart refresh.

## Fix

`Index.OnAfterRenderAsync` now attempts the shared chart-state location substitution after route resolution. When a newly resolved location differs from the previous location, `ApplyLocationChangeToChartState`:

1. checks that previous/current locations, data definitions, regions, and current chart state are available;
2. skips if the current location has already been applied to the chart state;
3. uses `ChartSeriesLocationSubstitutionService` to move the current chart state to the new location;
4. surfaces any substitution warnings through the snackbar;
5. calls `ApplyChartStateAsync`, which rebuilds chart data and reflects the canonical URL.

The `/locations` path remains intact because initial chart creation still happens through `EnsureInitialChartStateAsync` before there is a previous location to substitute from.

The fix keeps the deferred location dictionary behavior. GUID route resolution uses `GetLocationById`; name route resolution uses `GetLocationByPath` unless the dictionary has already been loaded for another reason.

## Risks

The main risk is repeated substitution on subsequent renders. The fix tracks the location id already applied to chart state, so a location change is substituted once and normal render churn does not repeatedly rebuild chart data.

Another risk is preserving the previous location for the Add Data Set comparison folder while also preventing repeated chart substitution. The fix keeps `PreviousLocation` intact for that UI and uses a separate applied-location marker for the chart refresh guard.

Locked chart series retain their existing behavior: they remain in place and an unlocked duplicate is added for the new location when compatible data exists.

## Test Coverage

Reviewed existing UI tests in `ClimateExplorer.Web.UiTests/ChartTests.cs`.

Updated/added tests for:

- `/locations` table click still reaches the canonical chart-state URL and renders Hobart chart series;
- map marker location change updates dashboard, chart series, chart data count, and canonical URL;
- near me location change updates dashboard, chart series, chart data count, and canonical URL;
- random location change updates dashboard, chart series, chart data count, and canonical URL;
- nearby location change updates dashboard, chart series, chart data count, and canonical URL;
- modal search location change now also asserts chart and canonical URL, not just dashboard.

## Test Status

- `dotnet build ClimateExplorer.sln`: passed on 2026-06-21.
- UI tests: not run, per request.
