# Station selection and map density

- **Date:** 2026-11-12
- **Status:** Implemented 2026-11-12: map (Proposal 2) and station selection (Proposal 1); see the
  addenda. The GHCNm, GHCNd and precipitation importers and the DataPipeline haven't been re-run yet.
- **Author:** Patrick (with Claude)
- **Scope:** `ClimateExplorer.Data.Ghcnm` (station selection), `ClimateExplorer.Core` (spacing
  algorithm, `Location`), `ClimateExplorer.WebApi` (map zoom per location), `ClimateExplorer.Web.Client`
  (`MapContainer`)
- **Branch context:** written on `development`

## Question

The GHCNm importer keeps roughly one station per 75 km (45 km in Germany, 70 km in the US). Two asks:

1. Combine distance and quality, so a station with a long record can sit closer to its neighbours (for
   example 50 km).
2. The map shows every location with no control over overlap. More stations are wanted, but not at the
   cost of render time or overlapping markers.

Also: why is Sevilla missing? It looks like Huelva or Cádiz pushed it out.

## Method

I downloaded the current GHCNm v4 adjusted file (`ghcnm.tavg.latest.qcf`, build 2026-11-10) and
re-implemented `Program.cs`'s selection in a script: the per-station stats, the filters (last year
≥ 2016, score ≥ 60), the country top-up, the pre-existing seeds, and DBSCAN per country. It selects
1,906 stations; production ships 1,901. The 5 are `RemoveDuplicateLocations` plus newer data. All
figures below come from that script, run on the real inventory.

## Findings: current selection

### DBSCAN with `minimumPointsPerCluster: 2` chains

With `minPts = 2`, any station that has one neighbour within `eps` is a core point. A cluster is
therefore every station you can reach by hops of under 75 km, however far apart the ends are. One
station is kept per cluster.

| Cluster | Stations | End-to-end span | Kept |
|---|---|---|---|
| US (most of the eastern half) | 892 | continental | Providence T. F. Green AP |
| US (northwest) | 122 | 942 km | Boise Air Terminal |
| Japan | 65 | 871 km | Tokyo |
| US | 40 | 589 km | Columbus |
| US | 37 | 577 km | Bozeman |

So the eastern US is not thinned to one station per 70 km. 892 candidates collapse to a single
station. Only 43 shipped US stations lie east of 90°W, and those are the ones isolated enough to
belong to no cluster. The `DE`/`US` overrides were tuning around this chaining; it isn't a density
problem. Lowering `eps` breaks the chains into smaller ones but never stops them.

### Sevilla

Sevilla San Pablo (`SPE00120512`, 1897–2025, 29 years missing, score 100) is a good station. Huelva
(91 km) and Cádiz (107 km) are both more than 75 km away, so neither knocked it out directly. A chain
did:

```
Sevilla ──37 km── Morón ──~60 km── Jerez ──~33 km── Cádiz
score 100         65              65             157
```

All four end up in one cluster, and Cádiz wins on score, 107 km from Sevilla. Córdoba Aeropuerto
(score 56) never got in: it falls below the 60-year minimum.

### Other points

- **Clusters are per country.** Stations either side of a border are never compared, so they can
  sit close together (Rhine valley, Benelux, the Great Lakes). This is deliberate and stays: people
  look for a station in their own country, even when a better one is 20 km across the border. The map
  deals with the overlap (Proposal 2).
- **Score is strict.** A year missing a single month counts as a missing year
  (`IsValidYear`), and gaps are taken off the score. That's fine for ranking, but it is why Córdoba
  and others narrowly miss 60.
- **Re-running drops stations regardless.** 101 shipped stations no longer pass the recency or
  length filters on today's data. Any re-run drops them and breaks their URLs, whichever algorithm
  is used.

## Proposal 1: greedy selection with a quality-dependent radius

Replace DBSCAN with greedy priority selection (non-maximum suppression):

1. Seed with the pre-existing locations (BOM, UK), as now.
2. Sort candidates by priority: stations shipped today first (so their URLs stay stable), then by
   score, descending.
3. Walk the list. Keep a station only if no kept station lies within `r(score)` of it.
4. `r(score)` falls as the record gets longer: 75 km at score ≤ 60, linear down to 40 km at
   score ≥ 120.
5. Check distance within each country only, as now. Keep the country top-up as is.

