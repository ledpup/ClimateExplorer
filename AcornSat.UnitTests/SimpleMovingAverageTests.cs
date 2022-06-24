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
            var ma = new SimpleMovingAverage(5);
            Assert.IsNull(ma.AddSample(3));
            Assert.AreEqual(2, ma.AddSample(1));
            Assert.AreEqual(2.667D, Math.Round(ma.AddSample(4).Value, 3));
        }

        [TestMethod]
        public void ValuesWithNullTest()
        {
            var ma = new SimpleMovingAverage(5);    // null null null null null
            Assert.IsNull(ma.AddSample(3));         // null null null null 3    | 1/5 = 0.2 values available < 0.25, so returns null
            Assert.AreEqual(2, ma.AddSample(1));    // null null null 3 1       | 2/5 = 0.4 values available > 0.25, so returns val
            Assert.AreEqual(2, ma.AddSample(null)); // null null 3 1 null       | 2/5 = 0.4 values available > 0.25, so returns val
            Assert.AreEqual(2, ma.AddSample(null)); // null 3 1 null null       | 2/5 = 0.4 values available > 0.25, so returns val
            Assert.AreEqual(2, ma.AddSample(null)); // 3 1 null null null       | 2/5 = 0.4 values available > 0.25, so returns val
            Assert.AreEqual(3, ma.AddSample(5));    // 1 null null null 5       | 2/5 = 0.4 values available > 0.25, so returns val
            Assert.AreEqual(4, ma.AddSample(3));    // null null null 5 3       | 2/5 = 0.4 values available > 0.25, so returns val
            Assert.AreEqual(4, ma.AddSample(null)); // null null 5 3 null       | 2/5 = 0.4 values available > 0.25, so returns val
            Assert.AreEqual(4, ma.AddSample(null)); // null 5 3 null null       | 2/5 = 0.4 values available > 0.25, so returns val
            Assert.AreEqual(4, ma.AddSample(null)); // 5 3 null null null       | 2/5 = 0.4 values available > 0.25, so returns val
            Assert.IsNull(ma.AddSample(null));      // 3 null null null null    | 1/5 = 0.2 values available < 0.25, so returns null
            Assert.IsNull(ma.AddSample(null));      // null null null null null | 0/5 = 0.0 values available < 0.25, so returns null
            Assert.IsNull(ma.AddSample(1));         // null null null null 1    | 1/5 = 0.2 values available < 0.25, so returns null
            Assert.AreEqual(2, ma.AddSample(3));    // null null null 1 3       | 2/5 = 0.4 values available > 0.25, so returns val
            Assert.AreEqual(3, ma.AddSample(5));    // null null 1 3 5          | 3/5 = 0.6 values available > 0.25, so returns val
            Assert.AreEqual(2, ma.AddSample(-1));   // null 1 3 5 -1            | 4/5 = 0.8 values available > 0.25, so returns val
            Assert.AreEqual(3, ma.AddSample(7));    // 1 3 5 -1 7               | 5/5 = 1.0 values available > 0.25, so returns val
        }
    }
}