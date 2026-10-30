using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

internal static class TestAssert
{
    public static void AreWithin(double expected, double actual, double relativeTolerance, string? message = null) =>
        Assert.AreEqual(expected, actual, Math.Abs(expected) * relativeTolerance, message);

    public static string ThrowsForField(Action action)
    {
        var exception = Assert.ThrowsExactly<InputValidationException>(action);

        Assert.HasCount(1, exception.Errors, exception.Message);

        return exception.Errors[0].Field;
    }
}
