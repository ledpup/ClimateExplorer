using ClimateExplorer.Core;
using System;

namespace ClimateExplorer.Core.DataPreparation
{
    public class SeriesSpecification
    {
        public Guid DataSetDefinitionId { get; set; }
        public Enums.DataAdjustment? DataAdjustment { get; set; }
        public Enums.DataType? DataType { get; set; }
        public Guid? LocationId { get; set; }
    }
}
