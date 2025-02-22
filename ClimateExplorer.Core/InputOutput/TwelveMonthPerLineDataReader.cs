﻿namespace ClimateExplorer.Core.InputOutput;

using ClimateExplorer.Core.Model;
using System.Text.RegularExpressions;

public static class TwelveMonthPerLineDataReader
{
    public static async Task<List<DataRecord>> GetTwelveMonthsPerRowData(MeasurementDefinition measurementDefinition, List<DataFileFilterAndAdjustment>? dataFileFilterAndAdjustments)
    {
        var dataPath = $@"{measurementDefinition.FolderName}\{measurementDefinition!.FileNameFormat!}";
        if (dataFileFilterAndAdjustments != null)
        {
            dataPath = dataPath.Replace("[station]", dataFileFilterAndAdjustments!.Single().Id);
        }

        var records = await DataReaderFunctions.GetLinesInDataFileWithCascade(dataPath);
        if (records == null)
        {
            throw new Exception("Unable to read data " + dataPath);
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
