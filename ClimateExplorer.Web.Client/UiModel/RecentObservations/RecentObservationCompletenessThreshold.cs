namespace ClimateExplorer.Web.Client.UiModel.RecentObservations;

public static class RecentObservationCompletenessThreshold
{
    public const float Default = 0.80f;
    public const int MinimumPercentage = 0;
    public const int MaximumPercentage = 100;

    public static float FromPercentage(int percentage)
    {
        return Math.Clamp(percentage, MinimumPercentage, MaximumPercentage) / 100f;
    }

    public static int ToPercentage(float threshold)
    {
        return Math.Clamp(
            (int)Math.Round(Math.Clamp(threshold, 0f, 1f) * 100f, MidpointRounding.AwayFromZero),
            MinimumPercentage,
            MaximumPercentage);
    }
}
