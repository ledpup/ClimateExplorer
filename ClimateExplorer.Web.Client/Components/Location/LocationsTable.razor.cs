namespace ClimateExplorer.Web.Client.Components.Location;

using System.Globalization;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Services.Trends;
using ClimateExplorer.Web.Client.UiModel.Trends;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiLogic;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The table of all locations, with its filters, sorting and paging, and the side panels its links open. The country
/// and trend score filters are bindable, so a page can keep them in its URL. With <see cref="ShowFilters"/> off, the
/// table header (filters and count) is hidden and the "Has ..." checkboxes start unticked, so only
/// <see cref="Country"/> and <see cref="TrendScore"/> filter the list.
/// </summary>
public partial class LocationsTable
{
    /// <summary>
    /// The <see cref="TrendScore"/> filter for locations without a trend score.
    /// </summary>
    public const string TrendScoreFilterNullScore = "null-score";

    /// <summary>
    /// The <see cref="TrendScore"/> filter for locations that have cooled (a negative trend score).
    /// </summary>
    public const string TrendScoreFilterNegative = "negative";

    private List<Location> allLocations = [];
    private IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions = [];
    private Dictionary<Guid, List<DataType>> locationDataTypes = new();
    private Dictionary<Guid, List<string>> locationDataSources = new();
    private Dictionary<Guid, LocationTrendField> locationTrends = new();
    private string? countryFilter;
    private string? trendScoreFilter;
    private bool requireAnomaly = true;
    private bool requireTrendScore = true;
    private bool requireTrend = true;
    private string sortColumn = "name";
    private bool sortAscending = true;
    private int currentPage = 1;
    private int pageSize = 25;
    private List<string> availableCountries = [];
    private int filteredCount;
    private int totalPages = 1;
    private SidePanel? climateRecordsSidePanel;
    private SidePanel? recentObservationsSidePanel;
    private LocationDataSetMetadataSidePanel? dataSetMetadataSidePanel;
    private Location? selectedLocation;
    private string? nameFilter;

    // The parameter values last applied, so a re-render of the parent doesn't reset a filter changed here
    private string? appliedCountry;
    private string? appliedTrendScore;

    /// <summary>
    /// Shows the table header: the filters and the location count. When it's hidden, the "Has ..." checkboxes start
    /// unticked.
    /// </summary>
    [Parameter]
    public bool ShowFilters { get; set; } = true;

    /// <summary>
    /// The country filter. A country with no locations is ignored.
    /// </summary>
    [Parameter]
    public string? Country { get; set; }

    [Parameter]
    public EventCallback<string?> CountryChanged { get; set; }

    /// <summary>
    /// The trend score filter: a score from 0 to 9, <see cref="TrendScoreFilterNegative"/> or
    /// <see cref="TrendScoreFilterNullScore"/> (see <see cref="GetTrendScoreFilter"/>). Other values are ignored.
    /// </summary>
    [Parameter]
    public string? TrendScore { get; set; }

    [Parameter]
    public EventCallback<string?> TrendScoreChanged { get; set; }

    /// <summary>
    /// Raised when a location's name link is clicked. The browser still follows the link.
    /// </summary>
    [Parameter]
    public EventCallback<Location> LocationLinkClicked { get; set; }

    [Inject]
    private IDataService? DataService { get; set; }

    [Inject]
    private ILogger<LocationsTable>? Logger { get; set; }

    // Formatting only: sorting stays on the metric values, and every conversion is increasing, so the
    // order is the same in every unit system.
    [CascadingParameter]
    private IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    private int StartRecord => filteredCount > 0 ? ((currentPage - 1) * pageSize) + 1 : 0;

    private int EndRecord => filteredCount > 0 ? Math.Min(StartRecord + pageSize - 1, filteredCount) : 0;

