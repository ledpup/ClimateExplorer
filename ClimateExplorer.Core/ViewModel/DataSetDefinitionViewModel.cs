using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Core.ViewModel;

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
        Guid? locationId, 
        DataType dataType, 
        DataAdjustment? dataAdjustment, 
        bool allowNullDataAdjustment = false,
        DataType? alternativeDataType = null,
        bool throwIfNoMatch = true)
    {
        var dsds = new List<DataSetDefinitionViewModel>();

        if (locationId == null)
        {
            // Reference data
            dsds = dataSetDefinitions.Where(x =>    x.LocationIds == null
                                                 && x.MeasurementDefinitions!.Any(y => y.DataType == dataType && y.DataAdjustment == dataAdjustment))
                                         .ToList();
        }
        else
        {
            // Exact match first.
            dsds = dataSetDefinitions.Where(x =>    x.LocationIds != null
                                                 && x.LocationIds.Any(y => y == locationId)
                                                 && x.MeasurementDefinitions!.Any(y => y.DataType == dataType && y.DataAdjustment == dataAdjustment))
                                         .ToList();
        }

        // If no exact match, try again, looking for the alternative data type
        if (!dsds.Any() && alternativeDataType != null)
        { 
            dsds = dataSetDefinitions.Where(x =>    x.LocationIds != null
                                                 && x.LocationIds.Any(y => y == locationId)
                                                 && x.MeasurementDefinitions!.Any(y => y.DataType == alternativeDataType && y.DataAdjustment == dataAdjustment))
                                        .ToList();

            // Found via alternative data type, so use that instead
            if (dsds.Any())
            {
                dataType = alternativeDataType.Value;
            }
        }

        // If no exact match
        if (!dsds.Any() && allowNullDataAdjustment)
        {
            // If they did not specify a value for adjustment, try again, looking for "Adjusted". If they did specify
            // a value, try again, looking for null.
            dataAdjustment = (dataAdjustment == null) ? DataAdjustment.Adjusted : null;

            dsds = dataSetDefinitions.Where(x =>   x.LocationIds != null
                                                && x.LocationIds.Any(y => y == locationId)
                                                && x.MeasurementDefinitions!.Any(y => y.DataType == dataType && y.DataAdjustment == dataAdjustment))
                                     .ToList();
            
        }

        if (!dsds.Any())
        {
            if (throwIfNoMatch)
            {
                throw new Exception($"No matching dataset definition found with parameters: location ID = {locationId}, data type = {dataType} data adjustment {dataAdjustment}");
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
                MeasurementDefinition = md
            };
    }
}
