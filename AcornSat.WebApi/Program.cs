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

var builder = WebApplication.CreateBuilder(args);

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

app.MapGet("/about",                                                          GetAbout);
app.MapGet("/datasetdefinition",                                              GetDataSetDefinitions);
app.MapGet("/location",                                                       GetLocations);
app.MapGet("/dataset/{dataType}/{resolution}/{dataAdjustment}/{locationId}",  GetDataSets);
//app.MapGet("/dataset/{dataType}/{resolution}/{dataAdjustment}",               GetTemperaturesByLatitude);
app.MapGet("/dataset/{dataType}/{resolution}/{dataAdjustment}",               GetDataSets);
app.MapGet("/reference/enso/{index}/{resolution}",                            GetEnso);
app.MapGet("/reference/enso-metadata",                                        GetEnsoMetaData);

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
                PublishedAtEndpoint = x.PublishedAtEndpoint
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

                var dataAdjustment = Enums.DataAdjustment.Unadjusted;
                if (definition.MeasurementDefinitions.Any(x => x.DataType == DataType.TempMax && x.DataAdjustment == Enums.DataAdjustment.Adjusted))
                {
                    dataAdjustment = Enums.DataAdjustment.Adjusted;
                }

                var datasets = await GetDataSets(DataType.TempMax, DataResolution.Yearly, dataAdjustment, AggregationMethod.GroupByDayThenAverage, location.Id, null, 14, .7f, null);
                var dataset = datasets.Single();
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
    return await Location.GetLocations(dataSetFolder);
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

                    var temperatureRecord = new DataRecord() { Year = year };

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

            var locationDataSet = await GetYearlyTemperaturesFromDaily(definition, queryParameters);

            dataSet.AddRange(locationDataSet);
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

async Task<List<DataSet>> LoadCachedDataSets(QueryParameters queryParameters)
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
        var dataSets = JsonSerializer.Deserialize<List<DataSet>>(file);
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

async Task<List<DataSet>> GetDataSets(DataType dataType, DataResolution resolution, DataAdjustment dataAdjustment, AggregationMethod? aggregationMethod, Guid? locationId, short? year, short? dayGrouping, float? dayGroupingThreshold, short? numberOfBins)
{
    return await GetDataSetsInternal(
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

async Task<List<DataSet>> GetDataSetsInternal(QueryParameters queryParameters)
{
    var dataSets = await LoadCachedDataSets(queryParameters);

    if (dataSets != null)
    {
        return dataSets;
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
        var unadjusted = await LoadCachedDataSets(queryParameters) ?? await BuildDataSet(queryParameters, dataSets, definition, true);

        queryParameters.DataAdjustment = DataAdjustment.Adjusted;
        var adjusted = await LoadCachedDataSets(queryParameters) ?? await BuildDataSet(queryParameters, dataSets, definition, true);

        var dataSet = GenerateDifferenceDataSet(unadjusted.Single(), adjusted.Single(), location, queryParameters);

        dataSets = new List<DataSet> { dataSet };

        queryParameters.DataAdjustment = DataAdjustment.Difference;
        await SaveToCache(queryParameters, dataSets);
    }
    else
    {
        if (!definition.MeasurementDefinitions.Any(x => x.DataType == queryParameters.DataType && x.DataAdjustment == queryParameters.DataAdjustment))
        {
            return new List<DataSet>();
        }

        dataSets = await BuildDataSet(queryParameters, dataSets, definition, true);
    }

    return dataSets;
}

async Task<List<DataSet>> BuildDataSet(QueryParameters queryParameters, List<DataSet> dataSets, DataSetDefinition definition, bool saveToCache)
{
    switch (queryParameters.Resolution)
    {
        case DataResolution.Daily:
            dataSets = await GetDataFromFile(definition, queryParameters.DataType, queryParameters.DataAdjustment, queryParameters.LocationId, queryParameters.Year);
            break;
        case DataResolution.Yearly:
            if (definition.DataResolution == DataResolution.Daily)
            {
                dataSets = await GetYearlyTemperaturesFromDaily(definition, queryParameters);
            }
            else if (definition.DataResolution == DataResolution.Monthly)
            {
                dataSets = await GetYearlyTemperaturesFromMonthly(definition, queryParameters.DataType, queryParameters.DataAdjustment, queryParameters.LocationId, queryParameters.Year);
            }
            if (queryParameters.AggregationMethod.HasValue && queryParameters.AggregationMethod == AggregationMethod.GroupByDayThenAverage_Anomaly)
            {
                foreach (var dataSet in dataSets)
                {
                    var mean = dataSet.Mean;
                    dataSet.DataRecords.ForEach(x =>
                    {
                        x.Value = x.Value - mean;
                    });
                }
            }

            dataSets = TrimNulls(dataSets);

            break;
        case DataResolution.Weekly:
            dataSets = await GetAverageFromDailyTemperatures(definition, queryParameters.DataType, DataResolution.Weekly, queryParameters.DataAdjustment, queryParameters.LocationId.Value, queryParameters.Year.Value, ((GroupThenAverage)queryParameters.StatsParameters).DayGroupingThreshold);
            break;
        case DataResolution.Monthly:
            if (definition.DataResolution == DataResolution.Daily)
            {
                dataSets = await GetAverageFromDailyTemperatures(definition, queryParameters.DataType, DataResolution.Monthly, queryParameters.DataAdjustment, queryParameters.LocationId.Value, queryParameters.Year.Value, ((GroupThenAverage)queryParameters.StatsParameters).DayGroupingThreshold);
            }
            else if (definition.DataResolution == DataResolution.Monthly)
            {
                dataSets = await GetDataFromFile(definition, queryParameters.DataType, queryParameters.DataAdjustment, queryParameters.LocationId, queryParameters.Year);
            }
            break;
    }

    if (saveToCache)
    {
        await SaveToCache(queryParameters, dataSets);
    }

    return dataSets;
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
            difference.DataRecords.Add(new DataRecord
            {
                Year = y.Year,
                Value = adjustedTemp == null ? null : adjustedTemp.Value - y.Value,
            });
        });
    return difference;
}

