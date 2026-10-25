# Chart title labelling, starting with ISO country codes

- **Date:** 2026-10-26
- **Status:** Proposed
- **Author:** Patrick (with Claude)
- **Scope:** Stage 1: `ClimateExplorer.Data.Ghcnm` (FIPS to ISO mapping after download),
  `ClimateExplorer.WebApi` (`MetaData/countries.txt`, location and station files),
  `ClimateExplorer.Data.Bom`, `ClimateExplorer.Data.Niwa`, `ClimateExplorer.Data.Misc` (country list copies, BOM's
  hard-coded code). Stage 2: `ChartLogic.BuildChartTitle`, `Location`.
- **Branch context:** written on `issues/us-units`

## Problem

Chart titles repeat text that the legend already shows. With one series, the title is
`Canberra, Australia | Mean temperature | Adjusted`, and the legend under the chart says
`Canberra | Mean temperature | Adjusted | °C` again. The title should only say *where*: the
`GeographicalEntity` name and, for a `Location`, its country. The subtitle (date range or bin granularity)
doesn't repeat anything and is out of scope.

The new title scheme (stage 2) shortens some countries to their `CountryCode`. Today those codes are
the FIPS 10-4 codes that GHCNm uses, not ISO 3166-1 codes, so they mislead: `AS` is Australia (ISO: American
Samoa), `CH` is China (ISO: Switzerland), `SZ` is Switzerland (ISO: Eswatini), `GM` is Germany (ISO: Gambia).
A title like `Bern, SZ | Beijing, CH | Canberra, AS` would be read wrongly. `Location.DisplayTitle` already falls
back to the code when `Name, Country` is longer than 30 characters, so readers see FIPS codes today.

