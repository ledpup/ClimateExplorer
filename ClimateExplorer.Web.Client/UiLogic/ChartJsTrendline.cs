namespace ClimateExplorer.Web.UiLogic;

/// <summary>
/// chartjs-plugin-trendline's <c>trendlineLinear</c> dataset option.
/// </summary>
/// <param name="Style">Line colour, any CSS colour string.</param>
/// <param name="LineStyle">"solid", "dotted", "dashed" or "dashdot".</param>
/// <param name="Width">Line width in pixels.</param>
public sealed record ChartJsTrendline(string Style, string LineStyle, int Width);
