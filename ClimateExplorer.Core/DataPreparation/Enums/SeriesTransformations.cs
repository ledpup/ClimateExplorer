namespace ClimateExplorer.Core.DataPreparation;

public enum SeriesTransformations
{
    Identity,
    IsPositive,
    IsNegative,
    Negate,
    IsFrosty,
    DayOfYearIfFrost,
    Custom,
}
