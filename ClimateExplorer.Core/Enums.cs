using ClimateExplorer.Core.DataPreparation;

namespace ClimateExplorer.Core;

public static class Enums
{
    public enum DataType
    {
        TempMax,
        TempMin,
        Rainfall,
        SolarRadiation,

        MEIv2,
        SOI,
        Nino34,
        ONI,

        CO2,
        CH4,
        N2O,
        IOD,
    }

    public enum DataResolution
    {
        Yearly,
        Monthly,
        Weekly,
        Daily,
    }

    public enum DataAdjustment
    {
        Unadjusted,
        Adjusted,
        Difference
    }

    public enum UnitOfMeasure
    {
        DegreesCelsius,
        DegreesCelsiusAnomaly,
        Millimetres,
        PartsPerMillion,
        PartsPerBillion,
        EnsoIndex,
        MegajoulesPerSquareMetre
    }

    public static string UnitOfMeasureLabel(SeriesTransformations seriesTransformations, UnitOfMeasure unitOfMeasure)
    {
        return seriesTransformations switch
        {
            SeriesTransformations.IsFrosty => "Days of frost",
            SeriesTransformations.DayOfYearIfFrost => "Day of year",
            SeriesTransformations.EqualOrAbove35 => "Days of 35°C or above",
            SeriesTransformations.EqualOrAbove1 => "Days of 1mm of rain or more",
            SeriesTransformations.EqualOrAbove1AndLessThan10 => "Days between 1mm and 10mm of rain",
            SeriesTransformations.EqualOrAbove10 => "Days of 10mm of rain or more",
            SeriesTransformations.EqualOrAbove10AndLessThan25 => "Days between 10mm and 25mm of rain",
            SeriesTransformations.EqualOrAbove25 => "Days of 25mm of rain or more",
            _ => UnitOfMeasureLabel(unitOfMeasure),
        };
    }

    static string UnitOfMeasureLabel(UnitOfMeasure unitOfMeasure)
    {
        switch (unitOfMeasure)
        {
            case UnitOfMeasure.DegreesCelsius:
                return "Degrees Celsius (°C)";
            case UnitOfMeasure.DegreesCelsiusAnomaly:
                return "Degrees Celsius (°C) - Anomaly";
            case UnitOfMeasure.Millimetres:
                return "Millimetres (mm)";
            case UnitOfMeasure.PartsPerMillion:
                return "Parts per million (ppm)";
            case UnitOfMeasure.PartsPerBillion:
                return "Parts per billion (ppb)";
            case UnitOfMeasure.EnsoIndex:
                return "ENSO index";
            case UnitOfMeasure.MegajoulesPerSquareMetre:
                return "Megajoules per square metre (MJ/m²)";
        }
        throw new NotImplementedException();
    }

    public static string UnitOfMeasureLabelShort(UnitOfMeasure unitOfMeasure)
    {
        switch (unitOfMeasure)
        {
            case UnitOfMeasure.DegreesCelsius:
                return "°C";
            case UnitOfMeasure.DegreesCelsiusAnomaly:
                return "°C Anomaly";
            case UnitOfMeasure.Millimetres:
                return "mm";
            case UnitOfMeasure.PartsPerMillion:
                return "ppm";
            case UnitOfMeasure.PartsPerBillion:
                return "ppb";
            case UnitOfMeasure.EnsoIndex:
                return "ENSO index";
            case UnitOfMeasure.MegajoulesPerSquareMetre:
                return "MJ/m²";
        }
        throw new NotImplementedException();
    }

    public enum AggregationMethod
    {
        GroupByDayThenAverage,
        GroupByDayThenAverage_Anomaly,
        BinThenCount,
        Sum
    }

    public enum RowDataType
    {
        OneValuePerRow,
        TwelveMonthsPerRow
    }
}
