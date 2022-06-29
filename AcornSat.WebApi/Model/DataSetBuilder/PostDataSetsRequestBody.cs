namespace AcornSat.WebApi.Model.DataSetBuilder
{
    public class PostDataSetsRequestBody
    {
        public SeriesDerivationTypes SeriesDerivationType { get; set; }
        public SeriesSpecification[] SeriesSpecifications { get; set; }
        public SeriesTransformations SeriesTransformation { get; set; }

        public TemperateSeasons? FilterToTemperateSeason { get; set; }
        public TropicalSeasons? FilterToTropicalSeason { get; set; }
        public int? FilterToYearsAfterAndIncluding { get; set; }
        public int? FilterToYearsBefore { get; set; }

        public int SubBinSize { get; set; }
        public float SubBinRequiredDataProportion { get; set; }

        public BinningRules BinningRule { get; set; }
        public BinAggregationFunctions BinAggregationFunction { get; set; }
    }
}
