using AcornSat.Core;
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
    public DataResolution DataResolution { get; set; }
    public bool HasLocations { get; set; }

    public List<MeasurementDefinitionViewModel> MeasurementDefinitions { get; set; }
    public List<Location> Locations { get; set;}
}
