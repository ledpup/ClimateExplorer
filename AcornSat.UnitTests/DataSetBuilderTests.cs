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
                new PostDataSetsRequestBody
                {
                    BinningRule = BinningRules.ByYear,
                    BinAggregationFunction = BinAggregationFunctions.Mean,
                    SubBinSize = 14,
                    SubBinRequiredDataProportion = 0.7f
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
                new PostDataSetsRequestBody
                {
                    BinningRule = BinningRules.ByYear,
                    BinAggregationFunction = BinAggregationFunctions.Mean,
                    SubBinSize = 14,
                    SubBinRequiredDataProportion = 0.7f
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
                new PostDataSetsRequestBody
                {
                    BinningRule = BinningRules.ByYear,
                    BinAggregationFunction = BinAggregationFunctions.Min,
                    SubBinSize = 14,
                    SubBinRequiredDataProportion = 0.7f
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
                new PostDataSetsRequestBody
                {
                    BinningRule = BinningRules.ByYear,
                    BinAggregationFunction = BinAggregationFunctions.Max,
                    SubBinSize = 14,
                    SubBinRequiredDataProportion = 0.7f
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
                new PostDataSetsRequestBody
                {
                    BinningRule = BinningRules.ByYearAndMonth,
                    BinAggregationFunction = BinAggregationFunctions.Mean,
                    SubBinSize = 14,
                    SubBinRequiredDataProportion = 0.7f
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
                new PostDataSetsRequestBody
                {
                    BinningRule = BinningRules.ByYearAndMonth,
                    BinAggregationFunction = BinAggregationFunctions.Median,
                    SubBinSize = 14,
                    SubBinRequiredDataProportion = 0.7f
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
                new PostDataSetsRequestBody
                {
                    BinningRule = BinningRules.ByYear,
                    BinAggregationFunction = BinAggregationFunctions.Sum,
                    SubBinSize = 14,
                    SubBinRequiredDataProportion = 0.7f
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
                new PostDataSetsRequestBody
                {
                    BinningRule = BinningRules.ByYearAndMonth,
                    BinAggregationFunction = BinAggregationFunctions.Sum,
                    SubBinSize = 14,
                    SubBinRequiredDataProportion = 0.7f
                }
            );

            Assert.AreEqual(12, cdp.Length); // Twelve months
            Assert.AreEqual(310, cdp[0].Value); // Jan has 31 days
            Assert.AreEqual("Jan 1990", cdp[0].Label);
            Assert.AreEqual(280, cdp[1].Value); // Feb has 28 days
            Assert.AreEqual("Feb 1990", cdp[1].Label);
        }

        TemporalDataPoint[] BuildConstantTemporalDataPointArrayFor1990()
        {
            return 
                Enumerable.Range(0, 365)
                .Select(x => new TemporalDataPoint(new DateOnly(1990, 1, 1)
                .AddDays(x), 10))
                .ToArray();
        }

        TemporalDataPoint[] BuildLinearlyIncreasingTemporalDataPointArrayFor1990(float min, float dailyIncrement)
        {
            return
                Enumerable.Range(0, 365)
                .Select(x => new TemporalDataPoint(new DateOnly(1990, 1, 1).AddDays(x), (float?)(min + x * dailyIncrement)))
                .ToArray();
        }

    }
}
