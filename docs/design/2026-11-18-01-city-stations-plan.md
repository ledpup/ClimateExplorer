# City stations

- **Date:** 2026-11-18
- **Status:** Implemented 2026-11-18 (code); see the addendum. The importers haven't been re-run yet.
- **Author:** Patrick (with Claude)
- **Scope:** `ClimateExplorer.Data.Ghcnm` (station selection, location names, GeoNames extract),
  `ClimateExplorer.Core` (new `CityStationSelector`; `Location.StationName` and titles), `ClimateExplorer.WebApi`
  (station names in the data sources panel), `ClimateExplorer.Web.Client` (About page credit)
- **Builds on:** [Station selection and map density](2026-11-12-01-station-selection-and-map-density-plan.md),
  in particular its isolation rule addendum
- **Branch context:** written on `development`

## Question

Geelong, Wollongong and Bendigo still have no location. Every station in those towns fails the 60
complete years rule, and the isolation rule doesn't bring them back, because each has a shipped location
within 75 km. These are places people search for.

Instead of asking "is this station far from others?", ask "does this city have a station?". Match stations to
a list of cities with their populations. Where a city has no location nearby and a decent station near its
centre, add that station and name it after the city.

## Method

- Cities: GeoNames `cities15000.zip` (every place of 15,000 or more people), downloaded 2026-11-18.
  34,247 rows: 11,578 of 50,000 or more and 5,871 of 100,000 or more, after dropping suburbs (below).
- Stations: the GHCNm file the last plan used (`ghcnm.tavg.v4.0.1.20261110.qcf`), with complete years,
  complete years in the last 30, and last year with any value per station.
- Kept stations: the shipped `MetaData` (5,422 GHCNm stations, plus the BOM locations and Central England
  from HadObs). The script splits the GHCNm stations into those the spacing step kept (60 or more complete
  years, or in a country with fewer than 5 of those, standing in for the country top-up) and those the
  isolation rule added. Then it re-runs isolation on its own. It adds 1,480 stations against 1,482 shipped,
  so the split is close.
- The script, the GeoNames download and the results are in the session scratchpad, not the repo.

## Findings

### The three towns

The nearest shipped location to each town's centre:

| City | Population (GeoNames) | Nearest | 2nd |
|---|---|---|---|
| Geelong | 282,809 | Laverton, 48 km | Melbourne, 65 km |
| Wollongong | 280,153 | Nowra, 67 km | Sydney, 69 km |
| Bendigo | 103,034 | Maryborough, 59 km | Echuca, 79 km |

Laverton is Melbourne's station, but it's 48 km from Geelong, so a "nothing within 50 km" test would still
block Geelong. 30 km works for all three.

Each town has a station near its centre that is still reporting:

| City | Station | From centre | Complete years | Complete years, last 30 |
|---|---|---|---|---|
| Geelong | Avalon Airport | 16.7 km | 27 | 27 |
| Wollongong | Bellambi AWS | 6.9 km | 21 | 21 |
| Bendigo | Bendigo Airport | 4.6 km | 32 | 29 |

The importer's existing rules turn down the two poorer candidates:

- Port Kembla Signal Station (Wollongong, 6.2 km) has 23 complete years, all of them old. It has data in only
  2 of the last 30 years, so it fails "10 complete years in the last 30". Without that rule, it would have
  been picked over Bellambi.
- Grovedale (Geelong Airport, 8.5 km) closed in 2011, so it fails "a value in the last 10 years".

### Measure from the city, not the station

The isolation rule measures from the candidate station. Avalon is 17 km from Geelong but 31 km from
Laverton, so it's blocked even though Geelong itself has nothing within 48 km. Measuring from the city centre
asks the right question: does this city have a location?

## Proposal

### Cities

Use GeoNames `cities15000`. Keep a filtered extract in the repo, so runs can be repeated and names don't
change because GeoNames changed:

- `ClimateExplorer.Data.Ghcnm/MetaData/geonames-cities.tsv`: cities of 50,000 or more, about 11,600 rows,
  about 0.5 MB.
- Columns: GeoNames ID, ASCII name, latitude, longitude, country code, population.
- Drop suburbs and other non-city places: feature codes `PPLX` (section of a populated place), `PPLH`, `PPLQ`
  and `PPLW` (historical, abandoned, destroyed). `PPLX` alone is 417 of the 100,000+ rows.
