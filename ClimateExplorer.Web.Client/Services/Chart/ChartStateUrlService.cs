namespace ClimateExplorer.Web.Client.Services.Chart;

using ClimateExplorer.Web.UiLogic;
using Microsoft.AspNetCore.WebUtilities;

public sealed class ChartStateUrlService(ILogger<ChartStateUrlService> logger) : IChartStateUrlService
{
    public ChartUrlStateResult Parse(Uri uri, ChartPageContext context)
    {
        try
        {
            var query = QueryHelpers.ParseQuery(uri.Query);
            var hasChartAllData = query.ContainsKey("chartAllData");
            var hasChartSeriesDefinition = query.TryGetValue("csd", out var csdSpecifier);

            if (!hasChartSeriesDefinition && !hasChartAllData)
            {
                return new ChartUrlStateResult(ChartUrlStateKind.Missing, null, null);
            }

            var state = new ChartState
            {
                ChartAllData = hasChartAllData && bool.Parse(query["chartAllData"].ToString()),
                StartYear = query.TryGetValue("startYear", out var startYear) ? startYear.ToString() : null,
                EndYear = query.TryGetValue("endYear", out var endYear) ? endYear.ToString() : null,
                GroupingDays = query.TryGetValue("groupingDays", out var groupingDays) ? short.Parse(groupingDays.ToString()) : (short)14,
                GroupingThresholdText = query.TryGetValue("groupingThreshold", out var groupingThreshold) && !string.IsNullOrWhiteSpace(groupingThreshold.ToString())
                    ? groupingThreshold.ToString()
                    : "70",
                UserOverrideAggregationSettings = query.TryGetValue("userOverride", out var userOverride) && userOverride.ToString() == "true",
                AxesScaleToZero = ParseAxesScaleToZero(query.TryGetValue("axisScaleToZero", out var axisScaleToZero)
                    ? axisScaleToZero.ToString()
                    : null),
            };

            if (!hasChartSeriesDefinition)
            {
                return new ChartUrlStateResult(ChartUrlStateKind.ExplicitEmpty, state, null);
            }

            var series = ChartSeriesListSerializer.ParseChartSeriesDefinitionList(
                logger,
                csdSpecifier.ToString(),
                context.DataSetDefinitions,
                context.Locations,
                context.Regions);

            return new ChartUrlStateResult(
                ChartUrlStateKind.Valid,
                state with { Series = series.ToList() },
                null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse chart URL state.");
            return new ChartUrlStateResult(ChartUrlStateKind.Invalid, null, ex.Message);
        }
    }

    public string BuildRelativeUrl(string pagePath, ChartState state)
    {
        var url = $"{pagePath}?chartAllData={state.ChartAllData.ToString().ToLower()}";

        if (!string.IsNullOrWhiteSpace(state.StartYear))
        {
            url += $"&startYear={state.StartYear}";
        }

        if (!string.IsNullOrWhiteSpace(state.EndYear))
        {
            url += $"&endYear={state.EndYear}";
        }

        if (state.GroupingDays > 0)
        {
            url += $"&groupingDays={state.GroupingDays}";
        }

        if (!string.IsNullOrWhiteSpace(state.GroupingThresholdText))
        {
            url += $"&groupingThreshold={state.GroupingThresholdText}";
        }

        if (state.UserOverrideAggregationSettings)
        {
            url += "&userOverride=true";
        }

        var scaledAxes = state.AxesScaleToZero.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        if (scaledAxes.Count > 0)
        {
            url += $"&axisScaleToZero={string.Join(",", scaledAxes)}";
        }

        var chartSeriesUrlComponent = ChartSeriesListSerializer.BuildChartSeriesListUrlComponent(state.Series);
        if (chartSeriesUrlComponent.Length > 0)
        {
            url += "&csd=" + chartSeriesUrlComponent;
        }

        return url;
    }

    private static Dictionary<string, bool> ParseAxesScaleToZero(string? axisScaleToZeroParam)
    {
        if (string.IsNullOrWhiteSpace(axisScaleToZeroParam))
        {
            return [];
        }

        return axisScaleToZeroParam
            .Split(',')
            .Where(axisId => !string.IsNullOrWhiteSpace(axisId))
            .ToDictionary(axisId => axisId.Trim(), _ => true);
    }
}
