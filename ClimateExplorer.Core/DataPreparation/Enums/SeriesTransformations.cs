namespace ClimateExplorer.Core.DataPreparation;

public enum SeriesTransformations
{
    Identity,
    IsPositive,
    IsNegative,
    EnsoCategory,
    Negate,
    IsFrosty,
    DayOfYearIfFrost,
    EqualOrAbove35,
    EqualOrAbove1,
    EqualOrAbove1AndLessThan10,
    EqualOrAbove10,
    EqualOrAbove10AndLessThan25,
    EqualOrAbove25,
}
