namespace ClimateExplorer.Core.Model;

/// <summary>
/// A compact, display-ready summary of one linear trend, small enough to ship on every <see cref="Location"/>.
/// The full statistics are recalculated on demand from the underlying series - see
/// <see cref="Calculators.TemperatureTrendCalculator"/>. The window's last year isn't stored here because
/// every window ends on the series' last complete year, which lives once on the owning entity
/// (e.g. <see cref="Location.LastYear"/>).
/// </summary>
/// <param name="Window">Which period of the series the trend is fitted over.</param>
/// <param name="FirstYear">The first year in the trend's window.</param>
/// <param name="Slope">The fitted rate of change, in the series' units per year.</param>
/// <param name="IsSignificant">Whether the trend is statistically significant (overall-model F-test, p &lt; 0.05).</param>
public sealed record TrendSummary(TrendWindow Window, int FirstYear, double Slope, bool IsSignificant);
