using static AcornSat.Core.Enums;

public class DataSet
{
    public DataSet()
    {
        DataRecords = new List<DataRecord>();
    }

    public DataResolution Resolution { get; set; }
    public Location Location { get;  set; }
    public DataType DataType { get; set; }
    public List<Location> Locations { get; set; }

    public string Station { get; set; }
    public DataAdjustment DataAdjustment { get; set; }
    public List<DataRecord> DataRecords { get; set; }

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
                DataRecords
                .Where(x => x.Value != null)
                .Select(x => x.Year)
                .Distinct()
                .ToList();
        }
    }

    public int NumberOfRecords
    {
        get { return DataRecords.Count; }
    }

    public int NumberOfMissingValues
    {
        get { return DataRecords.Count(x => x.Value == null); }
    }

    public float? Mean
    {
        get { return DataRecords.Average(x => x.Value); }
    }

    public float? WarmingIndex
    {
        get
        {
            float? warmingIndex = null;
            if (DataRecords.Count > 40)
            {
                var averageOfEarliestTemperatures = DataRecords.OrderBy(x => x.Year).Take(DataRecords.Count / 2).Average(x => x.Value).Value;
                var averageOfLastTwentyYearsTemperatures = DataRecords.OrderByDescending(x => x.Year).Take(20).Average(x => x.Value).Value;
                warmingIndex = MathF.Round(averageOfLastTwentyYearsTemperatures - averageOfEarliestTemperatures, 1);
            }
            return warmingIndex;
        }
    }

    public string WarmingIndexAsString
    {
        get
        {
            var warmingIndex = WarmingIndex;
            var warmingIndexAsString = "NA";
            if (warmingIndex != null)
            {
                warmingIndexAsString = $"{ (warmingIndex >= 0 ? "+" : "") }{warmingIndex}";
            }
            return warmingIndexAsString;
        }
    }
}

