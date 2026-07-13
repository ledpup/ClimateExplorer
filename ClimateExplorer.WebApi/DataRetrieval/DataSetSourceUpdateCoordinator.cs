namespace ClimateExplorer.WebApi.DataRetrieval;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;

internal sealed class DataSetSourceUpdateCoordinator : IDataSetSourceUpdateCoordinator
{
    public Task<DataSetSourcePreparationResult> PrepareAsync(
        PostDataSetsRequestBody request,
        DataSet cachedData,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var definitions = DataSetDefinitionsBuilder.BuildDataSetDefinitions();
        var managedSourceRequested = request.SeriesSpecifications?.Any(specification =>
        {
            var dataSet = definitions.Single(x => x.Id == specification.DataSetDefinitionId);
            var measurement = dataSet.MeasurementDefinitions!.Single(x =>
                x.DataType == specification.DataType &&
                x.DataAdjustment == specification.DataAdjustment);
            return measurement.DataDownloaderKey != null || dataSet.DataDownloaderKey != null;
        }) == true;

        if (managedSourceRequested)
        {
            return Task.FromResult(new DataSetSourcePreparationResult(DataSetSourcePreparationOutcome.RefreshFailed));
        }

        var outcome = cachedData == null
            ? DataSetSourcePreparationOutcome.Rebuild
            : DataSetSourcePreparationOutcome.UseCached;
        return Task.FromResult(new DataSetSourcePreparationResult(outcome));
    }
}
