using static AcornSat.Core.Enums;

public class DataSet
{
    public DataSet()
    {
        Temperatures = new List<DataRecord>();
    }

    public DataResolution Resolution { get; set; }
    public Location Location { get;  set; }
    public DataType DataType { get; set; }
    public List<Location> Locations { get; set; }

    public string Station { get; set; }
    public MeasurementType MeasurementType { get; set; }
    public List<DataRecord> Temperatures { get; set; }

    public short? StartYear { get; set; }
    public short? Year { get; set; }

    public List<short> Years
    { 
        get
        {
            if (Year != null)
            {
                return new List<short> { Year.Value };
            }

            return 
                Temperatures
                .Where(x => x.Value != null)
                .Select(x => x.Year)
                .Distinct()
                .ToList();
        }
    }

    public int NumberOfRecords
    {
        get { return Temperatures.Count; }
    }

    public int NumberOfMissingValues
    {
        get { return Temperatures.Count(x => x.Value == null); }
    }

    public float? Mean
    {
        get { return Temperatures.Average(x => x.Value); }
    }
}

