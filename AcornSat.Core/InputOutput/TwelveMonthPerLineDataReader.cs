using AcornSat.Core.InputOutput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AcornSat.Core.Enums;

namespace ClimateExplorer.Core.InputOutput
{
    public static class TwelveMonthPerLineDataReader
    {
        public static async Task<DataSet> GetTwelveMonthsPerRowData(MeasurementDefinition measurementDefinition)
        {
            string[]? records = null;

            string dataPath = $@"{measurementDefinition.FolderName}\{measurementDefinition.FileNameFormat}";

            records = await DataReader.GetLinesInDataFileWithCascade(dataPath);

            if (records == null)
            {
                throw new Exception("Unable to read ENSO data " + dataPath);
            }

            var regEx = new Regex(measurementDefinition.DataRowRegEx);

            var list = new List<DataRecord>();
            var dataRowFound = false;
            foreach (var record in records)
            {
                if (!regEx.Match(record).Success)
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

                var groups = regEx.Match(record).Groups;

                var values = new List<float>();
                for (var i = 2; i < groups.Count; i++)
                {
                    if (!groups[i].Value.StartsWith(measurementDefinition.NullValue))
                    {
                        var value = float.Parse(groups[i].Value);

                        list.Add(new DataRecord(short.Parse(groups[1].Value), (short)(i - 1), null, value));
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
