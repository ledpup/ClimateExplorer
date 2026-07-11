using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.UnitTests;

/// <summary>
/// Demonstrates why <see cref="ClimateExplorer.Core.Stats.LinearRegressionCalculator"/> squares
/// deviations with a direct multiplication instead of <c>Math.Pow(value, 2)</c>. <c>Math.Pow</c>
/// implements the general real-exponent case (handling negative/fractional exponents, NaN, and
/// infinities) and is dramatically slower than a single hardware multiply for the fixed exponent
/// of 2 used throughout the regression sum-of-squares calculations.
/// </summary>
[TestClass]
public class SquaringPerformanceTests
{
    private const int WarmupIterations = 200_000;
    private const int MeasuredIterations = 5_000_000;

    [TestMethod]
    public void DirectMultiplication_IsMuchFasterThan_MathPow()
    {
        var values = CreateSampleValues(MeasuredIterations);

        // Warm up both code paths so the JIT has tiered up before we start timing.
        RunMathPow(values, WarmupIterations);
        RunMultiplication(values, WarmupIterations);

        var powElapsed = RunMathPow(values, MeasuredIterations);
        var multiplyElapsed = RunMultiplication(values, MeasuredIterations);

        Assert.IsTrue(
            powElapsed.SumOfSquares > 0 && multiplyElapsed.SumOfSquares > 0,
            "Sanity check to prevent the JIT from eliding either loop as dead code.");
        Assert.AreEqual(
            powElapsed.SumOfSquares,
            multiplyElapsed.SumOfSquares,
            Math.Abs(powElapsed.SumOfSquares) * 1e-9,
            "Math.Pow(value, 2) and value * value must produce the same result.");

        // Observed locally on net10.0/Release: Math.Pow is ~30-35x slower than a direct multiply.
        // Assert a much smaller margin so the test isn't flaky on slower or noisier CI hardware.
        const double minimumSpeedupFactor = 5.0;

        Assert.IsGreaterThan(
            multiplyElapsed.Duration.TotalMilliseconds * minimumSpeedupFactor,
            powElapsed.Duration.TotalMilliseconds,
            $"Expected Math.Pow(value, 2) to be at least {minimumSpeedupFactor}x slower than value * value. " +
            $"Math.Pow: {powElapsed.Duration.TotalMilliseconds:F1} ms, " +
            $"multiplication: {multiplyElapsed.Duration.TotalMilliseconds:F1} ms.");
    }

    private static double[] CreateSampleValues(int count)
    {
        var random = new Random(Seed: 42);
        var values = new double[count];

        for (var i = 0; i < count; i++)
        {
            values[i] = (random.NextDouble() * 100) - 50;
        }

        return values;
    }

    private static (TimeSpan Duration, double SumOfSquares) RunMathPow(double[] values, int iterations)
    {
        var sum = 0.0;
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
        {
            sum += Math.Pow(values[i % values.Length], 2);
        }

        stopwatch.Stop();

        return (stopwatch.Elapsed, sum);
    }

    private static (TimeSpan Duration, double SumOfSquares) RunMultiplication(double[] values, int iterations)
    {
        var sum = 0.0;
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
        {
            var value = values[i % values.Length];
            sum += value * value;
        }

        stopwatch.Stop();

        return (stopwatch.Elapsed, sum);
    }
}
