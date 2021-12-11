
public enum DataSetType
{
    Unadjusted,
    Adjusted
}
public class DataSet
{
    public Location Location { get;  set; }
    public DataSetType Type { get; set; }
    public List<YearlyAverageTemps> Temperatures { get; set; }
}

