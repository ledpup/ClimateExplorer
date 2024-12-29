namespace ClimateExplorer.Core.Model;
public class DataFileFilterAndAdjustment
{
    required public string Id { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
