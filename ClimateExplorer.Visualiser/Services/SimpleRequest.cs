using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Visualiser.UiModel;

namespace ClimateExplorer.Visualiser.Services
{
    public class SimpleRequest
    {
        public SimpleRequest(BinGranularities binGranularity, ContainerAggregationFunctions binAggregationFunction, ContainerAggregationFunctions bucketAggregationFunction, ContainerAggregationFunctions cupAggregationFunction, SeriesValueOptions seriesValueOption, SeriesSpecification[] seriesSpecifications, SeriesDerivationTypes seriesDerivationType, float requiredBinDataProportion, float requiredBucketDataProportion, float requiredCupDataProportion, int cupSize, SeriesTransformations seriesTransformation, short? year = null)
        {
            BinGranularity = binGranularity;
            BinAggregationFunction = binAggregationFunction;
            BucketAggregationFunction = bucketAggregationFunction;
            CupAggregationFunction = cupAggregationFunction;
            SeriesValueOption = seriesValueOption;
            SeriesSpecifications = seriesSpecifications;
            SeriesDerivationType = seriesDerivationType;
            RequiredBinDataProportion = requiredBinDataProportion;
            RequiredBucketDataProportion = requiredBucketDataProportion;
            RequiredCupDataProportion = requiredCupDataProportion;
            CupSize = cupSize;
            SeriesTransformation = seriesTransformation;
            Year = year;
        }

        public BinGranularities BinGranularity { get; set; }
        public ContainerAggregationFunctions BinAggregationFunction {get; set;}
        public ContainerAggregationFunctions BucketAggregationFunction { get; set; }
        public ContainerAggregationFunctions CupAggregationFunction {get; set;}
        public SeriesValueOptions SeriesValueOption { get; set; }
        public SeriesSpecification[] SeriesSpecifications {get; set;}
        public SeriesDerivationTypes SeriesDerivationType { get; set; }
        public float RequiredBinDataProportion { get; set; }
        public float RequiredBucketDataProportion { get; set; }
        public float RequiredCupDataProportion { get; set; }
        public int CupSize { get; set; }
        public SeriesTransformations SeriesTransformation { get; set; }
        public short? Year { get; set; }

        public PostDataSetsRequestBody ToPostDataSetsRequestBody()
        {
            return new PostDataSetsRequestBody
            {
                BinAggregationFunction = this.BinAggregationFunction,
                BucketAggregationFunction = this.BucketAggregationFunction,
                CupAggregationFunction = this.CupAggregationFunction,
                BinningRule = this.BinGranularity,
                CupSize = this.CupSize,
                RequiredBinDataProportion = this.RequiredBinDataProportion,
                RequiredBucketDataProportion = this.RequiredBucketDataProportion,
                RequiredCupDataProportion = this.RequiredCupDataProportion,
                SeriesDerivationType = this.SeriesDerivationType,
                SeriesSpecifications = this.SeriesSpecifications,
                SeriesTransformation = this.SeriesTransformation,
                Anomaly = this.SeriesValueOption == SeriesValueOptions.Anomaly,
                FilterToYear = this.Year,
            };
        }
    }
}
