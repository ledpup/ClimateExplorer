# Recent Observations Reference Date

## Problem

Recent Observations currently treats the latest observations as current-year data and derives periods from the local current date. That fails for GHCNd stations where the newest daily record may be weeks old, in a previous year, or sparse across only part of the station record.

The tile generator should instead work from the point of view of a selected `ReferenceDate`.

## Goals

- Default `ReferenceDate` to the latest available daily observation for the station and metric domain.
- Allow callers to request a historical `ReferenceDate` later.
- Resolve requested dates by snapping to the latest available observation on or before the requested date.
- Generate all tile periods relative to the resolved `ReferenceDate`.
- Build a single normalized observation collection for tile generation by merging `GetRecentObservations` and `GetClimateRecords`.
- Preserve existing dynamic add/remove tile behavior and completeness threshold behavior.

## Data Strategy

For each domain:

- Temperature loads recent Tmax and Tmin, plus historical Tmax and Tmin when available. If historical Tmax/Tmin are unavailable, it falls back to historical mean records for mean-temperature tiles and comparisons.
- Precipitation loads recent precipitation and historical precipitation.
- Recent and historical records are normalized into daily observations, merged by date, ordered, and deduplicated.
- When both recent and climate records contain the same date, recent values win for tile observations because they are the freshest endpoint.
- Historical comparisons use the merged daily series but exclude the reference period year and later years when building comparable distributions, preserving the existing "do not compare against itself" behavior for current data and avoiding hindsight for historical views.

## Reference Date Rules

- If no date is requested, use the latest available observation date from the normalized station observations.
- If a date is requested, resolve it to the latest available observation date on or before the requested date.
- If no observation exists on or before the requested date, return an empty/unavailable result instead of throwing.
- Include `ReferenceDate`, `RequestedReferenceDate`, `MinimumReferenceDate`, and `MaximumReferenceDate` in the tab result so the UI can render a bounded date picker.

## Period Rules

All periods are derived from `ReferenceDate`, not `DateTime.Today`:

- Day tiles: `ReferenceDate`, then previous available observation days.
- Latest 7 days: seven calendar days ending on `ReferenceDate`.
- Current month: first day of the `ReferenceDate` month through `ReferenceDate`.
- Previous months: completed months before the `ReferenceDate` month.
- Current season-to-date: season containing `ReferenceDate`, through `ReferenceDate`, hidden during the first month of the season.
- Previous seasons: completed seasons before the season containing `ReferenceDate`.
- Year to date: January 1 through `ReferenceDate`.

Tiles with no observations are omitted. Tiles with partial observations are generated with completeness metadata so existing threshold logic can suppress comparisons when required.

## UI

Add a compact `View as of` date input above the tile controls once tab data has loaded. It uses the result date bounds, defaults to the latest available date, and reloads both tab results when changed. The service resolves the selected value to the latest available observation on or before the chosen date, so the UI can show sparse station records without an error state.

## Tests

Service tests should cover:

- Latest data yesterday.
- Latest data two weeks ago.
- Latest data in a previous year.
- Only one year of data.
- Only six months of data.
- Requested `ReferenceDate` in the past.
- Overlapping recent and climate records are deduplicated with recent records winning.
- Missing current-year data still generates tiles.
- Latest 7 days, current month, year-to-date, and season-to-date are relative to `ReferenceDate`.
