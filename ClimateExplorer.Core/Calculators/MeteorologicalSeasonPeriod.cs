namespace ClimateExplorer.Core.Calculators;

public sealed record MeteorologicalSeasonPeriod(
    MeteorologicalSeason Season,
    MeteorologicalHemisphere Hemisphere,
    DateOnly StartDate,
    DateOnly EndDate)
{
    public bool SpansCalendarYears => StartDate.Year != EndDate.Year;

    /// <summary>
    /// True when EndDate is the season's actual last day (as opposed to a "to date" period
    /// whose EndDate has been overridden to some earlier reference date).
    /// </summary>
    public bool IsComplete => EndDate == StartDate.AddMonths(3).AddDays(-1);
}
