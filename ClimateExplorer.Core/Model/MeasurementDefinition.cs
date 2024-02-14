namespace ClimateExplorer.Core.Model;

using ClimateExplorer.Core.ViewModel;
using static ClimateExplorer.Core.Enums;
public class MeasurementDefinition
{
    required public DataType DataType { get; set; }
    required public UnitOfMeasure UnitOfMeasure { get; set; }
    required public DataResolution DataResolution { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
    public RowDataType RowDataType { get; set; }
    public string? FolderName { get; set; }
    public string? FileNameFormat { get; set; }
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
