---
layout: single
title: "Adding more stations to the website"
date: 2026-11-13 13:00:00 +1100
categories: datasets
---
ClimateExplorer now has 5,535 locations, up from 2,014. All of the new ones are weather stations from NOAA's [Global Historical Climatology Network monthly (GHCNm)](https://www.ncei.noaa.gov/products/land-based-station/global-historical-climatology-network-monthly). This post explains why the map had to change first, where the new stations are, and the rules that now decide which stations are in.

## The map came first

The old map drew a marker for every location in view and never removed any. Zoomed out to the whole world, it drew all 2,000 at once, most of them on top of each other. Each marker also took three calls between the app and the map library, so about 6,000 calls in all. Nearly tripling the number of stations would have made the map slower and harder to read.

Now each location has a minimum zoom level: the lowest zoom at which its marker doesn't overlap the marker of a location with a longer record. Zoomed out, you see the longest records. Zoom in and more markers appear. Markers never overlap, the location you've selected always shows, and the number of markers on screen depends on the size of the map, not on how many stations there are. A typical desktop map shows about 300 at most.

Once the map could handle overlap, station selection no longer needed to keep stations far apart for the map's sake. That's what made more stations possible.

## Why the rules changed

The [original selection](https://climateexplorer.net/blog/the-rest-of-the-world) grouped stations with a clustering algorithm called DBSCAN and kept the best station in each group. Any two stations within 75 km of each other went in the same group. That made the groups chain together: a group was every station you could reach in hops of less than 75 km, however far apart the two ends were.

- In the eastern half of the US, 892 candidate stations formed one group, and only one station was kept (Providence, Rhode Island). Only 43 stations east of 90°W made it in, and only because they were too isolated to join a group.
- In Japan, 65 stations formed one group, and only Tokyo was kept.
- Sevilla, which has records back to 1897, was missing. Cádiz is 107 km away, but Sevilla, Morón, Jerez and Cádiz form a chain of hops under 75 km. Cádiz has the longer record, so it won.

The special distances for Germany (45 km) and the US (70 km) were attempts to work around this. The real problem was keeping one station per group, not the distance.

A second problem was empty country. Large parts of Canada, Russia, inland Australia, Brazil and Antarctica had no station at all, because none of the stations there had the 60-year record the rules required.

## The new stations

There are 3,543 new stations, and 22 have gone (they no longer meet the rules, or NOAA no longer publishes them). About 1,760 of the new stations come from the new spacing rule and about 1,780 from the new isolation rule (both are described below).

| Country | Before | Now |
|---|---|---|
| United States | 226 (43 east of 90°W) | 1,730 (578 east of 90°W) |
| Russia | 374 | 628 |
| Canada | 75 | 340 |
| Australia (in addition to the 112 ACORN-SAT locations) | 20 | 199 |
| Japan | 28 | 133 |
| Germany | 60 | 106 |
| Brazil | 6 | 82 |
| Indonesia | 5 | 80 |
| Spain | 22 | 59 |
| Antarctica | 8 | 57 |

The spacing rule mostly adds stations where stations are dense: the US, Japan, Germany and the Nordic countries. The isolation rule fills the gaps: Canada, Russia, inland Australia, Brazil, Indonesia, Mexico, Central Asia, Africa and Antarctica. [Sevilla](https://climateexplorer.net/location/sevilla-san-pablo) is in, and so is the Amundsen–Scott station at the [South Pole](https://climateexplorer.net/location/amundsen-scott).

Most stations added under the isolation rule have less than 60 years of complete records. They have a grey map marker and no warming anomaly or heating score, but they do have daily data, records and recent observations.

## The rules

A complete year is one with all 12 months of data. A station's record length is its number of complete years.

**Long-record stations**

1. **Current:** the last complete year is within the last 10 years (2016 or later).
2. **Long record:** at least 60 complete years.
3. **Recent record:** at least 10 of the last 30 years (1996–2025) are complete.
4. **Every country represented:** if a country has fewer than 5 stations after rules 1–3, its best stations with at least 10 complete years and data since 2016 are added, up to 5 in total.
5. **Spacing:** stations are checked one at a time, starting with the stations already on ClimateExplorer and then the longest records. A station is kept unless a station already kept in the same country, or an existing ACORN-SAT or UK location, is within its radius. The radius is 25 km for a 60-year record and shrinks to 10 km at 120 years or more.

**Isolated stations**

6. **Isolation:** any other station with at least 10 complete years, and any monthly data in the last 10 years, is added if no location is within 75 km, in any country. Longest records go first, and each station added blocks others within 75 km of it.

**Duplicates**

7. A GHCNm station is removed if it's the same as an ACORN-SAT or UK location in the same country, by name or by position.

## Why these rules

- **60 complete years** is the minimum for the warming anomaly. A station with less would have no anomaly or heating score.
- **The last 10 years** stays as before. We tried 5 and 3, but most long-record stations whose last complete year looked old were still reporting. A single missing month makes a year incomplete.
- **10 of the last 30 years complete** matters because the "last 30 years" trend uses a station's last 30 *complete* years. For a station with many gaps, that can stretch over 50 calendar years. We started at 20, but that dropped stations we wanted, such as [Cape Sorell](https://climateexplorer.net/location/cape-sorell) in Tasmania (11 of 30), where recent years often miss a month. Dropping the rule altogether added 480 stations, most with thin recent records. 10 keeps most of them.
- **Spacing of 25 to 10 km** only has to remove near-duplicates now that the map handles overlap: an airport and the town next to it, or several stations in one city. With no spacing at all, the extra stations were mostly second and third stations in the same place (Lethbridge in Canada had four). A longer record gets a smaller radius because it's worth keeping even close to a neighbour. The same radius applies in every country, so the special cases for Germany and the US are gone. Each station is compared only with stations kept before it, so stations no longer chain together.
- **Existing stations go first,** so a re-run keeps them, and their URLs, unless they fail rules 1–3.
- **Spacing is checked within a country** because people look for a station in their own country, even when a better one is just across the border. The map hides the extra markers until you zoom in.
- **Isolation crosses borders** because its purpose is coverage. It started with regional Australian towns such as Geelong, Wollongong and Bendigo, whose long-record sites have closed and whose replacements are only 20 to 35 years old. Joining old and new stations into one record is careful work that the Bureau of Meteorology does for ACORN-SAT, and we won't do it ourselves. Those three towns are each within 75 km of an existing location, so this rule doesn't add them. It does fill a lot of empty country. 75 km is the spacing ClimateExplorer used before. At 50 km the rule would add about 2,700 stations, and at 100 km about 1,260.

ClimateExplorer's data files are now about 1.2 GB, mostly the daily records. An earlier 1 GB storage limit for the site no longer applies, which left room for all of this.