    private IEnumerable<Location> FilteredAndSortedLocations
    {
        get
        {
            var query = allLocations.AsEnumerable();

            if (!string.IsNullOrEmpty(countryFilter))
            {
                query = query.Where(l => (l.Country ?? l.CountryCode) == countryFilter);
            }

            if (!string.IsNullOrEmpty(trendScoreFilter))
            {
                query = trendScoreFilter switch
                {
                    TrendScoreFilterNullScore => query.Where(l => !l.TrendScore.HasValue),
                    TrendScoreFilterNegative => query.Where(l => l.TrendScore.HasValue && l.TrendScore.Value < 0),
                    _ when int.TryParse(trendScoreFilter, out var score) => query.Where(l => l.TrendScore == score),
                    _ => query,
                };
            }

            if (requireAnomaly)
            {
                query = query.Where(l => l.WarmingAnomaly.HasValue);
            }

            if (requireTrendScore)
            {
                query = query.Where(l => l.TrendScore.HasValue);
            }

            if (requireTrend)
            {
                query = query.Where(l => locationTrends.ContainsKey(l.Id));
            }

            if (!string.IsNullOrEmpty(nameFilter))
            {
                query = query.Where(l => l.FullTitle.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
            }

            return sortColumn switch
            {
                "country" => sortAscending
                    ? query.OrderBy(l => l.Country ?? l.CountryCode)
                    : query.OrderByDescending(l => l.Country ?? l.CountryCode),
                "anomaly" => sortAscending
                    ? query.OrderBy(l => l.WarmingAnomaly ?? double.MinValue)
                    : query.OrderByDescending(l => l.WarmingAnomaly ?? double.MinValue),
                "trendscore" => sortAscending
                    ? query.OrderBy(l => l.TrendScore ?? int.MinValue)
                    : query.OrderByDescending(l => l.TrendScore ?? int.MinValue),
                "trend" => sortAscending
                    ? query.OrderBy(l => GetTrendSlope(l.Id))
                    : query.OrderByDescending(l => GetTrendSlope(l.Id)),
                "recordhigh" => sortAscending
                    ? query.OrderBy(l => l.RecordHigh?.Value ?? double.MinValue)
                    : query.OrderByDescending(l => l.RecordHigh?.Value ?? double.MinValue),
                _ => sortAscending
                    ? query.OrderBy(l => l.Name)
                    : query.OrderByDescending(l => l.Name),
            };
        }
    }

    private IEnumerable<Location> PagedLocations =>
        FilteredAndSortedLocations.Skip((currentPage - 1) * pageSize).Take(pageSize);

    private Location? Location => selectedLocation;

    private IEnumerable<DataSetDefinitionViewModel> DataSetDefinitions => dataSetDefinitions;

    /// <summary>
    /// The <see cref="TrendScore"/> filter that lists the locations with this score: null for the locations with
    /// no score, and a negative score for the locations that have cooled.
    /// </summary>
    public static string GetTrendScoreFilter(int? score) => score switch
    {
        null => TrendScoreFilterNullScore,
        < 0 => TrendScoreFilterNegative,
        _ => score.Value.ToString(CultureInfo.InvariantCulture),
    };

    protected override async Task OnInitializedAsync()
    {
        var log = new LogAugmenter(Logger!, nameof(OnInitializedAsync));
        log.LogInformation("Starting");

        // Hidden checkboxes can't be unticked, so they don't filter
        if (!ShowFilters)
        {
            requireAnomaly = false;
            requireTrendScore = false;
            requireTrend = false;
        }

        allLocations = [.. (await DataService!.GetLocations())!];
        dataSetDefinitions = [.. await DataService!.GetDataSetDefinitions()];

        foreach (var dsd in dataSetDefinitions)
        {
            if (dsd.LocationIds == null)
            {
                continue;
            }

            foreach (var locationId in dsd.LocationIds)
            {
                if (!locationDataTypes.TryGetValue(locationId, out var types))
                {
                    types = [];
                    locationDataTypes[locationId] = types;
                }

                foreach (var md in dsd.MeasurementDefinitions ?? [])
                {
                    if (!types.Contains(md.DataType))
                    {
                        types.Add(md.DataType);
                    }
                }

                if (!locationDataSources.TryGetValue(locationId, out var sources))
                {
                    sources = [];
                    locationDataSources[locationId] = sources;
                }

                if (dsd.ShortName != null && !sources.Contains(dsd.ShortName))
                {
                    sources.Add(dsd.ShortName);
                }
            }
        }

        foreach (var location in allLocations)
        {
            if (LocationTrendFieldBuilder.Build(location) is { } trend)
            {
                locationTrends[location.Id] = trend;
            }
        }

        availableCountries = [.. allLocations
            .Select(l => l.Country ?? l.CountryCode)
            .Distinct()
            .Order()];

        UpdateFilteredCount();
        log.LogInformation("Done");
    }

    // Runs after OnInitializedAsync has loaded the locations, so the country can be checked against them
    protected override void OnParametersSet()
    {
        var changed = false;

        if (Country != appliedCountry)
        {
            appliedCountry = Country;
            countryFilter = !string.IsNullOrEmpty(Country) && availableCountries.Contains(Country) ? Country : null;
            changed = true;
        }

        if (TrendScore != appliedTrendScore)
        {
            appliedTrendScore = TrendScore;
            SetTrendScoreFilter(!string.IsNullOrEmpty(TrendScore) && IsValidTrendScoreFilter(TrendScore) ? TrendScore : null);
            changed = true;
        }

        if (changed)
        {
            currentPage = 1;
            UpdateFilteredCount();
        }
    }

    private static string DataTypeToShortName(DataType dt) => dt switch
    {
        DataType.TempMax => "TMax",
        DataType.TempMin => "TMin",
        DataType.TempMean => "TMean",
        DataType.Precipitation => "Precip",
        DataType.SolarRadiation => "Solar",
        DataType.CO2 => "CO₂",
        DataType.CH4 => "CH₄",
        DataType.N2O => "N₂O",
        DataType.SeaIceExtent => "Sea ice",
        DataType.OzoneHoleArea => "Ozone",
        DataType.SeaLevel => "Sea level",
        DataType.CO2Deseasoned => "CO₂ deseasoned",
        _ => dt.ToString(),
    };

    private static string GetTrendScoreBadgeStyle(short score)
    {
        var (background, text) = TrendScoreColours.Get(score);
        return $"background-color: {background}; color: {text};";
    }

    private static bool IsValidTrendScoreFilter(string value) =>
        value is TrendScoreFilterNullScore or TrendScoreFilterNegative
        || (int.TryParse(value, out var score) && score is >= 0 and <= 9);

    private void SetTrendScoreFilter(string? value)
    {
        trendScoreFilter = value;

        // "No trend score" and "Has trend score" can't both hold, so picking the former clears the latter.
        if (trendScoreFilter == TrendScoreFilterNullScore)
        {
            requireTrendScore = false;
        }
    }

    private void UpdateFilteredCount()
    {
        filteredCount = FilteredAndSortedLocations.Count();
        totalPages = filteredCount > 0 ? (int)Math.Ceiling((double)filteredCount / pageSize) : 1;
    }

    private async Task OnCountryFilterChanged(string value)
    {
        countryFilter = string.IsNullOrEmpty(value) ? null : value;
        currentPage = 1;
        UpdateFilteredCount();
        await RaiseCountryChangedAsync();
    }

    private async Task OnTrendScoreFilterChanged(string value)
    {
        SetTrendScoreFilter(string.IsNullOrEmpty(value) ? null : value);
        currentPage = 1;
        UpdateFilteredCount();
        await RaiseTrendScoreChangedAsync();
    }

    private Task RaiseCountryChangedAsync()
    {
        appliedCountry = countryFilter;
        return CountryChanged.InvokeAsync(countryFilter);
    }

    private Task RaiseTrendScoreChangedAsync()
    {
        appliedTrendScore = trendScoreFilter;
        return TrendScoreChanged.InvokeAsync(trendScoreFilter);
    }

    private void OnRequireAnomalyChanged(bool value)
    {
        requireAnomaly = value;
        currentPage = 1;
        UpdateFilteredCount();
    }

    private async Task OnRequireTrendScoreChanged(bool value)
    {
        requireTrendScore = value;
        currentPage = 1;

        if (requireTrendScore && trendScoreFilter == TrendScoreFilterNullScore)
        {
            trendScoreFilter = null;
            UpdateFilteredCount();
            await RaiseTrendScoreChangedAsync();
            return;
        }

        UpdateFilteredCount();
    }

    private void OnRequireTrendChanged(bool value)
    {
        requireTrend = value;
        currentPage = 1;
        UpdateFilteredCount();
    }

    private void OnNameFilterChanged(string? text)
    {
        nameFilter = string.IsNullOrEmpty(text) ? null : text;
        currentPage = 1;
        UpdateFilteredCount();
    }

    private void OnSortChanged(string column)
    {
        if (sortColumn == column)
        {
            sortAscending = !sortAscending;
        }
        else
        {
            sortColumn = column;
            sortAscending = true;
        }
    }

    private void OnPageChanged(int page)
    {
        if (page < 1 || page > totalPages)
        {
            return;
        }

        currentPage = page;
    }

    private void OnPageSizeChanged(int value)
    {
        pageSize = value;
        currentPage = 1;
        totalPages = filteredCount > 0 ? (int)Math.Ceiling((double)filteredCount / pageSize) : 1;
    }

    private string GetSortIconClass(string column)
    {
        if (sortColumn != column)
        {
            return "fa-sort";
        }

        return sortAscending ? "fa-sort-up" : "fa-sort-down";
    }

    // Null leaves aria-sort off the unsorted columns.
    private string? GetAriaSort(string column)
    {
        if (sortColumn != column)
        {
            return null;
        }

        return sortAscending ? "ascending" : "descending";
    }

    private string FormatAnomaly(Location location)
    {
        return ApiMeasures.WarmingAnomaly(location) is { } anomaly ? Units.Format(anomaly, signed: true) : "—";
    }

    private string FormatRecordHigh(Location location) =>
        $"{Units.Format(ApiMeasures.RecordHigh(location)!.Value)} ({location.RecordHigh!.Year})";

    private string GetTrendDisplay(Guid locationId) =>
        locationTrends.TryGetValue(locationId, out var trend) ? LocationTrendFieldBuilder.FormatAmountPerDecade(trend.Slope, Units) : "—";

    private double GetTrendSlope(Guid locationId) =>
        locationTrends.TryGetValue(locationId, out var trend) ? trend.Slope : double.MinValue;

    private string GetDataTypesDisplay(Guid locationId)
    {
        if (!locationDataTypes.TryGetValue(locationId, out var types) || types.Count == 0)
        {
            return "—";
        }

        return string.Join(", ", types.Select(DataTypeToShortName));
    }

    private string GetDataSourcesDisplay(Guid locationId)
    {
        if (!locationDataSources.TryGetValue(locationId, out var sources) || sources.Count == 0)
        {
            return "—";
        }

        return string.Join(", ", sources);
    }

    private bool HasDataSources(Guid locationId)
    {
        return locationDataSources.TryGetValue(locationId, out var sources) && sources.Count > 0;
    }

    private async Task ShowClimateRecordsAsync(Location location)
    {
        selectedLocation = location;
        await (climateRecordsSidePanel?.ShowAsync() ?? Task.CompletedTask);
    }

    private async Task ShowRecentObservationsAsync(Location location)
    {
        selectedLocation = location;
        await (recentObservationsSidePanel?.ShowAsync() ?? Task.CompletedTask);
    }

    private async Task ShowDataSetMetadataAsync(Location location)
    {
        await (dataSetMetadataSidePanel?.ShowAsync(location) ?? Task.CompletedTask);
    }
}
