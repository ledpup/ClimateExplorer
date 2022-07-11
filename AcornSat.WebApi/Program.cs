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
using AcornSat.Core.Model;

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

async Task<List<DataSetDefinitionViewModel>> GetDataSetDefinitions()
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
                Description = x.Description,
                LocationIds = x.DataLocationMapping?.LocationIdToDataFileMappings.Keys.ToList(),
                MeasurementDefinitions = x.MeasurementDefinitions.Select(x => x.ToViewModel()).ToList(),
            })
        .Select(x => x.Result)
        .ToList();

    return dtos;
}

async Task<List<Location>> GetLocations(bool includeNearbyLocations = false, bool includeWarmingMetrics = false)
{
    var locations = (await Location.GetLocations(includeNearbyLocations)).OrderBy(x => x.Name).ToList();

    if (includeWarmingMetrics)
    {
        var definitions = await GetDataSetDefinitions();

        // For each location, retrieve the TempMax dataset (Adjusted if available, Adjustment null otherwise), and copy its WarmingIndex
        // to the location we're about to return.
        foreach (var location in locations)
        {
            try
            {
                // First, find what data adjustments are available for TempMax for that location.
                (var dsdOfferingTempMax, var mdTempMax) =
                    DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
                        definitions,
                        location.Id,
                        DataType.TempMax,
                        Enums.DataAdjustment.Adjusted,
                        true);

                // Next, request that dataset (omitting DataSetDefinition.Id because GetDataSet looks it up manually
                // on the assumption (valid for now) that there's only ever one DataSetDefinition offering a given
                // DataType and DataAdjustment for a given location).
                var dataset =
                    await GetDataSet(
                        DataType.TempMax,
                        DataResolution.Yearly,
                        mdTempMax.DataAdjustment,
                        AggregationMethod.GroupByDayThenAverage,
                        location.Id,
                        null,
                        14,
                        .7f,
                        null);

                location.WarmingIndex = dataset.WarmingIndex;
            }
            catch (Exception ex)
            {
                // For now, just swallow failures here, caused by missing data
                Console.WriteLine("Exception while calculating warming index for " + location.Name + ": " + ex.ToString());
            }
        }

        var maxWarmingIndex = locations.Max(x => x.WarmingIndex).Value;

        locations
            .ToList()
            .ForEach(x => x.HeatingScore = x.WarmingIndex == null
                                                ? null
                                                : x.WarmingIndex > 0
                                                        ? Convert.ToInt16(MathF.Round(x.WarmingIndex.Value / maxWarmingIndex * 9, 0))
                                                        : Convert.ToInt16(MathF.Round(x.WarmingIndex.Value, 0)));

        return locations;
    }

    return await Location.GetLocations(false);
}

async Task<List<DataSet>> YearlyTemperaturesAcrossLocations(
    DataSetDefinition dataSetDefinition, 
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

            var measurementDefinition = dataSetDefinition.MeasurementDefinitions.Single(x => x.DataType == queryParameters.DataType
                                                                        && x.DataResolution == queryParameters.Resolution
                                                                        && (!queryParameters.DataAdjustment.HasValue || x.DataAdjustment == queryParameters.DataAdjustment));

            var dfms = dataSetDefinition.DataLocationMapping.LocationIdToDataFileMappings[location.Id];

            var locationDataSet = await GetYearlyRecordsFromDaily(measurementDefinition, dfms, queryParameters);

            dataSet.Add(locationDataSet);
        });

    return dataSet;
}

async Task<List<Location>> GetLocationsInLatitudeBand(float? minLatitude, float? maxLatitude)
{
    var locations = await GetLocations();

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

    var definition = definitions.Single(x => x.DataLocationMapping != null
                                                && x.DataLocationMapping.LocationIdToDataFileMappings.Keys.Any(y => y == queryParameters.LocationId)
                                                && x.MeasurementDefinitions.Any(y => y.DataType == queryParameters.DataType && y.DataAdjustment == queryParameters.DataAdjustment));
    Location location = null;
    if (queryParameters.LocationId != null)
    {
        var locations = await GetLocations();
        location = locations.Single(x => x.Id == queryParameters.LocationId);
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

    Location location =
        spec.LocationId != null
        ? (await Location.GetLocations(false)).Single(x => x.Id == spec.LocationId)
        : null;

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
            RawDataRecords =
                body.IncludeRawDataPoints
                ? series.RawDataPoints
                : null

        };

    return returnDataSet;

}


