namespace ClimateExplorer.Core.Model;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.ViewModel;
using System.Text.Json.Serialization;
using static ClimateExplorer.Core.Enums;

public class DataSet
{
    public DataSet()
    {
        DataRecords = [];
    }

    public BinGranularities BinGranularity { get; set; }

    // TODO: This will go (replaced by BinGranularity)
    public DataResolution Resolution { get; set; }
    public GeographicalEntity? GeographicalEntity { get; set; }
    public MeasurementDefinitionViewModel? MeasurementDefinition { get; set; }
    public DataType DataType
    {
        get { return MeasurementDefinition!.DataType; }
    }

    public DataAdjustment? DataAdjustment
    {
        get { return MeasurementDefinition!.DataAdjustment; }
    }

    // Used when doing multi-location series like "Temperature by latitude".
    public List<Location>? Locations { get; set; }

    public IList<BinnedRecord> DataRecords { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    public DataRecord[]? RawDataRecords { get; set; }

    public short? StartYear
    {
        get
        {
            if (!DataRecords.Any())
            {
                return null;
            }

            switch (Resolution)
            {
                case DataResolution.Daily:
                    // Return the first year that has a data record for January 1st, regardless of whether
                    // that data record has a value or not (this is different to the Yearly behaviour, not sure
                    // whether it's intentional)
                    return DataRecords.OrderBy(x => x.BinId).First(x => x.BinId.Contains("m01d01")).Year;

                case DataResolution.Weekly:
                    return DataRecords.Where(x => x.Value.HasValue).OrderBy(x => x.Year).First().Year;

                case DataResolution.Monthly:
                    // Return the first year that has a data record in January, regardless of whether that data
                    // record has a value or not (this is different to the Yearly behaviour, not sure whether
                    // it's intentional)
                    return DataRecords.OrderBy(x => x.BinId).First(x => x.BinId.Contains("m01")).Year;

                case DataResolution.Yearly:
                    // Return first year that has a data record that has a value
                    return DataRecords.OrderBy(x => x.Year).SkipWhile(x => !x.Value.HasValue).First().Year;

                default:
                    throw new NotImplementedException($"Resolution {Resolution}");
            }
        }
    }

    public short? Year { get; set; }

    [JsonIgnore]
    public List<short> Years
    {
        get
        {
            if (Year != null)
            {
                return [Year.Value];
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
        get { return DataRecords.Count(); }
    }

    public int NumberOfMissingValues
    {
        get { return DataRecords.Count(x => x.Value == null); }
    }

    public double? Mean
    {
        get { return DataRecords.Average(x => x.Value); }
    }

    public override string ToString()
    {
        return $"{DataType} | {DataAdjustment} | {BinGranularity} | {GeographicalEntity} | {DataRecords.Count()}";
    }
}
