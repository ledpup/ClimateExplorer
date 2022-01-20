using static AcornSat.Core.Enums;

namespace AcornSat.Core
{
    public class MeasurementDefinition
    {
        public DataType DataType { get; set; }
        public MeasurementType MeasurementType { get; set; }
        public string FolderName { get; set; }
        public string SubFolderName { get; set; }
        public string FileNameFormat { get; set; }

        public string DataRowRegEx { get; set; }
        public string NullValue { get; set; }
    }
}