- If the file is missing, the importer downloads `cities15000.zip`, filters it and writes it. Delete it to
  refresh, as with `GhcnIdToLocationIds.json`. Note the date and licence in `MetaData/readme.txt`.

### The rule

No new year rules. A candidate passes the importer's existing ones:

- `minimumCompleteYearsInRecentPeriod` (10) complete years in the last `recentPeriodYears` (30), from
  `GetCompleteYearsInRecentPeriod`. This is the spacing step's recent-completeness rule, and it implies the
  isolation rule's 10 complete years (`isolatedMinimumCompleteYears`).
- A monthly value in the last `isolatedRecentYears` (10), from `GetLastYearWithAnyValue`, as for isolation.

In population order, largest first, for each city of **50,000 or more**:

1. **Covered?** If a kept location in the same country (a pre-existing one, one spacing kept, or one this rule
   has already added) is within **30 km of the city centre**, skip the city.
2. **Candidates:** stations in the same country, within **25 km of the city centre**, that aren't already
   kept and:
   - pass the two rules above
   - have no kept station within **10 km** of them (the spacing floor, the radius `GetSpacingRadiusKm` gives a
     120-year record)
   - aren't nearer the centre of a bigger city (of 50,000 or more) than this one
3. **Pick:** the importer's usual order: last run's stations first, then score, then ID.
4. The picked station is kept, so it covers smaller cities within 30 km of it.

Same country throughout, as for spacing: people look for a station in their own country. Without that,
Tijuana got Chula Vista (a US station, 17 km away).

The 10 km floor: a city can be more than 30 km from a kept station while its candidate, up to 25 km from the
centre on the near side, is as close as 5 km to it. Then the kept station, with the better record, already
serves the area, and another station isn't wanted just because the test now starts from the city centre. In the
simulation no pick was within 10 km of a pre-existing or spaced station. Two were within 10 km of another
city's pick, and the floor drops them.

The bigger-city test stops a station being named after the wrong place. Without it, Boca Raton got Fort
Lauderdale Executive Airport, which is in Fort Lauderdale (Fort Lauderdale already has a location). A stricter
test, "this city is the station's nearest city", also cost Johannesburg: its airport is nearer the centre of
Kempton Park, which GeoNames lists as a separate city.

### Order of rules

1. Filters and country top-up (unchanged)
2. Spacing (unchanged)
3. **City rule (new)**, seeded with the pre-existing locations and the spaced stations
4. Isolation, now seeded with the city stations too, so it doesn't add a second station next to one
5. `RemoveDuplicateLocations` over all three sets

### Names

A city station's location is shown as the city, then the station in brackets: **"Geelong (Avalon Airport)"**.
The city is what people search for, and the station says where the data comes from.

- `Location.Name` is the city: "Geelong". So the URL is `/location/geelong`, and the compact titles stay short.
- New `Location.StationName` (nullable): "Avalon Airport". Set only for city locations. Every other location
  is still named after its station, so it doesn't need one.
- The titles add the station when there is one:

  | Title | Used for | Geelong |
  |---|---|---|
  | `FullTitle` | search (location box and Locations page), map tooltip, page title, exports | Geelong (Avalon Airport), Australia |
  | `DisplayTitle` | dashboard heading, chart titles | Geelong (Avalon Airport), Australia, or `AU` past 30 characters, as now |
  | `CodeTitle`, `Name` | the dashboard's compact and minimal headings | Geelong, AU and Geelong |

  Search is a case-insensitive "contains" on `FullTitle`, prefix matches first, so "Geelong" and "Avalon" both
  find it.
- `StationName` is the GHCN name formatted the way the importer formats location names today (underscores to
  spaces, title case). GHCN names are cut at 30 characters and use abbreviations, so some read oddly
  ("Bellambi Aws", "Dar Es Salaam Airpo"), as station-named locations do today.
- `Stations_ghcnm_adjusted.json` doesn't change: `ASN00087113` is still `AVALON_AIRPORT`. `RemoveDuplicateLocations`
  and the GHCNd and precipitation importers read the station's name, so nothing changes for them.
