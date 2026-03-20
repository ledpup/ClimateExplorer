namespace ClimateExplorer.Core.Model;

public class ClimateRecordsResponse
{
    public List<ClimateRecord> Records { get; set; } = [];
    public int? StartYear { get; set; }
    public int? EndYear { get; set; }
}
