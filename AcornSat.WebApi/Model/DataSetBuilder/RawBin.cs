namespace AcornSat.WebApi.Model.DataSetBuilder
{
    public class RawBin
    {
        public string BinId { get; set; }
        public string BinFriendlyName { get; set; }
        public TemporalDataPoint[][] SubBinnedDataPoints { get; set; }
    }
}
