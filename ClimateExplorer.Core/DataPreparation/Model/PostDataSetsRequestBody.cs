﻿namespace ClimateExplorer.Core.DataPreparation;

using static ClimateExplorer.Core.Enums;

public class PostDataSetsRequestBody
{
    public SeriesDerivationTypes SeriesDerivationType { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    public SeriesSpecification[]? SeriesSpecifications { get; set; }
    public SeriesTransformations SeriesTransformation { get; set; }

    public SouthernHemisphereTemperateSeasons? FilterToSouthernHemisphereTemperateSeason { get; set; }
    public SouthernHemisphereTropicalSeasons? FilterToTropicalSeason { get; set; }
    public int? FilterToYearsAfterAndIncluding { get; set; }
    public int? FilterToYearsBefore { get; set; }
    public short? FilterToYear { get; set; }

    public int CupSize { get; set; }
    public float RequiredCupDataProportion { get; set; }
    public float RequiredBucketDataProportion { get; set; }
    public float RequiredBinDataProportion { get; set; }

    public BinGranularities BinningRule { get; set; }
    public ContainerAggregationFunctions BinAggregationFunction { get; set; }
    public ContainerAggregationFunctions BucketAggregationFunction { get; set; }
    public ContainerAggregationFunctions CupAggregationFunction { get; set; }
    public bool Anomaly { get; set; }

    public bool IncludeRawDataRecords { get; set; }

    public DataResolution? MinimumDataResolution { get; set; }
}