There's no chaining: a station is only blocked by an actual neighbour that is already kept and
within its radius. Long-record stations go first, so they're never displaced by weaker ones. Use a
1° grid for the neighbour lookup.

Results on the same candidates:

| Variant | Total | US (east of 90°W) | JP | DE | ES | Sevilla |
|---|---|---|---|---|---|---|
| Current DBSCAN (shipped) | 1,901 | 226 (43) | 28 | 60 | 22 | no |
| Greedy, fixed 75 km | 2,301 | 591 (194) | 62 | 36 | 27 | yes |
| Greedy, 75 → 50 km (score 60 → 100) | 2,508 | 721 (253) | 82 | 48 | 31 | yes |
| Greedy, 75 → 40 km (score 60 → 120) | 2,519 | 723 (252) | 86 | 53 | 31 | yes |
| Greedy 75 → 40, seeded with shipped stations | 2,522 | — | — | — | — | yes |
| **Greedy 75 → 40, per country (chosen)** | **2,549** | **726 (253)** | **86** | **56** | **31** | **yes** |

Rows without "per country" checked distance across borders. Per country, about 30 more stations
survive near borders. The map hides the weaker one of each close pair until you zoom in.

Western Andalucía becomes Cádiz, Huelva, Málaga and Sevilla. The per-country overrides can go: the
graded radius gives Germany 53 stations against 60 today, with no special case. The 75 → 50 and
75 → 40 rows differ little, because most candidates with scores above 100 are already far apart.
Pick the floor from the map (see below), not from the counts.

The seeded run drops 135 shipped stations: the 101 that fail the filters anyway, plus about 34 lost
to cross-border or pre-existing neighbours. It adds 756.

Why not keep DBSCAN and weight `eps` by score? Any density-connected clustering chains. The
problem is "one per cluster", not the radius.

### The 40 km floor

The marker is a 48 px pin about 38 px wide. At the default zoom (7), 40 px is about 39 km at 37° N
and about 49 km at the equator. With a 40 km floor, markers mostly don't overlap at the default
zoom. Going lower only pays off once the map hides markers when zoomed out (Proposal 2).

### Revised rules, now that the map handles overlap

The map no longer needs stations spaced apart (Proposal 2). The radius now only removes near-duplicates
(airport and town, several stations in one city). Selection is about quality and storage.

**Storage.** Datasets is 477 MB against a 1 GB cap. GHCNd is 317 MB of it: 1,854 files, about 1.9 KB
per year of record, so about 190 KB for a typical location. GHCNm adds about 7 KB. The cap allows
about 2,700 more locations. Keeping 150 MB spare for yearly growth leaves room for about 1,800.

**Rules simulated** (current GHCNm, per country, today's stations first, no country top-up; the
storage column is GHCNd only):

| Last complete year | Complete years | Of last 30 complete | Radius by record length | Total | Shipped dropped | New | Storage |
|---|---|---|---|---|---|---|---|
| ≥ 2016 (today) | ≥ 60 | — | 75 → 40 km | 2,314 | 338 | 751 | +118 MB |
| ≥ 2016 | ≥ 60 | — | 50 → 25 km | 2,804 | 324 | 1,227 | +209 MB |
| ≥ 2023 | ≥ 60 | ≥ 25 | 50 → 25 km | 1,905 | 785 | 789 | +32 MB |
| ≥ 2023 | ≥ 60 | ≥ 20 | 50 → 25 km | 2,275 | 619 | 993 | +108 MB |
| ≥ 2016 | ≥ 60 | ≥ 20 | 25 → 10 km | 2,901 | 449 | 1,449 | +214 MB |
| **≥ 2021** | **≥ 60** | **≥ 20** | **25 → 10 km** | **2,764** | **541** | **1,404** | **+190 MB** |
| ≥ 2023 | ≥ 60 | ≥ 20 | 25 → 10 km | 2,639 | 618 | 1,356 | +171 MB |
| ≥ 2023 | ≥ 60 | ≥ 15 | 25 → 10 km | 2,834 | 565 | 1,498 | +213 MB |
| ≥ 2023 | ≥ 60 | ≥ 15 | none | 3,093 | 565 | 1,757 | +256 MB |

Why each rule:

