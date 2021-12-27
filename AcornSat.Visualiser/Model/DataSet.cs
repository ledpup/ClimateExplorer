using static AcornSat.Core.Enums;

public class DataSet
{
    public DataResolution Resolution { get; set; }
    public Location Location { get;  set; }
    public MeasurementType Type { get; set; }
    public List<TemperatureRecord> Temperatures { get; set; }
}

