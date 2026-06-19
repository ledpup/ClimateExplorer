namespace ClimateExplorer.WebApi.RecentObservations;

using System.Collections.Generic;
using ClimateExplorer.Core.Model;

/// <summary>The downloaded records and provenance for one series.</summary>
internal sealed record RecentObservationSeriesDownload(
    List<DataRecord> Records,
    MeasurementDefinition MeasurementDefinition,
    string StationId,
    string SourceUrl,
    string SourceUrlLabel);
