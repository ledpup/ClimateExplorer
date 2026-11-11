# Station selection and map density

- **Date:** 2026-11-12
- **Status:** Map (Proposal 2) implemented 2026-11-12 (see addendum); station selection (Proposal 1)
  proposed
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
