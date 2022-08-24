namespace ClimateExplorer.Core.DataPreparation
{
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
        EqualOrAbove1LessThan10,
        EqualOrAbove10,
        EqualOrAbove10LessThan25,
        EqualOrAbove25,
    }
}
