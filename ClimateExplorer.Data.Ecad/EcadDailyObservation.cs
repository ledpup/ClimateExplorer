namespace ClimateExplorer.Data.Ecad;

using static ClimateExplorer.Core.Enums;

/// <summary>
/// One day of a station's published series. All four measurements share a row - and a file - because
/// they are all read from the same source asset, so a day with only some measurements present carries
/// empty fields for the rest.
/// </summary>
public sealed class EcadDailyObservation(DateOnly date)
{
    public DateOnly Date { get; } = date;

    public double? TempMean { get; set; }

    public double? TempMax { get; set; }

    public double? TempMin { get; set; }

    public double? Precipitation { get; set; }

    public bool HasAnyValue => TempMean.HasValue || TempMax.HasValue || TempMin.HasValue || Precipitation.HasValue;

    public double? this[DataType dataType]
    {
        get => dataType switch
        {
            DataType.TempMean => TempMean,
            DataType.TempMax => TempMax,
            DataType.TempMin => TempMin,
            DataType.Precipitation => Precipitation,
            _ => throw new NotSupportedException($"ECA&D does not publish {dataType}."),
        };

        set
        {
            switch (dataType)
            {
                case DataType.TempMean:
                    TempMean = value;
                    break;
                case DataType.TempMax:
                    TempMax = value;
                    break;
                case DataType.TempMin:
                    TempMin = value;
                    break;
                case DataType.Precipitation:
                    Precipitation = value;
                    break;
                default:
                    throw new NotSupportedException($"ECA&D does not publish {dataType}.");
            }
        }
    }
}
