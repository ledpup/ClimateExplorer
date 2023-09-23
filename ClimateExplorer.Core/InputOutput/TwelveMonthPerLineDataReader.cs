using ClimateExplorer.Core.InputOutput;
using ClimateExplorer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Core.InputOutput
{
    public static class TwelveMonthPerLineDataReader
    {
        public static async Task<DataSet> GetTwelveMonthsPerRowData(MeasurementDefinition measurementDefinition, List<DataFileFilterAndAdjustment> dataFileFilterAndAdjustments)
        {
            string dataPath = $@"{measurementDefinition.FolderName}\{measurementDefinition.FileNameFormat.Replace("[station]", dataFileFilterAndAdjustments.Single().Id)}";

            var records = await DataReader.GetLinesInDataFileWithCascade(dataPath);
            if (records == null)
            {
                throw new Exception("Unable to read data " + dataPath);
            }

            var regEx = new Regex(measurementDefinition.DataRowRegEx);

            var list = new List<DataRecord>();
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

                var values = new List<float>();
                for (var i = 1; i < groups.Count - 1; i++)
                {
                    if (!groups[i].Value.StartsWith(measurementDefinition.NullValue))
                    {
                        var value = float.Parse(groups[i].Value);

                        if (measurementDefinition.ValueAdjustment != null)
                        {
                            value = value / measurementDefinition.ValueAdjustment.Value;
                        }

                        list.Add(new DataRecord(short.Parse(groups["year"].Value), (short)i, null, value));
                    }
                }
            }

            return
                new DataSet()
                {
                    DataRecords = list
                };
        }
    }
}
