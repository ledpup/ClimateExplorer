using ClimateExplorer.Core.ViewModel;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Core.Model;
public class MeasurementDefinition
{
    public required DataType DataType { get; set; }
    public required UnitOfMeasure UnitOfMeasure { get; set; }
    public required DataResolution DataResolution { get; set; }
    public required DataAdjustment? DataAdjustment { get; set; }
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
            UnitOfMeasure = UnitOfMeasure,
            DataResolution = DataResolution,
        };

        return viewModel;
    }
}
