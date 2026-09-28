namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;

public sealed class RecentObservationsDataSet
{
    private RecentObservationsDataSet(
        string domainKey,
        bool isSupported,
        string unsupportedMessage,
        string emptyMessage,
        string noPeriodsMessage,
        IReadOnlyList<DataRecord>? temperatureMaxRecords = null,
        IReadOnlyList<DataRecord>? temperatureMinRecords = null,
        IReadOnlyList<DataRecord>? temperatureMeanRecords = null,
        IReadOnlyList<DataRecord>? precipitationRecords = null,
        IReadOnlyList<DataRecord>? co2Records = null,
        IReadOnlyList<RecentObservationSourceMetadata>? sourceMetadata = null,
        bool hasHistoricalTemperatureMaxMin = false,
        bool refreshFailed = false)
    {
        DomainKey = domainKey;
        IsSupported = isSupported;
        UnsupportedMessage = unsupportedMessage;
        EmptyMessage = emptyMessage;
        NoPeriodsMessage = noPeriodsMessage;
        TemperatureMaxRecords = temperatureMaxRecords ?? [];
        TemperatureMinRecords = temperatureMinRecords ?? [];
        TemperatureMeanRecords = temperatureMeanRecords ?? [];
        PrecipitationRecords = precipitationRecords ?? [];
        Co2Records = co2Records ?? [];
        SourceMetadata = sourceMetadata ?? [];
        HasHistoricalTemperatureMaxMin = hasHistoricalTemperatureMaxMin;
        RefreshFailed = refreshFailed;
    }

    public string DomainKey { get; }
    public bool IsSupported { get; }

    internal string UnsupportedMessage { get; }
    internal string EmptyMessage { get; }
    internal string NoPeriodsMessage { get; }
    internal IReadOnlyList<DataRecord> TemperatureMaxRecords { get; }
    internal IReadOnlyList<DataRecord> TemperatureMinRecords { get; }
    internal IReadOnlyList<DataRecord> TemperatureMeanRecords { get; }
    internal IReadOnlyList<DataRecord> PrecipitationRecords { get; }
    internal IReadOnlyList<DataRecord> Co2Records { get; }
    internal IReadOnlyList<RecentObservationSourceMetadata> SourceMetadata { get; }
    internal bool HasHistoricalTemperatureMaxMin { get; }

    /// <summary>
    /// True when the API could not fetch fresh data from its upstream source for at least one of the
    /// records that make up this data set, and served an older stored/cached response instead.
    /// </summary>
    internal bool RefreshFailed { get; }

    internal static RecentObservationsDataSet Temperature(
        IReadOnlyList<DataRecord> maxRecords,
        IReadOnlyList<DataRecord> minRecords,
        IReadOnlyList<DataRecord> meanRecords,
        bool hasHistoricalMaxMin,
        IReadOnlyList<RecentObservationSourceMetadata>? sourceMetadata = null,
        bool refreshFailed = false)
    {
        return new RecentObservationsDataSet(
            ObservationDomainCatalog.TemperatureKey,
            isSupported: true,
            unsupportedMessage: "Recent temperature observations are not available for this location.",
            emptyMessage: "No recent temperature observations are available yet.",
            noPeriodsMessage: "No recent temperature observation periods can be calculated yet.",
            temperatureMaxRecords: maxRecords,
            temperatureMinRecords: minRecords,
            temperatureMeanRecords: meanRecords,
            sourceMetadata: sourceMetadata,
            hasHistoricalTemperatureMaxMin: hasHistoricalMaxMin,
            refreshFailed: refreshFailed);
    }

    internal static RecentObservationsDataSet UnsupportedTemperature()
    {
        return new RecentObservationsDataSet(
            ObservationDomainCatalog.TemperatureKey,
            isSupported: false,
            unsupportedMessage: "Recent temperature observations are not available for this location.",
            emptyMessage: "No recent temperature observations are available yet.",
            noPeriodsMessage: "No recent temperature observation periods can be calculated yet.");
    }

    internal static RecentObservationsDataSet Precipitation(
        IReadOnlyList<DataRecord> records,
        IReadOnlyList<RecentObservationSourceMetadata>? sourceMetadata = null,
        bool refreshFailed = false)
    {
        return new RecentObservationsDataSet(
            ObservationDomainCatalog.PrecipitationKey,
            isSupported: true,
            unsupportedMessage: "Recent precipitation observations are not available for this location.",
            emptyMessage: "No recent precipitation observations are available yet.",
            noPeriodsMessage: "No recent precipitation observation periods can be calculated yet.",
            precipitationRecords: records,
            sourceMetadata: sourceMetadata,
            refreshFailed: refreshFailed);
    }

    internal static RecentObservationsDataSet UnsupportedPrecipitation()
    {
        return new RecentObservationsDataSet(
            ObservationDomainCatalog.PrecipitationKey,
            isSupported: false,
            unsupportedMessage: "Recent precipitation observations are not available for this location.",
            emptyMessage: "No recent precipitation observations are available yet.",
            noPeriodsMessage: "No recent precipitation observation periods can be calculated yet.");
    }

    internal static RecentObservationsDataSet Co2(
        IReadOnlyList<DataRecord> records,
        IReadOnlyList<RecentObservationSourceMetadata>? sourceMetadata = null,
        bool refreshFailed = false)
    {
        return new RecentObservationsDataSet(
            ObservationDomainCatalog.Co2Key,
            isSupported: true,
            unsupportedMessage: "Recent CO₂ observations are not available.",
            emptyMessage: "No recent CO₂ observations are available yet.",
            noPeriodsMessage: "No recent CO₂ observation periods can be calculated yet.",
            co2Records: records,
            sourceMetadata: sourceMetadata,
            refreshFailed: refreshFailed);
    }

    internal static RecentObservationsDataSet UnsupportedCo2()
    {
        return new RecentObservationsDataSet(
            ObservationDomainCatalog.Co2Key,
            isSupported: false,
            unsupportedMessage: "Recent CO₂ observations are not available.",
            emptyMessage: "No recent CO₂ observations are available yet.",
            noPeriodsMessage: "No recent CO₂ observation periods can be calculated yet.");
    }
}
