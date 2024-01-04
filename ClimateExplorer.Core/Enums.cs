using ClimateExplorer.Core.DataPreparation;

namespace ClimateExplorer.Core;

public static class Enums
{
    public enum DataType
    {
        TempMax,
        TempMin,
        TempMean,
        Precipitation,
        SolarRadiation,

        MEIv2,
        SOI,
        Nino34,
        ONI,

        CO2,
        CH4,
        N2O,
        IOD,

        SeaIceExtent,
        IceMeltArea,

        SunspotNumber,
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
        MegajoulesPerSquareMetre,
        MillionSqKm,
        SqKm,
        Sn,
        WattsPerSquareMetre,
    }

    public enum SeriesAggregationOptions
    {
        Mean,
        Maximum,
        Minimum,
        Sum,
        Median
    }

    public enum SeriesValueOptions
    {
        Value,
        Anomaly
    }

    public enum ChartStartYears
    {
        FirstYear,
        LastYear
    }

    public static string UnitOfMeasureLabel(SeriesTransformations seriesTransformations, UnitOfMeasure unitOfMeasure, SeriesAggregationOptions seriesAggregationOptions, SeriesValueOptions seriesValueOptions)
    {
        var label = seriesTransformations switch
        {
            SeriesTransformations.IsFrosty => "Days of frost",
            SeriesTransformations.DayOfYearIfFrost => seriesAggregationOptions == SeriesAggregationOptions.Maximum ? "Last day of frost" : "First day of frost",
            SeriesTransformations.EqualOrAbove35 => "Days of 35°C or above",
            SeriesTransformations.EqualOrAbove1 => "Days of 1mm of rain or more",
            SeriesTransformations.EqualOrAbove1AndLessThan10 => "Days between 1mm and 10mm of rain",
            SeriesTransformations.EqualOrAbove10 => "Days of 10mm of rain or more",
            SeriesTransformations.EqualOrAbove10AndLessThan25 => "Days between 10mm and 25mm of rain",
            SeriesTransformations.EqualOrAbove25 => "Days of 25mm of rain or more",
            _ => UnitOfMeasureLabel(unitOfMeasure),
        };

        if (seriesValueOptions == SeriesValueOptions.Anomaly)
        {
            label += " - Anomaly";
        }

        return label;
    }

    static string UnitOfMeasureLabel(UnitOfMeasure unitOfMeasure)
    {
        return unitOfMeasure switch
        {
            UnitOfMeasure.DegreesCelsius => "Degrees Celsius (°C)",
            UnitOfMeasure.DegreesCelsiusAnomaly => "Degrees Celsius (°C) - Anomaly",
            UnitOfMeasure.Millimetres => "Millimetres (mm)",
            UnitOfMeasure.PartsPerMillion => "Parts per million (ppm)",
            UnitOfMeasure.PartsPerBillion => "Parts per billion (ppb)",
            UnitOfMeasure.EnsoIndex => "ENSO index",
            UnitOfMeasure.MegajoulesPerSquareMetre => "Megajoules per square metre (MJ/m²)",
            UnitOfMeasure.MillionSqKm => "Million square kilometres",
            UnitOfMeasure.SqKm => "Square kilometres (km²)",
            UnitOfMeasure.Sn => "Sunsport number (Sn)",
            UnitOfMeasure.WattsPerSquareMetre => "Watts per square metre (W/m²)",
            _ => throw new NotImplementedException(),
        };
    }

    public static string UnitOfMeasureLabelShort(UnitOfMeasure unitOfMeasure)
    {
        return unitOfMeasure switch
        {
            UnitOfMeasure.DegreesCelsius => "°C",
            UnitOfMeasure.DegreesCelsiusAnomaly => "°C Anomaly",
            UnitOfMeasure.Millimetres => "mm",
            UnitOfMeasure.PartsPerMillion => "ppm",
            UnitOfMeasure.PartsPerBillion => "ppb",
            UnitOfMeasure.EnsoIndex => "ENSO index",
            UnitOfMeasure.MegajoulesPerSquareMetre => "MJ/m²",
            UnitOfMeasure.MillionSqKm => "million km²",
            UnitOfMeasure.SqKm => "km²",
            UnitOfMeasure.Sn => "Sn",
            UnitOfMeasure.WattsPerSquareMetre => "W/m²",
            _ => throw new NotImplementedException(),
        };
    }

    public static int UnitOfMeasureRounding(UnitOfMeasure unitOfMeasure)
    {
        return unitOfMeasure switch
        {
            UnitOfMeasure.DegreesCelsius => 1,
            UnitOfMeasure.DegreesCelsiusAnomaly => 1,
            UnitOfMeasure.Millimetres => 0,
            _ => throw new NotImplementedException(),
        };
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