List<DataSet> TrimNulls(List<DataSet> dataSets)
{
    dataSets.ForEach(x =>
    {
        var trimmedNullsFromEndOfList = x.DataRecords.OrderByDescending(x => x.Year).SkipWhile(x => x.Value == null);
        var trimmedNullsFromStartOfList = trimmedNullsFromEndOfList.OrderBy(x => x.Year).SkipWhile(x => x.Value == null);
        x.DataRecords = trimmedNullsFromStartOfList.ToList();
    });

    return dataSets;
}

async Task<List<DataSet>> GetYearlyTemperaturesFromMonthly(DataSetDefinition dataSetDefinition, DataType dataType, DataAdjustment dataAdjustment, Guid? locationId, short? year)
{
    var dataSets = await GetDataFromFile(dataSetDefinition, dataType, dataAdjustment, locationId);

    var returnDataSets = new List<DataSet>();
    foreach (var dataset in dataSets)
    {
        var grouping = dataset.DataRecords.GroupBy(x => x.Year).ToList();
        var records = new List<DataRecord>();
        for (short i = 0; i < grouping.Count(); i++)
        {
            var value = (float)grouping[i].Count(x => x.Value != null) < 12 ? null : grouping[i].Average(x => x.Value);
            var record = new DataRecord()
            {
                Year = grouping[i].Key,
                Value = value,
            };

            records.Add(record);
        }

        dataset.Resolution = DataResolution.Yearly;
        dataset.DataRecords = records;

        returnDataSets.Add(dataset);
    }
    return returnDataSets;
}

