# BOM on-request refresh via anonymous FTP

- **Date:** 2026-10-21
- **Status:** Implemented 2026-10-21 (see addendum)
- **Author:** Patrick (with Claude)
- **Scope:** `ClimateExplorer.Data.Downloading` (BOM downloader, coordinator), `ClimateExplorer.WebApi`
  (registration, ACORN-SAT extension service), `ClimateExplorer.Data.Bom` (mapping generator), BOM station metadata
- **Builds on:** [2026-07-17-01-acorn-sat-on-request-extension-plan.md](2026-07-17-01-acorn-sat-on-request-extension-plan.md)

## Problem

Recent Observations shows "Showing the most recently available data — couldn't fetch the latest observations"
on every visit to a BOM-backed location (reproduced three times in a row on 2026-10-21, on both Blazor Server
and WASM), even when the tiles already show yesterday's data. Page loads also take a very long time,
up to around the web client's 100 second `HttpClient` timeout.

## Root cause

The Bureau of Meteorology is actively blocking automated access to the Climate Data Online (CDO) pages that
`BomDailyDataClient` scrapes. Tested from a dev machine on 2026-10-21 against station 082039:

| Request | Result |
|---|---|
| `availableYears`, sequential, curl defaults | 200 after ~10 s (temperature/rain) and ~60 s (solar) |
| zip download, sequential, no cookies | **403**: "Your access is blocked due to the detection of a potential automated access request. The Bureau of Meteorology website does not support web scraping … you should stop", pointing to the anonymous FTP channel |
| 4 products in parallel with the app's browser headers + cookies (what `BomDataSetDownloader` does) | `availableYears` ~60 s each; zips: one 200 after 10 s, three **500** after ~60 s |

Each request's on-request refresh is capped at 30 s (`DataSetSourceUpdateCoordinator.DefaultOnRequestRefreshTimeout`),
so the refresh times out on every request and is reported as `RefreshFailed`. The tiles can still show current data
because an earlier refresh that succeeded is still being served: the ACORN-SAT extension cache returns its conclusive
overlay with `RefreshFailed = true`, or a refresh saved the new zip before failing.

Several problems in our own code make it worse:

1. **No backoff after a failure.** A failed refresh records nothing, so the next request (and every other request
   on the page) starts the same 30 s attempt again.
2. **Unbounded ACORN-SAT extension lock.** `AcornSatClimateRecordService.ResolveAsync` takes the per-location/data-type
   lock without any timeout, *before* the 30 s refresh window starts. Requests for the same series queue behind
   each other, so three or four of them reach roughly 100 s.
3. **Cancellation isn't fully honoured.** `BomDailyDataClient` builds a `ZipArchive` over the response stream. That
   stream can't seek, so the archive reads the whole body synchronously and ignores the cancellation token. Only the
   BOM `HttpClient`'s default 100 s timeout stops a stalled read, and the asset lock is held all that time.
4. **Browser impersonation.** `BomHttpClientFactory` sends a spoofed Chrome user agent and `Sec-Fetch-*` headers.
   That works around BOM's bot detection, which BOM has explicitly asked automated clients not to do.

## Proposal

Stop scraping CDO from the API. Refresh BOM daily data on request from BOM's anonymous FTP daily station tables,
which BOM names as the supported automated channel:

```
ftp://ftp.bom.gov.au/anon/gen/clim_data/IDCKWCDEA0/tables/{state}/{folder}/{folder}-{yyyyMM}.csv
```

Each file is ~2 KB per station-month. It holds daily max and min temperature, rain (0900–0900), solar radiation,
evapotranspiration, humidity and wind, through yesterday. Checked against CDO for Rutherglen Research (082039),
October 2026:

- Max and min temperature: identical on every day.
- Rain: identical, on the same date attribution.
- Solar: identical to one decimal place (FTP gives two decimals).
- Difference: CDO already had today's 09:00 minimum and rain; the FTP table only goes to yesterday. That's
  enough, because freshness only requires yesterday's record.

Despite the product name, the columns we use are the standard daily observations, and they match CDO (above).

