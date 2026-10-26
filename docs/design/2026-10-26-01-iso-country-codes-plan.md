# ISO country codes

- **Date:** 2026-10-26
- **Status:** Implemented 2026-10-26 (see addendum)
- **Author:** Patrick (with Claude)
- **Scope:** `ClimateExplorer.Data.Ghcnm` (FIPS to ISO mapping after download),
  `ClimateExplorer.WebApi` (`MetaData/countries.txt`, location and station files),
  `ClimateExplorer.Data.Bom`, `ClimateExplorer.Data.Misc` (country list and location copies, BOM's
  hard-coded code)
- **Followed by:** [Chart title shows only places](2026-10-26-02-chart-title-places-plan.md)
- **Branch context:** written on `issues/us-units`

## Problem

`Location.CountryCode` holds the FIPS 10-4 codes that GHCNm uses, not ISO 3166-1 codes, so the codes mislead:
`AS` is Australia (ISO: American Samoa), `CH` is China (ISO: Switzerland), `SZ` is Switzerland (ISO: Eswatini),
`GM` is Germany (ISO: Gambia). `Location.DisplayTitle` already falls back to the code when `Name, Country` is
longer than 30 characters, so readers see FIPS codes today. The planned chart title
([Chart title shows only places](2026-10-26-02-chart-title-places-plan.md)) shows codes more often: a title like
`Bern, SZ | Beijing, CH | Canberra, AS` would be read wrongly.

## Where the FIPS codes come from

GHCNm station IDs start with a FIPS country code (`MetaData/readme.txt` in `ClimateExplorer.Data.Ghcnm`: "First
two characters are FIPS country code"). We download GHCNm as-is and can't change it; every new download will
still have FIPS codes. `StationFile.Load` takes the first two characters of the ID, checks them against
`ghcnm-countries.txt` and stores them as `Station.CountryCode`. `Program.cs` copies them into each `Location`, and
the results are written to `ClimateExplorer.WebApi/MetaData`.

The same FIPS list was then copied around, and other sources were given FIPS codes to match:

| Where | What | FIPS codes |
|---|---|---|
| `Data.Ghcnm/MetaData/ghcnm-countries.txt` | GHCNm's code list (FIPS) | 242 |
| `Data.Ghcnm/MetaData/pre-existing-locations.json` | copy of BOM and HadObs locations, used to avoid overlap | 112 × `AS`, 1 × `UK` |
| `Data.Ghcnm/Program.cs` `countryDistanceOverride` | clustering distance per country | `GM`, `US` |
| `Data.Bom/Bom/BomLocationsAndStationsMapper.cs` | hard-coded | `AS` (2 places) |
| `Data.Bom/MetaData/ACORN-SAT/Locations.json` | BOM input | 112 × `AS` |
| `WebApi/MetaData/countries.txt` and identical copies in `Data.Bom`, `Data.Misc` | code to name, read by `Location.GetLocationsFromFile` and `/country` | 242 |
| `WebApi/MetaData/Location/Locations_ghcnm_adjusted.json` | output of `Data.Ghcnm` | 1,901 locations, 196 codes |
| `WebApi/MetaData/Station/Stations_ghcnm_adjusted.json` | output of `Data.Ghcnm` | 1,901 stations |
| `WebApi/MetaData/Location/Locations_Australia.json`, `Station/Stations_Australia_unadjusted.json` | output of `Data.Bom` | 112 and 205 × `AS` |
| `WebApi/MetaData/Location/Locations_UK_HadObs.json`, `Station/Stations_UK_HadObs.json` | hand-maintained | 1 × `UK` each |

## Target

- GHCNm's own files keep their FIPS codes, and the station IDs keep their FIPS prefix.
- FIPS codes exist **only inside `ClimateExplorer.Data.Ghcnm`**, from reading a station ID up to the point
  where the code is mapped. The FIPS to ISO mapping lives in that project and nowhere else.
- Everything `ClimateExplorer.WebApi` loads or serves uses ISO 3166-1 alpha-2 codes: location and station files,
  `countries.txt`, and the `/country` endpoint.
- The other data projects (`Bom`, `Misc`) write ISO codes directly. They have no FIPS step.

## Design

### 1. Mapping file in `Data.Ghcnm`

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

### 2. Map when stations are read

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

### 3. The station cache

`Output\MetaData\stations.json` caches the stations after `StationFile.Load`, with codes from earlier runs. Some
FIPS codes are also valid ISO codes for a different country (see below), so there's no way to tell an old
cache from a new one by looking at it. Rename the cache file (e.g. `stations-iso.json`) so an old cache
is never read. `MetaData\stations.csv` has no country column and is unaffected.

### 4. Station IDs stay as they are

`Station.Id` and the `DataFileMapping` IDs stay GHCN IDs (e.g. `AE000041196`), because they name the data
files. They're opaque identifiers: no code in `Core`, `WebApi` or `Web.Client` gets a country from them (I
checked for `Id[..2]` / `Substring(0, 2)` and found none). That must stay true; only `StationFile.Load` reads
the prefix.

