namespace ClimateExplorer.Core.Stats.Model;

/// <summary>
/// The fitted curve's instantaneous rate of change at one X, with its own standard error and
/// confidence interval - the properly-propagated uncertainty behind
/// <see cref="PolynomialCurve.Derivative"/>, needed anywhere a rate is reported with a ± alongside it
/// rather than just the point value. For a degree-1 fit this is identical to the slope's own standard
/// error/confidence interval everywhere, since a line's derivative is the same constant at every X.
/// For a quadratic/cubic fit, the rate (and its uncertainty) both depend on <see cref="X"/> - a
/// derivative near the middle of a well-fitted window is typically pinned down more precisely than
/// one read far outside it.
/// </summary>
public sealed record RateOfChangeEstimate(double X, double Rate, double StandardError, ConfidenceInterval ConfidenceInterval);