Coverage: of the 112 open BOM stations in `DataFileMapping_Australia_unadjusted.json`, **101** appear in the FTP
`stations_db.txt`. The other 11 are still the ACORN-SAT 2.7 primary sites, so they can't be swapped for a nearby FTP
station. The unadjusted (CDO) station has to be the ACORN-SAT primary site for unadjusted data to line up with
adjusted data, and for the ACORN-SAT extension's comparison to work. Several have a newer same-town AWS in the FTP
tables (e.g. 080132 Kerang Airport, opened 2025-07-23), but ACORN-SAT doesn't use them yet.

| Station | Latest in committed zip | Latest in GHCNd (checked 2026-10-21) |
|---|---|---|
| 010092 Merredin | 2026-03-18 | 2026-10-08 |
| 018044 Kyancutta Town | 2026-04-13 | 2026-10-09 |
| 026026 Robe | 2026-04-13 | 2026-10-08 |
| 034084 Charters Towers Airport Comparison | 2026-04-13 | 2026-10-09 |
| 036007 Barcaldine Post Office | 2026-04-13 | 2026-10-09 |
| 038003 Boulia Airport | 2026-03-27 | 2026-10-08 |
| 056242 Inverell (Raglan St) | 2026-04-13 | 2026-10-09 |
| 063005 Bathurst Ag Station Comparison | 2025-12-17 | 2025-12-18 (stopped reporting) |
| 069018 Moruya Heads Pilot Station | 2026-04-13 | 2026-05-20 (stopped reporting?) |
| 080023 Kerang | 2026-09-21 | 2026-10-09 |
| 085072 East Sale | 2026-04-13 | 2026-10-09 |

GHCNd carries the same BOM observations (source flag `a`) for these stations, about 12 days behind. It's the only
non-scraped source for them.

### ACORN-SAT 2.7 alignment

`docs/notes/acorn-sat-2_7/primarysites.txt` (updated 16 October 2026) differs from
`ClimateExplorer.Data.Bom/MetaData/ACORN-SAT/primarysites.txt` only in the release cutoff: every final end date moves
from 20231231 to 20251231, and no primary sites change. Checked against the site's metadata:

- **111 of 112 locations line up**: the unadjusted station chain has the same stations and switch dates as the
  2.7 primary-site periods.
- **Sydney (Observatory Hill) doesn't.** ACORN-SAT switches 066062 → 066214 on **2017-10-18**; the site switches on
  2020-09-01 (066062 ends 2020-08-31). Both stations have temperature and rain over the whole overlap, so aligning to
  2017-10-18 loses only 066214's first three days of rain (2017-10-18 to -20).
- The all-null location `143983a0-…` is the deliberate Australia anomaly placeholder.

**End-date convention.** ACORN-SAT is released yearly, and each release's final end date (20251231 for 2.7) is the
release cutoff, not a closure. A station that isn't newly adjusted in a release is assumed to stay unadjusted, so its
CDO data stands in for ACORN-SAT after the cutoff: the final end date becomes null, which keeps the station open.
Each release resets this. A station that is newly adjusted doesn't use CDO as a stand-in.
`BomLocationsAndStationsMapper` implements this with a hard-coded `2019/12/31` (the 2.1 cutoff). Regenerating from the
2.7 file as-is would close all 112 stations at 2025-12-31.

### Alternatives considered

- **GHCNd** (`ASN000xxxxx`, through the existing `ghcnd-station` downloader). On 2026-10-21 NOAA's files for 082039 and
  080023 had been updated that day, but Australian data ended at **2026-10-09**, 12 days behind. It also has no
  solar radiation. Max and min match CDO exactly for the overlapping days. Too far behind for "yesterday" tiles,
  but usable as a lagged fallback for the 11 stations not in the FTP tables.
- **Substituting a newer same-town station** (e.g. 080132 Kerang Airport for 080023 Kerang). Rejected: the
  unadjusted station must be the ACORN-SAT primary site so unadjusted and adjusted data line up.
- **Keep scraping CDO.** BOM has explicitly asked automated clients to stop, and in testing it held requests for up
  to ~60 s or returned 403/500. Rejected.

### Changes

1. **`BomFtpDailyTableClient`** (new, `Data.Downloading/Downloaders`). Uses FluentFTP, which `Data.Bom` already uses
   for ACORN-SAT. Fetches one station-month CSV and parses the data rows into per-date max, min, rain and solar values.
   Async throughout, so the cancellation token is honoured.