- The data sources panel (`LocationDataSetMetadataSidePanel` → `AboutDataDetails`) already has a **Stations**
  table: station ID, station name, start and end dates, and a link to NOAA. It reads from
  `Stations_ghcnm_adjusted.json`, so Geelong's shows `ASN00087113` and `AVALON_AIRPORT` with no change. Format
  upper-case names there too (in `DataSetMetadataBuilder`), so it reads "Avalon Airport" for every GHCN location.
- The importer logs each match: city, population, station ID and name, distance from the centre.

Details:

- Use GeoNames' **ASCII name** ("Montreal", "Brasilia", "Muenster"). No location name has a non-ASCII
  character today, and neither `UrlReadyName` nor search ignores accents. 115 of the picks have accents in
  their proper name.
- The location keeps the **station's coordinates**. The data comes from there, and distances to nearby
  locations stay true. The median station is 5.8 km from its city centre, and 112 are more than 15 km away
  (Avalon is one), so the marker can sit off the town. Most of those are the city's airport. A few are named
  after a smaller nearby town ("Edmond (Guthrie Muni Ap)", 23 km away); the bracket makes that visible.
- 17 city names clash with an existing location's URL name (for example Hamilton, Wellington, Windsor,
  Kingston), and 3 repeat within the set (Hamilton, Gaya and Cordoba, each in two countries). As today,
  `/location-by-path` takes the first match, and the other is reachable from the map or by GUID.
- 343 stations the isolation rule added are renamed to their city. If that data has been deployed, their name
  URLs change; GUID links still work. Accepted.

### Credit

GeoNames is CC BY 4.0. Add it to the About page's data sources, with a line saying that some locations are
named after the nearest city from GeoNames.

## Results

Simulated as above. "Already shipped" means a station the isolation rule added and which now gets a city name;
"new" means not shipped today. "Net" is the change in GHCNm locations after the isolation rule re-runs (1,480
today).

| Cities of | Get a station | Already shipped | New | Isolation then adds | Net |
|---|---|---|---|---|---|
| ≥ 100K | 410 | 265 | 145 | 1,194 | +124 |
| **≥ 50K (chosen)** | **609** | **343** | **266** | **1,103** | **+232** |

Going down to 50,000 doesn't take anything from the bigger cities: the cities of 100,000 or more get the same
stations either way, and about 200 cities of 50,000–100,000 are added. New stations rise from 145 to 266, but
the net change in locations is +232, because the isolation rule then adds fewer.

Chosen row:

- Most added: US 73, Brazil 57, India 47, Mexico 44, Algeria 36, Russia 34, Canada 31, Turkey 25. Examples:
  Karachi (Jinnah Intl), Johannesburg (Johannesburg Intl), Rio de Janeiro (Galeão), Santiago (Quinta Normal),
  Toronto (Toronto City), Dubai (Dubai Intl). Of the 50,000–100,000 cities, Australia gets Bunbury.
- **Cost.** About 232 more GHCNd archives, roughly +50–60 MB (about 0.23 MB each today). `/location` grows by
  about 18 KB compressed (about 75 bytes per location, plus `StationName` on the city locations).

The other figures in this plan (names, distances, clashes) are from the run without the 10 km floor, which
had 2 more picks.

### What it doesn't fix

2,950 cities of 100,000 or more still have no location: no GHCNm station passes the rules within 25 km. Most
are where GHCNm has few live stations:

| Country | Cities ≥ 100K | With no station near |
|---|---|---|
| China | 692 | 498 |
| India | 526 | 375 |
| Brazil | 346 | 189 |
| Nigeria | 112 | 108 |
| Pakistan | 134 | 104 |
| Philippines | 147 | 89 |
| UK | 104 | 66 |

The largest include Shenzhen, Istanbul, Lagos, Taipei, Hong Kong and Baghdad. Filling these would need another
source, which is a separate question.

The UK is a GHCNm gap, not a selection one. HadObs gives one UK location, Central England (HadCET). The other
24 are GHCNm stations. GHCNm has 186 UK stations, but 101 last reported in the 2010s and only 27 have a value
since 2016. 24 of those 27 are already shipped. Of the other three, GHCNm's own Central England series is a
duplicate of the HadObs one, and Foula and Mildenhall have too few complete years (11 and 9). So the city rule
can't add UK cities. A UK source would be a separate plan. One option is the Met Office historic station data:
about 37 stations, monthly, some of them in cities GHCNm misses (for example Sheffield, Bradford and Cardiff;
to be checked).

