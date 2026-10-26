# Chart title shows only places

- **Date:** 2026-10-26
- **Status:** Proposed
- **Author:** Patrick (with Claude)
- **Scope:** `ChartLogic.BuildChartTitle`, `Location`, `ChartLogicTitleTests`
- **Builds on:** [ISO country codes](2026-10-26-01-iso-country-codes-plan.md). The title scheme below shows
  `CountryCode` in some cases, so the codes must be ISO 3166-1 first.
- **Branch context:** written on `issues/us-units`

## Problem

Chart titles repeat text that the legend already shows. With one series, the title is
`Canberra, Australia | Mean temperature | Adjusted`, and the legend under the chart says
`Canberra | Mean temperature | Adjusted | °C` again. The title should only say *where*: the
`GeographicalEntity` name and, for a `Location`, its country. The subtitle (date range or bin granularity)
doesn't repeat anything and is out of scope.

## Chart title

The title is built by `ChartLogic.BuildChartTitle` and contains only places:

- **One location** (any number of series): `DisplayTitle`. This means removing the single-series branch that
  calls `GetFriendlyTitle`. The legend is always shown (`ChartOptionsFactory` sets `Legend.Position = "bottom"`
  and never hides it), so no series information is lost.
- **Several locations, same country:** names separated by commas, then `|`, then `CountryWithoutOwner`, e.g.
  `Canberra, Hobart | Australia`.
- **Two countries:** each `DisplayTitle`, separated by `|`.
- **More than two countries:** `Name, CountryCode` for each, separated by `|`. These are ISO codes (see
  [ISO country codes](2026-10-26-01-iso-country-codes-plan.md)).
- **A location and a region:** `DisplayTitle | Region.Name`. Locations always come before regions.

`Location.CountryWithoutOwner` becomes public. Same-country checks compare `CountryCode`, not the name with the
owner removed, so Nuuk (`GL`) and Copenhagen (`DK`) stay different countries. Regions are recognised with
`Region.IsRegionId` / `Region.GetRegion`; they aren't in the `Locations` dictionary that `ChartView` passes in.

### Decisions needed

1. **Two countries with more than two locations.** The rule gives `Canberra, Australia | Hobart, Australia |
   Auckland, New Zealand`, which repeats the country. Suggest grouping by country, and doing the same for more
   than two countries.
2. **What `|` means.** In the same-country rule it separates names from the country. In the others it
   separates locations. Suggest choosing one meaning, e.g. "`|` separates country groups".
3. **Region cases not covered yet:** one region alone (suggest `Name`), several regions (common on the Global
   page, e.g. Atmosphere and Ocean), several locations plus regions, one location plus several regions.
   Suggest: the locations part by the rules above, then each region's `Name` after a `|`.
4. **Order of locations.** Suggest the order the series were added in, which is what the current
   `Distinct()` over the series gives.
5. **Location not in the dictionary** (`Locations` null, not loaded yet, or the ID missing): fall back to
   `SourceSeriesSpecification.LocationName` with no country. Keep `Climate data` for no series at all.
6. **Length.** The Chart.js title is one line and doesn't wrap. The 30-character limit only applies inside
   `DisplayTitle`, so same-country lists and more-than-two-countries titles have no limit and will overflow on
   mobile (≤767px). Options: cap the number of locations shown (`+2 more`), or pass the title to Chart.js as an
   array of lines.
7. **Minor:** a location in Greenland plus the `Greenland` region gives `Nuuk, Greenland | Greenland`. Two
   locations with the same name in the same country give `Richmond, Richmond | Australia`.

### Tests

`ChartLogicTitleTests`: the two single-series tests change to expect `DisplayTitle` only. Add one test per rule
and per decision above (same country, two countries, more than two countries, location plus region, region
order, missing dictionary entry, owner territory).

## Later: series labels

The legend labels repeat the location when every series is for the same place. That's a separate change
after this one, and the title rules above are meant to make it possible: once the title names the place, a
single-location chart can drop the location from each legend label.

## Summary

The chart title is rewritten to show only places: the location's `DisplayTitle` for one location, a grouped
list for several locations in one country, and `Name, CountryCode` when more than two countries are shown.
Regions follow locations. The decisions listed above need to be made first.

### Change size

**Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.Core` and `ClimateExplorer.UnitTests`.

| Area | Added | Changed | Removed | Lines (approx.) |
|---|---|---|---|---|
| `ChartLogic.BuildChartTitle` | – | 1 | – | ~60 |
| `Location.CountryWithoutOwner` made public | – | 1 | – | ~1 |
| Tests: `ChartLogicTitleTests` | – | 1 | – | ~130 |
| **Total** | **0** | **3** | **0** | **~190** |