2. **`BomDataSetDownloader` becomes a top-up** (keeps key `bom-station`, so freshness, adoption and state are unchanged):
   read the published `BOM/{station}.zip` through `DataSetSourceFileStore`, fetch the FTP months that cover the gap
   from the zip's last record date (current month, plus the previous month when the gap crosses a month boundary),
   append only dates **newer** than each entry's last record, and recompute `tempmean` for the appended dates. Write
   the rows in CDO format so readers and the validator are unaffected. Overlapping dates keep the CDO value, which
   has been quality-controlled.
3. **Station → FTP folder mapping** (new committed metadata, e.g. `MetaData/BomFtpDailyTables.json`). Folder names
   can't be derived from station names: only 100 of 226 VIC/NSW folders match a name-based rule, and others vary
   (e.g. `albury_airport` for "ALBURY AIRPORT AWS", `nowra_ran_air_station_`). Generate the mapping with a new
   `Data.Bom` command that lists the state folders, matches names, and accepts a manual override list. It verifies
   each match by comparing a month of FTP values with the station's CDO data.
4. **Stations not in the FTP tables.** `DataSetSourceAssetResolver` returns no managed BOM asset for the 11
   stations above. Requests take the existing unmanaged path (cached or rebuilt from the published file, no download,
   no `RefreshFailed`), and those stations update only through the batch tool, unless the GHCNd fallback is adopted.
   No successor stations are substituted, because the CDO station has to match the ACORN-SAT primary site.
8. **ACORN-SAT 2.7 metadata.**
   - Replace `Data.Bom/MetaData/ACORN-SAT/primarysites.txt` with the 2.7 file.
   - Change `BomLocationsAndStationsMapper` to treat the file's cutoff (its latest end date) as null, instead of the
     hard-coded `2019/12/31`.
   - Realign Sydney to 066062 → 066214 on 2017-10-18 in `DataFileMapping_Australia_unadjusted.json`.
5. **Failure backoff in `DataSetSourceUpdateCoordinator`.** Remember a failed refresh per asset in memory for a short
   window (e.g. 15 minutes). Within it, return `RefreshFailed` immediately without downloading or waiting for the lock.
   This applies to every source, not just BOM.
6. **Bound the ACORN-SAT extension lock.** Acquire it under the same 30 s refresh timeout, falling back to the cached
   overlay or base series as the existing `RefreshFailed` branch already does, so requests can't queue without limit.
7. **Remove the CDO scraper from the API.** Delete the `BomDailyDataClient` / `BomHttpClientFactory` registrations
   and the unused `ClimateExplorerApiServices.BomHttpClient` from `WebApi/Program.cs`.

### Out of scope / decisions for later

- **Batch tools.** `Data.Bom` and `Data.Misc` still scrape CDO for full station history, using the same browser
  headers. BOM's notice applies to them too. The FTP tables only hold recent months, so full history needs either
  BOM's data services or a manual request. This needs a separate decision.
- **The 11 stations not in the FTP tables** keep their last batch-tool data unless they fall back to GHCNd (about
  12 days behind). Bathurst Ag (063005) and probably Moruya Heads (069018) have stopped reporting. That's a question for
  the next ACORN-SAT release, not something to patch here.
- **`CO2Deseasoned` `/climate-record` failure.** Seen on 2026-10-21 as a CORS error after 1.7 s, which hides an API
  500/503. It's unrelated to BOM and needs the API exception from the logs.
- **Logging.** The API only logs to the console (`builder.Logging.AddConsole()`), which a Windows App Service doesn't
  surface in Log Stream. Adding the Azure App Service logging provider would have made this diagnosable from the portal.

## Risks

- **Publication time.** The October file was issued at 06:31 GMT (17:31 AEDT). If the tables only update once a day,
  then between 00:00 UTC and the update, the UTC-based "yesterday" record isn't available yet. The content-aware
  6-hour retry window keeps that to a few cheap FTP fetches a day. *(2026-10-22: shortened to 30 minutes. A 6-hour
  window could leave a new day hidden for up to 6 hours after BOM published it. Observed: the 22:31 UTC re-issue adds
  no new day; yesterday arrives with the 06:31 UTC issue, about 5:30pm AEDT, later in the day than CDO used to have it.)*
