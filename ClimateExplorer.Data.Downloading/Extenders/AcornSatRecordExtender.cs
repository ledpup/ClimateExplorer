namespace ClimateExplorer.Data.Downloading.Extenders;

using System.Security.Cryptography;
using System.Text;
using ClimateExplorer.Core.Model;

/// <summary>
/// A pure, deterministic algorithm that decides whether an adjusted ACORN-SAT daily series may be extended
/// with unadjusted CDO observations, and if so, produces the overlay records to append. Has no HTTP,
/// filesystem, endpoint, or cache concerns: every input is supplied by the caller.
/// </summary>
public static class AcornSatRecordExtender
{
    /// <summary>
    /// The retired <c>ClimateExplorer.Data.Bom.AcornSatUpdateFromRaw</c> executable's comparison tolerance:
    /// round the absolute difference to one decimal place and reject only if it then exceeds 0.1 degrees.
    /// </summary>
    private const double ToleranceDegrees = 0.1;

    public static AcornSatRecordExtensionResult Extend(
        IReadOnlyList<DataRecord>? acornSatRecords,
        IReadOnlyList<DataRecord>? cdoRecords,
        string? adjustedStationId,
        string? openCdoStationId,
        DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(adjustedStationId) || string.IsNullOrWhiteSpace(openCdoStationId))
        {
            return new AcornSatRecordExtensionResult
            {
                Decision = AcornSatExtensionDecision.NoOpenCdoStation,
                AdjustedStationId = adjustedStationId,
                OpenCdoStationId = openCdoStationId,
            };
        }

        if (acornSatRecords == null || acornSatRecords.Count == 0)
        {
            return new AcornSatRecordExtensionResult
            {
                Decision = AcornSatExtensionDecision.SourceUnavailable,
                AdjustedStationId = adjustedStationId,
                OpenCdoStationId = openCdoStationId,
            };
        }

        var latestAcornSatDate = acornSatRecords
            .Select(x => x.Date)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .DefaultIfEmpty()
            .Max();

        if (latestAcornSatDate == default)
        {
            return new AcornSatRecordExtensionResult
            {
                Decision = AcornSatExtensionDecision.SourceUnavailable,
                AdjustedStationId = adjustedStationId,
                OpenCdoStationId = openCdoStationId,
            };
        }

        if (cdoRecords == null)
        {
            return new AcornSatRecordExtensionResult
            {
                Decision = AcornSatExtensionDecision.SourceUnavailable,
                LatestAcornSatDate = latestAcornSatDate,
                AdjustedStationId = adjustedStationId,
                OpenCdoStationId = openCdoStationId,
            };
        }

        if (acornSatRecords.Any(x => x.Date is { } date && date.Year == today.Year))
        {
            return new AcornSatRecordExtensionResult
            {
                Decision = AcornSatExtensionDecision.AcornNotThroughPreviousYear,
                LatestAcornSatDate = latestAcornSatDate,
                AdjustedStationId = adjustedStationId,
                OpenCdoStationId = openCdoStationId,
            };
        }

        // The comparison year C is the latest year strictly before today's year for which ACORN-SAT reaches
        // 31 December. This is not necessarily today's year minus one: a data type can be stale by more than
        // one year, in which case the eventual overlay spans every year from C + 1 through today.
        var comparisonYear = acornSatRecords
            .Where(x => x.Date is { Month: 12, Day: 31 } date && date.Year < today.Year)
            .Select(x => x.Date!.Value.Year)
            .DefaultIfEmpty(-1)
            .Max();

        if (comparisonYear == -1)
        {
            return new AcornSatRecordExtensionResult
            {
                Decision = AcornSatExtensionDecision.AcornNotThroughPreviousYear,
                LatestAcornSatDate = latestAcornSatDate,
                AdjustedStationId = adjustedStationId,
                OpenCdoStationId = openCdoStationId,
            };
        }

        var acornComparisonYearRecords = acornSatRecords
            .Where(x => x.Date is { } date && date.Year == comparisonYear)
            .ToList();
        var comparisonSignature = ComputeSignature(latestAcornSatDate, acornComparisonYearRecords);

        var acornNonNullDates = acornComparisonYearRecords
            .Where(x => x.Value.HasValue)
            .Select(x => x.Date!.Value)
            .ToHashSet();
        var cdoComparisonYearRecords = cdoRecords
            .Where(x => x.Date is { } date && date.Year == comparisonYear && x.Value.HasValue)
            .ToDictionary(x => x.Date!.Value, x => x.Value!.Value);
        var cdoNonNullDates = cdoComparisonYearRecords.Keys.ToHashSet();

        if (acornNonNullDates.Count == 0 ||
            cdoNonNullDates.Count == 0 ||
            !acornNonNullDates.SetEquals(cdoNonNullDates))
        {
            return new AcornSatRecordExtensionResult
            {
                Decision = AcornSatExtensionDecision.InsufficientComparisonData,
                ComparisonYear = comparisonYear,
                LatestAcornSatDate = latestAcornSatDate,
                ComparisonSignature = comparisonSignature,
                AdjustedStationId = adjustedStationId,
                OpenCdoStationId = openCdoStationId,
            };
        }

        var acornValuesByDate = acornComparisonYearRecords
            .Where(x => x.Value.HasValue)
            .ToDictionary(x => x.Date!.Value, x => x.Value!.Value);

        var adjustmentDetected = acornNonNullDates.Any(date =>
            Math.Round(Math.Abs(acornValuesByDate[date] - cdoComparisonYearRecords[date]), 1) > ToleranceDegrees);

        if (adjustmentDetected)
        {
            return new AcornSatRecordExtensionResult
            {
                Decision = AcornSatExtensionDecision.AdjustmentsDetected,
                ComparisonYear = comparisonYear,
                LatestAcornSatDate = latestAcornSatDate,
                ComparisonSignature = comparisonSignature,
                AdjustedStationId = adjustedStationId,
                OpenCdoStationId = openCdoStationId,
            };
        }

        var overlayRecords = cdoRecords
            .Where(x => x.Date is { } date && date > latestAcornSatDate && date <= today && x.Value.HasValue)
            .OrderBy(x => x.Date)
            .ToList();

        return new AcornSatRecordExtensionResult
        {
            Decision = AcornSatExtensionDecision.Eligible,
            ComparisonYear = comparisonYear,
            LatestAcornSatDate = latestAcornSatDate,
            ComparisonSignature = comparisonSignature,
            AdjustedStationId = adjustedStationId,
            OpenCdoStationId = openCdoStationId,
            OverlayRecords = overlayRecords,
        };
    }

    private static string ComputeSignature(DateOnly latestAcornSatDate, IReadOnlyList<DataRecord> comparisonYearRecords)
    {
        var builder = new StringBuilder();
        builder.Append(latestAcornSatDate.ToString("O"));

        foreach (var record in comparisonYearRecords.OrderBy(x => x.Date))
        {
            builder.Append('|').Append(record.Date!.Value.ToString("O")).Append(':')
                .Append(record.Value?.ToString("R") ?? "null");
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash);
    }
}