### 5. WebApi country list and the other data projects

- `WebApi/MetaData/countries.txt` becomes an ISO 3166-1 alpha-2 list, in the same `CODE Name` format that
  `Country.GetCountries` already reads. Keep the `[Owner]` suffix on dependent territories (`GL Greenland
  [Denmark]`), because `Location.CountryWithoutOwner` and the title scheme rely on it. Use the full ISO list
  (~249 entries) rather than just the codes in use today, so new locations work without editing it.
- The two identical copies in `Data.Bom` and `Data.Misc` are deleted. Each csproj links to the
  WebApi file instead (`<Content Include="..\ClimateExplorer.WebApi\MetaData\countries.txt" Link="MetaData\countries.txt" .../>`),
  so there is one country list. `Data.Ghcnm` doesn't need it.
- `Data.Bom`: `CountryCode = "AS"` → `"AU"` in the mapper, and `AS` → `AU` in `MetaData/ACORN-SAT/Locations.json`.
- The `/country` endpoint returns ISO keys with no code change. Nothing in the UI calls it today
  (`DataService.GetCountries` has no callers).

### 6. Converting the committed WebApi files

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

## Codes that don't map one-to-one

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

## Tests

- `FipsToIsoMapper`: maps a plain code (`AS…` → `AU`), applies a station override before the prefix
  (`NT` station → `SX`/`CW`), returns false for an unknown code and for an `NT` station without an override.
  `ClimateExplorer.UnitTests` doesn't reference `Data.Ghcnm` today; add the project reference.
- Mapping file consistency: every FIPS code appears once, and every ISO target is a key in
  `WebApi/MetaData/countries.txt`.
- WebApi data: every `CountryCode` in `MetaData/Location/*.json` and `MetaData/Station/*.json` is a key in
  `countries.txt`. This can't catch every leftover FIPS code, because most of them are also valid ISO codes.
  So also pin a few known locations: Canberra → `AU`, a Swiss GHCNm location → `CH`, Beijing → `CN`, a
  German location → `DE`, the HadObs location → `GB`.

## What users see

- `DisplayTitle` uses the ISO code when it falls back to a code (`… , AU` instead of `… , AS`).
- The Locations page shows the code only when `Country` is null, which can't happen after the list is
  converted; its filter and sort use the country name.
- I found no URL or saved chart state that contains a country code.

## Summary

`CountryCode` moves from GHCNm's FIPS codes to ISO 3166-1 codes everywhere outside
`ClimateExplorer.Data.Ghcnm`. GHCNm downloads are unchanged. `StationFile.Load` maps each station's FIPS prefix
to ISO with a mapping file that lives only in `Data.Ghcnm`, with per-station overrides where a FIPS country was
split. The WebApi country list becomes ISO and is the only copy. BOM's hard-coded code changes from `AS` to
`AU`, and the committed WebApi location and station files are converted once.

### Change size

**Projects:** `ClimateExplorer.Data.Ghcnm`, `ClimateExplorer.WebApi` (data files only),
`ClimateExplorer.Data.Bom`, `ClimateExplorer.Data.Misc` (csproj and data copies only), and
`ClimateExplorer.UnitTests`.

Unchanged: `ClimateExplorer.Web`, `ClimateExplorer.Web.Client`, `ClimateExplorer.Core`,
`ClimateExplorer.WebApiClient`, WebApi code.

As shipped (see addendum for the differences from the plan):

