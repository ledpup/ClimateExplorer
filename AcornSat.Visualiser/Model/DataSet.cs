using static AcornSat.Core.Enums;

public class DataSet<T> where T : ITemperatureRecord
{
    public Location Location { get;  set; }
    public MeasurementType Type { get; set; }
    public List<T> Temperatures { get; set; }
}

