namespace ClimateExplorer.Core.DataPreparation;

using ClimateExplorer.Core.Model;

public class Cup
{
    public DateOnly FirstDayInCup { get; set; }
    public DateOnly LastDayInCup { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    public DataRecord[]? DataRecords { get; set; }

    /// <summary>
    /// This indicates how many records would fall into this cup, if the data set was complete throughout. This varies depending on the underlying
    /// data resolution. For example, for daily data set, is equal to the number of days in the cup. For a monthly data set, if the cup covers one month,
    /// it will equal 1.
    /// </summary>
    public int ExpectedDataRecordsInCup { get; set; }

    public override string ToString()
    {
        return FirstDayInCup.ToString("yyyy-MM-dd") + " -> " + LastDayInCup.ToString("yyyy-MM-dd") + " (" + DataRecords!.Where(x => x.Value != null).Count() + " data records / " + ExpectedDataRecordsInCup + " expected)";
    }
}
