namespace ClimateExplorer.Core.Model;

public class DataFileFilterAndAdjustment
{
    public required string Id { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public float? ValueAdjustment { get; set; }
}
