# `DataRecord` JSON payload size: investigation

## Summary

`Key` on [DataRecord.cs](../../ClimateExplorer.Core/Model/DataRecord.cs) is a redundant, string-encoded restatement of `Year`/`Month`/`Day` — and it's never read back anywhere: not by the server, not by the Blazor client, not by any test that inspects behaviour rather than the property itself. It's pure serialization cost. Dropping it from the wire shape cuts the gzipped size of a 40k-row daily series by **~26%** with zero consumer changes, because `Year`/`Month`/`Day` (the fields everything actually uses) are already in the payload.

The `{"dates": [...], "values": [...]}` columnar restructuring the issue proposes saves more (~38% gzipped vs. today) but costs real readability and robustness (parallel-array index alignment, can't eyeball one record, bigger blast radius across server/client/tests) for a small marginal win. Between "drop `Key`" and full columnar there's a middle option that gets nearly all of columnar's saving (~37% gzipped) with none of its alignment risk: serialize each `DataRecord` as a compact 2-element tuple array, `[dateInt, value]` (e.g. `[20150824,14.8]`), instead of a 4-property object. Each record is still self-contained and independently readable — there's no separate index-aligned array to misalign — while `dateInt` folds `Year`/`Month`/`Day` into a single number.

**Recommendation:** switch `DataRecord`'s JSON shape to the `[dateInt, value]` tuple array (see [Recommendation](#recommendation)). This supersedes the earlier "just drop `Key`" plan — it captures effectively all of the available saving (37 of 38 percentage points) while keeping the per-record readability and alignment-safety that ruled out full columnar. Don't adopt the columnar shape unless/until `RawDataRecords` gets a real caller and regularly ships large arrays and the tuple shape turns out not to be enough.

## Where `DataRecord` arrays actually get big

`DataRecord` is the element type of two response fields:

| Field | Populated when | Typical size |
|---|---|---|
| `ClimateRecordsResponse.Records` ([ClimateRecordsResponse.cs](../../ClimateExplorer.Core/Model/ClimateRecordsResponse.cs)) | Always, from [ClimateRecordsEndpoints.cs](../../ClimateExplorer.WebApi/ClimateRecordsEndpoints.cs) | Paginated in the UI (`take`/`skip`, default page size 10) — **but** [ClimateRecords.razor.cs:526](../../ClimateExplorer.Web.Client/Components/ClimateRecords/ClimateRecords.razor.cs#L526) (`DownloadAllRecords`, the CSV export button) calls it with `take: null, skip: null`, which returns every matching daily/monthly record — for a century-plus station this is exactly the 40k-record case the issue describes. |
| `DataSet.RawDataRecords` ([DataSet.cs:46](../../ClimateExplorer.Core/Model/DataSet.cs#L46)) | Only when `PostDataSetsRequestBody.IncludeRawDataRecords == true` | **Currently unused** — grepping the repo, nothing sets `IncludeRawDataRecords = true` anywhere. It's wired up server-side ([DataSetBuilder.cs:58](../../ClimateExplorer.Core/DataPreparation/DataSetBuilder.cs#L58), [DataSetEndpoints.cs:99-102](../../ClimateExplorer.WebApi/DataSetEndpoints.cs#L99-L102)) but no live caller exercises it, so it's not contributing to any payload today. |

The actual high-volume, always-on chart series (`DataSet.DataRecords`, a `List<BinnedRecord>`) doesn't use `DataRecord` at all — it uses [BinnedRecord.cs](../../ClimateExplorer.Core/Model/BinnedRecord.cs), which already sidesteps this exact problem: it serializes as `{"binId":"y2015m08d24","value":14.8}`, a single compact string plus the value, with `Year`/`BinIdentifier` recovered on demand via `[JsonIgnore]` computed properties. That's useful precedent (see [Recommendation](#recommendation)).

## Confirming `Key` is dead weight

`Key` is built once in the constructor (`CreateKey()`, [DataRecord.cs:70-82](../../ClimateExplorer.Core/Model/DataRecord.cs#L70-L82)) and then never consumed:

- The client builds its row view model straight from the discrete fields, not `Key`: `ClimateRecordViewModel.FromDataRecord` reads `record.Year`/`record.Month`/`record.Day` ([ClimateRecordViewModel.cs:14-23](../../ClimateExplorer.Web.Client/UiModel/ClimateRecordViewModel.cs#L14-L23)).
- CSV export (`Exporter.ExportClimateRecords`) formats from `ClimateRecordViewModel.Year`/`Month`, again never `Key` ([Exporter.cs:80-94](../../ClimateExplorer.Web.Client/Services/Exporter.cs#L80-L94)).
- A repo-wide search for `.Key` on `DataRecord` turns up exactly one hit, and it's a unit test asserting the *in-memory* property value after construction ([DataRecordSerializationTests.cs:29](../../ClimateExplorer.UnitTests/DataRecordSerializationTests.cs#L29)) — not a test of the wire format, and it survives fine if `Key` stops being serialized (the property still gets computed, just not emitted to JSON).

So `Key` is written by every `DataRecord`, shipped over the wire, and then discarded unread on arrival, on every request, forever.

## Measurements

Built a realistic 40,000-row daily series (110 years, sinusoidal seasonal temperature + noise, one decimal place) and measured each candidate shape as ASP.NET Core would actually emit it (`System.Text.Json` default: no whitespace) — both raw UTF-8 bytes and gzipped bytes, since that's what's actually contended here:

| Shape | Example element | Raw bytes | Gzip bytes | Gzip vs. today |
|---|---|--:|--:|--:|
| **Today** (`key` + `day` + `month` + `year` + `value`) | `{"key":"2015_8_24","day":24,"month":8,"year":2015,"value":14.8}` | 2,539,014 | 234,175 | — |
| Drop `Key`, keep `day`/`month`/`year`/`value` | `{"day":24,"month":8,"year":2015,"value":14.8}` | 1,820,819 | 173,722 | **−25.8%** |
| `key` (underscore form) + `value` only | `{"key":"2015_8_24","value":14.8}` | 1,300,819 | 170,143 | −27.3% |
| `key` (compact `"20150824"`) + `value` | `{"key":"20150824","value":14.8}` | 1,262,624 | 165,496 | −29.3% |
| `key` (unquoted int `20150824`) + `value` | `{"key":20150824,"value":14.8}` | 1,182,624 | 160,495 | −31.5% |
| Tuple array `[year, month, day, value]` | `[2015,8,24,14.8]` | 660,819 | 152,139 | −35.0% |
| Tuple array `[dateInt, value]` | `[20150824,14.8]` | 622,624 | 147,632 | −36.9% |
| **Columnar** `{"dates":[...],"values":[...]}` (ISO date strings) | — | 702,645 | 144,458 | **−38.3%** |

Three things stand out:

1. **Removing the one redundant field (`Key`) captures most of the available win** — 25.8 of the 38.3 percentage points, i.e. two-thirds of the total possible saving, from a one-line change with no consumer impact.
2. **After gzip, "keep a string key, drop the numeric fields" and "drop the key, keep the numeric fields" cost about the same** (170,143 vs. 173,722 bytes) — confirming the actual waste was never either representation on its own, it was encoding the same three numbers *twice*, in two different textual forms, in every record.
3. **The `[dateInt, value]` tuple gets within 0.7KB of full columnar** (147,632 vs. 144,458 bytes gzipped) by dropping field-name repetition (`"day":`/`"month":`/`"year":`/`"value":`, four keys × 40,000 records) without giving up the per-record structure — it's the best-of-both-worlds point on this table.

Everything past "drop `Key`" — compact keys, tuples, columnar arrays — is fighting over the remaining ~30-70KB, a 4-15% shrink of an already-865KB-out-of-2.5MB-reduced payload. But since the tuple shape captures nearly all of that for no readability cost (see below), it's worth taking anyway.

## Is response compression even happening?

**Update: it wasn't, and now it is.** [ClimateExplorer.WebApi/Program.cs](../../ClimateExplorer.WebApi/Program.cs) had no `AddResponseCompression`/`UseResponseCompression` call — every response left this API fully uncompressed regardless of what the Azure Static Web Apps hosting layer did or didn't add downstream. Added `AddResponseCompression` (Brotli preferred, gzip fallback, `CompressionLevel.Optimal`, `EnableForHttps = true` — safe here since every response is public, identical-for-everyone climate data with no per-user secret in the mix, same reasoning as the existing CORS comment) and `app.UseResponseCompression()` early in the pipeline.

Verified against a running instance: `GET /datasetdefinition` (a representative large JSON payload) went from **315,260 bytes uncompressed → 53,695 bytes with `Content-Encoding: br`**, an 83% reduction, on top of whatever `DataRecord.Key` removal saves on the endpoints that use it. This was the bigger, more general win the earlier draft of this note flagged as unverified — it's now closed out, not just confirmed.

## Why not the columnar `{"dates": [...], "values": [...]}` shape

This was the option the issue asked to specifically evaluate a justification for. Against it:

- **Marginal, not dramatic, gain over the tuple shape.** 144KB vs. 148KB gzipped on a 40k-row extreme case — well under a kilobyte per thousand records. For the common case (a paginated page of 10-100 records, or the CSV-export path which isn't JSON at all), the difference is a handful of bytes.
- **Readability cost is real, not just aesthetic.** A columnar shape can't be spot-checked by reading one record — `dates[17293]` and `values[17293]` have to be manually cross-referenced, and a single off-by-one in either array (a bad filter, a `Skip`/`Take` mismatch, a partial write) silently misaligns *every subsequent* value with the wrong date instead of producing one obviously-wrong record. The `[dateInt, value]` tuple doesn't have this problem — each record is still one self-contained array element; only the *labels* (`"day":`, `"month":`, `"year":`, `"value":`) are gone, not the per-record grouping.
- **No blast radius, vs. real blast radius for columnar.** The tuple shape lives entirely inside a `JsonConverter<DataRecord>` ([DataRecordJsonConverter.cs](../../ClimateExplorer.Core/Model/DataRecordJsonConverter.cs)) — `DataRecord`'s constructors, `ClimateRecordViewModel.FromDataRecord`/`ToDataRecord`, the CSV exporter, and `ClimateExplorer.WebApiClient` all keep working against the in-memory `Year`/`Month`/`Day`/`Value` properties unchanged. Columnar would instead require restructuring `ClimateRecordsResponse`/`DataSet` themselves (a `List<DataRecord>` can't become two parallel arrays without every consumer of that list changing), for a feature (`RawDataRecords`) that currently has zero live callers, and an endpoint (`Records`) whose large-payload case is a once-per-export CSV download, not a hot path.
- **No precedent need be broken.** The codebase's actual high-frequency, potentially-large series (`DataSet.DataRecords` / `BinnedRecord`) already solved this with a compact id + value per record, not columnar arrays. The `[dateInt, value]` tuple is the same idea taken one step further (a numeric id instead of a string one), so it's consistent with that precedent rather than introducing a second, different compaction strategy.

If `RawDataRecords` does get a real caller later and starts shipping large arrays routinely and the tuple shape's ~148KB isn't good enough, revisit columnar then — the numbers above will still hold roughly true.

## Recommendation

1. **Stop serializing `Key`.** ~~Keep the property... exclude it from JSON output (`[JsonIgnore]`)~~ — **Update:** a fuller look turned up that `Key` had one more consumer that the initial repo-wide grep missed (the grep output was truncated and the hit was past the preview window): `DataReaderFunctions.cs` used `record.Key` internally as the dictionary key while parsing raw source files into `Dictionary<string, DataRecord>`, and `BomDataSetDownloader.cs`/`ClimateExplorer.Data.Bom.CreateTempMean/Program.cs` used it to pair up matching max/min temperature records by date. None of that is a wire-format concern, so rather than just `[JsonIgnore]`-ing the property, `Key`/`CreateKey()` were deleted from `DataRecord` entirely: the dictionary-building key move into a local `DataReaderFunctions.BuildRecordKey` helper (same underscore-joined string, same behaviour, `[DataReaderTests.cs](../../ClimateExplorer.UnitTests/DataReaderTests.cs)` still asserts on it via the dictionary's own keys), and the max/min pairing in the two BOM call sites switched to matching on `DataRecord.Date` (a `DateOnly?` — already computed, strongly typed, no string round-trip) instead. Net effect is the same as originally planned from the wire's perspective (zero consumer changes on any JSON-facing path) but the model itself no longer carries a property that only three call sites, all internal to file-parsing, ever needed.
2. **Update: go further than dropping `Key` — serialize `DataRecord` as `[dateInt, value]`.** Implemented via [DataRecordJsonConverter.cs](../../ClimateExplorer.Core/Model/DataRecordJsonConverter.cs), attached to the type with `[JsonConverter(typeof(DataRecordJsonConverter))]` ([DataRecord.cs](../../ClimateExplorer.Core/Model/DataRecord.cs)) so it applies everywhere `DataRecord` is (de)serialized without touching call sites. `dateInt` folds `Year`/`Month`/`Day` into one number, encoding whichever of the three granularities `DataRecord` already supports (nullable `Month`/`Day`) and distinguished on read by digit count: `YYYY` (year only, e.g. `2015`), `YYYYMM` (year+month, e.g. `201508`), `YYYYMMDD` (year+month+day, e.g. `20150824`) — these ranges never overlap for any year in the dataset's actual range (4 vs. 6 vs. 8 digits). A missing `Value` serializes as `null`, e.g. `[20150824,null]`. Covered by [DataRecordSerializationTests.cs](../../ClimateExplorer.UnitTests/DataRecordSerializationTests.cs) (round-trip + all three granularities + null value, both directions).
3. **Don't adopt the columnar `{"dates": [...], "values": [...]}` shape** for the reasons above — the additional saving on top of the tuple shape is small, and the cost (readability, alignment risk, contract churn across a currently-unused feature and an export-only hot path) isn't justified by today's actual traffic pattern.
4. ~~Separately, confirm response compression is actually active on the deployed API~~ — **done**: it wasn't active at all, `AddResponseCompression`/`UseResponseCompression` have now been added (see previous section). This was the bigger, more general win of the two.