async Task<List<DataSet>> GetAverageFromDailyTemperatures(DataSetDefinition dataSetDefinition, DataType dataType, DataResolution dataResolution, DataAdjustment dataAdjustment, Guid locationId, short? year, float? threshold = .8f)
{
    if (threshold < 0 || threshold > 1)
    {
        throw new ArgumentOutOfRangeException("threshold", "Threshold needs to be between 0 and 1.");
    }

    var dataSets = await GetDataFromFile(dataSetDefinition, dataType, dataAdjustment, locationId, year.Value);
    var returnDataSets = new List<DataSet>();
    foreach (var dataset in dataSets)
    {
        List<IGrouping<short?, DataRecord>> grouping = null;
        if (dataResolution == DataResolution.Weekly)
        {
            grouping = dataset.DataRecords.GroupYearByWeek().ToList();
        }
        else if (dataResolution == DataResolution.Monthly)
        {
            grouping = dataset.DataRecords.GroupYearByMonth().ToList();
        }
        var records = new List<DataRecord>();

        for (short i = 0; i < grouping.Count(); i++)
        {
            var numberOfDaysInGroup = grouping[i].Count();

            var value = (float)grouping[i].Count(x => x.Value != null) / numberOfDaysInGroup < threshold ? null : grouping[i].Average(x => x.Value);
            var record = new DataRecord()
            {
                Year = year.Value,
                Value = value,
            };
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

        dataset.Resolution = dataResolution;
        dataset.DataRecords = records;

        returnDataSets.Add(dataset);
    }

    return returnDataSets;
}

async Task<List<DataSet>> GetDataFromFile(DataSetDefinition dataSetDefintion, DataType dataType, DataAdjustment dataAdjustment, Guid? locationId = null, short? year = null)
{
    var location = locationId == null ? null : (await Location.GetLocations(dataSetDefintion.FolderName)).Single(x => x.Id == locationId);

    var returnDataSets = new List<DataSet>();

    var measurementDefinition = dataSetDefintion.MeasurementDefinitions.Single(x => x.DataAdjustment == dataAdjustment && x.DataType == dataType);

    var dataSets = await DataReader.ReadDataFile(dataSetDefintion.FolderName, measurementDefinition, dataSetDefintion.DataResolution, dataAdjustment, location, year);
    dataSets = dataSets.Where(x => x.DataRecords != null).ToList();
    returnDataSets.AddRange(dataSets);
    
    return returnDataSets;
}

async Task<List<DataSet>> GetYearlyTemperaturesFromDaily(DataSetDefinition dataSetDefintion, QueryParameters queryParameters)
{
    var location = (await Location.GetLocations(dataSetDefintion.FolderName)).Single(x => x.Id == queryParameters.LocationId);

    var dailyDataSets = await GetDataFromFile(dataSetDefintion, queryParameters.DataType, queryParameters.DataAdjustment, queryParameters.LocationId);

    var averagedDataSets = new List<DataSet>();

    foreach (var dailyDataSet in dailyDataSets)
    {
        var yearSets = dailyDataSet.DataRecords.GroupBy(x => x.Year);

        List<DataRecord> returnDataRecords = null;
        switch (queryParameters.AggregationMethod)
        {
            case AggregationMethod.GroupByDayThenAverage:
            case AggregationMethod.GroupByDayThenAverage_Anomaly:
                returnDataRecords = GroupThenAverage(yearSets, (GroupThenAverage)queryParameters.StatsParameters);
                break;
            case AggregationMethod.BinThenCount:
                var min = dailyDataSet.DataRecords.Min(x => x.Value).Value;
                var max = dailyDataSet.DataRecords.Max(x => x.Value).Value;
                returnDataRecords = BinThenCount(yearSets, min, max, (BinThenCount)queryParameters.StatsParameters);
                break;
            case AggregationMethod.Sum:
                returnDataRecords = Sum(yearSets);
                break;
        }
        
        var returnDataSet = new DataSet
        {
            Location = location,
            Resolution = DataResolution.Yearly,
            MeasurementDefinition = dailyDataSet.MeasurementDefinition,
            DataRecords = returnDataRecords,
        };
        averagedDataSets.Add(returnDataSet);
    }

    if (averagedDataSets.Count <= 1)
    {
        return averagedDataSets;
    }

    // If the location has multiple datasets we need to average the year between the datasets
    // e.g., unadjusted data in ACORN-SAT sometimes has multiple stations operating at the same time for the same location
    {
        var temperatures = averagedDataSets.SelectMany(x => x.DataRecords).ToList();
        var yearGrouping = temperatures.GroupBy(x => x.Year).Select(x => x.ToList()).ToList();

        var aggregatedAveragedTemperatures = new List<DataRecord>();
        yearGrouping.ForEach(x =>
        {
            aggregatedAveragedTemperatures.Add(new DataRecord
            {
                Year = x.First().Year,
                Value = x.Where(x => x.Value != null).Average(y => y.Value),
            });
        });

        var aggregatedAveragedDataSets = new List<DataSet>();
        var dataSet = new DataSet
        {
            Location = location,
            Resolution = DataResolution.Yearly,
            MeasurementDefinition = averagedDataSets.First().MeasurementDefinition,
            DataRecords = aggregatedAveragedTemperatures
        };
        aggregatedAveragedDataSets.Add(dataSet);
        return aggregatedAveragedDataSets;
    }
}

List<DataRecord> Sum(IEnumerable<IGrouping<short, DataRecord>> yearRecords)
{
    var dataRecords = new List<DataRecord>();
    foreach (var yearRecord in yearRecords)
    {
        dataRecords.Add(new DataRecord()
        {
            Year = yearRecord.Key,
            Value = yearRecord.Sum(x => x.Value),
        });
    }
    return dataRecords;
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
            bins.Add(new DataRecord() 
            { 
                Year = yearSet.Key,
                Label = $"{MathF.Round(start, 1)}-{MathF.Round(end, 1)}",
                Value = count, 
            });
        }
    }
    return bins;
}

