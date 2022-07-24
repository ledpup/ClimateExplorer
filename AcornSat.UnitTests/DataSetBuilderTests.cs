using ClimateExplorer.Core.DataPreparation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcornSat.UnitTests
{
    [TestClass]
    public class DataSetBuilderTests
    {
        [TestMethod]
        public void NoDataPoints()
        {
            var dsb = new DataSetBuilder();

            var cdp = dsb.BuildDataSetFromDataPoints(
                new TemporalDataPoint[] { },
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYear,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(0, cdp.Length);
        }

        [TestMethod]
        public void OneYearOfIdenticalDataPoints_Mean()
        {
            var dsb = new DataSetBuilder();

            var cdp = dsb.BuildDataSetFromDataPoints(
                BuildConstantTemporalDataPointArrayFor1990(),
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYear,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(1, cdp.Length);
            Assert.AreEqual(10, cdp[0].Value);
            Assert.AreEqual("1990", cdp[0].Label);
        }

        [TestMethod]
        public void OneYearOfIdenticalDataPoints_CountIfOne()
        {
            var dsb = new DataSetBuilder();

            var cdp = dsb.BuildDataSetFromDataPoints(
                BuildConstantTemporalDataPointArrayFor1990(1),
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYear,
                    BinAggregationFunction = ContainerAggregationFunctions.Sum,
                    BucketAggregationFunction = ContainerAggregationFunctions.Sum,
                    CupAggregationFunction = ContainerAggregationFunctions.Sum,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(1, cdp.Length);
            Assert.AreEqual(10, cdp[0].Value);
            Assert.AreEqual("1990", cdp[0].Label);
        }

        [TestMethod]
        public void OneYearOfLinearlyIncreasingDataPoints_Min()
        {
            var dsb = new DataSetBuilder();

            var cdp = dsb.BuildDataSetFromDataPoints(
                BuildLinearlyIncreasingTemporalDataPointArrayFor1990(5, 0.1f),
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYear,
                    BinAggregationFunction = ContainerAggregationFunctions.Min,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(1, cdp.Length);
            Assert.AreEqual(5f, cdp[0].Value);
            Assert.AreEqual("1990", cdp[0].Label);
        }

        [TestMethod]
        public void OneYearOfLinearlyIncreasingDataPoints_Max()
        {
            var dsb = new DataSetBuilder();

            var cdp = dsb.BuildDataSetFromDataPoints(
                BuildLinearlyIncreasingTemporalDataPointArrayFor1990(5, 0.1f),
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYear,
                    BinAggregationFunction = ContainerAggregationFunctions.Max,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(1, cdp.Length);
            Assert.AreEqual(41.4f, cdp[0].Value);
            Assert.AreEqual("1990", cdp[0].Label);
        }

        [TestMethod]
        public void OneYearOfLinearlyIncreasingDataPoints_Monthly_Mean_TestingForWeighting()
        {
            var dsb = new DataSetBuilder();

            var cdp = dsb.BuildDataSetFromDataPoints(
                BuildLinearlyIncreasingTemporalDataPointArrayFor1990(5, 0.1f),
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYearAndMonth,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            // We expect one entry per month in the year
            Assert.AreEqual(12, cdp.Length);

            // First entry should be for Jan
            Assert.AreEqual("Jan 1990", cdp[0].Label);

            // There are two cups in Jan.
            //   First cup has 14 entries, from 5 up to 6.3, increasing by 0.1 at a time. Its mean is 5.65.
            //   Second cup has 17 entries, from 6.4 up to 8, increasing by 0.1 at a time. Its mean is 7.2.
            //      (the second cup has more entries because the remaining days when we get to the end of
            //       the month are added to the last cup).
            // We calculate the mean for Jan by averaging the cup aggregates (5.65 and 7.2). But to avoid
            // the entries in the first cup having more weight than the second, we take a WEIGHTED mean,
            // weighting based on the number of days (NOT data points - some could be missing) in the cup.
            // So the mean for January should be (14 * 5.65 + 17 * 7.2) / (14 + 17) = 6.5, NOT the naive
            // mean of cups (5.65 + 7.2) / 2 = 6.425.
            //
            // Notice that the weighted mean of cups equals the mean of all days in the month (5 up to 8 in
            // 0.1 increments), which is what we want.
            Assert.AreEqual(6.5f, cdp[0].Value);
        }

        [TestMethod]
        public void OneYearOfLinearlyIncreasingDataPoints_Monthly_Median()
        {
            var dsb = new DataSetBuilder();

            var cdp = dsb.BuildDataSetFromDataPoints(
                BuildLinearlyIncreasingTemporalDataPointArrayFor1990(5, 0.1f),
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYearAndMonth,
                    BinAggregationFunction = ContainerAggregationFunctions.Median,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            // We expect one entry per month in the year
            Assert.AreEqual(12, cdp.Length);

            // First entry should be for Jan
            Assert.AreEqual("Jan 1990", cdp[0].Label);
            Assert.AreEqual(6.5f, cdp[0].Value); // Jan has 31 entries - odd number, a unique midpoint, just return it.

            // Second entry should be for Feb
            Assert.AreEqual("Feb 1990", cdp[1].Label);
            Assert.AreEqual(9.45f, cdp[1].Value); // Feb has 28 entries - even number, so average two neighbours of midpoint. Entries 14 and 15 are 9.4 and 9.5.
        }

        [TestMethod]
        public void OneYearOfIdenticalDataPoints_Sum()
        {
            var dsb = new DataSetBuilder();

            var cdp = dsb.BuildDataSetFromDataPoints(
                BuildConstantTemporalDataPointArrayFor1990(),
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYear,
                    BinAggregationFunction = ContainerAggregationFunctions.Sum,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(1, cdp.Length);
            Assert.AreEqual(3650, cdp[0].Value);
            Assert.AreEqual("1990", cdp[0].Label);
        }

        [TestMethod]
        public void OneYearOfIdenticalDataPoints_Sum_YearAndMonth()
        {
            var dsb = new DataSetBuilder();

            var cdp = dsb.BuildDataSetFromDataPoints(
                BuildConstantTemporalDataPointArrayFor1990(),
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYearAndMonth,
                    BinAggregationFunction = ContainerAggregationFunctions.Sum,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(12, cdp.Length); // Twelve months
            Assert.AreEqual(310, cdp[0].Value); // Jan has 31 days
            Assert.AreEqual("Jan 1990", cdp[0].Label);
            Assert.AreEqual(280, cdp[1].Value); // Feb has 28 days
            Assert.AreEqual("Feb 1990", cdp[1].Label);
        }

        [TestMethod]
        public void OneDayOfMissingDataDoesNotCauseMonthToBeRejected()
        {
            var dsb = new DataSetBuilder();

            var dataPoints = 
                BuildLinearlyIncreasingTemporalDataPointArrayFor1990(5, 0.1f)
                .Where(x => x.Month != 1 || x.Day < 11 || x.Day > 11)
                .ToArray();

            var cdp = dsb.BuildDataSetFromDataPoints(
                dataPoints,
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYearAndMonth,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(12, cdp.Length); // Twelve months
            Assert.AreEqual(6.487841f, cdp[0].Value);
            Assert.AreEqual("Jan 1990", cdp[0].Label);
        }

        [TestMethod]
        public void TwoDaysOfMissingDataDoesNotCauseMonthToBeRejected()
        {
            var dsb = new DataSetBuilder();

            var dataPoints =
                BuildLinearlyIncreasingTemporalDataPointArrayFor1990(5, 0.1f)
                .Where(x => x.Month != 1 || x.Day < 11 || x.Day > 12)
                .ToArray();

            var cdp = dsb.BuildDataSetFromDataPoints(
                dataPoints,
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYearAndMonth,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(12, cdp.Length); // Twelve months
            Assert.AreEqual(6.469893f, cdp[0].Value.Value, 0.00001f);
            Assert.AreEqual("Jan 1990", cdp[0].Label);
        }

        [TestMethod]
        public void ThreeDaysOfMissingDataDoesNotCauseMonthToBeRejected()
        {
            var dsb = new DataSetBuilder();

            var dataPoints =
                BuildLinearlyIncreasingTemporalDataPointArrayFor1990(5, 0.1f)
                .Where(x => x.Month != 1 || x.Day < 11 || x.Day > 13)
                .ToArray();

            var cdp = dsb.BuildDataSetFromDataPoints(
                dataPoints,
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYearAndMonth,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(12, cdp.Length); // Twelve months
            Assert.AreEqual(6.444575f, cdp[0].Value);
            Assert.AreEqual("Jan 1990", cdp[0].Label);
        }

        [TestMethod]
        public void FiveDaysOfMissingDataCausesMonthToBeRejected_YearAndMonthly()
        {
            var dsb = new DataSetBuilder();

            var dataPoints =
                BuildLinearlyIncreasingTemporalDataPointArrayFor1990(5, 0.1f)
                .Where(x => x.Month != 1 || x.Day < 10 || x.Day > 14)
                .ToArray();

            var cdp = dsb.BuildDataSetFromDataPoints(
                dataPoints,
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYearAndMonth,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(11, cdp.Length); // Eleven months because Jan is rejected
            Assert.AreEqual("Feb 1990", cdp[0].Label);
        }

        [TestMethod]
        public void FiveDaysOfMissingDataCausesYearToBeRejected_Yearly()
        {
            var dsb = new DataSetBuilder();

            var dataPoints =
                BuildLinearlyIncreasingTemporalDataPointArrayFor1990(5, 0.1f)
                .Where(x => x.Month != 1 || x.Day < 10 || x.Day > 14)
                .ToArray();

            var cdp = dsb.BuildDataSetFromDataPoints(
                dataPoints,
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYear,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1f,
                    RequiredBinDataProportion = 1f,
                }
            );

            Assert.AreEqual(0, cdp.Length); // Year is rejected
        }

        [TestMethod]
        public void FiveDaysOfMissingDataCausesMonthToBeRejected_MonthOnly()
        {
            var dsb = new DataSetBuilder();

            var dataPoints =
                BuildLinearlyIncreasingTemporalDataPointArrayFor1990(5, 0.1f)
                .Where(x => x.Month != 1 || x.Day < 10 || x.Day > 14)
                .ToArray();

            var cdp = dsb.BuildDataSetFromDataPoints(
                dataPoints,
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByMonthOnly,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(11, cdp.Length); // Eleven months because Jan is rejected
            Assert.AreEqual("Feb", cdp[0].Label);
        }

        [TestMethod]
        public void MonthOnly()
        {
            var dsb = new DataSetBuilder();

            var dataPoints =
                BuildLinearlyIncreasingTemporalDataPointArray(new DateOnly(1990, 1, 1), 730, 5, 0.1f)
                //.Where(x => x.Month != 1 || x.Day < 10 || x.Day > 14)
                .ToArray();

            var cdp = dsb.BuildDataSetFromDataPoints(
                dataPoints,
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByMonthOnly,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 1.0f,
                    RequiredBinDataProportion = 1.0f,
                }
            );

            Assert.AreEqual(12, cdp.Length);
            Assert.AreEqual("Jan", cdp[0].Label);
            Assert.AreEqual(24.75, cdp[0].Value.Value);
        }

        [TestMethod]
        public void SouthernHemisphereTemperateSeasonOnly()
        {
            var dsb = new DataSetBuilder();

            var dataPoints =
                BuildLinearlyIncreasingTemporalDataPointArray(new DateOnly(1990, 1, 1), 5000, 5, 0.1f)
                //.Where(x => x.Month != 1 || x.Day < 10 || x.Day > 14)
                .ToArray();

            var cdp = dsb.BuildDataSetFromDataPoints(
                dataPoints,
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.BySouthernHemisphereTemperateSeasonOnly,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0.7f,
                    RequiredBucketDataProportion = 0.7f,
                    RequiredBinDataProportion = 0.7f,
                }
            );

            Assert.AreEqual(4, cdp.Length);
            Assert.AreEqual("Summer", cdp[0].Label);
            // This needs manual verification
            Assert.AreEqual(243.646973f, cdp[0].Value.Value);
        }


        [TestMethod]
        public void WeightedMeanHandlesMissingCupValues()
        {
            var dsb = new DataSetBuilder();

            var dataPoints =
                new TemporalDataPoint[]
                {
                    new TemporalDataPoint(1990, 01, 01, 10)
                };

            var cdp = dsb.BuildDataSetFromDataPoints(
                dataPoints,
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYearAndMonth,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0f, // Note that we don't have any data level requirements, so nulls will get through
                    RequiredBucketDataProportion = 0f,
                    RequiredBinDataProportion = 0f,
                }
            );

            Assert.AreEqual(1, cdp.Length); // One month, because that's the only month with any data
            Assert.AreEqual("Jan 1990", cdp[0].Label);
            Assert.AreEqual(10.0f, cdp[0].Value); // There's only one sample
        }

        [TestMethod]
        public void NoDataReturnsNoData()
        {
            var dsb = new DataSetBuilder();

            var dataPoints =
                new TemporalDataPoint[]
                {
                };

            var cdp = dsb.BuildDataSetFromDataPoints(
                dataPoints,
                Core.Enums.DataResolution.Daily,
                new PostDataSetsRequestBody
                {
                    BinningRule = BinGranularities.ByYearAndMonth,
                    BinAggregationFunction = ContainerAggregationFunctions.Mean,
                    CupSize = 14,
                    RequiredCupDataProportion = 0f, // Note that we don't have any data level requirements, so nulls will get through
                    RequiredBucketDataProportion = 0f,
                    RequiredBinDataProportion = 0f,
                }
            );

            Assert.AreEqual(0, cdp.Length);
        }


        TemporalDataPoint[] BuildConstantTemporalDataPointArrayFor1990(float? value = 10)
        {
            return 
                Enumerable.Range(0, 365)
                .Select(x => new TemporalDataPoint(new DateOnly(1990, 1, 1)
                .AddDays(x), value))
                .ToArray();
        }

        TemporalDataPoint[] BuildLinearlyIncreasingTemporalDataPointArrayFor1990(float min, float dailyIncrement)
        {
            return
                Enumerable.Range(0, 365)
                .Select(x => new TemporalDataPoint(new DateOnly(1990, 1, 1).AddDays(x), (float?)(min + x * dailyIncrement)))
                .ToArray();
        }

        TemporalDataPoint[] BuildLinearlyIncreasingTemporalDataPointArray(DateOnly startDate, int nDays, float min, float dailyIncrement)
        {
            return
                Enumerable.Range(0, nDays)
                .Select(x => new TemporalDataPoint(startDate.AddDays(x), (float?)(min + x * dailyIncrement)))
                .ToArray();
        }

    }
}
