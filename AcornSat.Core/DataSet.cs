using static AcornSat.Core.Enums;

public class DataSet
{
    public DataSet()
    {
        Temperatures = new List<TemperatureRecord>();
    }

    public DataResolution Resolution { get; set; }
    public Location Location { get;  set; }
    public string Station { get; set; }
    public MeasurementType Type { get; set; }
    public List<TemperatureRecord> Temperatures { get; set; }
    public short? Year { get; set; }

    public List<short> Years
    { 
        get
        {
            if (Year != null)
            {
                return new List<short> { Year.Value };
            }
            var years = Temperatures.Where(x => x.Min != null && x.Max != null).Select(x => x.Year).Distinct().ToList();
            return years;
        }
    }
}