- **Recency: last complete year ≥ current year − 5** (today's rule is − 10). Of stations with 60 or
  more complete years, 2,617 end in 2025 and 481 in 2024. The 2018 group (95 stations) is the main
  thing − 5 removes. − 3 was tried and is too strict. Of the 345 long-record stations whose last
  complete year is 2021–2023, 289 still reported monthly values in 2024–2026. They're live stations
  with an odd missing month, not closed ones: one missing month voids the year (`IsValidYear`). An
  alternative test, "any monthly value in the last 2 years", selects almost the same stations
  (2,745 against 2,764), so the simpler complete-year rule is enough.
- **History: keep 60 complete years.** `AnomalyCalculator` needs 60, so anything shorter has no
  warming anomaly. It would get a grey marker and no heating score.
- **Recent completeness: at least 20 of the last 30 years complete.** The "last 30 years" trend uses
  the last 30 *complete* years, which for a gappy station can span 50 calendar years. 25 is too
  harsh, because a single missing month voids a year (`IsValidYear`). Sevilla has 29.
- **Radius: 25 km for a 60-year record, falling to 10 km at 120 years.** This drops duplicates in the
  same town. Without any radius, the extra ~250 stations are mostly second and third stations for the
  same place (Lethbridge has four).
- Keep the country top-up and the per-country rule.

Of the 541 shipped stations dropped under the chosen rule: 217 came in through the country top-up (under
60 complete years; this simulation leaves the top-up out, so most of them stay), 149 end before 2021, 99
have fewer than 20 of the last 30 years, and 76 are no longer in the file (next point). None are lost to
the radius, because today's stations go first.

**Canadian station IDs changed.** NOAA renumbered Canada in GHCNm from `CA00nnnnnnn` to
`CAN0nnnnnnn` (Banff: `CA003050520` → `CAN03050520`). 75 shipped stations are affected. Before
re-running:

- `GhcnIdToLocationIds.json` has 3,643 `CA00` entries and 1 `CAN`. As things stand, every Canadian
  station gets a new location GUID, and the program exits to save them.
- The local `ghcnd-stations.txt` (from August) still uses `CA00`. GHCNd is looked up by the GHCNm ID,
  so Canadian locations would lose their daily data.

Map `CAN0` → `CA00` when reading the GHCNm files, or keep an alias both ways, so Canada keeps its GUIDs and
its GHCNd files.

**Compression.** In .NET 11, `ZipArchive` reads only Stored, Deflate and Deflate64, so Zstandard
and Brotli can't go inside the existing `.zip` files. As plain `.csv.br` or `.csv.zst` files, on a
sample of 41 GHCNd files, Brotli at maximum saves 35% (about 110 MB) and Zstandard level 19 saves
27%. The CSV is unchanged either way, but the files no longer open with a double-click. The rules
above fit without it, so keep it in reserve.

## Findings: the map

`MapContainer.CreateMapMarkers` adds every location inside the current bounds. Each marker costs
three JS interop calls: create and add, `BindTooltip`, and `OnClick`, which registers a
`DotNetObjectReference`. Markers are never removed when they leave the view or when you zoom out.
Zoomed out to the world, all ~2,000 are created: about 6,000 interop calls, and most of the markers
sit on top of each other.

To measure overlap, I worked out, at each zoom, how many markers can show without overlapping a
higher-priority one (38 × 44 px box, Web Mercator):

| Set | Count | z3 | z4 | z5 | z6 | z7 (default) | z8 |
|---|---|---|---|---|---|---|---|
| Shipped today | 2,014 | 303 | 683 | 1,290 | 1,877 | 2,002 | 2,011 |
| Proposal 1 + seeds | 2,632 | 297 | 688 | 1,370 | 2,210 | 2,620 | 2,632 |
| All 4,005 candidates + seeds | 4,118 | 297 | 691 | 1,395 | 2,302 | 3,186 | 3,775 |

Today, about 720 markers overlap at zoom 5, and about 1,700 at zoom 3.

`HandleMapMouseEvent` finds the clicked location by rounding to 0.1° and calling `Single`. No two
locations share a 0.1° cell today. A denser set makes that more likely, and when it happens the
click throws.

## Proposal 2: zoom tiers and one marker layer in JS

### Zoom tiers

Give each location a `MapMinZoom`: the lowest zoom at which it shows without overlapping a
higher-priority marker. Compute it once, with the same greedy pass in pixel space, from zoom 2 to 9:

- At each zoom, keep the markers already accepted at lower zooms. Then add, in priority order, any
  marker whose 38 × 44 px box doesn't overlap one already shown.
- Tiers nest: a marker visible at zoom z stays visible at every higher zoom.
- Priority: pre-existing (BOM) locations first, then station score.
- The selected location always shows, whatever its tier.

Two things follow. The map never overlaps at any zoom. And the number of markers in any viewport is
capped by its pixel area (about 300 in a 1000 × 600 px map), however many stations ship. Overlap is
then handled on the map, so Proposal 1's floor can drop if wanted. The last row of the table above
shows that even all 4,005 candidates stay readable.

Compute `MapMinZoom` in the WebApi when it loads locations, from a `Core` function. The marker size
is a fixed constant shared with the client. It belongs in the API because station scores live there
(`Stations_*.json`), and the client never sees them.

### One JS-managed layer

Replace the per-marker DPBlazorMapLibrary calls with a small `MapContainer.razor.js` module. It
receives the Leaflet map (`Map.MapReference`, exposed by LPBlazorMapLibrary 1.2.3) and the location
list once, as `{ id, lat, lng, label, title, minZoom }`. On `zoomend`/`moveend` it shows markers
with `minZoom <= zoom` that are inside the bounds, and removes the rest. Clicks call one
`[JSInvokable]` method with the location's `Id`.

- Interop drops from about 3 calls per marker to 1 call per map render. Expanding or collapsing the
  map re-sends the list.
- The click handler uses the `Id`, which fixes the `Single` throw.
- The heating-score colour icons stay as they are. Leaflet.markercluster was the alternative, but
  its count bubbles hide those colours and add a dependency.
- If marker DOM count ever becomes the bottleneck, the next step is canvas circle markers
  (`preferCanvas`). It isn't needed at the counts above.

## Recommendation

Do both, Proposal 2 first. The map fix stands alone and makes the denser set from Proposal 1 safe.
With tiers in place, use the 75 → 40 km graded radius. Seed it with the shipped stations and drop the
country overrides. Sevilla comes back, and coverage roughly triples in the eastern US and Japan.

## Summary

Station selection moves from DBSCAN, which chains clusters across hundreds of kilometres, to greedy
selection whose radius shrinks with record length. That brings back stations like Sevilla and adds
about 750 stations. The map gets a precomputed minimum zoom per location and one JS-managed marker
layer, so markers never overlap and render cost no longer grows with the station count.

### Change size

**Projects:** `ClimateExplorer.Core`, `ClimateExplorer.Data.Ghcnm`, `ClimateExplorer.WebApi`,
`ClimateExplorer.Web.Client`, `ClimateExplorer.Core.UnitTests`.
Not changed: `ClimateExplorer.WebApiClient` (the `Location` model carries the new field).

| Area | Added | Changed | Removed | Lines |
|---|---|---|---|---|
| Core: greedy spacing (km and pixel variants), `Location.MapMinZoom` | 1 | 1 | 0 | +90, ~3 |
| Data.Ghcnm: replace DBSCAN, drop overrides, seed with shipped stations | 0 | 2 (`Program.cs`, csproj) | 1 (`GeoListSpatialIndex.cs`) | ~70, −52 |
| `Directory.Packages.props`: remove `Dbscan` | 0 | 1 | 0 | ~1 |
| WebApi: set `MapMinZoom` on location load | 0 | 1 | 0 | ~20 |
| Web.Client: `MapContainer` marker code, new JS module | 1 | 1 | 0 | +80, ~130 (mostly deleted) |
| Tests: Core.UnitTests for both spacing passes | 1 | 0 | 0 | +120 |
| **Total** | **3** | **6** | **1** | **~+290 new, ~220 changed, −52** |

Regenerated data (`Locations_ghcnm_adjusted.json`, `Stations_ghcnm_adjusted.json`,
`DataFileMapping_ghcnm_adjusted.json`, `GHCNm.zip`) needs a GHCNm importer and DataPipeline run. That
isn't counted above.

**Risks:** about 135 location URLs stop working. 101 of those break on any re-run. Station data files
grow by about a third. The `NearbyLocations` and heating-score passes over the full location list
scale with the count; worth a quick timing after the regenerate.

## Addendum — implementation notes (Proposal 2, 2026-11-12)

What shipped:

- `ClimateExplorer.Web.Client/Services/Map/MapMarkerZoomTiers.cs` calculates each location's minimum
  zoom. It uses a 36 × 44 px marker box, zooms 0–12, and a 1-marker grid per zoom. Markers that
  still overlap at zoom 12 (the same coordinates) show from 12 on.
- `ClimateExplorer.Web.Client/Components/MapContainer.razor.js` is the marker layer.
  `createMarkerLayer(map, dotNetRef, markers, selectedId)` returns an object with `setLocations`,
  `setSelected` and `dispose`. It shows markers with `minZoom <= zoom`, plus the selected location,
  inside the view padded by 10%. It adds and removes them on `moveend`. Tooltips are text nodes, not
  HTML. Each marker's `alt` is the location's full title, which gives it an accessible name. It was
  the heating-score number before.
- `MapContainer.razor.cs` sends the markers once per map. Expanding or collapsing creates a new
  map, so it disposes the layer and creates another. The tiers are cached per `Locations` instance.
  Clicks come back through `[JSInvokable] OnMarkerClick(Guid)`. Removed: per-marker DPBlazorMapLibrary
  calls, `MarkerOptions`/`IIconFactory`, the `OnMoveEnd` handler, bounds filtering in C#, and the
  0.1° `Single` lookup. The component is now `IAsyncDisposable`.
- Tests: `ClimateExplorer.Web.UnitTests/MapMarkerZoomTiersTests.cs` (8). They include an invariant
  check: no two markers shown at any zoom overlap, across 400 random locations.

Deviations from the plan:

- **Tiers are computed in the client, not the WebApi.** Priority is the record length `Location`
  already carries: `LastYear` minus the full-period trend's `FirstYear`, plus one. Locations under
  30 years have no trend, so they rank last. No API, model or cache change, and no long-term cache
  rebuild. Record length ignores gaps, which station score counts. It's close enough for map
  priority. Every source (BOM, HadObs, GHCNm) gets the same measure.
- Pre-existing (BOM) locations aren't ranked first. They compete on record length like the rest.

Not verified here: the map in a browser. Per AGENTS.md only the build and unit tests were run (solution
builds with 0 warnings; `ClimateExplorer.Web.UnitTests` 510 passed). Worth checking by hand:

- markers appear and thin out when you zoom out
- clicking a marker, and pressing Enter on a focused one, changes location
- the selected location's marker shows at every zoom
- expanding and collapsing the map keeps the markers
- a view across the antimeridian (NZ, Fiji)

The Playwright `ChartTests` still selects `.leaflet-marker-icon`, which Leaflet's `L.marker` keeps.

## Addendum — implementation notes (Proposal 1, 2026-11-12)

Rules as implemented in `ClimateExplorer.Data.Ghcnm/Program.cs`:

- **Recency:** last complete year ≥ current year − 10, as before. The − 3 and − 5 options above were
  considered and not taken.
- **History:** at least 60 complete years (`AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly`),
  as before.
- **Recent completeness (new):** at least 20 complete years in the last 30 (current year − 30 to
  current year − 1), counted by `GetCompleteYearsInRecentPeriod`.
- **Country top-up:** unchanged. These stations skip the recent-completeness rule.
- **Spacing:** `StationSpacingSelector` (new, in `ClimateExplorer.Core`) replaces DBSCAN.
  - It works per country, with the pre-existing (BOM, UK) locations as seeds.
  - The radius is 25 km for a 60-year record, falling to 10 km at 120 years (`GetSpacingRadiusKm`).
  - Priority: stations in the last run's `Stations_ghcnm_adjusted.json` go first, then by complete
    years, then by ID.
  - The `DE`/`US` distance overrides are gone.
- **Duplicate removal:** `RemoveDuplicateLocations` now matches pre-existing locations within the same
  country only. It also removes every match, where `SingleOrDefault` would have thrown on two.

**Canadian renumbering.** Current GHCNd uses `CAN0` IDs as well (8,447, and no `CA00`). The August
local copy of `ghcnd-stations.txt` was out of date. GHCNm precipitation files exist under both forms.
So the new IDs are kept everywhere. `GhcnmStationIds.GetPreviousId` maps `CAN0nnnnnnn` →
`CA00nnnnnnn` for two things only:

- carrying the old location GUID into `GhcnIdToLocationIds.json`
- recognising a renumbered station as previously selected

The odd `CAN030828JF` (not 7 digits) is left alone.

**`Folders.SelectedStationsFile`** pointed at `ClimateExplorer.Data.Ghcnm\bin\Debug\net10.0\...` after the
move to `net11.0`. GHCNd and precipitation would have read a stale list. It now points at
`WebApi\MetaData\Station\Stations_ghcnm_adjusted.json`, which the GHCNm importer writes with the same
stations, and `selected-stations.json` is no longer written.

**Other changes:**

- `CreateStationDataFiles` uses a `HashSet` instead of `SingleOrDefault` for each line of the data file.
- The `Dbscan` package and `GeoListSpatialIndex.cs` are removed. The `GeoCoordinate.NetStandard1`
  reference is removed from `ClimateExplorer.Data.Ghcnm`; `ClimateExplorer.Core` brings it in.
- `GhcndBulkStationFileCache` skips a station GHCNd doesn't have (404) and logs it, instead of stopping
  the run. Any other error still throws. The first run stopped on `NZM00093780`. 12 of the 3,195
  selected stations aren't in GHCNd (5 Mexico, 3 Fiji, 2 Sint Maarten/Curaçao, 1 New Zealand,
  1 Jan Mayen); they get GHCNm monthly data only. Tests: `GhcndBulkStationFileCacheTests` (2).
