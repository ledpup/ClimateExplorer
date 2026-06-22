# Chart series chartability investigation

## Summary

Antananarivo Ivato, Madagascar is registered as having both temperature and precipitation data. Temperature produces chartable values, but precipitation can produce zero chartable yearly datapoints after the chart aggregation and completeness threshold are applied.

Before this fix, one requested series with no chartable values could prevent the whole chart result from being prepared. The location page then showed the full no-data state, "No chart data is available for the selected location and series.", and the page displayed the generic snackbar, "Failed to create the chart with the current settings."

The chart pipeline should treat this as a per-series chartability result. The requested chart state remains unchanged, renderable series are plotted, and non-renderable series are reported with a warning.

## Current chart availability model

Chart series availability starts in `ChartState.Series`. The chart builder filters this list to `DataAvailable` series before it calls the API. For each usable `ChartSeriesDefinition`, `ChartDataBuilder.RetrieveDataSets` calls `IDataService.PostDataSet` with the selected bin granularity, aggregation functions, value option, source specifications, grouping threshold, grouping days, transformation, year filter, and minimum data resolution.

That availability check only says the data type is registered and usable for the location or source. It does not prove that the current processing settings will produce chartable values.

## Completeness filtering

Completeness filtering is applied in the API data preparation path. `DataSetBuilder.BuildDataSetFromDataRecords` bins raw records, applies `BinRejector.ApplyBinRejectionRules`, aggregates surviving bins, and then calculates final bin values. If all resulting chartable datapoints are null, `DataSetBuilder.BuildDataSet` returns an empty datapoint array.

`DataSetEndpoints.PostDataSets` converts those datapoints into a `DataSet.DataRecords` list. Therefore, a returned `DataSet` can be valid and registered for the selected data type while having no chartable `DataRecords`.

## Empty series behavior before the fix

`ChartDataBuilder.BuildProcessedDataSets` previously passed every requested series into the gapless range calculation. For linear granularities, `ChartLogic.GetBinRangeToPlotForGaplessRange` asks each preprocessed dataset for its first and last record with a value. `DataSetExtensionMethods.GetFirstDataRecordWithValueInDataSet` throws when a dataset has no valued records.

That means a precipitation dataset with zero chartable records could stop the whole build before temperature was rendered. The parent page caught the exception as a generic build failure, set `ChartDataLoadingErrored`, produced an empty `ChartDataBuildResult`, and showed the generic failure snackbar.

## Antananarivo Ivato precipitation

Antananarivo Ivato precipitation is available at the data definition/location level, so the default location chart includes precipitation on non-mobile displays. The raw data can exist, but after grouping and completeness checks, no precipitation years meet the configured threshold. The API therefore returns an empty chartable dataset for precipitation. That is not the same as the precipitation data type being unavailable.

## Distinctions in the pipeline

Before this fix, the chart path only partially distinguished these states:

- Data type unavailable: handled before build by `ChartSeriesDefinition.DataAvailable`.
- Raw data unavailable: not represented as a structured chart result in the web chart builder.
- Raw data available but incomplete: collapsed into an empty chartable dataset after API preparation.
- Completeness filtering removed all points: previously surfaced indirectly as a build failure or no-data chart.
- Smoothing removed too many points: handled explicitly by falling back to unsmoothed data and returning a warning snackbar message.

The new result model adds `ChartSeriesDataStatus` and `ChartDataBuildResult.NonRenderedSeriesWithData`, so the builder can report that a requested series had no chartable data after completeness filtering without removing it from chart state.

## Partial rendering

The chart rendering path can render partial results as long as `ChartDataBuildResult.SeriesWithData` contains only renderable series and `HasRenderableData` is true. The fix makes `SeriesWithData` the render list and `NonRenderedSeriesWithData` the skipped list. `ChartView` already renders whatever `SeriesWithData` it receives, so no state removal is required in the component.

The selected/configured series remain in `ChartState.Series`. When the user changes to another location, the normal location substitution and rebuild flow runs again. If the previously skipped data type produces chartable data for the next location, it appears again automatically.

## Snackbar layer

Existing moving-average warnings are emitted by `ChartDataBuilder` as result messages and displayed by `ChartablePage`. This is the right layer for this case too: the builder knows why a requested series could not be rendered, while the page owns presentation via the snackbar stack.

The new warning is generic by data type:

`The completeness threshold removed all [data-type] observations for [location-name].`

`There is not enough complete [data-type] data to chart this series.`

## Root cause

The root cause was that chart series availability and chart series chartability were treated as the same thing. A registered data type was assumed to produce at least one valued chart datapoint. When completeness filtering invalidated every datapoint for one series, the linear-range calculation still tried to include that empty series and the whole chart build failed.

## Implemented solution

The builder now:

- Detects requested series whose API-returned dataset has no finite values.
- Marks those series as `NoChartableDataAfterCompletenessFiltering`.
- Adds a warning message for each skipped series.
- Builds chart bins and processed datasets from renderable series only.
- Returns renderable series in `SeriesWithData` and skipped series in `NonRenderedSeriesWithData`.
- Leaves `ChartState.Series` untouched, so navigation and location changes do not create stale chart configuration.
- Preserves the existing moving-average fallback path and marks it as `FallbackToUnsmoothedData`.

The chart shows the full no-data state only when no requested series has chartable datapoints.

## Risks and follow-ups

This fix infers "no chartable data after completeness filtering" from an empty API-returned chartable dataset. That matches the current API behavior for all-bins-rejected results, but the pipeline still does not fully distinguish no raw data from raw data filtered out unless raw records are explicitly requested or the API returns structured status metadata.

Future improvement: move the status model deeper into the API response so `NoRawData`, `NoChartableDataAfterCompletenessFiltering`, and other no-output causes are first-class rather than inferred by the web chart builder.
