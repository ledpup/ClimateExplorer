using AcornSat.Core.ViewModel;
using static AcornSat.Core.Enums;

public class DataSet
{
    public DataSet()
    {
        DataRecords = new List<DataRecord>();
    }

    public DataResolution Resolution { get; set; }
    public Location Location { get;  set; }
    public MeasurementDefinitionViewModel MeasurementDefinition { get; set; }
    public DataType DataType { get { return MeasurementDefinition.DataType; } }
    public DataAdjustment DataAdjustment { get { return MeasurementDefinition.DataAdjustment; } }
    public List<Location> Locations { get; set; }

    public string Station { get; set; }
    public List<DataRecord> DataRecords { get; set; }

    public short? StartYear 
    {
        get 
        {
            short? startYear = null;
            if (DataRecords.Any())
            {
                switch (Resolution)
                {
                    case DataResolution.Daily:
                        startYear = DataRecords.OrderBy(x => x.Date).First(x => x.Month == 1 && x.Day == 1).Year;
                        break;
                     case DataResolution.Monthly:
                        startYear = DataRecords.OrderBy(x => x.Year).First(x => x.Month == 1).Year;
                        break;
                    case DataResolution.Yearly:
                        startYear = DataRecords.OrderBy(x => x.Year).SkipWhile(x => !x.Value.HasValue).First().Year;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return startYear;
        }
    }
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

    float? averageOfEarliestTemperatures;
    float? averageOfLastTwentyYearsTemperatures;

    public float? WarmingIndex
    {
        get
        {
            float? warmingIndex = null;
            if (DataRecords.Count > 40)
            {
                averageOfEarliestTemperatures = DataRecords.OrderBy(x => x.Year).Take(DataRecords.Count / 2).Average(x => x.Value).Value;
                averageOfLastTwentyYearsTemperatures = DataRecords.OrderByDescending(x => x.Year).Take(20).Average(x => x.Value).Value;
                warmingIndex = MathF.Round(averageOfLastTwentyYearsTemperatures.Value - averageOfEarliestTemperatures.Value, 1);
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
                warmingIndexAsString = $"{ (warmingIndex >= 0 ? "+" : "") }{warmingIndex}°C";
            }
            return warmingIndexAsString;
        }
    }

    public string WarmingIndexDescription
    {
        get
        {
            if (WarmingIndex == null)
            {
                return $"Warming index: the temperature difference between the average of the last 20 years of maximum temperatures compared and the average of the first half of the dataset.";
            }

            var twentyYears = DataRecords.OrderByDescending(x => x.Year).Take(20);
            var firstHalf = DataRecords.OrderBy(x => x.Year).Take(DataRecords.Count / 2);

            return $@"Warming index: the temperature difference between the average of the last 20 years of maximum temperatures compared and the average of the first half ({firstHalf.Count()} years) of the dataset.
{Location.Name}, between the years {twentyYears.Last().Year}-{twentyYears.First().Year}, had an average max temp of {MathF.Round(averageOfLastTwentyYearsTemperatures.Value, 1)}°C.
{Location.Name}, between the years {firstHalf.First().Year}-{firstHalf.Last().Year}, had an average max temp of {MathF.Round(averageOfEarliestTemperatures.Value, 1)}°C.
The difference is {WarmingIndexAsString} (after rounding to 1 decimal place).";
        }
    }
}

