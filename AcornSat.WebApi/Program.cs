using AcornSat.Core.InputOutput;
using AcornSat.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static AcornSat.Core.Enums;
using System.Threading.Tasks;
using AcornSat.WebApi.Model;
using System.Text.Json;
using AcornSat.Core.ViewModel;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using System.Reflection;
using ClimateExplorer.Core.DataPreparation;
using AcornSat.WebApi;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(
    opt =>
    {
//        opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(
    options =>
    {
        options.AddDefaultPolicy(
            builder =>
            {
                // Don't limit which origins browsers will allow to invoke these web services. Why?
                //    1. the way GitHub actions deploys staging builds of Azure Static Web Apps generates
                //       a different client site DNS name each time. While the sequence is predictable, it'd
                //       be a pain to pre-register many names ahead of time, a pain to react to the allocated
                //       name at deployment time, and the wildcarding functionality (ref CorsPolicyBuilder.
                //       SetIsOriginAllowedToAllowWildcardSubdomains()) is limited to permitting all subdomains
                //       of a nominated domain, which would be overly permissive in our case. (Because the
                //       generated client site DNS name is of the form:
                //           lively-sky-06d813c1e-36.westus2.1.azurestaticapps.net
                //       It's the "36" which changes each time a deployment happens to staging.                
                //
                //    2. Our users aren't authenticated. External web apps can't induce our users' browsers to
                //       do anything using their credentials against our API site because they don't have
                //       credentials, and can't modify any data via the API. The exposure is minimal.
                builder.AllowAnyOrigin();

                builder.AllowAnyHeader();
            }
        );
    }
);

builder.Services.Configure<JsonOptions>(
    options =>
    {
        // This causes the JSON returned from API calls to omit properties if their value is null anyway
        options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    }
);

builder.Logging.AddConsole();

var app = builder.Build();
app.UseCors();

app.MapGet(
    "/", () =>
        "Hello, from minimal ACORN-SAT Web API!\n" +
        "\n" +
        "Operations:\n" +
        "   Call /about for basic API metadata\n" +
        "   Call /datasetdefinition for a list of the dataset definitions. (E.g., ACORN-SAT)\n" +
        "   Call /location for list of locations.\n" +
        "      Parameters:\n" +
        "          dataSetName (querystring parameter): filters to a particular dataset\n" +
        "      Examples:\n" +
        "          /location?dataSetName=ACORN-SET.\n" +
        "   Call /dataSet/{DataType}/{DataResolution}/{DataAdjustment}/{LocationId}?statisticalMethod=GroupThenAverage&dayGrouping=14&dayGroupingThreshold=0.7 for yearly average temperature records at locationId. Records are grouped by dayGrouping. If the number of records in the group does not meet the threshold, the data is considered invalid.\n" +
        "      Parameters:\n" +
        "          DataType: { TempMax | TempMin | Rainfall | Enso }\n" +
        "          DataResolution: { Yearly | Monthly | Weekly | Daily }\n" +
        "          DataAdjustment: { Unadjusted | Adjusted | Difference }\n" +
        "          LocationId: Guid for the target location. Refer to /location endpoint for a list.\n" +
        "          statisticalMethod (querystring parameter): { GroupThenAverage | GroupThenAverage_Relative | BinThenCount }\n" +
        "          dayGrouping (querystring parameter): int, x >= 1. Specifies how many days of data should be included in each group.\n" +
        "          dayGroupingThreshold (querystring parameter): float, 0 <= x <= 1. When grouping records in order to calculate averages, data must be available for at least this proportion of days in a group for the group to be included in the result.\n" +
        "      Examples:\n" +
        "          /dataSet/TempMax/Yearly/Adjusted/eb75b5eb-8feb-4118-b6ab-bbe9b4fbc334?statisticalMethod=GroupThenAverage&dayGrouping=14&dayGroupingThreshold=0.7\n");

app.MapGet("/about",                                                GetAbout);
app.MapGet("/datasetdefinition",                                    GetDataSetDefinitions);
app.MapGet("/location",                                             GetLocations);
app.MapGet("/dataset/{dataType}/{resolution}/{locationId}",         GetDataSet);
//app.MapGet("/dataset/{dataType}/{resolution}/{dataAdjustment}",               GetTemperaturesByLatitude);
app.MapGet("/dataset/{dataType}/{resolution}",                      GetDataSet);
app.MapPost("/dataset",                                             PostDataSets);

app.Run();

object GetAbout()
{
    var asm = Assembly.GetExecutingAssembly();

    return
        new
        {
            Version = asm.GetName().Version.ToString(),
            BuildTimeUtc = File.GetLastWriteTimeUtc(asm.Location)
        };
}

async Task<List<DataSetDefinitionViewModel>> GetDataSetDefinitions(bool includeLocations = false)
{
    var definitions = await DataSetDefinition.GetDataSetDefinitions();

    var dtos =
        definitions
        .Select(
            async x =>
            new DataSetDefinitionViewModel
            {
                Id = x.Id,
                Name = x.Name,
                MoreInformationUrl = x.MoreInformationUrl,
                StationInfoUrl = x.StationInfoUrl,
                LocationInfoUrl = x.LocationInfoUrl,
                DataResolution = x.DataResolution,
                Description = x.Description,
                MeasurementDefinitions = x.MeasurementDefinitions.Select(x => x.ToViewModel()).ToList(),
                HasLocations = x.HasLocations,
                Locations = x.HasLocations && includeLocations ? await GetLocations(x.FolderName) : new List<Location>(),
            })
        .Select(x => x.Result)
        .ToList();

    return dtos;
}

async Task<List<Location>> GetLocations(string dataSetFolder = null, bool includeNearbyLocations = false, bool includeWarmingMetrics = false)
{
    if (string.IsNullOrWhiteSpace(dataSetFolder))
    {
        var definitions = await GetDataSetDefinitions(true);
        var locations = definitions.Where(x => x.Locations.Any()).SelectMany(x => x.Locations).OrderBy(x => x.Name).ToList();
        if (includeNearbyLocations)
        {
            Location.SetNearbyLocations(locations);
        }

        if (includeWarmingMetrics)
        {
            foreach (var location in locations)
            {
                var definition = definitions.Single(x => x.Id == location.DataSetId);

                Enums.DataAdjustment? dataAdjustment = null;
                if (definition.MeasurementDefinitions.Any(x => x.DataType == DataType.TempMax && x.DataAdjustment == Enums.DataAdjustment.Adjusted))
                {
                    dataAdjustment = Enums.DataAdjustment.Adjusted;
                }

                var dataset = await GetDataSet(DataType.TempMax, DataResolution.Yearly, dataAdjustment, AggregationMethod.GroupByDayThenAverage, location.Id, null, 14, .7f, null);
                location.WarmingIndex = dataset.WarmingIndex;
            }

            var maxWarmingIndex = locations.Max(x => x.WarmingIndex).Value;
            locations
                .ToList()
                .ForEach(x => x.HeatingScore = x.WarmingIndex == null 
                                                    ? null 
                                                    : x.WarmingIndex > 0 
                                                            ? Convert.ToInt16(MathF.Round(x.WarmingIndex.Value / maxWarmingIndex * 9, 0))
                                                            : Convert.ToInt16(MathF.Round(x.WarmingIndex.Value, 0)));
        }

        return locations.OrderByDescending(x => x.WarmingIndex).ToList();
    }
    return await Location.GetLocations(dataSetFolder, false);
}

async Task<List<DataSet>> GetTemperaturesByLatitude(DataType dataType, DataResolution resolution, DataAdjustment dataAdjustment, float minLatitude, float maxLatitude, short dayGrouping, float dayGroupingThreshold, float locationGroupingThreshold)
{
    var definitions = (await DataSetDefinition.GetDataSetDefinitions()).Where(x => x.Id == Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321")).ToList();
    var locations = await GetLocationsInLatitudeBand(definitions.First().FolderName, minLatitude, maxLatitude);
    var numberOfLocations = locations.Count;

    if (numberOfLocations == 0)
    {
        return new List<DataSet>();
    }

    switch (resolution)
    {
        case DataResolution.Yearly:
            {
                var dataSets = await YearlyTemperaturesAcrossLocations(definitions, locations, dataType, dataAdjustment, dayGrouping, dayGroupingThreshold);

                var startYear = dataSets.Min(x => x.Years.Min());
                var endYear = dataSets.Max(x => x.Years.Max());

                var returnDataSet = new DataSet();

                Parallel.For(0, endYear - startYear, x =>
                {
                    var year = (short)(x + startYear);
                    var temperatureRecords = dataSets.Where(y => y.StartYear <= year && y.Years.Contains(year))
                                                     .SelectMany(y => y.DataRecords.Where(z => z.Year == year))
                                                     .ToList();

                    var temperatureRecord = new DataRecord(year);

                    temperatureRecord.Value = ((float)temperatureRecords.Count(y => y.Value != null) / (float)numberOfLocations) > locationGroupingThreshold ? temperatureRecords.Average(y => y.Value) : null;

                    returnDataSet.DataRecords.Add(temperatureRecord);
                });

                returnDataSet.DataRecords = returnDataSet.DataRecords.OrderBy(y => y.Year).ToList();

                returnDataSet.Locations = locations;

                return new List<DataSet> { returnDataSet };
            }
    }
    throw new InvalidOperationException("Only yearly aggregates are supported");
}

async Task<List<DataSet>> YearlyTemperaturesAcrossLocations(
    List<DataSetDefinition> definitions, 
    List<Location> locations, 
    DataType dataType, 
    DataAdjustment dataAdjustment, 
    short dayGrouping, 
    float dayGroupingThreshold)
{
    var dataSet = new List<DataSet>();

    await Parallel.ForEachAsync(
        locations, 
        async (location, cancellationToken) =>
        {
            var queryParameters = 
                new QueryParameters(
                    dataType,
                    DataResolution.Yearly,
                    dataAdjustment,
                    location.Id,
                    AggregationMethod.GroupByDayThenAverage,
                    null,
                    dayGrouping: dayGrouping,
                    dayGroupingThreshold: dayGroupingThreshold
                );

            var definition = definitions.Single(x => x.Id == location.DataSetId);

            var locationDataSet = await GetYearlyRecordsFromDaily(definition, queryParameters);

            dataSet.Add(locationDataSet);
        });

    return dataSet;
}

async Task<List<Location>> GetLocationsInLatitudeBand(string dataSetName, float? minLatitude, float? maxLatitude)
{
    var locations = await GetLocations(dataSetName);

    var min = minLatitude;
    var max = maxLatitude;
    Func<Location, bool> filter = x => x.Coordinates.Latitude >= min && x.Coordinates.Latitude < max;
    if (min != null && max != null)
    {
        // Turn the world upside-down?
        if (minLatitude < 0 && maxLatitude < 0)
        {
            min = maxLatitude;
            max = minLatitude;
        }
    }
    else
    {
        filter = x => true;
    }

    var locationsInLatitudeBand = locations.Where(filter).ToList();

    return locationsInLatitudeBand;
}

async Task<DataSet> LoadCachedDataSets(QueryParameters queryParameters)
{
    var fileName = queryParameters.ToBase64String() + ".json";

    var cache = new DirectoryInfo("cache");
    if (!cache.Exists)
    {
        app.Logger.LogInformation("Creating cache folder");
        cache.Create();
    }

    var filePath = @"cache\" + fileName;
    if (File.Exists(filePath))
    {
        app.Logger.LogInformation($"Cache entry exists. File is at {filePath}");

        var file = await File.ReadAllTextAsync(filePath);
        var dataSets = JsonSerializer.Deserialize<DataSet>(file);
        return dataSets;
    }

    app.Logger.LogInformation($"Cache entry does not exist. Checked path {filePath}");

    return null;
}

async Task SaveToCache<T>(QueryParameters queryParameters, T dataToSave)
{
    var fileName = queryParameters.ToBase64String() + ".json";
    var filePath = @"cache\" + fileName;

    app.Logger.LogInformation($"Writing to cache file at {filePath}");

    var json = JsonSerializer.Serialize(dataToSave);
    await File.WriteAllTextAsync(filePath, json);
}

async Task<DataSet> GetDataSet(DataType dataType, DataResolution resolution, DataAdjustment? dataAdjustment, AggregationMethod? aggregationMethod, Guid? locationId, short? year, short? dayGrouping, float? dayGroupingThreshold, short? numberOfBins)
{
    return await GetDataSetInternal(
        new QueryParameters(
            dataType, 
            resolution, 
            dataAdjustment, 
            locationId, 
            aggregationMethod, 
            year, 
            dayGrouping, 
            dayGroupingThreshold, 
            numberOfBins));
}

async Task<DataSet> GetDataSetInternal(QueryParameters queryParameters)
{
    var dataSet = await LoadCachedDataSets(queryParameters);

    if (dataSet != null)
    {
        return dataSet;
    }

    var definitions = await DataSetDefinition.GetDataSetDefinitions();

    DataSetDefinition definition = null;
    Location location = null;
    if (queryParameters.LocationId != null)
    {
        var locations = await GetLocations();
        location = locations.Single(x => x.Id == queryParameters.LocationId);
        definition = definitions.SingleOrDefault(x => x.Id == location.DataSetId);
    }
    else
    {
        definition = definitions.Single(x => x.MeasurementDefinitions.Any(y => y.DataType == queryParameters.DataType));
    }

    if (definition == null)
    {
        throw new ArgumentException(nameof(definition));
    }

    if (queryParameters.DataAdjustment == DataAdjustment.Difference)
    {
        queryParameters.DataAdjustment = DataAdjustment.Unadjusted;
        var unadjusted = await LoadCachedDataSets(queryParameters) ?? await BuildDataSet(queryParameters, definition, true);

        queryParameters.DataAdjustment = DataAdjustment.Adjusted;
        var adjusted = await LoadCachedDataSets(queryParameters) ?? await BuildDataSet(queryParameters, definition, true);

        dataSet = GenerateDifferenceDataSet(unadjusted, adjusted, location, queryParameters);

        queryParameters.DataAdjustment = DataAdjustment.Difference;
        await SaveToCache(queryParameters, dataSet);
    }
    else
    {
        if (!definition.MeasurementDefinitions.Any(x => x.DataType == queryParameters.DataType && x.DataAdjustment == queryParameters.DataAdjustment))
        {
            throw new Exception($"There is no data available for data-type {queryParameters.DataType}");
        }

        dataSet = await BuildDataSet(queryParameters, definition, true);
    }

    return dataSet;
}

async Task<DataSet> PostDataSets(PostDataSetsRequestBody body)
{
    var dsb = new DataSetBuilder();

    var series = await dsb.BuildDataSet(body);

    var definitions = await DataSetDefinition.GetDataSetDefinitions();
    var spec = body.SeriesSpecifications[0];
    var dsd = definitions.Single(x => x.Id == spec.DataSetDefinitionId);

    var location = (await Location.GetLocations(dsd.FolderName, false)).Single(x => x.Id == spec.LocationId);

    var returnDataSet =
        new DataSet
        {
            Location = location,
            Resolution = DataResolution.Yearly,
            MeasurementDefinition = 
                new MeasurementDefinitionViewModel 
                { 
                    DataAdjustment = spec.DataAdjustment, 
                    DataType = spec.DataType.Value,
                    UnitOfMeasure = series.UnitOfMeasure,
                    DataCategory = series.DataCategory
                },
            DataRecords = 
                series.DataPoints
                .Select(
                    x => 
                    new DataRecord 
                    { 
                        Label = x.Label, 
                        Value = x.Value, 
                        BinId = x.BinId,
                    }
                )
                .ToList(),
        };

    return returnDataSet;

}


async Task<DataSet> BuildDataSet(QueryParameters queryParameters, DataSetDefinition definition, bool saveToCache)
{
    DataSet dataSet = null;
    switch (queryParameters.Resolution)
    {
        case DataResolution.Daily:
            dataSet = await GetDataFromFile(definition, queryParameters.DataType, queryParameters.DataAdjustment, queryParameters.LocationId, queryParameters.Year);
            break;

        case DataResolution.Yearly:
            if (definition.DataResolution == DataResolution.Daily)
            {
                dataSet = await GetYearlyRecordsFromDaily(definition, queryParameters);
            }
            else if (definition.DataResolution == DataResolution.Monthly)
            {
                dataSet = await GetYearlyRecordsFromMonthly(definition, queryParameters.DataType, queryParameters.DataAdjustment, queryParameters.LocationId, queryParameters.Year);
            }
            if (queryParameters.AggregationMethod.HasValue && queryParameters.AggregationMethod == AggregationMethod.GroupByDayThenAverage_Anomaly)
            {
                var mean = dataSet.Mean;
                dataSet.DataRecords.ForEach(x =>
                {
                    x.Value = x.Value - mean;
                });
            }

            dataSet = TrimLeadingAndPrecedingNullRecordsFromDataSet(dataSet);

            break;

        case DataResolution.Weekly:
            dataSet = 
                await GetAverageFromDailyRecords(
                    definition, 
                    queryParameters.DataType, 
                    DataResolution.Weekly, 
                    queryParameters.DataAdjustment, 
                    queryParameters.LocationId.Value, 
                    queryParameters.Year.Value, 
                    ((GroupThenAverage)queryParameters.StatsParameters).DayGroupingThreshold);

            break;

        case DataResolution.Monthly:
            if (definition.DataResolution == DataResolution.Daily)
            {
                var statsParameters = (GroupThenAverage)queryParameters.StatsParameters;

                if (statsParameters == null)
                {
                    throw new Exception("Cannot transform daily data into monthly data if StatsParameters have not been supplied to govern that transformation.");
                }

                dataSet = 
                    await GetAverageFromDailyRecords(
                        definition,
                        queryParameters.DataType,
                        DataResolution.Monthly,
                        queryParameters.DataAdjustment,
                        queryParameters.LocationId.Value,
                        queryParameters.Year.Value,
                        statsParameters.DayGroupingThreshold
                    );
            }
            else
            {
                if (definition.DataResolution == DataResolution.Monthly)
                {
                    var measurementDefinition = definition.MeasurementDefinitions.Single(x => x.DataAdjustment == queryParameters.DataAdjustment && x.DataType == queryParameters.DataType);
                    switch (measurementDefinition.RowDataType)
                    {
                        case RowDataType.OneValuePerRow:
                            dataSet = await GetDataFromFile(definition, queryParameters.DataType, queryParameters.DataAdjustment, queryParameters.LocationId);
                            break;
                        case RowDataType.TwelveMonthsPerRow:
                            dataSet = await GetTwelveMonthsPerRowData(definition, queryParameters.DataType, DataResolution.Monthly, "mean");
                            break;
                    }
                }
            }

            break;
    }

    if (saveToCache)
    {
        await SaveToCache(queryParameters, dataSet);
    }

    return dataSet;
}

DataSet GenerateDifferenceDataSet(DataSet unadjusted, DataSet adjusted, Location location, QueryParameters queryParameters)
{
    var difference = new DataSet
    {
        Location = location,
        Resolution = queryParameters.Resolution,
        MeasurementDefinition = new MeasurementDefinitionViewModel { DataAdjustment = DataAdjustment.Difference, DataType = queryParameters.DataType},
    };
    var firstCompleteYear = false;
    unadjusted.DataRecords.ForEach(y =>
        {
            var adjustedTemp = adjusted.DataRecords.SingleOrDefault(z => z.Year == y.Year);

            if (!firstCompleteYear && adjustedTemp == null)
            {
                return;
            }
            firstCompleteYear = true;
            difference.DataRecords.Add(new DataRecord(y.Year, adjustedTemp == null ? null : adjustedTemp.Value - y.Value));
        });
    return difference;
}

DataSet TrimLeadingAndPrecedingNullRecordsFromDataSet(DataSet dataSet)
{
    var trimmedNullsFromEndOfList = dataSet.DataRecords.OrderByDescending(x => x.Year).SkipWhile(x => x.Value == null);
    var trimmedNullsFromStartOfList = trimmedNullsFromEndOfList.OrderBy(x => x.Year).SkipWhile(x => x.Value == null);
    dataSet.DataRecords = trimmedNullsFromStartOfList.ToList();
    
    return dataSet;
}

async Task<DataSet> GetYearlyRecordsFromMonthly(DataSetDefinition dataSetDefinition, DataType dataType, DataAdjustment? dataAdjustment, Guid? locationId, short? year)
{
    DataSet dataSet = null;

    var measurementDefinition = dataSetDefinition.MeasurementDefinitions.Single(x => x.DataType == dataType);
    switch (measurementDefinition.RowDataType)
    {
        case RowDataType.OneValuePerRow:
            dataSet = await GetDataFromFile(dataSetDefinition, dataType, dataAdjustment, locationId);
            break;
        case RowDataType.TwelveMonthsPerRow:
            dataSet = await GetTwelveMonthsPerRowData(dataSetDefinition, dataType, DataResolution.Monthly, "mean");
            break;
    }

   
    var grouping = dataSet.DataRecords.GroupBy(x => x.Year).ToList();
    var records = new List<DataRecord>();
    for (short i = 0; i < grouping.Count(); i++)
    {
        var value = (float)grouping[i].Count(x => x.Value != null) < 12 ? null : grouping[i].Average(x => x.Value);
        var record = new DataRecord(grouping[i].Key, value);

        records.Add(record);
    }

    dataSet.Resolution = DataResolution.Yearly;
    dataSet.DataRecords = records;

    
    return dataSet;
}

async Task<DataSet> GetAverageFromDailyRecords(DataSetDefinition dataSetDefinition, DataType dataType, DataResolution dataResolution, DataAdjustment? dataAdjustment, Guid locationId, short? year, float? threshold = .8f)
{
    if (threshold < 0 || threshold > 1)
    {
        throw new ArgumentOutOfRangeException("threshold", "Threshold needs to be between 0 and 1.");
    }

    var dataSet = await GetDataFromFile(dataSetDefinition, dataType, dataAdjustment, locationId, year.Value);
    var returnDataSets = new List<DataSet>();
    
    List<IGrouping<short?, DataRecord>> grouping = null;
    if (dataResolution == DataResolution.Weekly)
    {
        grouping = dataSet.DataRecords.GroupYearByWeek().ToList();
    }
    else if (dataResolution == DataResolution.Monthly)
    {
        grouping = dataSet.DataRecords.GroupYearByMonth().ToList();
    }
    var records = new List<DataRecord>();

    for (short i = 0; i < grouping.Count(); i++)
    {
        var numberOfDaysInGroup = grouping[i].Count();

        var value = (float)grouping[i].Count(x => x.Value != null) / numberOfDaysInGroup < threshold ? null : grouping[i].Average(x => x.Value);
        var record = new DataRecord(year.Value, value);
        if (dataResolution == DataResolution.Weekly)
        {
            record.Week = i;
        }
        else if (dataResolution == DataResolution.Monthly)
        {
            record.Month = i;
        }

        records.Add(record);
    }

    dataSet.Resolution = dataResolution;
    dataSet.DataRecords = records;

    return dataSet;
}

async Task<DataSet> GetDataFromFile(DataSetDefinition dataSetDefintion, DataType dataType, DataAdjustment? dataAdjustment, Guid? locationId = null, short? year = null)
{
    var location = locationId == null ? null : (await Location.GetLocations(dataSetDefintion.FolderName, false)).Single(x => x.Id == locationId);

    var measurementDefinition = dataSetDefintion.MeasurementDefinitions.Single(x => x.DataAdjustment == dataAdjustment && x.DataType == dataType);

    var dataSet = await DataReader.GetDataSet(dataSetDefintion.FolderName, measurementDefinition, dataSetDefintion.DataResolution, location, year);
    
    return dataSet;
}

async Task<DataSet> GetYearlyRecordsFromDaily(DataSetDefinition dataSetDefintion, QueryParameters queryParameters)
{
    var location = (await Location.GetLocations(dataSetDefintion.FolderName, false)).Single(x => x.Id == queryParameters.LocationId);

    var dailyDataSet = await GetDataFromFile(dataSetDefintion, queryParameters.DataType, queryParameters.DataAdjustment, queryParameters.LocationId);

    var yearSets = dailyDataSet.DataRecords.GroupBy(x => x.Year);

    List<DataRecord> returnDataRecords = null;
    switch (queryParameters.AggregationMethod)
    {
        case AggregationMethod.GroupByDayThenAverage:
        case AggregationMethod.GroupByDayThenAverage_Anomaly:
        case AggregationMethod.Sum:
            returnDataRecords = AggregateToYearlyValues(yearSets, queryParameters);
            break;
        case AggregationMethod.BinThenCount:
            var min = dailyDataSet.DataRecords.Min(x => x.Value).Value;
            var max = dailyDataSet.DataRecords.Max(x => x.Value).Value;
            returnDataRecords = BinThenCount(yearSets, min, max, (BinThenCount)queryParameters.StatsParameters);
            break;
    }
        
    var returnDataSet = new DataSet
    {
        Location = location,
        Resolution = DataResolution.Yearly,
        MeasurementDefinition = dailyDataSet.MeasurementDefinition,
        DataRecords = returnDataRecords,
    };
    
    return returnDataSet;
}

List<DataRecord> BinThenCount(IEnumerable<IGrouping<short, DataRecord>> yearSets, float min, float max, BinThenCount statsParameters)
{
    var range = max - min;
    var binSize = range / statsParameters.NumberOfBins.Value;
    var bins = new List<DataRecord>();
    foreach (var yearSet in yearSets)
    {
        var orderedValues = yearSet.ToList().Where(x => x.Value.HasValue).OrderBy(x => x.Value);
        
        for (var i = 0; i < statsParameters.NumberOfBins; i++)
        {
            var start = min + (i * binSize);
            var end = min + ((i + 1) * binSize);
            var count = orderedValues.Count(x => x.Value >= start && x.Value < end);
            bins.Add(new DataRecord(yearSet.Key, count) 
            { 
                Label = $"{MathF.Round(start, 1)}-{MathF.Round(end, 1)}",
            });
        }
    }
    return bins;
}

List<DataRecord> AggregateToYearlyValues(IEnumerable<IGrouping<short, DataRecord>> yearSets, QueryParameters queryParameters)
{
    var yearlyAverageRecords = new List<DataRecord>();
    var groupThenAverage = (GroupThenAverage)queryParameters.StatsParameters;
    foreach (var yearSet in yearSets)
    {
        var grouping = yearSet.ToList().GroupYearByDays(groupThenAverage.DayGrouping).ToList();
        var aggregates = new List<DataRecord>();

        for (short i = 0; i < grouping.Count(); i++)
        {
            var numberOfDaysInGroup = grouping[i].Count();

            float? groupValue = null;
            if ((float)grouping[i].Count(x => x.Value != null) / numberOfDaysInGroup >= groupThenAverage.DayGroupingThreshold)
            {
                groupValue = queryParameters.AggregationMethod == AggregationMethod.Sum ? grouping[i].Sum(x => x.Value) : grouping[i].Average(x => x.Value);
            }
            var record = new DataRecord(yearSet.Key, groupValue);

            aggregates.Add(record);
        }

        var yearMean = aggregates.Any(x => x.Value == null) ? null :
                queryParameters.AggregationMethod == AggregationMethod.Sum ? 
                    aggregates.Sum(x => x.Value) :
                    aggregates.Average(x => x.Value);
        var yearlyAverageRecord = new DataRecord(yearSet.Key, yearMean);
        yearlyAverageRecords.Add(yearlyAverageRecord);
    }
    return yearlyAverageRecords;
}

async Task<DataSet> GetTwelveMonthsPerRowData(DataSetDefinition dataSetDefinition, DataType dataType, DataResolution resolution, string measure)
{
    string[]? records = null;

    var measurementDefinition = dataSetDefinition.MeasurementDefinitions.Single(x => x.DataType == dataType);

    string dataPath = $@"Reference\{dataSetDefinition.FolderName}\{measurementDefinition.FileNameFormat}";

    records = await DataReader.GetLinesInDataFileWithCascade(dataPath);

    if (records == null)
    {
        throw new Exception("Unable to read ENSO data " + dataPath);
    }
    
    var regEx = new Regex(measurementDefinition.DataRowRegEx);

    var list = new List<DataRecord>();
    var dataRowFound = false;
    foreach (var record in records)
    {
        if (!regEx.Match(record).Success)
        {
            if (dataRowFound)
            {
                break;
            }
            else
            {
                continue;
            }
        }
        dataRowFound = true;

        var groups = regEx.Match(record).Groups;

        var values = new List<float>();
        for (var i = 2; i < groups.Count; i++)
        {
            if (!groups[i].Value.StartsWith(measurementDefinition.NullValue))
            {
                var value = float.Parse(groups[i].Value);

                if (resolution == DataResolution.Monthly)
                {
                    list.Add(new DataRecord(short.Parse(groups[1].Value), (short)(i - 1), null, value));
                }
                else
                {
                    values.Add(value);
                }
            }
        }

        if (resolution == DataResolution.Yearly)
        {
            var value = measure == "median"
                                        ? values.Median()
                                        : (measure == "mode" ? values.Mode() : values.Average());
            
            list.Add(new DataRecord(short.Parse(groups[1].Value), value));
        }
    }

    var dataSet = new DataSet
    {
        Resolution = resolution,
        MeasurementDefinition = measurementDefinition.ToViewModel(),
        DataRecords = list,
    };

    return dataSet;
}