- **FTP from Azure App Service.** This needs outbound passive-mode FTP. The batch tool already uses FTP for ACORN-SAT,
  but the App Service has not been tested.
- **Product continuity.** IDCKWCDEA0 is an evapotranspiration product. BOM could change or retire it, and the
  downloader should fail cleanly (validation fails, previous source kept) rather than publish partial data.
- **Later quality-control revisions** to recent CDO values are only picked up when the batch tool refreshes a zip.
  This is the same trade-off as today, where the API's zip is only as good as its last download.
- **Redeploys reset `Datasets/`** to the committed zips. After each deploy, every station tops up from FTP again.
  Cheap with FTP; it was expensive (and blocked) with CDO.

## Addendum — implementation notes

Implemented 2026-10-21 on `development`.

**What shipped**

- `BomFtpDataSetDownloader` (key `bom-station`), with `IBomDailyTableSource` / `BomFtpDailyTableSource` (FluentFTP),
  registered in the API in place of the CDO scraper. `BomDataSetDownloader` / `BomDailyDataClient` stay for the batch
  tools. The downloader reads the published zip, finds each entry's last date, fetches the table months that cover the
  gap (up to 24), and appends newer rows in CDO format. It checks the table's column header and fails the download if
  the layout has changed.
- Failure backoff in `DataSetSourceUpdateCoordinator` (`RefreshFailureBackoff`, 15 minutes). It's only recorded when
  a download was permitted, so a `/dataset` request (which can't download) never blocks `/climate-record` refreshes.
- `DataSetAssetLockProvider.TryAcquireAsync`. The ACORN-SAT extension waits for its lock for at most
  `DefaultOnRequestRefreshTimeout`, then carries on without it.
- ACORN-SAT 2.7 `primarysites.txt` copied into `Data.Bom`. `BomLocationsAndStationsMapper` now treats the file's
  latest end date as the release cutoff (open), instead of the hard-coded 2019/12/31. Sydney realigned to
  066062 → 066214 on 2017-10-18.
- Removed `ClimateExplorerApiServices.BomHttpClient`, which nothing used.

**Deviations from the plan**

- **No station → folder mapping file.** The folder is derived as `{state}/{name}`. The state comes from the BOM
  station number (001–013 WA, 014–015 NT, 016–026 SA, 027–045 QLD, 046–075 NSW/ACT, 076–090 VIC, 091– TAS). The name
  is the station name in lower case, with spaces and `/` as `_` and `.` removed. To make that hold, 12 station names in
  `Stations_Australia_unadjusted.json` lost their " AWS" suffix. No mapping generator command was needed.
  *Follow-up:* regenerating station metadata with `BomLocationsAndStationsMapper` takes names from BOM's
  `stations.txt` and would bring the suffixes back.
- **No resolver change for stations without a table.** Their download fails (no table found), which reports
  `RefreshFailed` and backs off. The warning they show is accurate: their data isn't current.

**Verification (2026-10-21)**

- All 101 derived folders have data to 2026-10-20. For March 2026, every value present in both sources matched CDO.
  The only differences were days blank in the FTP table that CDO had filled later (e.g. 8 at Karijini North, 6 at
  Nowra).
- The 11 stations without a table are as listed above. East Sale has a `vic/east_sale` folder, but it isn't current.
- Perth Airport M.O. (009021) is `wa/perth_airport_mo`, which only starts in 2026-06. `wa/perth_airport` now belongs to
  the new station 009291 "PERTH AIRPORT" (opened 2026-03-05), confirmed by matching 009021's GHCNd rainfall. 009021
  keeps a 2026-04-14 to 2026-05-31 gap until a batch refresh fills it.
- Halls Creek (002079)'s committed zip ends 2024-12-31, which is why the cap is 24 months rather than 12.
- Live run of the real FTP client (not committed): Rutherglen topped up to 2026-10-20 in 0.9 s; Sydney, from April, in
  3.1 s.
- `dotnet build` and the unit test suite (786 tests) pass.

**Not done**

- Azure App Service logging provider, the `CO2Deseasoned` 500, the batch tools' CDO scraping, and a GHCNd fallback for
  the stations without a table.
