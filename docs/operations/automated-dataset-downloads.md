# Operating the automated dataset download pipeline

- **Date:** 2026-07-15
- **Scope:** `ClimateExplorer.Data.Downloading`, `ClimateExplorer.WebApi` (`/dataset`, `/climate-record`), `ClimateExplorer.Data.Misc`, `ClimateExplorer.DataPipeline`
- **Builds on:** [Automated dataset downloads for historical API data](../design/2026-07-13-01-automated-dataset-downloads-plan.md) (Stage 9)

This is the operational reference for the coordinator/downloader pipeline introduced by
the automated-downloads plan: what must be writable, how a source file is replaced,
what the cache keys mean, what gets logged, and how to force a safe re-download.

## Source-data write permissions

The Web API needs **write** access — not just read — to its `Datasets/` folder at
runtime (`DataSetSourceFileStore`, constructed with `"Datasets"` in
[`ClimateExplorer.WebApi/Program.cs`](../../ClimateExplorer.WebApi/Program.cs)). Every
successful refresh writes a new file into that tree. There is no separate
read-only/writable split — the folder that ships in the deployment package is the same
folder later refreshes replace files in.

`ClimateExplorer.Data.Misc` (the batch host) instead points `DataSetSourceFileStore` at
`ClimateExplorer.SourceData` ([`Folders.SourceDataFolder`](../../ClimateExplorer.Core/Folders.cs)),
so a batch run needs write access to the build-time source tree, not the deployed one.

`DataSetSourceState` (the freshness/hash record for each asset) is written to a
**different** location per host, and that location must also be writable:

