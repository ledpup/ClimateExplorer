namespace ClimateExplorer.WebApi;

using ClimateExplorer.Core.Model;

/// <summary>
/// A built <see cref="DataSet"/> response together with whether the source refresh that produced it failed
/// (in which case the data set was served from a previously cached response or the existing published source
/// file, rather than a freshly retrieved one). Shared by <see cref="DataSetEndpoints.PostDataSetsCore"/> and
/// <see cref="AcornSat.AcornSatClimateRecordService.BuildComposedDataSetAsync"/> so <see cref="ClimateRecordsEndpoints"/>
/// can surface the same signal regardless of which path served the request.
/// </summary>
internal readonly record struct DataSetRetrievalOutcome(DataSet DataSet, bool RefreshFailed);