## Decisions

- **Renames:** accepted (see Names).
- **Close stations:** the 10 km floor (see The rule).
- **Showing the station:** in brackets after the city in the titles, not as a separate line on the dashboard.

## Implementation

1. **GeoNames extract.** In `ClimateExplorer.Data.Ghcnm`: create `MetaData/geonames-cities.tsv` if missing
   (download, filter, write), and add it as a content item in the `.csproj`. Add a note to `readme.txt`.
2. **`CityStationSelector`** in `ClimateExplorer.Core`, next to `StationSpacingSelector`: takes cities (largest
   first), seeds, candidates in priority order, a group key (country), the covered and candidate radii, and the
   floor. It returns (city, station) pairs.
3. **Tests** in `ClimateExplorer.Core.UnitTests/CityStationSelectorTests.cs`:
   - a covered city is skipped
   - a candidate beyond the candidate radius isn't picked
   - a candidate within 10 km of a kept station isn't picked
   - a picked station covers a smaller city near it
   - Geelong's case: kept station 48 km from the city and 31 km from the candidate, candidate 17 km from the
     city → added
   - a station in another country doesn't cover a city and isn't picked for it
   - a station nearer a bigger city's centre isn't picked for the smaller city
4. **`Location`** in `ClimateExplorer.Core`: add `StationName`, and use it in `FullTitle` and `DisplayTitle`.
   Tests for both titles, with and without a station name, including the 30-character country fallback.
5. **`Program.cs`**: load cities of 50,000 or more, filter candidates with the existing
   `completeYearsInRecentPeriod` and `lastYearsWithAnyValue` lookups, run the selector between spacing and
   isolation, and seed isolation with the result. For those locations, set `Name` to the city and `StationName`
   to the station. New constants for the population threshold and the radii only.
6. **Station names** in `ClimateExplorer.WebApi/DataSetMetadata/DataSetMetadataBuilder.cs`: format upper-case
   names. A test in `ClimateExplorer.WebApi.UnitTests`.
7. **About page**: GeoNames credit.
8. **Run**: as for the isolation rule. `Data.Ghcnm` (twice if new IDs are added), then
   `Data.Ghcnm.Precipitation`, `Data.Ghcnd` and `DataPipeline`, then rebuild the WebApi location cache.

## Summary

Adds a city rule between spacing and isolation. For each city of 50,000 or more (GeoNames) with no location
within 30 km in the same country, it keeps the best station within 25 km of the centre that passes the
importer's existing recency rules (10 complete years in the last 30, a value in the last 10), has no kept station
within 10 km and isn't nearer a bigger city. The location is shown as the city with the station in brackets,
"Geelong (Avalon Airport)", and the URL uses the city. Geelong, Wollongong (Bellambi AWS) and Bendigo (Bendigo
Airport) come back, along with 606 other cities, about 232 more locations overall.

### Change size

**Projects:** `ClimateExplorer.Data.Ghcnm`, `ClimateExplorer.Core`, `ClimateExplorer.Core.UnitTests`,
`ClimateExplorer.WebApi`, `ClimateExplorer.WebApi.UnitTests`, `ClimateExplorer.Web.Client`.

Not changed: `ClimateExplorer.Data.Ghcnd`, `ClimateExplorer.Data.Ghcnm.Precipitation`, `ClimateExplorer.DataPipeline`
(they read the station list as now). The Web.Client components that show titles don't change; they read
`FullTitle`, `DisplayTitle` and `CodeTitle` as now.

| Area | Added | Changed | Removed |
|---|---|---|---|
| `Data.Ghcnm`: `Program.cs`, `.csproj`, `readme.txt` | | 3 files, ~80 | |
| `Data.Ghcnm`: GeoNames extract (data) | 1 file, ~11,600 rows | | |
| `Core`: `CityStationSelector` | 1 file, +110 | | |
| `Core`: `Location` | | 1 file, ~20 | |
| `Core.UnitTests`: `CityStationSelectorTests` | 1 file, +170 | | |
| `Core.UnitTests`: `LocationTitleTests` | 1 file, +50 | | |
| `WebApi`: `DataSetMetadataBuilder` | | 1 file, ~15 | |
| `WebApi.UnitTests` | | 1 file, ~30 | |
| `Web.Client`: About page | | 1 file, ~5 | |
| **Total (code)** | **3 files, +330** | **7 files, ~150** | |

