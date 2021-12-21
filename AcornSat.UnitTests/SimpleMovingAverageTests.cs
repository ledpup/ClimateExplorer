using AcornSat.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AcornSat.UnitTests
{
    [TestClass]
    public class SimpleMovingAverageTests
    {
        [TestMethod]
        public void ThreeValuesTest()
        {
            var ma = new SimpleMovingAverage(3);
            Assert.IsNull(ma.AddSample(3));
            Assert.AreEqual(2, ma.AddSample(1));
            Assert.AreEqual(2.667D, Math.Round(ma.AddSample(4).Value, 3));
        }

        [TestMethod]
        public void ValuesWithNullTest()
        {
            var ma = new SimpleMovingAverage(3);
            Assert.IsNull(ma.AddSample(3));
            Assert.AreEqual(2, ma.AddSample(1));
            Assert.IsNull(ma.AddSample(null));
        }

        [TestMethod]
        public void ValuesWithNullThenValuesTest()
        {
            var ma = new SimpleMovingAverage(3);
            Assert.IsNull(ma.AddSample(3));
            Assert.AreEqual(2, ma.AddSample(1));
            Assert.IsNull(ma.AddSample(null));
            Assert.IsNull(ma.AddSample(3));
            Assert.AreEqual(2.5, ma.AddSample(2));
            Assert.AreEqual(3, ma.AddSample(4));
        }
    }
}