namespace ClimateExplorer.WebApi.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Ghcnm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Checks the committed country data: WebApi files use ISO 3166-1 codes, and GHCNm's FIPS codes map onto them.
/// </summary>
[TestClass]
public sealed class CountryCodeDataTests
{
    [TestMethod]
    public async Task GhcnmMappingFiles_EveryIsoCode_IsInWebApiCountryList()
    {
        var ghcnmMetaData = Path.Combine(GetSolutionRoot(), "ClimateExplorer.Data.Ghcnm");
        var mapper = await FipsToIsoMapper.Load(
            Path.Combine(ghcnmMetaData, FipsToIsoMapper.MappingFile),
            Path.Combine(ghcnmMetaData, FipsToIsoMapper.StationOverridesFile));
        var countries = await GetWebApiCountries();

        var missing = mapper.IsoCodes.Where(x => !countries.ContainsKey(x)).ToList();

        Assert.IsEmpty(missing, $"ISO codes not in countries.txt: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public async Task WebApiMetaData_EveryCountryCode_IsInCountryList()
    {
        var countries = await GetWebApiCountries();
        var codes = GetLocations().Select(x => (x.File, x.Location.CountryCode))
            .Concat(GetStations().Where(x => x.Station.CountryCode != null).Select(x => (x.File, x.Station.CountryCode!)));

        var missing = codes.Where(x => !countries.ContainsKey(x.Item2)).Distinct().ToList();

        Assert.IsEmpty(missing, $"Codes not in countries.txt: {string.Join(", ", missing.Select(x => $"{x.Item2} ({x.File})"))}");
    }

    // Most FIPS codes are also valid ISO codes for a different country, so a missed conversion isn't caught by
    // the country list. Pin a few locations whose FIPS code meant something else.
    [TestMethod]
    [DataRow("c0f2743b-fb00-4128-9024-bafa4e1c85ed", "AU", DisplayName = "Canberra (FIPS AS)")]
    [DataRow("0b6f825c-782e-4cd8-a91e-80b032fc2485", "AS", DisplayName = "Pago Pago (FIPS AQ)")]
    [DataRow("c6673b47-e158-4dac-95de-3ee4c31d284e", "AQ", DisplayName = "Mawson (FIPS AY)")]
    [DataRow("71a607b6-b1d0-4270-867e-ae4510ac7307", "CH", DisplayName = "Zuerich Fluntern (FIPS SZ)")]
    [DataRow("df31ecee-c375-437b-adac-ab672d3cace1", "CN", DisplayName = "Beijing (FIPS CH)")]
    [DataRow("f63801a6-b540-41b5-9566-6069ff1ea04e", "DE", DisplayName = "Berlin Dahlem (FIPS GM)")]
    [DataRow("a5a179b5-5394-44d1-b653-bbf6b052279c", "GB", DisplayName = "Central England (FIPS UK)")]
    [DataRow("332f2ee9-dad0-4759-b0c8-8801ce30d9d8", "SX", DisplayName = "Princess Juliana Intl (FIPS NT)")]
    [DataRow("ffcb17aa-a8b0-40e7-83c4-3ed0fba13920", "CW", DisplayName = "Hato (FIPS NT)")]
    public void WebApiMetaData_KnownLocation_HasIsoCountryCode(string locationId, string expectedCode)
    {
        var location = GetLocations().Single(x => x.Location.Id == Guid.Parse(locationId)).Location;

        Assert.AreEqual(expectedCode, location.CountryCode);
    }

    private static string WebApiMetaData => Path.Combine(GetSolutionRoot(), "ClimateExplorer.WebApi", "MetaData");

    private static Task<Dictionary<string, Country>> GetWebApiCountries() => Country.GetCountries(Path.Combine(WebApiMetaData, "countries.txt"));

    private static IEnumerable<(string File, Location Location)> GetLocations() =>
        Directory.GetFiles(Path.Combine(WebApiMetaData, "Location"), "*.json")
            .SelectMany(file => JsonSerializer.Deserialize<List<Location>>(File.ReadAllText(file))!.Select(x => (Path.GetFileName(file), x)));

    private static IEnumerable<(string File, Station Station)> GetStations() =>
        Directory.GetFiles(Path.Combine(WebApiMetaData, "Station"), "*.json")
            .SelectMany(file => JsonSerializer.Deserialize<List<Station>>(File.ReadAllText(file))!.Select(x => (Path.GetFileName(file), x)));

    private static string GetSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ClimateExplorer.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the solution root.");
    }
}
