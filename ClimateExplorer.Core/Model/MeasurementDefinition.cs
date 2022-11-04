using ClimateExplorer.Core.ViewModel;
using static ClimateExplorer.Core.Enums;

public class MeasurementDefinition
{
    public DataCategory? DataCategory { get; set; }
    public DataType DataType { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public DataResolution DataResolution { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
    public RowDataType RowDataType { get; set; }
    public string? FolderName { get; set; }
    public string? FileNameFormat { get; set; }
    public string? DataRowRegEx { get; set; }
    public string? NullValue { get; set; }

    public MeasurementDefinitionViewModel ToViewModel()
    {
        var viewModel = new MeasurementDefinitionViewModel
        {
            DataCategory = DataCategory,
            DataAdjustment = DataAdjustment,
            DataType = DataType,
            UnitOfMeasure = UnitOfMeasure
        };

        return viewModel;
    }
}
