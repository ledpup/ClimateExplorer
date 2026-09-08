# `DataRecord` JSON payload size: investigation

## Summary

`Key` on [DataRecord.cs](../../ClimateExplorer.Core/Model/DataRecord.cs) is a redundant, string-encoded restatement of `Year`/`Month`/`Day` — and it's never read back anywhere: not by the server, not by the Blazor client, not by any test that inspects behaviour rather than the property itself. It's pure serialization cost. Dropping it from the wire shape cuts the gzipped size of a 40k-row daily series by **~26%** with zero consumer changes, because `Year`/`Month`/`Day` (the fields everything actually uses) are already in the payload.

The `{"dates": [...], "values": [...]}` columnar restructuring the issue proposes saves more (~38% gzipped vs. today, ~15-17% beyond just dropping `Key`) but the marginal win over "drop `Key`" is small once compression is accounted for, and it costs real readability and robustness (parallel-array index alignment, can't eyeball one record, bigger blast radius across server/client/tests). Given neither of the two paths that actually emit `DataRecord` arrays is routinely pushing tens of thousands of rows today (see below), that trade isn't worth it right now.

**Recommendation:** stop serializing `Key` (see [Recommendation](#recommendation)). Don't adopt the columnar shape unless/until `RawDataRecords` gets a real caller and regularly ships large arrays — and if that happens, prefer the pattern the codebase already uses for its highest-volume series (`BinnedRecord`: a compact string id + value, no columnar split) over introducing a new, riskier shape.

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

Two things stand out:

1. **Removing the one redundant field (`Key`) captures most of the available win** — 25.8 of the 38.3 percentage points, i.e. two-thirds of the total possible saving, from a one-line change with no consumer impact.
2. **After gzip, "keep a string key, drop the numeric fields" and "drop the key, keep the numeric fields" cost about the same** (170,143 vs. 173,722 bytes) — confirming the actual waste was never either representation on its own, it was encoding the same three numbers *twice*, in two different textual forms, in every record.

Everything past "drop `Key`" — compact keys, tuples, columnar arrays — is fighting over the remaining ~30-70KB, a 4-15% shrink of an already-865KB-out-of-2.5MB-reduced payload.

## Is response compression even happening?

Worth flagging as a separate, prior question: [ClimateExplorer.WebApi/Program.cs](../../ClimateExplorer.WebApi/Program.cs) has no `AddResponseCompression`/`UseResponseCompression` call. Whether responses are actually gzipped depends entirely on the hosting layer (this is deployed as an Azure Static Web Apps-fronted API, per the CORS comment at [Program.cs:30-39](../../ClimateExplorer.WebApi/Program.cs#L30-L39)) doing it transparently. That's plausible but unverified here — worth confirming with a real `curl -H "Accept-Encoding: gzip" -D -` against the deployed endpoint. If compression turns out *not* to be applied, the raw-byte column above is what users actually download, and removing `Key` alone is a 49% cut, not 26%.

## Why not the columnar `{"dates": [...], "values": [...]}` shape

This was the option the issue asked to specifically evaluate a justification for. Against it:

- **Marginal, not dramatic, gain over the free fix.** 144KB vs. 174KB gzipped (after dropping `Key`) on a 40k-row extreme case — about 30KB. For the common case (a paginated page of 10-100 records, or the CSV-export path which isn't JSON at all), the difference is a few dozen bytes.
- **Readability cost is real, not just aesthetic.** A columnar shape can't be spot-checked by reading one record — `dates[17293]` and `values[17293]` have to be manually cross-referenced, and a single off-by-one in either array (a bad filter, a `Skip`/`Take` mismatch, a partial write) silently misaligns *every subsequent* value with the wrong date instead of producing one obviously-wrong record. A per-record object shape can't misalign like that.
- **Bigger blast radius for the win available.** It changes the wire contract for `ClimateRecordsResponse` and (if applied there too) `DataSet`, which means updating `DataRecord`'s constructors/serialization, `ClimateRecordViewModel.FromDataRecord`/`ToDataRecord`, the CSV exporter, `ClimateExplorer.WebApiClient` deserialization, and every test in [DataRecordSerializationTests.cs](../../ClimateExplorer.UnitTests/DataRecordSerializationTests.cs), [ChartDataBuilderTests.cs](../../ClimateExplorer.UnitTests/ChartDataBuilderTests.cs), etc. — for a feature (`RawDataRecords`) that currently has zero live callers, and an endpoint (`Records`) whose large-payload case is a once-per-export CSV download, not a hot path.
- **No precedent need be broken.** The codebase's actual high-frequency, potentially-large series (`DataSet.DataRecords` / `BinnedRecord`) already solved this with a compact id string + value per record, not columnar arrays — and that's held up fine. Introducing a second, different compaction strategy just for `DataRecord` adds inconsistency for a small extra return.

If `RawDataRecords` does get a real caller later and starts shipping large arrays routinely, revisit — and lean towards mirroring `BinnedRecord`'s approach (single compact id + value) rather than columnar, for the consistency and alignment-safety reasons above.

## Recommendation

1. **Stop serializing `Key`.** ~~Keep the property... exclude it from JSON output (`[JsonIgnore]`)~~ — **Update:** a fuller look turned up that `Key` had one more consumer that the initial repo-wide grep missed (the grep output was truncated and the hit was past the preview window): `DataReaderFunctions.cs` used `record.Key` internally as the dictionary key while parsing raw source files into `Dictionary<string, DataRecord>`, and `BomDataSetDownloader.cs`/`ClimateExplorer.Data.Bom.CreateTempMean/Program.cs` used it to pair up matching max/min temperature records by date. None of that is a wire-format concern, so rather than just `[JsonIgnore]`-ing the property, `Key`/`CreateKey()` were deleted from `DataRecord` entirely: the dictionary-building key move into a local `DataReaderFunctions.BuildRecordKey` helper (same underscore-joined string, same behaviour, `[DataReaderTests.cs](../../ClimateExplorer.UnitTests/DataReaderTests.cs)` still asserts on it via the dictionary's own keys), and the max/min pairing in the two BOM call sites switched to matching on `DataRecord.Date` (a `DateOnly?` — already computed, strongly typed, no string round-trip) instead. Net effect is the same as originally planned from the wire's perspective (zero consumer changes on any JSON-facing path, same ~26% gzipped / ~49% raw win) but the model itself no longer carries a property that only three call sites, all internal to file-parsing, ever needed.
2. **Leave `Year`/`Month`/`Day` as discrete fields.** They're both the smallest safe unit (no further redundancy to remove) and the exact shape every current consumer already wants — deserializing straight into `ClimateRecordViewModel` and the CSV columns with no client-side date parsing.
3. **Don't adopt the columnar `{"dates": [...], "values": [...]}` shape** for the reasons above — the additional saving on top of step 1 is small, and the cost (readability, alignment risk, contract churn across a currently-unused feature and an export-only hot path) isn't justified by today's actual traffic pattern.
4. **Separately, confirm response compression is actually active** on the deployed API (see previous section) — if it isn't, that's a bigger and more general win than anything about `DataRecord`'s shape, and would change how urgent step 1 is.