- Tests: `ClimateExplorer.Core.UnitTests/StationSpacingSelectorTests.cs` (6). They include the chaining
  case: a–b–c, 11 km apart, keeps a and c.

**Expected result.** A Python re-implementation of these rules on the 2026-11-10 GHCNm file gives
3,195 stations (1,901 today):

| | Count |
|---|---|
| Today's stations kept | 1,768 |
| Today's stations dropped | 133 |
| New | 1,427 |

By country: US 1,189, JP 129, DE 104, CA 73, ES 41 (Sevilla included), AU 30, GB 22.

GHCNd for the new stations is about +280 MB, so Datasets ends up near 750 MB of the 1 GB cap.

**To run** (in order):

1. `ClimateExplorer.Data.Ghcnm`. The first run adds the `CAN0` IDs to `GhcnIdToLocationIds.json` and
   exits ("PLEASE RE-RUN"). Rebuild so the updated file is copied to `bin`, then run it again.
2. `ClimateExplorer.Data.Ghcnm.Precipitation`
3. `ClimateExplorer.Data.Ghcnd`
4. `ClimateExplorer.DataPipeline`

Then rebuild the WebApi location cache.

**DataPipeline fix (2026-11-12).** The first DataPipeline run failed: "6112 source files were not
packaged". Two causes:

