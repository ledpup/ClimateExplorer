namespace ClimateExplorer.Core.Model;

using ClimateExplorer.Core.DataPreparation;

public static class DataSetExtensionMethods
{
    public static BinnedRecord GetFirstDataRecordWithValueInDataSet(this DataSet dataSet)
    {
        var firstRecordWithValueIfAny = dataSet.DataRecords.FirstOrDefault(x => x.Value.HasValue);

        if (firstRecordWithValueIfAny == null)
        {
            throw new Exception("No records have a value in DataSet " + dataSet.ToString());
        }

        return firstRecordWithValueIfAny;
    }

    public static BinnedRecord GetLastDataRecordWithValueInDataSet(this DataSet dataSet)
    {
        return dataSet.DataRecords.Last(x => x.Value.HasValue);
    }

    public static short GetEndYearForDataSet(this DataSet dataSet)
    {
        return GetYearForDataRecord(GetLastDataRecordWithValueInDataSet(dataSet));
    }

    public static short GetStartYearForDataSet(this DataSet dataSet)
    {
        return GetYearForDataRecord(GetFirstDataRecordWithValueInDataSet(dataSet));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:Closing parenthesis should be spaced correctly", Justification = "Rule conflict")]
    private static short GetYearForDataRecord(BinnedRecord dr)
    {
        BinIdentifier parsedId = dr.BinIdentifier!;

        if (parsedId is BinIdentifierForGaplessBin id)
        {
            return (short)id.FirstDayInBin.Year;
        }

        throw new NotImplementedException();
    }
}
