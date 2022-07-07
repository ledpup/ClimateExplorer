using AcornSat.Core.Model;

public class Station
{
    public string ExternalStationCode { get; set; }
    public string Name { get; set; }
    public Coordinates Coordinates { get; set; }
    public DateTime? Opened { get; set; }
    public DateTime? Closed { get; set; }
}