- `Data.Ghcnd` still wrote split CSVs (`SourceData/GHCNd/Temperature/[station].csv` and
  `Precipitation/[station].csv`), the layout the July download plan replaced with one archive per station.
  DataPipeline only copies `SourceData/GHCNd/[station].zip`, so 6,109 CSVs were left out, and the 1,854
  archives it did copy were the old station set. `Data.Ghcnd` now writes `GHCNd/[station].zip` through
  `GhcndStationArchiveBuilder.WriteAsync`, the same layout the Web API's runtime refresh writes. It deletes
  the old archives and the legacy split folders first.
- DataPipeline's loose-file list was missing `CO2/co2_daily_mlo.txt`, `Glaciers/wgms-glacier-mass-balance-index.csv`
  and `Met/meantemp_daily_totals.txt`, which were added to `Datasets` directly.

DataPipeline now logs each step with counts, sizes and time, shows progress through long steps, and on
failure lists the unpackaged files grouped by folder. Re-run `Data.Ghcnd` (the downloaded CSVs are reused),
then DataPipeline.

**Result (2026-11-12).** DataPipeline packaged 13,066 source files into 3,393 assets. The final move into
`Datasets` needed a few retries while the new archives were scanned. `Datasets` is now 892 MB
(471 MB before), of the 1 GB cap:

| | Before | After |
|---|---|---|
| GHCNd archives | 1,854, 312 MB | 3,146, 731 MB |
| Everything else | 159 MB | 161 MB |

GHCNd grew more than the +280 MB estimate because many of the old archives were truncated, not
only because there are more stations. A sample of 21 old archives: `AE000041196` held 1955-1960 (2,184 days)
and now holds 1944-2025 (19,773); `LG000026313` held 2022-2025 and now 1941-2026; many others stopped
in 2015-2021 and now run to 2025-2026. The estimate was based on the old archives' size per year, so it
undercounted.
