namespace ClimateExplorer.Core.ViewModel;

using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public class DataSetDefinitionViewModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public string? PublisherUrl { get; set; }
    public string? MoreInformationUrl { get; set; }
    public string? StationInfoUrl { get; set; }
    public string? LocationInfoUrl { get; set; }

    public List<Guid>? LocationIds { get; set; }
    public List<MeasurementDefinitionViewModel>? MeasurementDefinitions { get; set; }

    public static IEnumerable<Tuple<DataSetDefinitionViewModel, MeasurementDefinitionViewModel>> GetMeasurementsForLocation(IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, Guid locationId)
    {
        var dsds = dataSetDefinitions.Where(x => x.LocationIds != null
                                        && x.LocationIds.Any(y => y == locationId))
                                     .ToList();

        foreach (var dsd in dsds)
        {
            foreach (var md in dsd.MeasurementDefinitions!)
            {
                yield return new Tuple<DataSetDefinitionViewModel, MeasurementDefinitionViewModel>(dsd, md);
            }
        }
    }

    public static DataSetAndMeasurementDefinition? GetDataSetDefinitionAndMeasurement(
            IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions,
            Guid locationId,
            List<DataSubstitute> dataSubstitutes,
            bool throwIfNoMatch = true)
    {
        var dsds = new List<DataSetDefinitionViewModel>();

        Enums.DataType? dataType = null;
        DataAdjustment? dataAdjustment = null;

        foreach (var dataMatch in dataSubstitutes)
        {
            dataType = dataMatch.DataType;
            dataAdjustment = dataMatch.DataAdjustment;
            dsds = dataSetDefinitions.Where(x => x.LocationIds != null
                                                && x.LocationIds.Any(y => y == locationId)
                                                && x.MeasurementDefinitions!.Any(y => y.DataType == dataType && y.DataAdjustment == dataAdjustment))
                                        .ToList();

            // If we find a match, break out of the search (we need to order the matches on preference
            if (dsds.Any())
            {
                break;
            }
        }

        if (dataType == null && dataAdjustment == null)
        {
            throw new NullReferenceException("No dataType or dataAdjustment");
        }
        else if (!dsds.Any())
        {
            if (throwIfNoMatch)
            {
                throw new Exception($"No matching dataset definition found with parameters: location ID = {locationId}, data type = {dataType}, data adjustment = {dataAdjustment}");
            }
            else
            {
                return null;
            }
        }

        // TODO: This could be generalised in case one day we incorporate multiple "publishers" of the same data for the same location
        var dsd = dsds.SingleOrDefault();

        var md = dsd!.MeasurementDefinitions!.Single(x => x.DataType == dataType && x.DataAdjustment == dataAdjustment);

        return
            new DataSetAndMeasurementDefinition
            {
                DataSetDefinition = dsd,
                MeasurementDefinition = md,
            };
    }

    public static DataSetAndMeasurementDefinition? GetDataSetDefinitionAndMeasurement(
        IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions,
        Guid locationId,
        Enums.DataType dataType,
        DataAdjustment? dataAdjustment,
        bool throwIfNoMatch = true)
    {
        var dataMatches = new List<DataSubstitute>
        {
            new DataSubstitute
            {
                DataType = dataType,
                DataAdjustment = dataAdjustment,
            },
        };

        return GetDataSetDefinitionAndMeasurement(dataSetDefinitions, locationId, dataMatches, throwIfNoMatch);
    }
}
