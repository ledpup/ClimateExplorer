using ClimateExplorer.Core.ViewModel;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Core.Model;
public class MeasurementDefinition
{
    public DataType DataType { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public DataResolution DataResolution { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
    public RowDataType RowDataType { get; set; }
    public string? FolderName { get; set; }
    public string? FileNameFormat { get; set; }
    public string? DataRowRegEx { get; set; }
    public string? NullValue { get; set; }
    public float? ValueAdjustment { get; set; }

    public MeasurementDefinitionViewModel ToViewModel()
    {
        var viewModel = new MeasurementDefinitionViewModel
        {
            DataAdjustment = DataAdjustment,
            DataType = DataType,
            UnitOfMeasure = UnitOfMeasure
        };

        return viewModel;
    }
}
