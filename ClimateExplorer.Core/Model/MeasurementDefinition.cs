namespace ClimateExplorer.Core.Model;

using ClimateExplorer.Core.ViewModel;
using static ClimateExplorer.Core.Enums;
public class MeasurementDefinition
{
    public required DataType DataType { get; set; }
    public required UnitOfMeasure UnitOfMeasure { get; set; }
    public required DataResolution DataResolution { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
    public RowDataType RowDataType { get; set; }
    public required DataFileSourceDefinition DataFileSource { get; set; }
    public string? DataDownloadUrl { get; set; }
    public string? DataDownloaderKey { get; set; }
    public string? DataRowRegEx { get; set; }
    public string? NullValue { get; set; }
    public double? ValueAdjustment { get; set; }

    public MeasurementDefinitionViewModel ToViewModel()
    {
        var viewModel = new MeasurementDefinitionViewModel
        {
            DataAdjustment = DataAdjustment,
            DataType = DataType,
            UnitOfMeasure = UnitOfMeasure,
            DataResolution = DataResolution,
        };

        return viewModel;
    }
}
