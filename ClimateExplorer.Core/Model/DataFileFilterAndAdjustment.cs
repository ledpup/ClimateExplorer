namespace ClimateExplorer.Core.Model;
public class DataFileFilterAndAdjustment
{
    required public string Id { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public double? ValueAdjustment { get; set; }
}
