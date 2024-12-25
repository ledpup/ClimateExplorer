namespace ClimateExplorer.Core;

using ClimateExplorer.Core.DataPreparation;

public static class Enums
{
    public enum DataType
    {
        TempMax,
        TempMin,
        TempMean,
        Precipitation,
        SolarRadiation,

        Nino34,
        IOD,
        Amo,
        OceanAcidity,

        CO2,
        CH4,
        N2O,

        SeaIceExtent,
        IceMeltArea,

        SunspotNumber,

        CO2Emissions,

        ApparentTransmission,

        OzoneHoleArea,
        OzoneHoleColumn,
        Ozone,

        SeaLevel,
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
        Difference,
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
        MegaTonnes,
        AtmosphericTransmission,
        DobsonUnits,
        Odgi,
        Ph,
    }

    public enum SeriesAggregationOptions
    {
        Mean,
        Maximum,
        Minimum,
        Sum,
        Median,
    }

    public enum SeriesValueOptions
    {
        Value,
        Anomaly,
    }

    public enum AggregationMethod
    {
        GroupByDayThenAverage,
        GroupByDayThenAverage_Anomaly,
        BinThenCount,
        Sum,
    }

    public enum RowDataType
    {
        OneValuePerRow,
        TwelveMonthsPerRow,
    }

    public static string UnitOfMeasureLabel(SeriesTransformations seriesTransformations, UnitOfMeasure unitOfMeasure, SeriesAggregationOptions seriesAggregationOptions, SeriesValueOptions seriesValueOptions)
    {
        var label = seriesTransformations switch
        {
            SeriesTransformations.IsFrosty => "Days of frost",
            SeriesTransformations.DayOfYearIfFrost => seriesAggregationOptions == SeriesAggregationOptions.Maximum ? "Last day of frost" : "First day of frost",
            SeriesTransformations.EqualOrAbove25 => "Days of 25°C or above",
            SeriesTransformations.EqualOrAbove35 => "Days of 35°C or above",
            SeriesTransformations.EqualOrAbove1 => "Days of 1mm of rain or more",
            SeriesTransformations.EqualOrAbove1AndLessThan10 => "Days between 1mm and 10mm of rain",
            SeriesTransformations.EqualOrAbove10 => "Days of 10mm of rain or more",
            SeriesTransformations.EqualOrAbove10AndLessThan25 => "Days between 10mm and 25mm of rain",
            SeriesTransformations.EqualOrAbove25mm => "Days of 25mm of rain or more",
            _ => UnitOfMeasureLabel(unitOfMeasure),
        };

        if (seriesValueOptions == SeriesValueOptions.Anomaly)
        {
            label += " - Anomaly";
        }

        return label;
    }

    public static string UnitOfMeasureLabelShort(UnitOfMeasure unitOfMeasure)
    {
        return unitOfMeasure switch
        {
            UnitOfMeasure.DegreesCelsius => "°C",
            UnitOfMeasure.DegreesCelsiusAnomaly => "°C anomaly",
            UnitOfMeasure.Millimetres => "mm",
            UnitOfMeasure.PartsPerMillion => "ppm",
            UnitOfMeasure.PartsPerBillion => "ppb",
            UnitOfMeasure.EnsoIndex => "ENSO index",
            UnitOfMeasure.MegajoulesPerSquareMetre => "MJ/m²",
            UnitOfMeasure.MillionSqKm => "million km²",
            UnitOfMeasure.SqKm => "km²",
            UnitOfMeasure.Sn => "Sn",
            UnitOfMeasure.WattsPerSquareMetre => "W/m²",
            UnitOfMeasure.MegaTonnes => "Mt",
            UnitOfMeasure.AtmosphericTransmission => "Transmission",
            UnitOfMeasure.DobsonUnits => "DU",
            UnitOfMeasure.Odgi => "ODGI",
            UnitOfMeasure.Ph => "pH",
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

    private static string UnitOfMeasureLabel(UnitOfMeasure unitOfMeasure)
    {
        return unitOfMeasure switch
        {
            UnitOfMeasure.DegreesCelsius => "Degrees celsius (°C)",
            UnitOfMeasure.DegreesCelsiusAnomaly => "Degrees celsius (°C) - anomaly",
            UnitOfMeasure.Millimetres => "Millimetres (mm)",
            UnitOfMeasure.PartsPerMillion => "Parts per million (ppm)",
            UnitOfMeasure.PartsPerBillion => "Parts per billion (ppb)",
            UnitOfMeasure.EnsoIndex => "ENSO index",
            UnitOfMeasure.MegajoulesPerSquareMetre => "Megajoules per square metre (MJ/m²)",
            UnitOfMeasure.MillionSqKm => "Million square kilometres",
            UnitOfMeasure.SqKm => "Square kilometres (km²)",
            UnitOfMeasure.Sn => "Sunsport number (Sn)",
            UnitOfMeasure.WattsPerSquareMetre => "Watts per square metre (W/m²)",
            UnitOfMeasure.MegaTonnes => "Megatonnes",
            UnitOfMeasure.AtmosphericTransmission => "Atmospheric transmission",
            UnitOfMeasure.DobsonUnits => "Dobson Units",
            UnitOfMeasure.Odgi => "Ozone Depleting Gas Index",
            UnitOfMeasure.Ph => "pH",
            _ => throw new NotImplementedException(),
        };
    }
}