async Task<DataSet> BuildDataSet(QueryParameters queryParameters, DataSetDefinition definition, bool saveToCache)
{
    DataSet dataSet = null;

    var measurementDefinition = definition.MeasurementDefinitions.Single(x =>  x.DataType == queryParameters.DataType 
                                                                            && (!queryParameters.DataAdjustment.HasValue || x.DataAdjustment == queryParameters.DataAdjustment));

    List<DataFileFilterAndAdjustment> dfms = null;
    if (queryParameters.LocationId != null)
    {
        dfms = definition.DataLocationMapping.LocationIdToDataFileMappings[queryParameters.LocationId.Value];
    }

    switch (queryParameters.Resolution)
    {
        case DataResolution.Daily:
            dataSet = await GetDataFromFile(measurementDefinition, dfms);
            break;

        case DataResolution.Yearly:
            if (measurementDefinition.DataResolution == DataResolution.Daily)
            {
                dataSet = await GetYearlyRecordsFromDaily(measurementDefinition, dfms, queryParameters);
            }
            else if (measurementDefinition.DataResolution == DataResolution.Monthly)
            {
                dataSet = await GetYearlyRecordsFromMonthly(measurementDefinition, dfms);
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
                    measurementDefinition,
                    dfms, 
                    queryParameters.Year.Value, 
                    ((GroupThenAverage)queryParameters.StatsParameters).DayGroupingThreshold);

            break;

        case DataResolution.Monthly:
            if (measurementDefinition.DataResolution == DataResolution.Daily)
            {
                var statsParameters = (GroupThenAverage)queryParameters.StatsParameters;

                if (statsParameters == null)
                {
                    throw new Exception("Cannot transform daily data into monthly data if StatsParameters have not been supplied to govern that transformation.");
                }

                dataSet = 
                    await GetAverageFromDailyRecords(
                        measurementDefinition,
                        dfms,
                        queryParameters.Year.Value,
                        statsParameters.DayGroupingThreshold
                    );
            }
            else
            {
                if (measurementDefinition.DataResolution == DataResolution.Monthly)
                {
                    switch (measurementDefinition.RowDataType)
                    {
                        case RowDataType.OneValuePerRow:
                            dataSet = await GetDataFromFile(measurementDefinition, dfms);
                            break;
                        case RowDataType.TwelveMonthsPerRow:
                            dataSet = await GetTwelveMonthsPerRowData(measurementDefinition, "mean");
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

async Task<DataSet> GetYearlyRecordsFromMonthly(MeasurementDefinition measurementDefinition, List<DataFileFilterAndAdjustment> dataFileFilterAndAdjustments)
{
    DataSet dataSet = null;

    switch (measurementDefinition.RowDataType)
    {
        case RowDataType.OneValuePerRow:
            dataSet = await GetDataFromFile(measurementDefinition, dataFileFilterAndAdjustments);
            break;
        case RowDataType.TwelveMonthsPerRow:
            dataSet = await GetTwelveMonthsPerRowData(measurementDefinition, "mean");
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

async Task<DataSet> GetAverageFromDailyRecords(MeasurementDefinition measurementDefinition, List<DataFileFilterAndAdjustment> dataFileFilterAndAdjustments, short? year, float? threshold = .8f)
{
    if (threshold < 0 || threshold > 1)
    {
        throw new ArgumentOutOfRangeException("threshold", "Threshold needs to be between 0 and 1.");
    }

    var dataSet = await GetDataFromFile(measurementDefinition, dataFileFilterAndAdjustments);
    var returnDataSets = new List<DataSet>();
    
    List<IGrouping<short?, DataRecord>> grouping = null;
    if (measurementDefinition.DataResolution == DataResolution.Weekly)
    {
        grouping = dataSet.DataRecords.GroupYearByWeek().ToList();
    }
    else if (measurementDefinition.DataResolution == DataResolution.Monthly)
    {
        grouping = dataSet.DataRecords.GroupYearByMonth().ToList();
    }
    var records = new List<DataRecord>();

    for (short i = 0; i < grouping.Count(); i++)
    {
        var numberOfDaysInGroup = grouping[i].Count();

        var value = (float)grouping[i].Count(x => x.Value != null) / numberOfDaysInGroup < threshold ? null : grouping[i].Average(x => x.Value);
        var record = new DataRecord(year.Value, value);
        if (measurementDefinition.DataResolution == DataResolution.Weekly)
        {
            record.Week = i;
        }
        else if (measurementDefinition.DataResolution == DataResolution.Monthly)
        {
            record.Month = i;
        }

        records.Add(record);
    }

    dataSet.Resolution = measurementDefinition.DataResolution;
    dataSet.DataRecords = records;

    return dataSet;
}

async Task<DataSet> GetDataFromFile(MeasurementDefinition measurementDefinition, List<DataFileFilterAndAdjustment> dataFileFilterAndAdjustments)
{
    var dataSet = await DataReader.GetDataSet(measurementDefinition, dataFileFilterAndAdjustments);
    
    return dataSet;
}

async Task<DataSet> GetYearlyRecordsFromDaily(MeasurementDefinition measurementDefinition, List<DataFileFilterAndAdjustment> dataFileFilterAndAdjustments, QueryParameters queryParameters)
{
    var location = (await Location.GetLocations(false)).Single(x => x.Id == queryParameters.LocationId);

    var dailyDataSet = await GetDataFromFile(measurementDefinition, dataFileFilterAndAdjustments);

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

async Task<DataSet> GetTwelveMonthsPerRowData(MeasurementDefinition measurementDefinition, string measure)
{
    string[]? records = null;

    string dataPath = $@"Reference\{measurementDefinition.FolderName}\{measurementDefinition.FileNameFormat}";

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

                if (measurementDefinition.DataResolution == DataResolution.Monthly)
                {
                    list.Add(new DataRecord(short.Parse(groups[1].Value), (short)(i - 1), null, value));
                }
                else
                {
                    values.Add(value);
                }
            }
        }

        if (measurementDefinition.DataResolution == DataResolution.Yearly)
        {
            var value = measure == "median"
                                        ? values.Median()
                                        : (measure == "mode" ? values.Mode() : values.Average());
            
            list.Add(new DataRecord(short.Parse(groups[1].Value), value));
        }
    }

    var dataSet = new DataSet
    {
        Resolution = measurementDefinition.DataResolution,
        MeasurementDefinition = measurementDefinition.ToViewModel(),
        DataRecords = list,
    };

    return dataSet;
}