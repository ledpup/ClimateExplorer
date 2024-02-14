namespace ClimateExplorer.Core.DataPreparation;

public class SeriesSpecification
{
    required public Guid DataSetDefinitionId { get; set; }
    required public Guid LocationId { get; set; }
    required public Enums.DataType DataType { get; set; }
    public Enums.DataAdjustment? DataAdjustment { get; set; }
}
