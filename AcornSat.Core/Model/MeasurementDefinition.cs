using AcornSat.Core.ViewModel;
using static AcornSat.Core.Enums;

public class MeasurementDefinition
{
    public DataType DataType { get; set; }
    public DataAdjustment DataAdjustment { get; set; }
    public string FolderName { get; set; }
    public string SubFolderName { get; set; }
    public string FileNameFormat { get; set; }

    public string DataRowRegEx { get; set; }
    public string NullValue { get; set; }
    public int PreferredColour { get; set; }

    public MeasurementDefinitionViewModel ToViewModel()
    {
        var viewModel = new MeasurementDefinitionViewModel
        {
            DataAdjustment = DataAdjustment,
            DataType = DataType,
            PreferredColour = PreferredColour
        };
        return viewModel;
    }
}
