# BOM Product Code → Station ID Mapper

This console app scans BOM product codes (IDCJDW0000..IDCJDW9999) and scrapes the BOM "Source of data" section to discover the station ID and name.

Usage examples

- Run the full scan (no args, starts at 0000 and goes through 9999):

```bash
dotnet run --project ClimateExplorer.Data.BomProductCodeMapping
```

- Run the full scan with progress logging:

```bash
dotnet run --project ClimateExplorer.Data.BomProductCodeMapping -- --log-level=progress
```

- Dry-run a single product code (Canberra example `2801`):

```bash
dotnet run --project ClimateExplorer.Data.BomProductCodeMapping -- --single=2801
# or
dotnet run --project ClimateExplorer.Data.BomProductCodeMapping -- --single=IDCJDW2801
```

- Run a small range:

```bash
dotnet run --project ClimateExplorer.Data.BomProductCodeMapping -- --range=2800-2810
# or
dotnet run --project ClimateExplorer.Data.BomProductCodeMapping -- --start=2800 --end=2810
```

Files produced (created/appended in working directory):
- `acorn_station_product_mapping.csv` — `stationId,productCode`
- `valid_responses.csv` — `productCode,stationId,stationName,url`
- `failed_product_codes.txt` — one product code per line (skipped on subsequent runs)

Log level options:
- `--log-level=info` (default)
- `--log-level=progress` (prints periodic progress updates during long scans)

Notes
- The app expects reference files in `Reference/` relative to the current working directory: `acorn_sat_stations.csv` and/or `acorn_sat_stations.txt`.
- If `failed_product_codes.txt` exists it will be loaded and those codes skipped. Delete it to force rechecking all codes.
- The app uses simple regex-based scraping; check `valid_responses.csv` for results and verify rough name matches.

