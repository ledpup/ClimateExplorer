namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// The output of an <see cref="ISeriesSmoother"/>. Every array has the same length as the input.
/// <see cref="WindowSizes"/> holds the size of the window each value was averaged over (the total
/// weight of its slots, rounded down), or 0 where <see cref="Values"/> is <c>null</c>. A window
/// smaller than the requested window size means the window was shrunk near an end of the data.
/// <see cref="WindowStarts"/> and <see cref="WindowEnds"/> hold the input indexes of the first and
/// last slot of that window (for an even window, its two half-weight slots), and are only
/// meaningful where <see cref="Values"/> isn't <c>null</c>.
/// </summary>
public sealed record SmoothedSeries(double?[] Values, int[] WindowSizes, int[] WindowStarts, int[] WindowEnds);
