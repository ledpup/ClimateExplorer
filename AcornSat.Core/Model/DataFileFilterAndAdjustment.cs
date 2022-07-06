namespace AcornSat.Core.Model;

public class DataFileFilterAndAdjustment
{
    public string ExternalStationCode { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public float? ValueAdjustment { get; set; }
}
