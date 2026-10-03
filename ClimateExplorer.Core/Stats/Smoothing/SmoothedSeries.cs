namespace ClimateExplorer.Core.Stats.Smoothing;

/// <summary>
/// The output of an <see cref="ISeriesSmoother"/>. Both arrays have the same length as the input.
/// <see cref="WindowSizes"/> holds the size of the window each value was averaged over (the total
/// weight of its slots, rounded down), or 0 where <see cref="Values"/> is <c>null</c>. A window
/// smaller than the requested window size means the window was shrunk near an end of the data.
/// </summary>
public sealed record SmoothedSeries(double?[] Values, int[] WindowSizes);