- Web API: `%TEMP%\ClimateExplorer\DataSetSourceState\` (`Path.GetTempPath()`-relative —
  not guaranteed durable across a container restart or VM redeploy).
- `ClimateExplorer.Data.Misc`: `Output\DataSetSourceState\` relative to the batch tool's
  working directory.

If the Web API's temp directory is wiped (container restart, VM redeploy without a
mapped persistent volume), every asset's state is lost. This is not corruption — the
next request for each asset just re-downloads once, since a missing state record is
treated the same as a stale one (`DataSetSourceUpdateCoordinator.GetCurrentStateAsync`
returns `null`). Map the temp path to persistent storage if avoiding that first-request
cold-refresh cost after a restart matters operationally.

## Deployment replacement semantics

Two different "replace the file in place" mechanisms exist, at two different stages:

1. **Packaging** (`ClimateExplorer.DataPipeline`, build time): `DataPackageBuilder.Build()`
   builds the whole `Datasets/` tree into a `.staging-{guid}` sibling directory, then
   `ReplaceOutput()` swaps staging → backup → output (a directory-level move/swap with
   the old contents retained as a backup until the swap succeeds, and restored if any
   step fails). This runs once per deployment/package build, not per asset.
2. **Runtime refresh** (`DataSetSourceFileStore.PublishAsync`, per asset, per download):
   the downloaded/validated candidate is copied to a `.tmp-{guid}` sibling file in the
   **same directory** as the destination, flushed (`FileOptions.WriteThrough`), then
   `File.Move(tmpPath, destinationPath, overwrite: true)` — an atomic same-volume rename.
   A reader can never observe a partially written file; a failed download leaves the
   previously published file completely untouched.

A deployment can replace a file that a runtime refresh previously wrote (e.g. a new
release re-packages `Datasets/BOM/086071.zip` with fresher build-time data). The
coordinator detects this without any deployment-aware code: `GetCurrentStateAsync`
compares the live file's length + SHA-256 against the values recorded in
`DataSetSourceState`. A mismatch — from any cause, deployment or otherwise — makes the
cached state unusable and triggers a normal refresh on the next request. There is no
separate "deployment version" concept to track.

## Cache keys

Two different keyed stores exist and must not be confused:

- **Asset key** (`DataSetDownloadRequest.AssetKey`, computed by
  `DataSetSourceAssetResolver.GetAssetKey`): the asset's relative path,
  backslashes normalised to `/` and upper-cased — e.g. `Datasets/BOM/086071.zip` →
  `BOM/086071.ZIP`. This is the per-asset lock key and the key `DataSetSourceState` is
  stored under. Two measurements that resolve to the same relative path (the two Mauna
  Loa CO₂ measurements; every measurement bundled into one BOM/GHCNd station archive)
  share one asset key, one lock, one download, and one state record by construction —
  this is intentional, not a collision to fix.
- **State-store filename**: `FileDataSetSourceStateStore` does not use the asset key as
  a filename directly — it stores each state record at
  `{stateFolder}/{SHA256-hex(asset key, UTF-8)}.json`. To find or delete a specific
  asset's state file from the state folder, hash the asset key yourself (or just clear
  the whole state folder — see "Forcing a refresh" below).
- **Response cache key** (`DataSetEndpoints.PostDataSets`): `"DataSet_v2_" +
  JsonSerializer.Serialize(body)`, stored in the separate `FileBackedTwoLayerCache`
  (`cache` / `cache-longterm` folders). This is a different cache from source state —
  it holds built chart responses, keyed by request shape, not by asset. Clearing it
  never forces a source re-download; clearing source state never invalidates a cached
  response shape (it rebuilds lazily on that response's own next request).

## Operational logs

The coordinator and the `/dataset` endpoint log through the standard ASP.NET Core
console logger (`builder.Logging.AddConsole()`) / the batch host's console logger
factory. Representative messages, all from
[`DataSetSourceUpdateCoordinator`](../../ClimateExplorer.Data.Downloading/Orchestration/DataSetSourceUpdateCoordinator.cs)
unless noted:

| Level | When | Message shape |
| --- | --- | --- |
| Debug | A cached response is fresh and every contributing asset's state still matches its file | `Cached response is fresh for assets [{AssetKeys}]; no download attempted` |
| Information | A refresh completed and published successfully | `Refreshed dataset source {AssetKey} via {DownloaderKey} in {ElapsedMs}ms; published {Length} bytes, latest record date {LatestRecordDate}` |
| Warning | Asset resolution itself threw (bad request shape, mapping error) | `Failed to resolve dataset source assets for request` (with exception) |
| Warning | Reading existing state threw (corrupt state file, I/O error) | `Failed to read current dataset source state for asset {AssetKey}` (with exception) |
| Warning | No handler is registered for a `DownloaderKey` | `No downloader registered for key {DownloaderKey}, asset {AssetKey}` |
| Warning | Download, validation, or publication failed | `Failed to refresh dataset source {AssetKey} via {DownloaderKey} after {ElapsedMs}ms; retaining previously published source` (with exception) |
| Warning | One or more assets could not be refreshed for a request | `Refresh failed for one or more of assets [{AssetKeys}]; falling back to {the cached response \| the existing published source file}` |
| Warning | `DataSetEndpoints.PostDataSets` served a stale cached response after a refresh failure | `PostDataSets falling back to the previously cached response after a refresh failure` |
| Warning | `PostDataSets` had no cached response and refresh failed, so it built from the currently published source file | `PostDataSets has no cached response and refresh failed; building from the existing published source file` |

A failed refresh always logs the exception before falling back — the previous behavior
(a bare `catch { return null; }` with no trace at all) has been replaced. There is
deliberately **no** logging of response bodies, file contents, or unsanitised path
input beyond the resolved asset key/relative path, which are server-controlled values,
not raw user input.

`ClimateExplorer.Data.Misc` (`DataSetBatchRefresher.RefreshAllAsync`) shares the same
coordinator and therefore the same log messages for every asset it force-refreshes —
watch its console output for `Refreshed dataset source ...` / `Failed to refresh
dataset source ...` lines, one pair of possible outcomes per opted-in asset.

## Forcing a refresh without deleting the current source file

There is no per-asset CLI flag. Two supported ways to force a re-download, from
weakest to strongest:

1. **Run `ClimateExplorer.Data.Misc`.** `DataSetBatchRefresher.RefreshAllAsync` always
   calls `EnsureCurrentAsync(..., forceRefresh: true, ...)` for every opted-in asset,
   regardless of current freshness. This force-refreshes *everything* opted into
   automatic retrieval in one run — there's no way to target a single asset with it.
2. **Delete just that asset's state file** from the running host's state folder
   (`%TEMP%\ClimateExplorer\DataSetSourceState\{sha256-hex-of-asset-key}.json` for the
   Web API, `Output\DataSetSourceState\{...}.json` for the batch host — see "Cache
   keys" above for how the filename is derived). With no state record, the next
   request/run for that asset finds `GetCurrentStateAsync` returning `null` and
   performs a normal refresh — while the **currently published source file stays in
   place** and keeps being served as the last-known-good fallback if the forced
   re-download fails, since `PublishAsync` only overwrites the file after the new
   download validates successfully. This is the correct way to force one specific
   asset without any risk of losing the current good file, but it requires locating or
   computing the hashed filename — there's no first-class "reset this one asset" tool
   command.

Deleting the source file itself (rather than its state) is never necessary and should
be avoided: if a refresh fails, an asset with no source file has no fallback to serve
and `/dataset`/`/climate-record` requests for it will fail outright with no cached
response and no cold packaged file to read.