Stage 1 fixes the codes, stage 2 changes the title, and series labels come later (see
[Later: series labels](#later-series-labels)).

## Stage 1: ISO country codes

### Where the FIPS codes come from

GHCNm station IDs start with a FIPS country code (`MetaData/readme.txt` in `ClimateExplorer.Data.Ghcnm`: "First
two characters are FIPS country code"). We download GHCNm as-is and can't change it; every new download will
still have FIPS codes. `StationFile.Load` takes the first two characters of the ID, checks them against
`ghcnm-countries.txt` and stores them as `Station.CountryCode`. `Program.cs` copies them into each `Location`, and
the results are written to `ClimateExplorer.WebApi/MetaData`.

The same FIPS list was then copied around, and other sources were given FIPS codes to match:

| Where | What | FIPS codes |
|---|---|---|
| `Data.Ghcnm/MetaData/ghcnm-countries.txt` | GHCNm's code list (FIPS) | 241 |
| `Data.Ghcnm/MetaData/pre-existing-locations.json` | copy of BOM and HadObs locations, used to avoid overlap | 112 × `AS`, 1 × `UK` |
| `Data.Ghcnm/Program.cs` `countryDistanceOverride` | clustering distance per country | `GM`, `US` |
| `Data.Bom/Bom/BomLocationsAndStationsMapper.cs` | hard-coded | `AS` (2 places) |
| `Data.Bom/MetaData/ACORN-SAT/Locations.json` | BOM input | 112 × `AS` |
| `Data.Niwa/Niwa/NiwaLocationsAndStationsMapper.cs`, NIWA reference files | hard-coded | `NZ` (also ISO, no change) |
| `WebApi/MetaData/countries.txt` and identical copies in `Data.Bom`, `Data.Niwa`, `Data.Misc` | code to name, read by `Location.GetLocationsFromFile` and `/country` | 241 |
| `WebApi/MetaData/Location/Locations_ghcnm_adjusted.json` | output of `Data.Ghcnm` | 1,901 locations, 196 codes |
| `WebApi/MetaData/Station/Stations_ghcnm_adjusted.json` | output of `Data.Ghcnm` | 1,901 stations |
| `WebApi/MetaData/Location/Locations_Australia.json`, `Station/Stations_Australia_unadjusted.json` | output of `Data.Bom` | 112 and 205 × `AS` |
| `WebApi/MetaData/Location/Locations_UK_HadObs.json`, `Station/Stations_UK_HadObs.json` | hand-maintained | 1 × `UK` each |

### Target

- GHCNm's own files keep their FIPS codes, and the station IDs keep their FIPS prefix.
- FIPS codes exist **only inside `ClimateExplorer.Data.Ghcnm`**, from reading a station ID up to the point
  where the code is mapped. The FIPS to ISO mapping lives in that project and nowhere else.
- Everything `ClimateExplorer.WebApi` loads or serves uses ISO 3166-1 alpha-2 codes: location and station files,
  `countries.txt`, and the `/country` endpoint.
- The other data projects (`Bom`, `Niwa`, `Misc`) write ISO codes directly. They have no FIPS step.

### Design

#### 1. Mapping file in `Data.Ghcnm`

Replace `ghcnm-countries.txt` with `MetaData/ghcnm-fips-to-iso.txt`, one line per GHCNm code:

```text
AS AU Australia
AQ AS American Samoa [United States of America]
AY AQ Antarctica
CH CN China
SZ CH Switzerland
NT -- Netherlands Antilles (split by station, see overrides)
```

Columns: FIPS code, ISO code, GHCNm's name. The name isn't used for mapping; it's there so a reviewer can check
each line. A `--` in the ISO column means the code can't be mapped as a whole and every station with it needs
an override.

Station overrides go in a short section of the same file (or a second file, `ghcnm-station-iso-overrides.txt`),
keyed by the full GHCN station ID:

```text
<Princess Juliana Intl station ID> SX
<Hato station ID> CW
```

A small `FipsToIsoMapper` class in `Data.Ghcnm` loads both and offers `TryMap(string stationId, out string iso)`:
check the station override first, then the FIPS prefix.

#### 2. Map when stations are read

`StationFile.Load` calls the mapper instead of the country dictionary and sets `station.CountryCode` to the
ISO code. If a code isn't mapped (a new GHCNm code, or an `NT` station with no override), it logs an error and
skips the station, as it does today for unknown codes. That makes a new code show up in the run log instead of
reaching the WebApi as FIPS.

After that point, everything in `Program.cs` uses ISO codes:

- `EnsureCountryRepresentation` loops over the distinct ISO codes from the mapper instead of
  `ghcnm-countries.txt`.
- `countryDistanceOverride` becomes `{ "DE", 45 }, { "US", 70 }`.
- `pre-existing-locations.json`: `AS` → `AU` and `UK` → `GB`, so pre-existing locations cluster with the
  GHCNm stations of the same country.

#### 3. The station cache

`Output\MetaData\stations.json` caches the stations after `StationFile.Load`, with codes from earlier runs. Some
FIPS codes are also valid ISO codes for a different country (see below), so there's no way to tell an old
cache from a new one by looking at it. Rename the cache file (e.g. `stations-iso.json`) so an old cache
is never read. `MetaData\stations.csv` has no country column and is unaffected.

#### 4. Station IDs stay as they are

`Station.Id` and the `DataFileMapping` IDs stay GHCN IDs (e.g. `AE000041196`), because they name the data
files. They're opaque identifiers: no code in `Core`, `WebApi` or `Web.Client` gets a country from them (I
checked for `Id[..2]` / `Substring(0, 2)` and found none). That must stay true; only `StationFile.Load` reads
the prefix.

#### 5. WebApi country list and the other data projects

- `WebApi/MetaData/countries.txt` becomes an ISO 3166-1 alpha-2 list, in the same `CODE Name` format that
  `Country.GetCountries` already reads. Keep the `[Owner]` suffix on dependent territories (`GL Greenland
  [Denmark]`), because `Location.CountryWithoutOwner` and the title scheme rely on it. Use the full ISO list
  (~249 entries) rather than just the codes in use today, so new locations work without editing it.
- The three identical copies in `Data.Bom`, `Data.Niwa` and `Data.Misc` are deleted. Each csproj links to the
  WebApi file instead (`<Content Include="..\ClimateExplorer.WebApi\MetaData\countries.txt" Link="MetaData\countries.txt" .../>`),
  so there is one country list. `Data.Ghcnm` doesn't need it.
- `Data.Bom`: `CountryCode = "AS"` → `"AU"` in the mapper, and `AS` → `AU` in `MetaData/ACORN-SAT/Locations.json`.
- `Data.Niwa`: no change. `NZ` is the same in both.
- The `/country` endpoint returns ISO keys with no code change. Nothing in the UI calls it today
  (`DataService.GetCountries` has no callers).

#### 6. Converting the committed WebApi files

Running the full `Data.Ghcnm` pipeline would also download the newest GHCNm. That changes which stations are
selected and rewrites about 3,800 data files, which is the annual refresh and shouldn't be mixed into this change.
Instead, convert the committed files once with the same mapping file, changing only the `CountryCode` lines:

| File | Change |
|---|---|
| `Locations_ghcnm_adjusted.json`, `Stations_ghcnm_adjusted.json` | FIPS → ISO by the mapping file and station overrides (station file keyed by `Id`; location file via `DataFileMapping_ghcnm_adjusted.json`) |
| `Locations_Australia.json`, `Stations_Australia_unadjusted.json` | `AS` → `AU` |
| `Locations_UK_HadObs.json`, `Stations_UK_HadObs.json` | `UK` → `GB` |

The conversion is a throwaway script and isn't committed. It must run once on the original FIPS files, never
on files that were already converted, because several codes change meaning (next section). The next annual
GHCNm run should produce the same codes; diff its `CountryCode` lines against the converted files to check.

### Codes that don't map one-to-one

**Codes that mean another country in ISO.** These are why the conversion can only run once, and why a missed
file fails silently instead of with an error (`Location.SetCountries` would find a different country under the
old code):

| FIPS | Country | ISO | What the FIPS code means in ISO |
|---|---|---|---|
| `AS` | Australia | `AU` | American Samoa |
| `AQ` | American Samoa | `AS` | Antarctica |
| `AY` | Antarctica | `AQ` | – |
| `CH` | China | `CN` | Switzerland |
| `SZ` | Switzerland | `CH` | Eswatini |
| `RS` | Russia | `RU` | Serbia |
| `RI` | Serbia | `RS` | – |
| `CW` | Cook Islands | `CK` | Curaçao |
| `CK` | Cocos (Keeling) Islands | `CC` | Cook Islands |
| `SX` | South Georgia | `GS` | Sint Maarten |
| `GM` | Germany | `DE` | Gambia |

These are examples from the 196 codes in use, not the full list. The mapping file covers all of them.

**Several FIPS codes become one ISO code.** The locations keep their own names; only the country changes.

| FIPS codes in use | Locations | ISO |
|---|---|---|
| `EU` Europa Island, `JU` Juan De Nova, `TE` Tromelin, `FS` French Southern and Antarctic Lands | Ile Europa, Ile Juan De Nova, Serge Frolow Ile Tromelin, Martin De Vivies I, Alfred Faure Iles Crozet, Iles Glorieuses | `TF` French Southern Territories [France] |
| `JN` Jan Mayen, `SV` Svalbard | Jan Mayen, Barencburg | `SJ` Svalbard and Jan Mayen [Norway] |
| `WQ` Wake Island | Wake Island | `UM` United States Minor Outlying Islands [United States of America] |

These names are longer than 30 characters, so `DisplayTitle` shows the code for them (`Ile Europa, TF`).

**One FIPS code splits.** `NT` Netherlands Antilles was dissolved in 2010. Its two stations get overrides:
Princess Juliana Intl → `SX` Sint Maarten [Netherlands], Hato → `CW` Curaçao [Netherlands].

**Sudan.** `SU` → `SD`. All five current stations (Wadi Halfa, Khartoum, Kassala, El Fasher, El Obeid) are in
Sudan, not South Sudan. A future South Sudan station under `SU` would need an override.

**Clustering side effect (next annual run only).** `SelectStationsByDbscanClusteringAndTakingHighestScore` and
`EnsureCountryRepresentation` group stations by country. When `TF`, `SJ` and `UM` are merged, those islands are
grouped together. They're hundreds of km apart, so the 75 km clustering doesn't change. But the "at least 5
stations per country" top-up now counts `TF` as one country with 6 stations instead of four countries with
fewer than 5 each, so a later run may add fewer stations there. This is acceptable.

### Tests

- `FipsToIsoMapper`: maps a plain code (`AS…` → `AU`), applies a station override before the prefix
  (`NT` station → `SX`/`CW`), returns false for an unknown code and for an `NT` station without an override.
  `ClimateExplorer.UnitTests` doesn't reference `Data.Ghcnm` today; add the project reference.
- Mapping file consistency: every FIPS code appears once, and every ISO target is a key in
  `WebApi/MetaData/countries.txt`.
- WebApi data: every `CountryCode` in `MetaData/Location/*.json` and `MetaData/Station/*.json` is a key in
  `countries.txt`. This can't catch every leftover FIPS code, because most of them are also valid ISO codes.
  So also pin a few known locations: Canberra → `AU`, a Swiss GHCNm location → `CH`, Beijing → `CN`, a
  German location → `DE`, the HadObs location → `GB`.

### What users see

- `DisplayTitle` uses the ISO code when it falls back to a code (`… , AU` instead of `… , AS`).
- The Locations page shows the code only when `Country` is null, which can't happen after the list is
  converted; its filter and sort use the country name.
- I found no URL or saved chart state that contains a country code.

## Stage 2: Chart title

The title is built by `ChartLogic.BuildChartTitle` and contains only places:

- **One location** (any number of series): `DisplayTitle`. This means removing the single-series branch that
  calls `GetFriendlyTitle`. The legend is always shown (`ChartOptionsFactory` sets `Legend.Position = "bottom"`
  and never hides it), so no series information is lost.
- **Several locations, same country:** names separated by commas, then `|`, then `CountryWithoutOwner`, e.g.
  `Canberra, Hobart | Australia`.
- **Two countries:** each `DisplayTitle`, separated by `|`.
- **More than two countries:** `Name, CountryCode` for each, separated by `|`. After stage 1 these are ISO
  codes.
- **A location and a region:** `DisplayTitle | Region.Name`. Locations always come before regions.

`Location.CountryWithoutOwner` becomes public. Same-country checks compare `CountryCode`, not the name with the
owner removed, so Nuuk (`GL`) and Copenhagen (`DK`) stay different countries. Regions are recognised with
`Region.IsRegionId` / `Region.GetRegion`; they aren't in the `Locations` dictionary that `ChartView` passes in.

### Decisions needed before stage 2

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
after stage 2, and the title rules above are meant to make it possible: once the title names the place, a
single-location chart can drop the location from each legend label.

## Summary

Stage 1 moves `CountryCode` from GHCNm's FIPS codes to ISO 3166-1 codes everywhere outside
`ClimateExplorer.Data.Ghcnm`. GHCNm downloads are unchanged. `StationFile.Load` maps each station's FIPS prefix
to ISO with a mapping file that lives only in `Data.Ghcnm`, with per-station overrides where a FIPS country was
split. The WebApi country list becomes ISO and is the only copy. BOM's hard-coded code changes from `AS` to
`AU`, and the committed WebApi location and station files are converted once. Stage 2 rewrites the chart
title to show only places, after the decisions listed above are made.

### Change size

**Projects:** stage 1 changes `ClimateExplorer.Data.Ghcnm`, `ClimateExplorer.WebApi` (data files only),
`ClimateExplorer.Data.Bom`, `ClimateExplorer.Data.Niwa` and `ClimateExplorer.Data.Misc` (csproj only), and
`ClimateExplorer.UnitTests`. Stage 2 changes `ClimateExplorer.Web.Client`, `ClimateExplorer.Core` and
`ClimateExplorer.UnitTests`.

Unchanged: `ClimateExplorer.Web`, `ClimateExplorer.WebApiClient`, WebApi code.

| Area | Added | Changed | Removed | Lines (approx.) |
|---|---|---|---|---|
| **Stage 1** | | | | |
| `Data.Ghcnm`: `FipsToIsoMapper` | 1 | – | – | +60 |
| `Data.Ghcnm`: mapping file (replaces `ghcnm-countries.txt`) and station overrides | 1 | – | 1 | +245 / −241 |
| `Data.Ghcnm`: `StationFile.cs`, `Program.cs`, csproj | – | 3 | – | ~30 |
| `Data.Ghcnm`: `pre-existing-locations.json` | – | 1 | – | ~113 |
| `Data.Bom`: mapper, `ACORN-SAT/Locations.json`, csproj link | – | 3 | 1 (`countries.txt`) | ~118 |
| `Data.Niwa`, `Data.Misc`: csproj link | – | 2 | 2 (`countries.txt`) | ~8 |
| `WebApi/MetaData/countries.txt` (ISO list) | – | 1 | – | ~250 |
| `WebApi` location and station files (`CountryCode` lines only) | – | 6 | – | ~4,120 |
| Tests: mapper, mapping file, WebApi data codes; UnitTests csproj reference | 3 | 1 | – | +150, ~1 |
| **Stage 2** | | | | |
| `ChartLogic.BuildChartTitle` | – | 1 | – | ~60 |
| `Location.CountryWithoutOwner` made public | – | 1 | – | ~1 |
| Tests: `ChartLogicTitleTests` | – | 1 | – | ~130 |
| **Total** | **5** | **20** | **4** | **~455 new, ~4,830 changed (≈4,480 of it data lines: country codes and the country list)** |

**Risk.** The main risk is a file missed by the conversion. Because many FIPS codes are also valid ISO codes, a
missed file shows the wrong country instead of failing. The pinned-location tests and the single country list
reduce that risk. The second risk is running the conversion twice: American Samoa goes `AQ` → `AS` on the
first run and `AS` → `AU` (Australia) on the second. The script is run once and thrown away, and the next GHCNm
run is checked against it.
