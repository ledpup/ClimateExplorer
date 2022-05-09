using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static AcornSat.Core.Enums;

namespace AcornSat.Core
{
    public class DataSetDefinition
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
        public string MoreInformationUrl { get; set; }
        public DataResolution DataResolution { get; set; }
        public string FolderName { get; set; }
        public string StationInfoUrl { get; set; }
        public string LocationInfoUrl { get; set; }
        public string DataDownloadUrl { get; set; }

        public List<MeasurementDefinition> MeasurementDefinitions { get; set; }

        public bool HasLocations { get; set; }
        public List<Location> Locations { get; set;}

        public static async Task<List<DataSetDefinition>> GetDataSetDefinitions(string filePath = @"MetaData\DataSetDefinitions.json")
        {
            var text = await File.ReadAllTextAsync(filePath);
            var ddd = JsonSerializer.Deserialize<List<DataSetDefinition>>(text);

            return ddd;
        }
    }
}
