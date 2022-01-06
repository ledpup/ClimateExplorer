using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static AcornSat.Core.Enums;

namespace AcornSat.WebApi.Model
{
    public class DataSetDefinition
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DataType DataType { get; set; }
        public DataResolution DataResolution { get; set; }
        public List<Location> Locations { get; set;}
    }
}
