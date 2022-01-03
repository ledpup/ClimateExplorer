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
                        Call /location for list of locations.
                        Call /temperature/Yearly/{temperatureType}/{locationId}?dayGrouping=14&dayGroupingThreshold=.8 for yearly average temperature records at locationId. Records are grouped by dayGrouping. If the number of records in the group does not meet the threshold, the data is considered invalid.");
app.MapGet("/location", () => Location.GetLocations());
app.MapGet("/temperature/{resolution}/{measurementType}/{locationId}", (DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year, short? dayGrouping, float? dayGroupingThreshold) => GetTemperatures(resolution, measurementType, locationId, year, dayGrouping, dayGroupingThreshold));
app.MapGet("/temperature/{resolution}/{measurementType}", (DataResolution resolution, MeasurementType measurementType, float? minLatitude, float? maxLatitude, short dayGrouping, float dayGroupingThreshold, float locationGroupingThreshold) => GetTemperaturesByLatitudeGroups(resolution, measurementType, minLatitude, maxLatitude, dayGrouping, dayGroupingThreshold, locationGroupingThreshold));
app.MapGet("/reference/co2/", () => GetCarbonDioxide());
app.MapGet("/reference/enso/{index}/{resolution}", (EnsoIndex index, DataResolution resolution, string measure) => GetEnso(index, resolution, measure));
app.MapGet("/reference/enso-metadata", () => GetEnsoMetaData());

app.Run();

List<DataSet> GetTemperaturesByLatitudeGroups(DataResolution resolution, MeasurementType measurementType, float? minLatitude, float? maxLatitude, short dayGrouping, float dayGroupingThreshold, float locationGroupingThreshold)
{
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
                var dataSets = YearlyTemperaturesAcrossLocations(locations, measurementType, dayGrouping, dayGroupingThreshold);

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

List<DataSet> YearlyTemperaturesAcrossLocations(List<Location> locations, MeasurementType measurementType, short dayGrouping, float dayGroupingThreshold)
{

    var dataSet = new List<DataSet>();
    Parallel.ForEach(locations, location => {
        var locationDataSet = GetYearlyTemperatures(measurementType, location.Id, dayGrouping, dayGroupingThreshold);
        dataSet.AddRange(locationDataSet);
    });

    return dataSet;
}

List<Location> GetLocationsInLatitudeBand(float? minLatitude, float? maxLatitude)
{
    var locations = Location.GetLocations();

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

List<DataSet> GetTemperatures(DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year, short? dayGrouping, float? dayGroupingThreshold)
{
    switch (resolution)
    {
        case DataResolution.Daily:
            return GetDailyTemperatures(measurementType, locationId, year);
        case DataResolution.Yearly:
            return GetYearlyTemperatures(measurementType, locationId, dayGrouping, dayGroupingThreshold);
        case DataResolution.Weekly:
            return GetAverageTemperatures(DataResolution.Weekly, measurementType, locationId, year.Value, dayGroupingThreshold);
        case DataResolution.Monthly:
            return GetAverageTemperatures(DataResolution.Monthly, measurementType, locationId, year.Value, dayGroupingThreshold);
    }


    throw new NotImplementedException();
}

List<DataSet> GetAverageTemperatures(DataResolution dataResolution, MeasurementType measurementType, Guid locationId, short? year, float? threshold = .8f)
{
    if (threshold < 0 || threshold > 1)
    {
        throw new ArgumentOutOfRangeException("threshold", "Threshold needs to be between 0 and 1.");
    }

    var datasets = GetDailyTemperatures(measurementType, locationId, year.Value);
    var returnDataSets = new List<DataSet>();
    foreach (var dataset in datasets)
    {

        List<IGrouping<short?, TemperatureRecord>> grouping = null;
        if (dataResolution == DataResolution.Weekly)
        {
            grouping = dataset.Temperatures.GroupByWeek().ToList();
        }
        else if (dataResolution == DataResolution.Monthly)
        {
            grouping = dataset.Temperatures.GroupByMonth().ToList();
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

List<DataSet> GetDailyTemperatures(MeasurementType measurementType, Guid locationId, short? year = null)
{
    var location = Location.GetLocations().Single(x => x.Id == locationId);

    List<DataSet> returnDataSets;
    if (measurementType == MeasurementType.Adjusted)
    {
        var temperatures = AcornSat.Core.InputOutput.AcornSat.ReadAdjustedTemperatures(location, year);

        var dataSet = new DataSet
        {
            Location = location,
            Resolution = DataResolution.Daily,
            Type = MeasurementType.Adjusted,
            Year = year,
            Temperatures = temperatures
        };

        returnDataSets = new List<DataSet> { dataSet };
    }
    else if (measurementType == MeasurementType.Unadjusted)
    {
        returnDataSets = AcornSat.Core.InputOutput.AcornSat.ReadRawDataFile(location, year);
    }
    else
    {
        throw new ArgumentException(nameof(measurementType));
    }
    
    return returnDataSets;
}

List<DataSet> GetYearlyTemperatures(MeasurementType measurementType, Guid locationId, short? dayGrouping = 14, float? threshold = .8f)
{
    var location = Location.GetLocations().Single(x => x.Id == locationId);

    var dailyDataSets = GetDailyTemperatures(measurementType, locationId);

    var returnDataSets = new List<DataSet>();

    foreach (var dailyDataSet in dailyDataSets)
    {
        var yearSets = dailyDataSet.Temperatures.GroupBy(x => x.Year);

        var yearlyAverageRecords = new List<TemperatureRecord>();
        foreach (var yearSet in yearSets)
        {
            var grouping = yearSet.ToList().GroupByDays(dayGrouping.Value).ToList();

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

        var dataSet = new DataSet
        {
            Location = location,
            Resolution = DataResolution.Yearly,
            Type = measurementType,
            Temperatures = yearlyAverageRecords
        };
        returnDataSets.Add(dataSet);
    }

    return returnDataSets;
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