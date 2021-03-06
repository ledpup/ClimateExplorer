using AcornSat.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcornSat.UnitTests
{
    [TestClass]
    public class AverageTests
    {
        [TestMethod]
        public void SimpleMedianTest()
        {
            var values = new float[] { 1, 4, 7 };
            Assert.AreEqual(4, values.Median());
        }

        [TestMethod]
        [DataRow(new float[] { 9, 10, 12, 13, 13, 13, 15, 15, 16, 16, 18, 22, 23, 24, 24, 25 }, 15.5F)]
        [DataRow(new float[] { 1, 1, 2, 6, 6, 9 }, 4)]
        public void ComplexMedianTest(float[] values, float expected)
        {

            Assert.AreEqual(expected, values.Median());
        }

        [TestMethod]
        public void AverageWithNullTest()
        {
            var values = new float?[] { 9, 10, null, 13, 4 };
            var mean = values.Average();
            Assert.AreEqual(5, values.Length);
            Assert.AreEqual(9, mean); // This means that nulls are ignored when calculating mean
        }
    }
}
