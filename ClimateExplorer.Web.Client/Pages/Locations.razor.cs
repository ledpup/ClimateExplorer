namespace ClimateExplorer.Web.Client.Pages;

using System.Globalization;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.Client.Components.Location;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class Locations
{
    private const string HeatingScoreFilterNullScore = "null-score";
    private const string HeatingScoreFilterNegative = "negative";

    private List<Location> allLocations = [];
    private IEnumerable<DataSetDefinitionViewModel> dataSetDefinitions = [];
    private Dictionary<Guid, List<DataType>> locationDataTypes = new();
    private Dictionary<Guid, List<string>> locationDataSources = new();
    private string? countryFilter;
    private string? heatingScoreFilter;
    private string sortColumn = "name";
    private bool sortAscending = true;
    private int currentPage = 1;
    private int pageSize = 25;
    private List<string> availableCountries = [];
    private int filteredCount;
    private int totalPages = 1;
    private SidePanel? climateRecordsSidePanel;
    private LocationDataSetMetadataSidePanel? dataSetMetadataSidePanel;
    private Location? selectedLocation;
    private string? nameFilter;

    [Inject]
    private IDataService? DataService { get; set; }

    [Inject]
    private ILogger<Locations>? Logger { get; set; }

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

            if (!string.IsNullOrEmpty(heatingScoreFilter))
            {
                query = heatingScoreFilter switch
                {
                    HeatingScoreFilterNullScore => query.Where(l => !l.HeatingScore.HasValue),
                    HeatingScoreFilterNegative => query.Where(l => l.HeatingScore.HasValue && l.HeatingScore.Value < 0),
                    _ when int.TryParse(heatingScoreFilter, out var score) => query.Where(l => l.HeatingScore == score),
                    _ => query,
                };
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
                "heatingscore" => sortAscending
                    ? query.OrderBy(l => l.HeatingScore ?? int.MinValue)
                    : query.OrderByDescending(l => l.HeatingScore ?? int.MinValue),
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

    protected override async Task OnInitializedAsync()
    {
        var log = new LogAugmenter(Logger!, nameof(OnInitializedAsync));
        log.LogInformation("Starting");

        allLocations = [.. await DataService!.GetLocations()];
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

        availableCountries = [.. allLocations
            .Select(l => l.Country ?? l.CountryCode)
            .Distinct()
            .Order()];

        UpdateFilteredCount();
        log.LogInformation("Done");
    }

    private static string FormatAnomaly(double? anomaly)
    {
        if (!anomaly.HasValue)
        {
            return "—";
        }

        var sign = anomaly.Value >= 0 ? "+" : string.Empty;
        return $"{sign}{anomaly.Value.ToString("0.0", CultureInfo.InvariantCulture)}°C";
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
        _ => dt.ToString(),
    };

    private static string FormatRecordHigh(DataRecord record) =>
        $"{record.Value!.Value.ToString("0.0", CultureInfo.InvariantCulture)}°C ({record.Year})";

    private static string GetHeatingScoreBadgeStyle(short score)
    {
        var (bg, fg) = score switch
        {
            9 => ("#8B0000", "white"),
            8 => ("#c70000", "white"),
            7 => ("#e05000", "white"),
            6 => ("#e08000", "white"),
            5 => ("#c8a000", "black"),
            4 => ("#8fb339", "black"),
            3 => ("#5a9e3b", "white"),
            2 => ("#3a8a7c", "white"),
            1 => ("#3b78af", "white"),
            0 => ("#5b7fbd", "white"),
            _ => ("#4a6dab", "white"),
        };
        return $"background-color: {bg}; color: {fg};";
    }

    private void UpdateFilteredCount()
    {
        filteredCount = FilteredAndSortedLocations.Count();
        totalPages = filteredCount > 0 ? (int)Math.Ceiling((double)filteredCount / pageSize) : 1;
    }

    private void OnCountryFilterChanged(string value)
    {
        countryFilter = string.IsNullOrEmpty(value) ? null : value;
        currentPage = 1;
        UpdateFilteredCount();
    }

    private void OnHeatingScoreFilterChanged(string value)
    {
        heatingScoreFilter = string.IsNullOrEmpty(value) ? null : value;
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

    private async Task ShowDataSetMetadataAsync(Location location)
    {
        await (dataSetMetadataSidePanel?.ShowAsync(location) ?? Task.CompletedTask);
    }
}