Re-running the importers also regenerates `Locations_ghcnm_adjusted.json`, `Stations_ghcnm_adjusted.json`,
`DataFileMapping_ghcnm_adjusted.json` and the GHCNd archives, as for any selection change.

## Addendum — implementation notes (2026-11-18)

Implemented as planned, with these details and deviations.

**Core**

- `CityStationSelector.Select` (new) takes the cities, seeds, candidates in priority order, a country-code and a
  coordinates function, and the three radii. It returns `CityStation<T>` records (city, station, distance from the
  centre in km). It sorts the cities largest first itself. A 1° latitude check skips most distance calculations.
- `City` (new record in `Model`): name, country code, coordinates, population.
- `Location.StationName` (new, nullable) and `Location.NameWithStation` ("Geelong (Avalon Airport)"), used by
  `FullTitle` and `DisplayTitle`. `CodeTitle` and `UrlReadyName` still use `Name` only.
- `Station.ToDisplayName` (new): underscores to spaces, and title case when the name has no lower-case letter.
  The importer and `DataSetMetadataBuilder` share it. The importer used `CurrentCulture` title case before and
  now uses invariant. Every GHCNm name is upper case, so existing location names don't change.

**Importer (`ClimateExplorer.Data.Ghcnm`)**

- `GeoNamesCities` (new) reads `MetaData/geonames-cities.tsv`. If the file is missing, it downloads
  `cities15000.zip`, filters it and writes both the `bin` copy and the project copy.
- **Deviation:** the source, date, licence, filter and refresh steps are recorded as `#` comment lines at the
  top of the extract, not in `MetaData/readme.txt`. That file is NOAA's GHCN-M readme.
- The committed extract (11,578 cities, 0.5 MB) was written from the 2026-11-18 download by a script that applies
  the same filter and format as `GeoNamesCities`.
- New constants: `cityMinimumPopulation` (50,000), `cityCoveredRadiusKm` (30), `cityCandidateRadiusKm` (25) and
  `spacingFloorKm` (10). `GetSpacingRadiusKm` now uses `spacingFloorKm` for its 120-year radius, so the floor is
  defined once.
- City candidates use the existing `completeYearsInRecentPeriod` and `lastYearsWithAnyValue` lookups, and are
  ordered last run's stations first, then score, then ID. Isolation is seeded with, and excludes, the spaced and
  city stations. `RemoveDuplicateLocations` runs over all three sets.
- Each match is logged with the city, population, station and distance.

**Web**

- `DataSetMetadataBuilder` formats station names with `Station.ToDisplayName`. This applies to every
  station-backed dataset, so BOM names change too ("KALUMBURU MISSION" → "Kalumburu Mission").
- About page: a GeoNames section after the data sources list, with the CC BY 4.0 credit.
- The `/location` endpoint returns full `Location` objects, so `StationName` reaches the client with no other
  change.

**Tests**

- `CityStationSelectorTests` (9 tests):
  - covered city
  - candidate radius
  - 10 km floor
  - priority order
  - a pick covering a smaller city, and largest first
  - Geelong, with real coordinates
  - other country
  - nearer a bigger city
  - nearer a smaller city (Johannesburg)
- `LocationTitleTests` (8 tests): the title properties, `UrlReadyName` and `ToDisplayName`.
- `DataSetMetadataBuilderTests`: one new test (`BuildAsync_UpperCaseStationName_ReturnsTitleCaseWithSpaces`).
- The solution builds with no warnings. Passing: `Core.UnitTests` 326, `WebApi.UnitTests` 225, `Web.UnitTests` 535.

**To run** (not yet done): `Data.Ghcnm` (twice if new IDs are added), then `Data.Ghcnm.Precipitation`,
`Data.Ghcnd` and `DataPipeline`, then rebuild the WebApi location cache. The `Data.Ghcnm` log should show
Geelong, Wollongong and Bendigo among about 609 city stations.
