namespace ClimateExplorer.Visualiser.UiModel;

public class DataSetLibraryFolder
{
    public string? Name { get; set; }
    public List<DataSetLibraryFolder> SubFolders { get; set; } = new List<DataSetLibraryFolder>();
    public List<DataSetLibraryEntry> DataSets { get; set; } = new List<DataSetLibraryEntry>();
}
