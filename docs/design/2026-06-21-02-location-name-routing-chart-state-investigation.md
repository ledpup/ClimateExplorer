# Location name routing chart-state investigation

- **Date:** 2026-06-21
- **Status:** Fixed, UI regression test added but not run
- **Author:** Codex
- **Scope:** `Index` location routing, chart-state URL initialization, and location navigation UI tests

## Intended Flow

The `/locations` page renders table links in the form `/location/{location-name}` using `Location.UrlReadyName()`.

`Index` owns the local dashboard routes:

- `/`
- `/location/`
- `/location/{locationString}`

When the user clicks a location table link, `Index.OnParametersSetAsync` resolves `LocationString` into a `Location`. Name segments are resolved by `IDataService.GetLocationByPath`; GUID segments are resolved by `IDataService.GetLocationById`.

Once the route location, dataset definitions, regions, and device mode are available, `ChartablePage.EnsureInitialChartStateAsync` creates or parses the initial `ChartState`. For a name URL with no query string, it creates the default location chart state and calls `ApplyChartStateAsync`.

`ApplyChartStateAsync` builds chart data and calls `ReflectChartStateInUrl`, which delegates URL generation to `ChartStateUrlService.BuildRelativeUrl`. The expected final browser URL is therefore `/location?...&csd=...`; this is the canonical chart-state URL for a hydrated location dashboard.

## Component Responsibilities

`Index` is responsible for resolving `/location/{location-name}`. It prefers the deferred in-memory `LocationDictionary` when available, but otherwise uses the single-location lookup endpoint via `GetLocationByPath`. That avoids eagerly loading every location just to resolve a name route.

`ChartStateUrlService` is responsible for parsing and generating the chart-state query string, including `csd`. `ChartablePage` owns applying the parsed/default state, reflecting it in the URL, and building the corresponding chart data.

`/location/{location-guid}` is still supported as a compatibility and UI-navigation route, especially from map/change-location flows. It is not the desired canonical final URL. A GUID route should resolve to a `Location`, initialize or update chart state, then settle to `/location?...&csd=...` once chart state is reflected.

## Root Cause

The deferred location dictionary change made name-route resolution asynchronous in more paths. `Index.OnAfterRenderAsync` only called `EnsureInitialChartStateAsync` inside the one-time branch where dataset definitions and regions were first loaded.

If `/location/{location-name}` was reached from `/locations`, the first render could load dataset definitions and regions while the name lookup was still pending. At that moment `Location` was still null, so chart initialization was skipped. When the lookup later completed, `Location` was set and the dashboard/map could render, but the one-time chart initialization branch had already been passed.

That left `ChartDataLoading` at its initial `true` value, `CurrentChartState` and `CurrentChartData` unset, and no call to `ReflectChartStateInUrl`. The chart therefore displayed the loading state indefinitely and the address bar stayed on `/location/{location-name}`.

Dashboard and map rendered because they only need the resolved `Location` plus already-loaded metadata. The map can render from a single-location fallback (`MapLocations => [Location]`) when the full dictionary is still deferred. The chart needs a full `ChartState` and built `ChartDataBuildResult`, and those are only produced by `EnsureInitialChartStateAsync`.

## Fix

`Index.OnAfterRenderAsync` now retries initial chart-state resolution whenever the prerequisites are available:

- dataset definitions loaded;
- regions loaded;
- `Location` resolved;
- initial chart state not already resolved.

This keeps the full location dictionary deferred. Name routes continue to use the dedicated lookup endpoint unless the dictionary has already been loaded for another reason.

## Risks

The main risk is repeated initialization, but `ChartablePage.EnsureInitialChartStateAsync` already guards with `initialChartStateResolved`, so the retry path is idempotent.

Another risk is render churn. The fix only requests another render when prerequisites are loaded or chart initialization actually runs.

## Test Coverage

Existing UI tests already covered direct `/location/hobart` loads, refreshes, and navigation between name URLs. They did not cover clicking from `/locations` or asserting the canonical chart-state URL.

Added `LocationTableClickResolvesToCanonicalChartStateUrlAndRendersChart` in `ClimateExplorer.Web.UiTests/ChartTests.cs`:

- opens `/locations`;
- filters to Hobart;
- clicks the table link;
- verifies the location dashboard title renders;
- waits for the URL to reach `/location?chartAllData...csd=...`;
- waits for the Chart.js dataset count to reach the expected default series count.

Tests were not run, per request.