List<DataRecord> GroupThenAverage(IEnumerable<IGrouping<short, DataRecord>> yearSets, GroupThenAverage groupThenAverage)
{
    var yearlyAverageRecords = new List<DataRecord>();
    foreach (var yearSet in yearSets)
    {
        var grouping = yearSet.ToList().GroupYearByDays(groupThenAverage.DayGrouping).ToList();
        var groupingAverages = new List<DataRecord>();

        for (short i = 0; i < grouping.Count(); i++)
        {
            var numberOfDaysInGroup = grouping[i].Count();

            var value = (float)grouping[i].Count(x => x.Value != null) / numberOfDaysInGroup < groupThenAverage.DayGroupingThreshold ? null : grouping[i].Average(x => x.Value);
            var record = new DataRecord()
            {
                Value = value,
            };

            groupingAverages.Add(record);
        }

        var yearMean = groupingAverages.Any(x => x.Value == null) ? null : groupingAverages.Average(x => x.Value);
        var yearlyAverageRecord = new DataRecord
        {
            Year = yearSet.Key,
            Value = yearMean,
        };
        yearlyAverageRecords.Add(yearlyAverageRecord);
    }
    return yearlyAverageRecords;
}

async Task<DataSet> GetReferenceData(DataType dataType, DataResolution resolution)
{
    var defintions = await DataSetDefinition.GetDataSetDefinitions();
    var definition = defintions.Single(x => x.MeasurementDefinitions.Any(y => y.DataType == dataType));

    var dataSet = await GetDataSetsInternal(new QueryParameters(dataType, resolution, definition.MeasurementDefinitions.Single().DataAdjustment, null, null, null));

    return dataSet.Single();
}

List<EnsoMetaData> GetEnsoMetaData()
{
    var ensos = new List<EnsoMetaData>()
    {
        new EnsoMetaData { Index = EnsoIndex.Mei, ElNinoOrientation = 1, Name = "Multivariate ENSO index (MEI)", ShortName = "MEI.v2", FileName = "meiv2.data.txt", Url = "https://psl.noaa.gov/enso/mei/data/meiv2.data" },
        new EnsoMetaData { Index = EnsoIndex.Nino34, ElNinoOrientation = 1, Name = "Niño 3.4", ShortName = "Niño 3.4", FileName = "nino34.long.anom.data.txt", Url ="https://psl.noaa.gov/gcos_wgsp/Timeseries/Data/nino34.long.anom.data" },
        new EnsoMetaData { Index = EnsoIndex.Oni, ElNinoOrientation = 1, Name = "Oceanic Niño Index (ONI)", ShortName = "ONI", FileName = "oni.data.txt", Url ="https://psl.noaa.gov/data/correlation/oni.data" },
        new EnsoMetaData { Index = EnsoIndex.Soi, ElNinoOrientation = -1, Name = "Southern Oscillation Index (SOI)", ShortName = "SOI", FileName = "soi.long.data.txt", Url = "https://psl.noaa.gov/gcos_wgsp/Timeseries/Data/soi.long.data" },
    };

    return ensos;
}

async Task<List<DataRecord>> GetEnso(EnsoIndex index, DataResolution resolution, string measure)
{
    string[]? records = null;

    var ensoMetaData = GetEnsoMetaData().Single(x => x.Index == index);

    string dataPath = $@"Reference\ENSO\{ensoMetaData.FileName}";

    records = await DataReader.GetLinesInDataFileWithCascade(dataPath);

    if (records == null)
    {
        throw new Exception("Unable to read ENSO data " + dataPath);
    }

    var regEx = new Regex(@"^\s*(\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)");

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
            if (!groups[i].Value.StartsWith("-99"))
            {
                var value = float.Parse(groups[i].Value);

                if (resolution == DataResolution.Monthly)
                {
                    list.Add(
                    new DataRecord
                    {
                        Month = (short)(i - 1),
                        Year = short.Parse(groups[1].Value),
                        Value = value,
                    });
                }
                else
                {
                    values.Add(value);
                }
            }
        }

        if (resolution == DataResolution.Yearly)
        {
            list.Add(
            new DataRecord
            {
                Year = short.Parse(groups[1].Value),
                Value = measure == "median"
                                        ? values.Median()
                                        : (measure == "mode" ? values.Mode() : values.Average()),
            });
        }
    }

    return list;
}