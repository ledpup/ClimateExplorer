namespace ClimateExplorer.Core;

using ClimateExplorer.Core.Calculators;
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

        CO2Deseasoned,

        GlacierMassBalance,
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
        MetresWaterEquivalent,
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

    public static string UnitOfMeasureLabel(SeriesTransformations seriesTransformations, string? customTransformation, UnitOfMeasure unitOfMeasure, SeriesAggregationOptions seriesAggregationOptions, SeriesValueOptions seriesValueOptions, MeteorologicalHemisphere? hemisphere = null)
    {
        var label = seriesTransformations switch
        {
            SeriesTransformations.DayOfYearIfFrost => GetDayOfYearIfFrostLabel(seriesAggregationOptions, hemisphere),
            SeriesTransformations.Custom => seriesAggregationOptions == SeriesAggregationOptions.Sum ? $"Count of {customTransformation}" : customTransformation!,
            _ => UnitOfMeasureLabel(unitOfMeasure),
        };

        if (seriesValueOptions == SeriesValueOptions.Anomaly)
        {
            label += " - Anomaly";
        }

        return label;
    }

    /// <summary>
    /// Labels a DayOfYearIfFrost series' Min/Max aggregation as "first"/"last day of frost". In the
    /// Southern Hemisphere the frost season sits mid-calendar-year, so within one year-bin the
    /// smallest day-of-year value is that season's first frost and the largest is its last. In the
    /// Northern Hemisphere the season straddles the year boundary instead, so the smallest value is
    /// actually the tail (last, spring) frost of the winter that started the previous year, and the
    /// largest is the first (autumn) frost of the winter about to start - the reverse mapping.
    /// </summary>
    /// <param name="aggregation">Whether the series aggregates by Minimum or Maximum day-of-year.</param>
    /// <param name="hemisphere">The source location's meteorological hemisphere, or null if unknown (treated as Southern).</param>
    /// <returns>"First day of frost" or "Last day of frost".</returns>
    public static string GetDayOfYearIfFrostLabel(SeriesAggregationOptions aggregation, MeteorologicalHemisphere? hemisphere)
    {
        var isMaximum = aggregation == SeriesAggregationOptions.Maximum;
        var isLast = hemisphere == MeteorologicalHemisphere.Northern ? !isMaximum : isMaximum;

        return isLast ? "Last day of frost" : "First day of frost";
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
            UnitOfMeasure.MetresWaterEquivalent => "m w.e.",
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
            UnitOfMeasure.MegaTonnes => 0,
            UnitOfMeasure.MillionSqKm => 1,
            UnitOfMeasure.SqKm => 0,
            UnitOfMeasure.MetresWaterEquivalent => 1,
            _ => 1,
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
            UnitOfMeasure.Sn => "Sunspot number (Sn)",
            UnitOfMeasure.WattsPerSquareMetre => "Watts per square metre (W/m²)",
            UnitOfMeasure.MegaTonnes => "Megatonnes",
            UnitOfMeasure.AtmosphericTransmission => "Atmospheric transmission",
            UnitOfMeasure.DobsonUnits => "Dobson Units",
            UnitOfMeasure.Odgi => "Ozone Depleting Gas Index",
            UnitOfMeasure.Ph => "pH",
            UnitOfMeasure.MetresWaterEquivalent => "Metres water equivalent (m w.e.)",
            _ => throw new NotImplementedException(),
        };
    }
}
