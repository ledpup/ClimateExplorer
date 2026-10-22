# Keep committed datasets untouched; store refreshed data separately

- **Date:** 2026-10-22
- **Status:** Proposed
- **Author:** Patrick (with Claude)
- **Scope:** `ClimateExplorer.Core` (data reader, dataset definitions), `ClimateExplorer.Data.Downloading`
  (BOM FTP and GHCNd downloaders, coordinator, file and state stores), `ClimateExplorer.WebApi` (configuration,
  diagnostics endpoint)
- **Builds on:** [2026-10-21-03-bom-ftp-daily-refresh-plan.md](2026-10-21-03-bom-ftp-daily-refresh-plan.md)

## Problem

On-request refresh rewrites source files inside the deployment folder (`wwwroot/Datasets`). The app was designed
for those files to change only at deploy time, and three things go wrong now that they don't:

1. **Every deploy discards every refresh.** The deploy copies the committed files back over the refreshed ones
   (seen 2026-10-21: every BOM zip's modified time was the deploy time).
2. **The refresh record is per instance; the files are shared.** The record of what was downloaded and when
   lives in each instance's `C:\local\Temp`, while the files live on shared storage. Instances can disagree about
   whether a file is current, and each refreshes the same files independently.
3. **Shared storage isn't strictly consistent.** A read straight after a write has returned the file the write
   replaced. The refresh recorded that old file's hash with the new download's latest date. A later deploy restored
   the old file, which then matched the record, so stale data was served as current (Canberra, Hobart and Melbourne
   on 2026-10-21, reproduced on Wagga). The immediate bug is fixed by hashing the downloaded copy rather than
   reading the published file back, but 1 and 2 remain.

## Proposal

Treat the committed `Datasets` folder as read-only at runtime, as originally designed. Write everything a refresh
produces to a separate **refreshed data folder** outside the deployment, which deploys never touch. Readers combine
the two.

### One folder, mirroring `Datasets`

Rather than a new folder per source, one root mirrors the `Datasets` layout, so a refreshed file sits at the same
relative path as the committed file it supplements:

```
<refreshed root>/
  BOM/070351.zip            top-up: only the days after the committed zip ends
  GHCNd/ASN00070005.zip     top-up
  CO2/co2_mm_mlo.txt        replacement: the whole downloaded file
  CO2/co2_daily_mlo.txt     replacement
  _state/<hash>.json        refresh records, one per source file
```

- **App Service:** an app setting points it at `C:\home\data\ClimateExplorer\Refreshed`. That storage is
  persistent, shared by all instances, and outside `wwwroot`.
- **Local development:** a folder under `%LOCALAPPDATA%\ClimateExplorer\Refreshed`, so refreshes never dirty the
  repo's `Datasets` files (as the BOM zips in `git status` show they do today).

### Two kinds of refreshed file

Sources differ in whether old values change, so each dataset declares how its refreshed file combines with the
committed one. This is a new `SourceRefreshMode` on `DataSetDefinition`, set in the definition builders:

| Mode | Sources | Refreshed file holds | Reader |
|---|---|---|---|
| `TopUp` | `bom-station`, `ghcnd-station` | Rows dated after the committed file's last row, same format, same archive entries | Committed lines, then top-up lines |
| `Replace` | `direct-http` (CO₂, CH₄, N₂O, ocean, sea level …), NOAA global temperature, Greenland, glaciers | The whole downloaded file | The refreshed file instead of the committed one |

- **Top-up suits station daily data.** It's large and only grows at the end, so a top-up is a few hundred rows
  at most (BOM FTP is capped at 24 months).
- **Replacement suits the small global series.** NOAA revises recent months of the CO₂/CH₄/N₂O monthly means, so
  only a whole new file is correct, and these files are small anyway.

### Reading: one change in `DataReaderFunctions`

Every reader (`OneValuePerRow`, `TwelveMonthsPerRow`, the download validator, the ACORN-SAT extension via
`SeriesProvider`) gets its lines from `DataReaderFunctions.GetLinesInDataFileSource`. That's the single point to
combine the two folders:

- **`TopUp`:** return the committed lines followed by the top-up's lines for the same archive entry.
  `ProcessDataFile` already skips out-of-order and duplicate dates, keeping the first, so the committed row wins
  any overlap. That happens when a later commit updates the committed zip past the top-up's start. No merge logic
  is needed.
- **`Replace`:** read the refreshed file if one exists, otherwise the committed one.

The refreshed root is set once at startup, like the committed `Datasets` folder is today (a static setting on the
reader, or a parameter threaded through `SeriesProvider`; to decide during implementation).

### Writing: downloaders build top-ups from the committed file

- **BOM FTP:** read the last dates from the **committed** zip (read-only), download the months since, and write a
  zip containing only rows after those dates. Each refresh rebuilds the whole top-up from scratch rather than
  appending to the previous one, so a bad top-up is replaced on the next refresh.
- **GHCNd:** keep downloading the full station CSV, but keep only rows after the committed archive's last date.
  Revisions to older values are ignored until the committed archive is regenerated by the batch tools, which stay
  the way history is updated.
- **Replace-mode downloaders:** unchanged, apart from publishing to the refreshed root.

`DataSetSourceFileStore` gains the second root. It publishes only to the refreshed root (temporary file then rename,
as now) and never writes to `Datasets`.

### Refresh records move to the shared folder

`FileDataSetSourceStateStore` moves from `C:\local\Temp` to `<refreshed root>/_state`, so all instances share one
record per source file and survive restarts and deploys alike.

- **The record describes the refreshed file.** Its hash and size are computed from the downloaded copy before
  publishing (as now), and its latest record date from the combined view (committed plus top-up).
- **Instances can still both refresh the same file.** The per-asset lock is per instance. Both build the same
  content from the same upstream data and replace the file atomically, so the last writer wins harmlessly. A shared
  lock isn't worth adding.
- **Taking over an existing file stays.** For when a top-up exists but its record is missing, the coordinator still
  takes over the file without downloading, checking and hashing a single copy of it (as now).

### Migration

- **On first run after deploying:** no records exist in the new location, so each source refreshes once on first
  request, writing its top-up or replacement.
- **Old state:** the `C:\local\Temp\ClimateExplorer\DataSetSourceState` records are simply never read again.
- **Committed files:** they stop changing at runtime. Anything a refresh has already written into `wwwroot/Datasets`
  is replaced by the next deploy and not written again.
- **ACORN-SAT extension cache:** needs nothing. Entries are rebuilt when they're behind the source's latest
  record date (since 2026-10-22).

### Changes

- `ClimateExplorer.Core`:
  - add `SourceRefreshMode` to `DataSetDefinition` and set it in the BOM, GHCN and other definition builders;
  - make `DataReaderFunctions.GetLinesInDataFileSource` combine the refreshed root;
  - pass the root through to `DataSetDownloadValidator`.
- `ClimateExplorer.Data.Downloading`:
  - `DataSetSourceFileStore`: committed root (read-only) plus refreshed root;
  - `BomFtpDataSetDownloader` and `GhcndDataSetDownloader`: write top-ups based on the committed file;
  - coordinator: validate the combined view;
  - `FileDataSetSourceStateStore`: new location.
- `ClimateExplorer.WebApi`:
  - `Program.cs`: refreshed root from configuration, with the App Service app setting;
  - `/diagnostics/source-state`: report the committed file, refreshed file and record separately.
- `ClimateExplorer.Data.Misc`: unchanged. The batch tools keep regenerating the committed files, which are then
  committed and deployed.
- Unit tests:
  - the reader combining top-ups (including the committed row winning an overlap);
  - replacement files taking precedence;
  - downloaders writing only rows after the committed end;
  - the coordinator never writing to the committed root.

### Alternatives considered

- **Keep rewriting whole files, but outside `wwwroot`.** This fixes deploys and shared records, but keeps rewriting
  large archives on every refresh and leaves the refreshed copy as the only full copy of history.
- **A database or Azure Blob Storage.** This gives atomic, shared updates (Blob Storage's conditional writes would
  replace the per-instance lock), but adds a service, credentials and local-development setup for a problem the
  file split solves. The `_state` and top-up files could move to Blob Storage later without changing readers,
  since only the file store would change.

## Risks

- **Overlap semantics depend on `ProcessDataFile`.** "First row for a date wins" must stay true; a unit test should
  pin it.
- **The top-up is capped.** Where the committed BOM zip ends more than 24 months ago, the FTP top-up can't bridge
  the gap. That's the existing limit, but it becomes more visible because nothing accumulates between deploys any
  more. The batch tools remain the fix.
- **Shared-storage staleness still applies to reads.** A reader straight after a publish may briefly see the
  previous top-up. That's harmless now: the record is computed from the downloaded copy, and the committed file
  never changes underneath it.
- **Replacement files are trusted wholesale.** A truncated download that passes validation would hide committed
  history for that series. The validator already rejects empty and non-finite files; consider also rejecting a
  replacement that ends earlier than the committed file.
