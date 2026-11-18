namespace ClimateExplorer.Core;

using ClimateExplorer.Core.Model;
using GeoCoordinatePortable;

/// <summary>
/// Gives cities that have no location a station of their own. Cities are taken largest first. A city is covered
/// when a kept station in its country is near its centre; otherwise it gets the first candidate (in priority
/// order) near its centre that isn't close to a kept station and isn't nearer the centre of a bigger city. A
/// station picked for one city is kept, so it covers the smaller cities around it.
/// Distances are measured from the city centre, not from the candidate: a station on the near side of a town can
/// be closer to the next town's station than the town itself is.
/// </summary>
public static class CityStationSelector
{
    // A degree of latitude is at least 110.5 km, so stations further apart in latitude than this can't be within range.
    private const double MinimumKmPerDegreeOfLatitude = 110.5;

    /// <param name="cities">The cities to cover, in any order.</param>
    /// <param name="seeds">Stations already kept. They cover cities and block candidates near them, but aren't returned.</param>
    /// <param name="candidatesInPriorityOrder">Candidates, best first.</param>
    /// <param name="countryCode">A station's country (ISO 3166-1 alpha-2, as <see cref="City.CountryCode"/>). Only stations in a city's country cover it or are picked for it.</param>
    /// <param name="coordinates">Where the station is.</param>
    /// <param name="coveredRadiusKm">A city with a kept station within this distance of its centre is covered.</param>
    /// <param name="candidateRadiusKm">How far from the centre a candidate can be.</param>
    /// <param name="minimumDistanceToKeptKm">A candidate this close to a kept station isn't picked.</param>
    /// <returns>The picked stations with their cities, largest city first.</returns>
    public static List<CityStation<T>> Select<T>(
        IEnumerable<City> cities,
        IEnumerable<T> seeds,
        IEnumerable<T> candidatesInPriorityOrder,
        Func<T, string> countryCode,
        Func<T, Coordinates> coordinates,
        double coveredRadiusKm,
        double candidateRadiusKm,
        double minimumDistanceToKeptKm)
    {
        var citiesLargestFirst = cities
            .OrderByDescending(x => x.Population)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ThenBy(x => x.CountryCode, StringComparer.Ordinal)
            .ToList();

        var citiesByCountry = citiesLargestFirst
            .GroupBy(x => x.CountryCode)
            .ToDictionary(x => x.Key, x => x.Select(city => (City: city, Centre: ToGeoCoordinate(city.Coordinates))).ToList());

        var keptByCountry = seeds
            .GroupBy(countryCode)
            .ToDictionary(x => x.Key, x => x.Select(seed => ToGeoCoordinate(coordinates(seed))).ToList());

        var candidatesByCountry = candidatesInPriorityOrder
            .GroupBy(countryCode)
            .ToDictionary(x => x.Key, x => x.Select(candidate => new Candidate<T>(candidate, ToGeoCoordinate(coordinates(candidate)))).ToList());

        var selected = new List<CityStation<T>>();
        foreach (var city in citiesLargestFirst)
        {
            if (!keptByCountry.TryGetValue(city.CountryCode, out var kept))
            {
                kept = [];
                keptByCountry[city.CountryCode] = kept;
            }

            var centre = ToGeoCoordinate(city.Coordinates);
            if (kept.Any(x => IsWithin(x, centre, coveredRadiusKm)))
            {
                continue;
            }

            if (!candidatesByCountry.TryGetValue(city.CountryCode, out var candidates))
            {
                continue;
            }

            foreach (var candidate in candidates)
            {
                if (candidate.Picked || !IsWithin(centre, candidate.Position, candidateRadiusKm))
                {
                    continue;
                }

                var distanceKm = centre.GetDistanceTo(candidate.Position) / 1000;
                if (kept.Any(x => IsWithin(x, candidate.Position, minimumDistanceToKeptKm))
                    || IsNearerABiggerCity(citiesByCountry[city.CountryCode], city, candidate.Position, distanceKm))
                {
                    continue;
                }

                candidate.Picked = true;
                kept.Add(candidate.Position);
                selected.Add(new CityStation<T>(city, candidate.Station, distanceKm));
                break;
            }
        }

        return selected;
    }

    // Stops a station in one city being named after a smaller one nearby (Fort Lauderdale's airport for Boca Raton).
    private static bool IsNearerABiggerCity(List<(City City, GeoCoordinate Centre)> citiesInCountry, City city, GeoCoordinate position, double distanceKm)
    {
        foreach (var other in citiesInCountry)
        {
            // Largest first, so the rest are no bigger.
            if (other.City.Population <= city.Population)
            {
                return false;
            }

            if (IsWithin(other.Centre, position, distanceKm) && other.Centre.GetDistanceTo(position) / 1000 < distanceKm)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWithin(GeoCoordinate a, GeoCoordinate b, double km)
    {
        if (Math.Abs(a.Latitude - b.Latitude) * MinimumKmPerDegreeOfLatitude > km)
        {
            return false;
        }

        return a.GetDistanceTo(b) / 1000 <= km;
    }

    private static GeoCoordinate ToGeoCoordinate(Coordinates coordinates)
    {
        return new GeoCoordinate(coordinates.Latitude, coordinates.Longitude);
    }

    private sealed class Candidate<T>(T station, GeoCoordinate position)
    {
        public T Station { get; } = station;

        public GeoCoordinate Position { get; } = position;

        public bool Picked { get; set; }
    }
}

/// <summary>A station picked for a city by <see cref="CityStationSelector"/>.</summary>
/// <param name="City">The city.</param>
/// <param name="Station">The station.</param>
/// <param name="DistanceKm">From the city centre to the station.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "The selector's result type")]
public sealed record CityStation<T>(City City, T Station, double DistanceKm);
