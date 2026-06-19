namespace ClimateExplorer.WebApi.RecentObservations;

using System.Collections.Generic;
using ClimateExplorer.Core.Model;

internal sealed record RecentObservationsDownloadResult(
    List<DataRecord> Records,
    string SourceUrl);
