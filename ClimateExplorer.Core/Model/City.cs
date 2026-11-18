namespace ClimateExplorer.Core.Model;

/// <summary>A city and its population (from GeoNames), used to find cities that have no location.</summary>
/// <param name="Name">The city's name, in ASCII.</param>
/// <param name="CountryCode">ISO 3166-1 alpha-2.</param>
/// <param name="Coordinates">The city centre.</param>
/// <param name="Population">The population GeoNames gives.</param>
public sealed record City(string Name, string CountryCode, Coordinates Coordinates, int Population);
