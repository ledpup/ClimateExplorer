namespace ClimateExplorer.Core.DataPreparation;

public class SeriesSpecification
{
    public required Guid DataSetDefinitionId { get; set; }
    public required Guid LocationId { get; set; }
    public required Enums.DataType DataType { get; set; }
    public Enums.DataAdjustment? DataAdjustment { get; set; }

    /// <summary>
    /// Disambiguates which <see cref="Model.MeasurementDefinition"/> to use when a dataset defines the
    /// same DataType/DataAdjustment combination at more than one resolution (e.g. CO₂ daily + monthly).
    /// Null when the dataset is unambiguous, or to fall back to the historical Monthly-preferred default.
    /// </summary>
    public Enums.DataResolution? DataResolution { get; set; }
}
