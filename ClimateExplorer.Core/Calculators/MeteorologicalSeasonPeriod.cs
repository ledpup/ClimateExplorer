namespace ClimateExplorer.Core.Calculators;

public sealed record MeteorologicalSeasonPeriod(
    MeteorologicalSeason Season,
    MeteorologicalHemisphere Hemisphere,
    DateOnly StartDate,
    DateOnly EndDate)
{
    public bool SpansCalendarYears => StartDate.Year != EndDate.Year;
}
