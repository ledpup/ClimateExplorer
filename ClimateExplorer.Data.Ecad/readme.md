# ClimateExplorer.Data.Ecad

**Shelved 2026-08-01** — working, but a live-vs-live freshness check found the update-frequency
premise this integration was built on doesn't hold broadly enough to justify it. See "Did this
actually help?" in the design doc linked below before reactivating or extending this.

Builds ClimateExplorer's coverage of the [European Climate Assessment & Dataset](https://www.ecad.eu/),
read from the `ecad-nonblended` collection of EUMETNET's MeteoGate API. Run manually and
periodically, like the other `ClimateExplorer.Data.*` tools. The design and the reasoning behind
it are in [docs/design/2026-07-30-01-ecad-daily-dataset-plan.md](../docs/design/2026-07-30-01-ecad-daily-dataset-plan.md).

## Running it

The tool resolves paths relative to its build output, so run it from there rather than with
`dotnet run`:

```
cd bin\Debug\net10.0
dotnet ClimateExplorer.Data.Ecad.dll
```

`--max-stations N` publishes only the first N matches. That is for smoke-testing against the live
API; the metadata a capped run writes is not a release, and the tool says so.

A full run takes upwards of an hour: it bootstraps each matched station's entire history, and the
longest series here begin in the 18th century.

### What it writes

| Path | Contents |
|---|---|
| `ClimateExplorer.WebApi\Datasets\Ecad\Unadjusted\{ghcnId}.zip` | What the site serves and the runtime downloader extends |
| `ClimateExplorer.SourceData\Ecad\Unadjusted\{ghcnId}.zip` | The checked-in copy the test suite validates every asset against |
| `ClimateExplorer.WebApi\MetaData\EcadNonBlendedStationIds.json` | GHCN station id to ECA&D station id crosswalk, read by `EcadDataSetDownloader` |
| `ClimateExplorer.WebApi\MetaData\DataFileMapping\DataFileMapping_ecad_unadjusted.json` | Which locations ECA&D serves |

It reads `Stations_ghcnm_adjusted.json` and `ClimateExplorer.Data.Ghcnm\MetaData\GhcnIdToLocationIds.json`,
so `ClimateExplorer.Data.Ghcnm` has to have been run at least once. It does not touch the GHCNd
mapping files: ECA&D takes precedence over GHCNd purely by being declared first in
`DataSetDefinitionsBuilder`, which keeps the whole integration additive and reversible.

## Reading the log

Two kinds of line are worth attention.

`Skipping {station}: Ambiguous` and `: NameNotCorroborated` are the ones a human might act on — a
real ECA&D station was there and could not be confidently identified as the same site. ECA&D and
GHCN name the same place differently often enough that some genuine matches are lost this way
(`BOURNEMOUTH` and `Hurn` are the same airport); that is deliberate, because the alternative is
silently attaching a location to its neighbour. If a skip is wrong, the fix is a manual entry, not
a looser threshold.

`chosen from several registrations of the same station` means two participants contributed the same
site and the one reporting most recently was taken.

Everything else — the several hundred `IncompleteMeasurements` skips — is just ECA&D having a
station near a ClimateExplorer location that no longer reports, or does not report all four
measurements. They are counted rather than listed.

## Notes on the API

- It is flagged **pre-release**; its shape can still change.
- Queries are capped at 300,000 data points, counted as `timePoints * parameterCount * stationCount`,
  and every requested parameter is billed twice because of its `_q` quality flag companion. Exceeding
  it returns HTTP 413 with the arithmetic spelled out.
- There is also a **request quota** — 400 per window, reported via `X-RateLimit-Limit`,
  `X-RateLimit-Remaining` and `X-RateLimit-Reset`, with a 429 once spent. A full build sits close
  enough to that limit to cross it. `EcadApiClient` waits out the window and retries, so a throttled
  run takes longer rather than failing; if enough stations fail anyway the tool abandons the run
  instead of publishing a partial mapping, because a partial mapping removes working locations from
  the site.
- A window with no observations returns **404**. That is the normal answer for an up-to-date source,
  not an error. An unknown station returns **400**.
- Parameter codes must come from the collection's own `parameter_names`; the numbered variants are
  not contiguous, and an unknown code fails the whole query.
- Only the non-blended edition exists today. When the blended one ships it maps onto
  `DataAdjustment.Adjusted`; see the design document's "Out of scope".
