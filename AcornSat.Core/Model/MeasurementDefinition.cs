using AcornSat.Core.ViewModel;
using static AcornSat.Core.Enums;

public class MeasurementDefinition
{
    public DataCategory? DataCategory { get; set; }
    public DataType DataType { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public RowDataType RowDataType { get; set; }
    public DataAdjustment? DataAdjustment { get; set; }
    public string? FolderName { get; set; }
    public string? SubFolderName { get; set; }
    public string? FileNameFormat { get; set; }

    public string? DataRowRegEx { get; set; }
    public string? NullValue { get; set; }
    public int PreferredColour { get; set; }
    public bool UseStationDatesWhenCompilingAcrossFiles { get; set; }

    public MeasurementDefinitionViewModel ToViewModel()
    {
        var viewModel = new MeasurementDefinitionViewModel
        {
            DataCategory = DataCategory,
            DataAdjustment = DataAdjustment,
            DataType = DataType,
            UnitOfMeasure = UnitOfMeasure,
            PreferredColour = PreferredColour
        };

        return viewModel;
    }
}
