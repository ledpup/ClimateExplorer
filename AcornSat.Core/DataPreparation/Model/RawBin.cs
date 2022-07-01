namespace ClimateExplorer.Core.DataPreparation
{
    public class RawBin
    {
        public BinIdentifier Identifier { get; set; }
        public Bucket[] Buckets { get; set; }

        public override string ToString()
        {
            return Identifier.ToString();
        }
    }
}
