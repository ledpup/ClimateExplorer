namespace ClimateExplorer.Core.Stats.Model;

public sealed record RegressionPrediction(
    double X,
    double PredictedY,
    ConfidenceInterval MeanConfidenceInterval,
    ConfidenceInterval ObservationPredictionInterval,
    double Alpha);
