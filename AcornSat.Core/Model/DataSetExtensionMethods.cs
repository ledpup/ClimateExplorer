using ClimateExplorer.Core.DataPreparation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateExplorer.Core.Model
{
    public static class DataSetExtensionMethods
    {
        public static DataRecord GetFirstDataRecordWithValueInDataSet(this DataSet dataSet)
        {
            var firstRecordWithValueIfAny = dataSet.DataRecords.FirstOrDefault(x => x.Value.HasValue);

            if (firstRecordWithValueIfAny == null)
            {
                throw new Exception("No records have a value in DataSet " + dataSet.ToString());
            }

            return firstRecordWithValueIfAny;
        }

        public static DataRecord GetLastDataRecordWithValueInDataSet(this DataSet dataSet)
        {
            return dataSet.DataRecords.Last(x => x.Value.HasValue);
        }

        public static short GetStartYearForDataSet(this DataSet dataSet)
        {
            return GetYearForDataRecord(GetFirstDataRecordWithValueInDataSet(dataSet));
        }

        static short GetYearForDataRecord(DataRecord dr)
        {
            BinIdentifier parsedId = dr.GetBinIdentifier();

            if (parsedId is BinIdentifierForGaplessBin id)
            {
                return (short)id.FirstDayInBin.Year;
            }

            throw new NotImplementedException();
        }

    }
}
