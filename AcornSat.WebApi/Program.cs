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

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
                      builder =>
                      {
                          builder.WithOrigins("http://localhost:5298");
                      });
});

var app = builder.Build();
app.UseCors();

app.MapGet("/", () => @"Hello, from minimal ACORN-SAT Web API!
                        Call /dataSet for a list of the dataset definitions. (E.g., ACORN-SAT)
                        Call /ACORN-SAT/location for list of locations.
                        Call /ACORN-SAT/Yearly/{temperatureType}/{locationId}?dayGrouping=14&dayGroupingThreshold=.8 for yearly average temperature records at locationId. Records are grouped by dayGrouping. If the number of records in the group does not meet the threshold, the data is considered invalid.");
app.MapGet("/dataSet", (bool? includeLocations) => GetDataSetDefinitions(includeLocations));
app.MapGet("/location", (string dataSetName) => GetLocations(dataSetName));
app.MapGet("/dataSet/{resolution}/{measurementType}/{locationId}", (DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year, short? dayGrouping, float? dayGroupingThreshold) => GetDataSets(resolution, measurementType, locationId, year, dayGrouping, dayGroupingThreshold));
app.MapGet("/dataSet/{resolution}/{measurementType}", (DataResolution resolution, MeasurementType measurementType, float? minLatitude, float? maxLatitude, short dayGrouping, float dayGroupingThreshold, float locationGroupingThreshold) => GetTemperaturesByLatitudeGroups(resolution, measurementType, minLatitude, maxLatitude, dayGrouping, dayGroupingThreshold, locationGroupingThreshold));
app.MapGet("/reference/co2/", () => GetCarbonDioxide());
app.MapGet("/reference/enso/{index}/{resolution}", (EnsoIndex index, DataResolution resolution, string measure) => GetEnso(index, resolution, measure));
app.MapGet("/reference/enso-metadata", () => GetEnsoMetaData());

app.Run();

List<AcornSat.WebApi.Model.DataSetDefinition> GetDataSetDefinitions(bool? includeLocations = false)
{
    var definitions = DataSetDefinition.GetDataSetDefinitions();

    var dtos = definitions.Select(x => new AcornSat.WebApi.Model.DataSetDefinition
    {
        Id = x.Id,
        Name = x.Name,
        DataResolution = x.DataResolution,
        DataType = x.DataType,
        Description = x.Description,
        Locations = includeLocations.GetValueOrDefault() ? GetLocations(x.Name) : null,
    }).ToList();

    return dtos;
}

List<Location> GetLocations(string dataSetName = null)
{
    if (string.IsNullOrWhiteSpace(dataSetName))
    {
        var definitions = GetDataSetDefinitions(true);
        var locations = definitions.SelectMany(x => x.Locations).OrderBy(x => x.Name).ToList();
        Location.SetNearbyLocations(locations);
        return locations;
    }
    return Location.GetLocations(dataSetName);
}

List<DataSet> GetTemperaturesByLatitudeGroups(DataResolution resolution, MeasurementType measurementType, float? minLatitude, float? maxLatitude, short dayGrouping, float dayGroupingThreshold, float locationGroupingThreshold)
{
    var definitions = DataSetDefinition.GetDataSetDefinitions();
    var locations = GetLocationsInLatitudeBand(minLatitude, maxLatitude);
    var numberOfLocations = locations.Count;

    if (numberOfLocations == 0)
    {
        return new List<DataSet>();
    }

    switch (resolution)
    {
        case DataResolution.Yearly:
            {
                var dataSets = YearlyTemperaturesAcrossLocations(definitions, locations, measurementType, dayGrouping, dayGroupingThreshold);

                var startYear = dataSets.Min(x => x.Years.Min());
                var endYear = dataSets.Max(x => x.Years.Max());

                var returnDataSet = new DataSet();

                Parallel.For(0, endYear - startYear, x =>
                {
                    var year = (short)(x + startYear);
                    var temperatureRecords = dataSets.Where(y => y.Years.Contains(year))
                                                     .SelectMany(y => y.Temperatures.Where(z => z.Year == year))
                                                     .ToList();

                    var temperatureRecord = new TemperatureRecord() { Year = year };

                    temperatureRecord.Min = ((float)temperatureRecords.Count(y => y.Min != null) / (float)numberOfLocations) > locationGroupingThreshold ? temperatureRecords.Average(y => y.Min) : null;
                    temperatureRecord.Max = ((float)temperatureRecords.Count(y => y.Max != null) / (float)numberOfLocations) > locationGroupingThreshold ? temperatureRecords.Average(y => y.Max) : null;

                    returnDataSet.Temperatures.Add(temperatureRecord);
                });

                returnDataSet.Temperatures = returnDataSet.Temperatures.OrderBy(y => y.Year).ToList();

                returnDataSet.Locations = locations;

                return new List<DataSet> { returnDataSet };
            }
    }
    throw new InvalidOperationException("Only yearly aggregates are supported");
}

List<DataSet> YearlyTemperaturesAcrossLocations(List<DataSetDefinition> definitions, List<Location> locations, MeasurementType measurementType, short dayGrouping, float dayGroupingThreshold)
{
    var dataSet = new List<DataSet>();
    Parallel.ForEach(locations, location => {
        var definition = definitions.Single(x => x.Id == location.DataSetId);
        var locationDataSet = GetYearlyTemperaturesFromDaily(definition, measurementType, location.Id, dayGrouping, dayGroupingThreshold);
        dataSet.AddRange(locationDataSet);
    });

    return dataSet;
}

List<Location> GetLocationsInLatitudeBand(float? minLatitude, float? maxLatitude)
{
    var locations = GetLocations();

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

List<DataSet> GetDataSets(DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year, short? dayGrouping, float? dayGroupingThreshold)
{
    var definitions = DataSetDefinition.GetDataSetDefinitions();
    var locations = GetLocations();
    var location = locations.Single(x => x.Id == locationId);
    var definition = definitions.SingleOrDefault(x => x.Id == location.DataSetId);
    if (definition == null)
    {
        throw new ArgumentException(nameof(definition));
    }

    switch (resolution)
    {
        case DataResolution.Daily:
            return GetTemperaturesFromFile(definition, measurementType, locationId, year);
        case DataResolution.Yearly:
            if (definition.DataResolution == DataResolution.Daily)
            {
                return GetYearlyTemperaturesFromDaily(definition, measurementType, locationId, dayGrouping, dayGroupingThreshold);
            }
            else if (definition.DataResolution == DataResolution.Monthly)
            {
                return GetYearlyTemperaturesFromMonthly(definition, measurementType, locationId, year);
            }
            break;
        case DataResolution.Weekly:
            return GetAverageFromDailyTemperatures(definition, DataResolution.Weekly, measurementType, locationId, year.Value, dayGroupingThreshold);
        case DataResolution.Monthly:
            if (definition.DataResolution == DataResolution.Daily)
            {
                return GetAverageFromDailyTemperatures(definition, DataResolution.Monthly, measurementType, locationId, year.Value, dayGroupingThreshold);
            }
            else if (definition.DataResolution == DataResolution.Monthly)
            {
                return GetTemperaturesFromFile(definition, measurementType, locationId, year);
            }
            break;
    }

    throw new NotImplementedException();
}

List<DataSet> GetYearlyTemperaturesFromMonthly(DataSetDefinition dataSetDefinition, MeasurementType measurementType, Guid locationId, short? year)
{
    var location = Location.GetLocations(dataSetDefinition.FolderName).Single(x => x.Id == locationId);

    var dataSets = GetTemperaturesFromFile(dataSetDefinition, measurementType, locationId);

    var returnDataSets = new List<DataSet>();
    foreach (var dataset in dataSets)
    {
        var grouping = dataset.Temperatures.GroupBy(x => x.Year).ToList();
        var records = new List<TemperatureRecord>();
        for (short i = 0; i < grouping.Count(); i++)
        {
            var min = (float)grouping[i].Count(x => x.Min != null) < 12 ? null : grouping[i].Average(x => x.Min);
            var max = (float)grouping[i].Count(x => x.Max != null) < 12 ? null : grouping[i].Average(x => x.Max);
            var record = new TemperatureRecord()
            {
                Year = grouping[i].Key,
                Min = min,
                Max = max,
            };

            records.Add(record);
        }

        dataset.Resolution = DataResolution.Yearly;
        dataset.Temperatures = records;

        returnDataSets.Add(dataset);
    }
    return returnDataSets;
}

List<DataSet> GetAverageFromDailyTemperatures(DataSetDefinition dataSetDefinition, DataResolution dataResolution, MeasurementType measurementType, Guid locationId, short? year, float? threshold = .8f)
{
    if (threshold < 0 || threshold > 1)
    {
        throw new ArgumentOutOfRangeException("threshold", "Threshold needs to be between 0 and 1.");
    }

    var dataSets = GetTemperaturesFromFile(dataSetDefinition, measurementType, locationId, year.Value);
    var returnDataSets = new List<DataSet>();
    foreach (var dataset in dataSets)
    {
        List<IGrouping<short?, TemperatureRecord>> grouping = null;
        if (dataResolution == DataResolution.Weekly)
        {
            grouping = dataset.Temperatures.GroupYearByWeek().ToList();
        }
        else if (dataResolution == DataResolution.Monthly)
        {
            grouping = dataset.Temperatures.GroupYearByMonth().ToList();
        }
        var records = new List<TemperatureRecord>();

        for (short i = 0; i < grouping.Count(); i++)
        {
            var numberOfDaysInGroup = grouping[i].Count();

            var min = (float)grouping[i].Count(x => x.Min != null) / numberOfDaysInGroup < threshold ? null : grouping[i].Average(x => x.Min);
            var max = (float)grouping[i].Count(x => x.Max != null) / numberOfDaysInGroup < threshold ? null : grouping[i].Average(x => x.Max);
            var record = new TemperatureRecord()
            {
                Year = year.Value,
                Min = min,
                Max = max,
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
        dataset.Temperatures = records;

        returnDataSets.Add(dataset);
    }

    return returnDataSets;
}

List<DataSet> GetTemperaturesFromFile(DataSetDefinition dataSetDefintion, MeasurementType measurementType, Guid locationId, short? year = null)
{
    var location = Location.GetLocations(dataSetDefintion.FolderName).Single(x => x.Id == locationId);

    var returnDataSets = new List<DataSet>();
    if (measurementType == MeasurementType.Adjusted)
    {
        var temperatures = DataReader.ReadAdjustedTemperatures(dataSetDefintion, location, year);

        if (temperatures != null)
        { 
            var dataSet = new DataSet
            {
                Location = location,
                Resolution = dataSetDefintion.DataResolution,
                Type = measurementType,
                Year = year,
                Temperatures = temperatures
            };

            returnDataSets.Add(dataSet);
        }
    }
    else if (measurementType == MeasurementType.Unadjusted)
    {
        var dataSets = DataReader.ReadRawDataFile(location, year);
        dataSets = dataSets.Where(x => x.Temperatures != null).ToList();
        returnDataSets.AddRange(dataSets);
    }
    else
    {
        throw new ArgumentException(nameof(measurementType));
    }

    return returnDataSets;
}

List<DataSet> GetYearlyTemperaturesFromDaily(DataSetDefinition dataSetDefintion, MeasurementType measurementType, Guid locationId, short? dayGrouping = 14, float? threshold = .8f)
{
    var location = Location.GetLocations(dataSetDefintion.FolderName).Single(x => x.Id == locationId);

    var dailyDataSets = GetTemperaturesFromFile(dataSetDefintion, measurementType, locationId);

    var averagedDataSets = new List<DataSet>();

    foreach (var dailyDataSet in dailyDataSets)
    {
        var yearSets = dailyDataSet.Temperatures.GroupBy(x => x.Year);

        var yearlyAverageRecords = new List<TemperatureRecord>();
        foreach (var yearSet in yearSets)
        {
            var grouping = yearSet.ToList().GroupYearByDays(dayGrouping.Value).ToList();

            var groupingAverages = new List<TemperatureRecord>();

            for (short i = 0; i < grouping.Count(); i++)
            {
                var numberOfDaysInGroup = grouping[i].Count();

                var min = (float)grouping[i].Count(x => x.Min != null) / numberOfDaysInGroup < threshold ? null : grouping[i].Average(x => x.Min);
                var max = (float)grouping[i].Count(x => x.Max != null) / numberOfDaysInGroup < threshold ? null : grouping[i].Average(x => x.Max);
                var record = new TemperatureRecord()
                {
                    Min = min,
                    Max = max,
                };

                groupingAverages.Add(record);
            }

            var yearMin = groupingAverages.Any(x => x.Min == null) ? null : groupingAverages.Average(x => x.Min);
            var yearMax = groupingAverages.Any(x => x.Max == null) ? null : groupingAverages.Average(x => x.Max);

            var yearlyAverageRecord = new TemperatureRecord
            {
                Year = yearSet.Key,
                Min = yearMin,
                Max = yearMax,
            };
            yearlyAverageRecords.Add(yearlyAverageRecord);
        }

        var returnDataSet = new DataSet
        {
            Location = location,
            Resolution = DataResolution.Yearly,
            Type = measurementType,
            Temperatures = yearlyAverageRecords
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
        var temperatures = averagedDataSets.SelectMany(x => x.Temperatures).ToList();
        var yearGrouping = temperatures.GroupBy(x => x.Year).Select(x => x.ToList()).ToList();

        var aggregatedAveragedTemperatures = new List<TemperatureRecord>();
        yearGrouping.ForEach(x =>
        {
            aggregatedAveragedTemperatures.Add(new TemperatureRecord
            {
                Year = x.First().Year,
                Min = x.Where(x => x.Min != null).Average(y => y.Min),
                Max = x.Where(x => x.Max != null).Average(y => y.Max),
            });
        });

        var aggregatedAveragedDataSets = new List<DataSet>();
        var dataSet = new DataSet
        {
            Location = location,
            Resolution = DataResolution.Yearly,
            Type = measurementType,
            Temperatures = aggregatedAveragedTemperatures
        };
        aggregatedAveragedDataSets.Add(dataSet);
        return aggregatedAveragedDataSets;
    }
}

List<ReferenceData> GetCarbonDioxide()
{
    var records = File.ReadAllLines($@"Reference\CO2\co2_annmean_mlo.csv").ToList();
    records = records.Take(new Range(56, records.Count)).ToList();

    var list = new List<ReferenceData>();
    foreach (var record in records)
    {
        var splitRow = record.Split(',');
        list.Add(
            new ReferenceData
            {
                Year = short.Parse(splitRow[0]),
                Value = float.Parse(splitRow[1]),
            }
        );
    }
    return list;
}

List<EnsoMetaData> GetEnsoMetaData()
{
    var ensos = new List<EnsoMetaData>()
    {
        new EnsoMetaData { Index = EnsoIndex.Mei, ElNinoOrientation = 1, Name = "Multivariate ENSO index (MEI)", ShortName = "MEI.v2", FileName = "meiv2.data", Url = "https://psl.noaa.gov/enso/mei/data/meiv2.data" },
        new EnsoMetaData { Index = EnsoIndex.Nino34, ElNinoOrientation = 1, Name = "Niño 3.4", ShortName = "Niño 3.4", FileName = "nino34.long.anom.data.txt", Url ="https://psl.noaa.gov/gcos_wgsp/Timeseries/Data/nino34.long.anom.data" },
        new EnsoMetaData { Index = EnsoIndex.Oni, ElNinoOrientation = 1, Name = "Oceanic Niño Index (ONI)", ShortName = "ONI", FileName = "oni.data.txt", Url ="https://psl.noaa.gov/data/correlation/oni.data" },
        new EnsoMetaData { Index = EnsoIndex.Soi, ElNinoOrientation = -1, Name = "Southern Oscillation Index (SOI)", ShortName = "SOI", FileName = "soi.long.data.txt", Url = "https://psl.noaa.gov/gcos_wgsp/Timeseries/Data/soi.long.data" },
    };

    return ensos;
}

List<ReferenceData> GetEnso(EnsoIndex index, DataResolution resolution, string measure)
{
    List<string> records = null;

    var ensoMetaData = GetEnsoMetaData().Single(x => x.Index == index);

    records = File.ReadAllLines($@"Reference\ENSO\{ensoMetaData.FileName}").ToList();


    var regEx = new Regex(@"^\s*(\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)");

    var list = new List<ReferenceData>();
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
                    new ReferenceData
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
            new ReferenceData
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