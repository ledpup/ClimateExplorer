using AcornSat.Core;
using AcornSat.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static AcornSat.Core.Enums;

namespace AcornSat.Core.ViewModel;

public class DataSetDefinitionViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string MoreInformationUrl { get; set; }
    public string StationInfoUrl { get; set; }
    public string LocationInfoUrl { get; set; }

    public List<Guid> LocationIds { get; set; }
    public List<MeasurementDefinitionViewModel> MeasurementDefinitions { get; set; }


    public static IEnumerable<Tuple<DataSetDefinitionViewModel, MeasurementDefinitionViewModel>> GetMeasurementsForLocation(IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, Guid locationId)
    {
        var dsds = dataSetDefinitions.Where(x => x.LocationIds != null
                                        && x.LocationIds.Any(y => y == locationId))
                                     .ToList();

        foreach (var dsd in dsds)
        {
            foreach (var md in dsd.MeasurementDefinitions)
            {
                yield return new Tuple<DataSetDefinitionViewModel, MeasurementDefinitionViewModel>(dsd, md);
            }           
        }
    }

    public static Tuple<DataSetDefinitionViewModel, MeasurementDefinitionViewModel>  GetDataSetDefinitionAndMeasurement(IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions, Guid locationId, DataType dataType, DataAdjustment? dataAdjustment, bool allowNullDataAdjustment = false)
    {
        var dsds = dataSetDefinitions.Where(x =>   x.LocationIds != null
                                                && x.LocationIds.Any(y => y == locationId) 
                                                && x.MeasurementDefinitions.Any(y => y.DataType == dataType && y.DataAdjustment == dataAdjustment))
                                     .ToList();

        if (!dsds.Any() && allowNullDataAdjustment)
        {
            dataAdjustment = null;
            dsds = dataSetDefinitions.Where(x =>   x.LocationIds != null
                                                && x.LocationIds.Any(y => y == locationId)
                                                && x.MeasurementDefinitions.Any(y => y.DataType == dataType && y.DataAdjustment == dataAdjustment))
                                     .ToList();
            
        }

        if (!dsds.Any())
        {
            throw new Exception($"No matching dataset defintion found with parameters: location ID = {locationId}, data type = {dataType} data adjustment {dataAdjustment}");
        }

        // TODO: This could be generalised in case one day we incorporate multiple "publishers" of the same data for the same location
        var dsd = dsds.SingleOrDefault();

        var md = dsd.MeasurementDefinitions.Single(x => x.DataType == dataType && x.DataAdjustment == dataAdjustment);
                
        return new Tuple<DataSetDefinitionViewModel, MeasurementDefinitionViewModel>(dsd, md);
    }
}
