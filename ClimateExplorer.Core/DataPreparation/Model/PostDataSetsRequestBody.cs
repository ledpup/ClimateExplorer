namespace ClimateExplorer.Core.DataPreparation;

using static ClimateExplorer.Core.Enums;

public class PostDataSetsRequestBody
{
    public SeriesDerivationTypes SeriesDerivationType { get; set; } = SeriesDerivationTypes.ReturnSingleSeries;
    public SeriesSpecification[]? SeriesSpecifications { get; set; }
    public SeriesTransformations SeriesTransformation { get; set; } = SeriesTransformations.Identity;
    public string? CustomTransformation { get; set; }

    public required BinGranularities BinningRule { get; set; }
    public required ContainerAggregationFunctions BinAggregationFunction { get; set; }
    public ContainerAggregationFunctions BucketAggregationFunction { get; set; }
    public ContainerAggregationFunctions CupAggregationFunction { get; set; }

    public required int CupSize { get; set; }
    public required float RequiredCupDataProportion { get; set; }
    public required float RequiredBucketDataProportion { get; set; }
    public required float RequiredBinDataProportion { get; set; }

    public SouthernHemisphereTemperateSeasons? FilterToSouthernHemisphereTemperateSeason { get; set; }
    public SouthernHemisphereTropicalSeasons? FilterToTropicalSeason { get; set; }
    public int? FilterToYearsAfterAndIncluding { get; set; }
    public int? FilterToYearsBefore { get; set; }
    public short? FilterToYear { get; set; }

    public bool? Anomaly { get; set; }

    public bool? IncludeRawDataRecords { get; set; }

    public DataResolution? MinimumDataResolution { get; set; }
}
