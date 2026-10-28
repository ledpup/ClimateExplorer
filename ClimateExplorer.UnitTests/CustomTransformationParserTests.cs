namespace ClimateExplorer.UnitTests;

using ClimateExplorer.Core.DataPreparation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CustomTransformationParserTests
{
    [TestMethod]
    public void IsValid_NullExpression_ReturnsFalse()
    {
        // The custom-expression field treats empty as "nothing entered yet" before asking the parser
        Assert.IsFalse(CustomTransformationParser.IsValid(null));
    }

    [TestMethod]
    public void IsValid_SimpleComparison_ReturnsTrue()
    {
        Assert.IsTrue(CustomTransformationParser.IsValid("x > 25"));
    }

    [TestMethod]
    public void IsValid_CompoundExpression_ReturnsTrue()
    {
        Assert.IsTrue(CustomTransformationParser.IsValid("x >= 1 AND x < 10"));
    }

    [TestMethod]
    public void IsValid_Garbage_ReturnsFalse()
    {
        Assert.IsFalse(CustomTransformationParser.IsValid("hello world"));
    }

    [TestMethod]
    public void IsValid_MalformedNumber_ReturnsFalse()
    {
        // Matches the pattern, but double.Parse rejects it
        Assert.IsFalse(CustomTransformationParser.IsValid("x > 1.2.3"));
    }

    [TestMethod]
    public void IsValid_UnsupportedOperator_ReturnsFalse()
    {
        Assert.IsFalse(CustomTransformationParser.IsValid("x >> 3"));
    }
}
