using AcornSat.Core;
using System;

namespace AcornSat.WebApi.Model.DataSetBuilder
{
    public class SeriesSpecification
    {
        public Guid DataSetDefinitionId { get; set; }
        public Enums.DataAdjustment? DataAdjustment { get; set; }
        public Enums.DataType? DataType { get; set; }
        public Guid? LocationId { get; set; }
    }
}
