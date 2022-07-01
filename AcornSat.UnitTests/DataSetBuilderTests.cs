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
    }
}
