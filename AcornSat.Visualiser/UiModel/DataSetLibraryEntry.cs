using static AcornSat.Core.Enums;

namespace AcornSat.Visualiser.UiModel
{
    public class DataSetLibraryEntry
    {
        public string Name { get; set; }
        public Guid SourceDataSetId { get; set; }
        public DataAdjustment DataAdjustment { get; set; }
        public DataType DataType { get; set; }
    }
}
