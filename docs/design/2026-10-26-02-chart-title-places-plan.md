# Chart title shows only places

- **Date:** 2026-10-26
- **Status:** Implemented
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

The title is built by `ChartLogic.BuildChartTitle` and contains only places. Locations are grouped by
country, and the title is a list of groups.

**Delimiters**, tightest first:

| Delimiter | Separates | Example |
|---|---|---|
| `, ` | location names in a group, or a single name from its country | `Canberra, Hobart` / `Canberra, Australia` |
| ` \| ` | a group's names from its country (two or more names) | `Canberra, Hobart \| Australia` |
| ` — ` (spaced em dash) | unrelated groups: different countries, or a region | `… \| Australia — Auckland, New Zealand` |

`|` keeps the meaning it has in the legend labels: it separates aspects of one thing (there, a series; here, a
country group). The group separator has to look like a bigger break than `|`, or
`Canberra, Hobart | Australia; Auckland, New Zealand` reads as if `Australia; Auckland` belong together. That
rules out `;` and `·` (look tighter than `|`) and `/` (looks like `|`). `&` or `and` work for two groups but
not for three or more.

**A group** is the locations from one country, in the order the series were added:

- one location: `Name, Country` (this is `DisplayTitle` when the country is the full name)
- two or more: `Name, Name | Country`

`Country` is `CountryWithoutOwner` when the chart has one or two countries, and `CountryCode` when it has more
than two. These are ISO codes (see [ISO country codes](2026-10-26-01-iso-country-codes-plan.md)).

**Examples:**

| Locations | Title |
|---|---|
| Canberra | `Canberra, Australia` |
| Canberra, Hobart | `Canberra, Hobart \| Australia` |
| Canberra, Auckland | `Canberra, Australia — Auckland, New Zealand` |
| Canberra, Hobart, Auckland | `Canberra, Hobart \| Australia — Auckland, New Zealand` |
| Canberra, Hobart, Auckland, Suva | `Canberra, Hobart \| AU — Auckland, NZ — Suva, FJ` |
| Canberra + Greenland region | `Canberra, Australia — Greenland` |

- **One location** (any number of series): `DisplayTitle`. This means removing the single-series branch that
  calls `GetFriendlyTitle`. The legend is always shown (`ChartOptionsFactory` sets `Legend.Position = "bottom"`
  and never hides it), so no series information is lost.
- **A location and a region:** the region is its own group, after the locations: `DisplayTitle — Region.Name`.
  Locations always come before regions.

`Location.CountryWithoutOwner` becomes public. Same-country checks compare `CountryCode`, not the name with the
owner removed, so Nuuk (`GL`) and Copenhagen (`DK`) stay different countries. Regions are recognised with
`Region.IsRegionId` / `Region.GetRegion`; they aren't in the `Locations` dictionary that `ChartView` passes in.

### Decided

- **Several countries with several locations:** group by country (see above), for any number of countries.
- **What `|` means:** it separates a group's names from its country, as in the legend where it separates
  aspects of a series. Unrelated groups are separated by ` — `.
- **Order of groups:** the order the series were added in, which is what the current `Distinct()` over the
  series gives. A group appears where its first location appears, and names within a group keep that order.
- **Location not in the dictionary** (`Locations` null, not loaded yet, or the ID missing): this shouldn't
  happen, but if it does, fall back to `SourceSeriesSpecification.LocationName` with no country. Keep
  `Climate data` for no series at all.

- **Regions:** the location groups by the rules above, then each region's `Name` as its own group after
  ` — `. One region alone is its `Name`; several regions give e.g. `Atmosphere — Ocean`.
- **Length:** with more than four locations, the locations part is just their names separated by commas,
  with no countries (regions still follow after ` — `). Charts with that many series are rare and already hard
  to read, so this is a simple rule rather than a full fix for overflow.
- **Minor cases left as they are:** a location in Greenland plus the `Greenland` region gives
  `Nuuk, Greenland — Greenland`. Two locations with the same name in the same country give
  `Richmond, Richmond | Australia`.

### Tests

`ChartLogicTitleTests`: the two single-series tests change to expect `DisplayTitle` only. Add one test per
example row above and per decision (same country, two countries, two countries with a multi-location
group, more than two countries, more than four locations, location plus region, regions only, group order,
missing dictionary entry, owner territory).

## Later: series labels

The legend labels repeat the location when every series is for the same place. That's a separate change
after this one, and the title rules above are meant to make it possible: once the title names the place, a
single-location chart can drop the location from each legend label. See
[Chart series labels without repeated text](2026-10-26-03-chart-series-labels-plan.md).

## Summary

The chart title is rewritten to show only places. Locations are grouped by country: `,` separates names,
`|` separates a multi-location group's names from its country, and ` — ` separates groups. Countries are
full names for one or two countries and ISO codes for more. Regions are their own groups, after locations.
More than four locations shows names only.

### Change size

**Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.Core` and `ClimateExplorer.UnitTests`.

| Area | Added | Changed | Removed | Lines (approx.) |
|---|---|---|---|---|
| `ChartLogic.BuildChartTitle` | – | 1 | – | ~60 |
| `Location.CountryWithoutOwner` made public | – | 1 | – | ~1 |
| Tests: `ChartLogicTitleTests` | – | 1 | – | ~130 |
| **Total** | **0** | **3** | **0** | **~190** |
