namespace ClimateExplorer.Web.Client.UiModel.Trends;

/// <summary>
/// The shape of curve fitted for a chart series' trend module. The numeric value is the polynomial
/// degree passed to <c>PolynomialRegressionCalculator</c>, so converting is a plain cast.
/// </summary>
public enum TrendRegressionType
{
    Linear = 1,
    Quadratic = 2,
    Cubic = 3,
}