| Area | Added | Changed | Removed | Lines (approx.) |
|---|---|---|---|---|
| `Data.Ghcnm`: `FipsToIsoMapper` | 1 | – | – | +80 |
| `Data.Ghcnm`: mapping file (replaces `ghcnm-countries.txt`) and station overrides | 2 | – | 1 | +249 / −242 |
| `Data.Ghcnm`: `StationHistoryFileProcessor.cs` (`StationFile`), `Program.cs`, csproj | – | 3 | – | ~30 |
| `Data.Ghcnm`: `pre-existing-locations.json` | – | 1 | – | ~113 |
| `Data.Bom`: mapper, `ACORN-SAT/Locations.json`, csproj link | – | 3 | 1 (`countries.txt`) | ~120 |
| `Data.Misc`: csproj links | – | 1 | 4 (`countries.txt`, 3 location copies) | ~30, −20,540 |
| `WebApi/MetaData/countries.txt` (ISO list) | – | 1 | – | ~160 |
| `WebApi` location and station files (`CountryCode` lines only) | – | 6 | – | ~2,590 |
| Tests: `FipsToIsoMapperTests`, `CountryCodeDataTests`; UnitTests csproj reference | 2 | 1 | – | +183, ~1 |
| **Total** | **5** | **16** | **6** | **~510 new, ~3,040 changed (≈2,980 of it data lines); ~21,020 removed, almost all stale copies** |

The plan estimated ~4,120 changed lines in the WebApi files; only 2,591 `CountryCode` values differ, because
the 765 GHCNm locations whose FIPS code is the same in ISO (e.g. `US`, `CA`, `FR`) don't change.

**Risk.** The main risk is a file missed by the conversion. Because many FIPS codes are also valid ISO codes, a
missed file shows the wrong country instead of failing. The pinned-location tests and the single country list
reduce that risk. The second risk is running the conversion twice: American Samoa goes `AQ` → `AS` on the
first run and `AS` → `AU` (Australia) on the second. The script is run once and thrown away, and the next GHCNm
run is checked against it.

## Addendum — implementation notes

Shipped as designed, with these differences and findings:

- **Station overrides are a second file**, `ghcnm-station-iso-overrides.txt` (`<station ID> <ISO> <name>`):
  `NTM00078866` Princess Juliana Intl → `SX`, `NTM00078988` Hato → `CW`. Both mapping files allow `#` comment
  lines; `FipsToIsoMapper.ReadCodes` throws on a duplicate key or a malformed line.
- **GHCNm's list has 242 codes, not 241.** It includes `XX` Unknown, which maps to `--` (no station uses it).
  It also has `NN` Saint Maarten → `SX` and `UC` Curacao → `CW`, separate from `NT`. The three US atolls `JQ`
  Johnston, `LQ` Palmyra and `MQ` Midway also map to `UM`, alongside `WQ` Wake (none of those three are in use).
- **More codes change meaning than the table above lists**, e.g. `MF` (FIPS Mayotte → `YT`; ISO `MF` is Saint
  Martin) and `PS` (FIPS Palau → `PW`; ISO `PS` is Palestine). The mapping file covers all of them; the
  conversion script checked each location's old code against its station ID's FIPS prefix before changing it.
- **`Data.Misc` had its own copies of the three location files** (`MetaData/Location/*.json`, read by
  `Location.GetLocations()` in `BuildStaticContent`), two of them stale and all with FIPS codes. The plan
  missed them. They're deleted and the csproj now links `..\ClimateExplorer.WebApi\MetaData\Location\*.json`,
  the same way it links `countries.txt`.
- **Country names.** `countries.txt` is the full ISO list (249 entries). Names for codes already in use keep
  GHCNm's wording (e.g. `Burma`, `Korea, South`, `Swaziland`), so no country name users see changes, except for
  the merged or split codes: `TF` French Southern Territories [France], `SJ` Svalbard and Jan Mayen [Norway],
  `UM` United States Minor Outlying Islands [United States of America], `SX` Sint Maarten [Netherlands], `CW`
  Curacao [Netherlands]. Typos fixed: `Dijibouti` → `Djibouti`; `Montserrat` gained its `[United Kingdom]` owner.
- **`EnsureCountryRepresentation`** logs the ISO code instead of the country name, because the GHCNm name list
  is no longer loaded.
- **Pinned-location tests** cover Canberra `AU`, Pago Pago `AS`, Mawson `AQ`, Zuerich Fluntern `CH`, Beijing
  `CN`, Berlin Dahlem `DE`, Central England `GB`, and the two former `NT` stations.

**Follow-up:** the committed WebApi long-term cache (`cache-longterm/93d6f0e9-….json`, the locations list with
`CountryCode` and `Country`) still holds FIPS codes and must be regenerated with `ClimateExplorer.CachingTool`
before this is deployed.
