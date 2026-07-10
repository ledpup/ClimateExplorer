namespace ClimateExplorer.Core.Stats;

public sealed record RegressionPrediction(
    double X,
    double PredictedY,
    ConfidenceInterval MeanConfidenceInterval,
    ConfidenceInterval ObservationPredictionInterval,
    double Alpha);
