using System.Text.RegularExpressions;

namespace ClimateExplorer.Core.Model;

public class Country
{
    public required string Name { get; set; }
    public required string Code { get; set; }

    public override string ToString()
    {
        return Name;
    }

    public static async Task<Dictionary<string, Country>> GetCountries(string location)
    {
        var countryFile = await File.ReadAllLinesAsync(location);

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
