namespace ClimateExplorer.Core.InputOutput;

using System.Text.RegularExpressions;
using ClimateExplorer.Core.Model;

public static class TwelveMonthPerLineDataReader
{
    public static async Task<List<DataRecord>> GetTwelveMonthsPerRowData(
        MeasurementDefinition measurementDefinition,
        List<DataFileFilterAndAdjustment>? dataFileFilterAndAdjustments,
        string datasetsFolder = "Datasets")
    {
        var station = string.Empty;
        if (dataFileFilterAndAdjustments != null)
        {
            station = dataFileFilterAndAdjustments!.Single().Id;
        }

        var dataFileSource = measurementDefinition.DataFileSource
            ?? throw new InvalidOperationException("Every measurement definition must have an explicit data file source.");
        var records = await DataReaderFunctions.GetLinesInDataFileSource(dataFileSource, station, datasetsFolder);
        if (records == null)
        {
            throw new Exception("Unable to read data " + dataFileSource.FilePathFormat);
        }

        var regEx = new Regex(measurementDefinition.DataRowRegEx!);

        var dataRecords = new List<DataRecord>();
        var dataRowFound = false;
        foreach (var record in records)
        {
            var match = regEx.Match(record);
            if (!match.Success)
            {
                if (dataRowFound)
                {
                    break;
                }
                else
                {
                    continue;
                }
            }

            dataRowFound = true;

            var groups = match.Groups;

            var values = new List<double>();
            for (var i = 1; i < groups.Count - 1; i++)
            {
                if (!groups[i].Value.StartsWith(measurementDefinition.NullValue!))
                {
                    var value = double.Parse(groups[i].Value);

                    if (measurementDefinition.ValueAdjustment != null)
                    {
                        value = value / measurementDefinition.ValueAdjustment.Value;
                    }

                    dataRecords.Add(new DataRecord(short.Parse(groups["year"].Value), (short?)i, value));
                }
            }
        }

        return dataRecords;
    }
}
