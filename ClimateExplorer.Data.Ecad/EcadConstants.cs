namespace ClimateExplorer.Data.Ecad;

using static ClimateExplorer.Core.Enums;

public static class EcadConstants
{
    public const string BaseUrl = "https://api.meteogate.eu/eu-eumetnet-climate-observations/v1/";

    /// <summary>
    /// The non-blended edition: "the series as provided by the participants", which maps onto this
    /// codebase's <see cref="DataAdjustment.Unadjusted"/>. The blended (homogenised) edition, which would
    /// map onto <see cref="DataAdjustment.Adjusted"/>, is not published as a collection yet.
    /// </summary>
    public const string CollectionId = "ecad-nonblended";

    /// <summary>
    /// The API rejects any query whose <c>timePoints * parameterCount * stationCount</c> exceeds this,
    /// with an HTTP 413 that spells out the arithmetic. Callers must window their requests accordingly;
    /// see <see cref="EcadQueryWindowCalculator"/>.
    /// </summary>
    public const int MaximumDataPointsPerQuery = 300_000;

    /// <summary>
    /// Each requested parameter comes back with an ancillary <c>{code}_q</c> quality flag parameter, and
    /// the server counts both against <see cref="MaximumDataPointsPerQuery"/> - a 4-parameter request is
    /// billed as 8. Windowing that ignored this would be rejected at exactly double the allowed range.
    /// </summary>
    public const int ResponseParametersPerRequestedParameter = 2;

    /// <summary>
    /// ECA&amp;D's quality flag: 0 is valid, and anything else is suspect or missing. Non-zero values are
    /// discarded rather than published, matching how GHCNd's own quality flags are handled here.
    /// </summary>
    public const int ValidQualityFlag = 0;

    /// <summary>
    /// The parameter-code prefix carrying each measurement. ECA&amp;D fans every measurement out into
    /// numbered variants (<c>tg1</c>...<c>tg24</c> and so on) that encode each contributing country's own
    /// daily accumulation-period convention, so the prefix identifies the family and the number identifies
    /// the convention. A raw station reports exactly one variant per family.
    /// </summary>
    public static readonly IReadOnlyDictionary<DataType, string> ParameterPrefixByDataType =
        new Dictionary<DataType, string>
        {
            [DataType.TempMean] = "tg",
            [DataType.TempMax] = "tx",
            [DataType.TempMin] = "tn",
            [DataType.Precipitation] = "rr",
        };

    /// <summary>
    /// The measurements ClimateExplorer publishes from ECA&amp;D, in the column order of the CSV that
    /// <see cref="EcadCsvFormat"/> reads and writes.
    /// </summary>
    public static readonly IReadOnlyList<DataType> PublishedDataTypes =
        [DataType.TempMean, DataType.TempMax, DataType.TempMin, DataType.Precipitation];

    public static string GetParameterPrefix(DataType dataType)
    {
        return ParameterPrefixByDataType.TryGetValue(dataType, out var prefix)
            ? prefix
            : throw new NotSupportedException($"No ECA&D parameter family is configured for {dataType}.");
    }

    /// <summary>
    /// Tests whether a parameter code belongs to a family - <c>tg6</c> is in the <c>tg</c> family, while
    /// <c>tn6</c> is not. Matching on prefix alone is not enough: the catalogue contains 230 codes and
    /// several families share leading letters with other, unrelated ones.
    /// </summary>
    public static bool IsInFamily(string parameterCode, string familyPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(familyPrefix);

        return parameterCode.Length > familyPrefix.Length &&
            parameterCode.StartsWith(familyPrefix, StringComparison.Ordinal) &&
            parameterCode.AsSpan(familyPrefix.Length).ToString().All(char.IsAsciiDigit);
    }
}
