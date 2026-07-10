namespace ClimateExplorer.Core.Stats;

public sealed record RegressionLine(double Slope, double Intercept)
{
    public double Predict(double x) => Intercept + (Slope * x);
}
