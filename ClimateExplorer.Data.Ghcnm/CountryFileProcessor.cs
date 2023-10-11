using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ClimateExplorer.Data.Ghcnm;

public class CountryFileProcessor
{
    public static async Task<Dictionary<string, Country>> Transform()
    {
        var countryFile = await File.ReadAllLinesAsync(@"SiteMetaData\ghcnm-countries.txt");

        var countries = new Dictionary<string, Country>();

        var regEx = new Regex("^(?<id>\\w{2})\\s(?<name>.*)$");

        foreach (var country in countryFile)
        {
            if (!regEx.IsMatch(country))
            {
                throw new Exception($"RegEx does not match {country}");
            }

            var groups = regEx.Match(country).Groups;

            if (countries.ContainsKey(groups["id"].Value))
            {
                continue;
            }

            countries.Add(groups["id"].Value,
                new Country
                { 
                    Code = groups["id"].Value,
                    Name = groups["name"].Value.Trim(),
                });
        }

        return countries;
    }
}

public class Country
{
    public string Name { get; set; }
    public string Code { get; set; }

    public override string ToString()
    {
        return Name;
    }
}
