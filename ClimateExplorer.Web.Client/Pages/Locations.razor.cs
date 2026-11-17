namespace ClimateExplorer.Web.Client.Pages;

using Microsoft.AspNetCore.Components;

/// <summary>
/// All locations in a <see cref="Components.Location.LocationsTable"/>, with its country and trend score filters
/// kept in the query string.
/// </summary>
public partial class Locations
{
    private const string CountryQueryParameter = "country";
    private const string TrendScoreQueryParameter = "trendScore";

    private string? country;
    private string? trendScore;

    [Inject]
    private NavigationManager? NavManager { get; set; }

    [SupplyParameterFromQuery(Name = CountryQueryParameter)]
    private string? CountryFromQuery { get; set; }

    [SupplyParameterFromQuery(Name = TrendScoreQueryParameter)]
    private string? TrendScoreFromQuery { get; set; }

    // The table ignores unknown values, rather than showing an empty list.
    protected override void OnInitialized()
    {
        country = CountryFromQuery;
        trendScore = TrendScoreFromQuery;
    }

    // Replaces the current history entry, so each dropdown change doesn't add a back-button stop.
    private void UpdateQueryString()
    {
        var uri = NavManager!.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            [CountryQueryParameter] = country,
            [TrendScoreQueryParameter] = trendScore,
        });

        NavManager!.NavigateTo(uri, false, true);
    }
}
