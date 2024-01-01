namespace ClimateExplorer.Core.DataPreparation;

public class SeriesSpecification
{
    public required Guid DataSetDefinitionId { get; set; }
    public required Guid LocationId { get; set; }
    public required Enums.DataType DataType { get; set; }
    public Enums.DataAdjustment? DataAdjustment { get; set; }
}
