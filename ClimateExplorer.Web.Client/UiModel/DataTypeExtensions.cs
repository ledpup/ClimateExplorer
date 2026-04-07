namespace ClimateExplorer.Web.UiModel;

using static ClimateExplorer.Core.Enums;

public static class DataTypeExtensions
{
    public static string ToFriendlyName(this DataType dataType)
    {
        return dataType switch
        {
            DataType.TempMin => "Minimum temperature",
            DataType.TempMax => "Maximum temperature",
            DataType.TempMean => "Mean temperature",
            DataType.SolarRadiation => "Solar radiation",
            DataType.Precipitation => "Precipitation",
            DataType.Nino34 => "Nino 3.4",
            DataType.CO2 => "Carbon dioxide (CO\u2082)",
            DataType.CH4 => "Methane (CH\u2084)",
            DataType.N2O => "Nitrous oxide (N\u2082O)",
            DataType.IOD => "Indian Ocean Dipole (IOD)",
            DataType.Amo => "Atlantic Multidecadal Oscillation (AMO)",
            DataType.SeaIceExtent => "Sea ice extent",
            DataType.IceMeltArea => "Ice melt area",
            DataType.SunspotNumber => "Sunspot number",
            DataType.CO2Emissions => "Reported CO\u2082 emissions",
            DataType.ApparentTransmission => "Apparent atmospheric transmission",
            DataType.OzoneHoleArea => "Ozone Hole area",
            DataType.OzoneHoleColumn => "Ozone Hole column",
            DataType.Ozone => "Ozone",
            DataType.SeaLevel => "Sea level",
            DataType.OceanAcidity => "Ocean acidity",
            _ => throw new NotImplementedException(),
        };
    }
}
