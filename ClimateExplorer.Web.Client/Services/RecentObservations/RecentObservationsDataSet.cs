namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel;

public sealed class RecentObservationsDataSet
{
    private RecentObservationsDataSet(
        RecentObservationsTab tab,
        bool isSupported,
        string unsupportedMessage,
        string emptyMessage,
        string noPeriodsMessage,
        IReadOnlyList<DataRecord>? temperatureMaxRecords = null,
        IReadOnlyList<DataRecord>? temperatureMinRecords = null,
        IReadOnlyList<DataRecord>? temperatureMeanRecords = null,
        IReadOnlyList<DataRecord>? precipitationRecords = null,
        IReadOnlyList<RecentObservationSourceMetadata>? sourceMetadata = null,
        bool hasHistoricalTemperatureMaxMin = false)
    {
        Tab = tab;
        IsSupported = isSupported;
        UnsupportedMessage = unsupportedMessage;
        EmptyMessage = emptyMessage;
        NoPeriodsMessage = noPeriodsMessage;
        TemperatureMaxRecords = temperatureMaxRecords ?? [];
        TemperatureMinRecords = temperatureMinRecords ?? [];
        TemperatureMeanRecords = temperatureMeanRecords ?? [];
        PrecipitationRecords = precipitationRecords ?? [];
        SourceMetadata = sourceMetadata ?? [];
        HasHistoricalTemperatureMaxMin = hasHistoricalTemperatureMaxMin;
    }

    public RecentObservationsTab Tab { get; }
    public bool IsSupported { get; }

    internal string UnsupportedMessage { get; }
    internal string EmptyMessage { get; }
    internal string NoPeriodsMessage { get; }
    internal IReadOnlyList<DataRecord> TemperatureMaxRecords { get; }
    internal IReadOnlyList<DataRecord> TemperatureMinRecords { get; }
    internal IReadOnlyList<DataRecord> TemperatureMeanRecords { get; }
    internal IReadOnlyList<DataRecord> PrecipitationRecords { get; }
    internal IReadOnlyList<RecentObservationSourceMetadata> SourceMetadata { get; }
    internal bool HasHistoricalTemperatureMaxMin { get; }

    internal static RecentObservationsDataSet Temperature(
        IReadOnlyList<DataRecord> maxRecords,
        IReadOnlyList<DataRecord> minRecords,
        IReadOnlyList<DataRecord> meanRecords,
        bool hasHistoricalMaxMin,
        IReadOnlyList<RecentObservationSourceMetadata>? sourceMetadata = null)
    {
        return new RecentObservationsDataSet(
            RecentObservationsTab.Temperature,
            isSupported: true,
            unsupportedMessage: "Recent temperature observations are not available for this location.",
            emptyMessage: "No recent temperature observations are available yet.",
            noPeriodsMessage: "No recent temperature observation periods can be calculated yet.",
            temperatureMaxRecords: maxRecords,
            temperatureMinRecords: minRecords,
            temperatureMeanRecords: meanRecords,
            sourceMetadata: sourceMetadata,
            hasHistoricalTemperatureMaxMin: hasHistoricalMaxMin);
    }

    internal static RecentObservationsDataSet UnsupportedTemperature()
    {
        return new RecentObservationsDataSet(
            RecentObservationsTab.Temperature,
            isSupported: false,
            unsupportedMessage: "Recent temperature observations are not available for this location.",
            emptyMessage: "No recent temperature observations are available yet.",
            noPeriodsMessage: "No recent temperature observation periods can be calculated yet.");
    }

    internal static RecentObservationsDataSet Precipitation(
        IReadOnlyList<DataRecord> records,
        IReadOnlyList<RecentObservationSourceMetadata>? sourceMetadata = null)
    {
        return new RecentObservationsDataSet(
            RecentObservationsTab.Precipitation,
            isSupported: true,
            unsupportedMessage: "Recent precipitation observations are not available for this location.",
            emptyMessage: "No recent precipitation observations are available yet.",
            noPeriodsMessage: "No recent precipitation observation periods can be calculated yet.",
            precipitationRecords: records,
            sourceMetadata: sourceMetadata);
    }

    internal static RecentObservationsDataSet UnsupportedPrecipitation()
    {
        return new RecentObservationsDataSet(
            RecentObservationsTab.Precipitation,
            isSupported: false,
            unsupportedMessage: "Recent precipitation observations are not available for this location.",
            emptyMessage: "No recent precipitation observations are available yet.",
            noPeriodsMessage: "No recent precipitation observation periods can be calculated yet.");
    }
}
