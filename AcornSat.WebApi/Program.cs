using AcornSat.Analyser.Io;
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
                        Call /temperature/yearly/{temperatureType}/{locationId} for yearly temperature records at locationId.");
app.MapGet("/location", () => Location.GetLocations(@"Locations.json"));
app.MapGet("/temperature/{resolution}/{measurementType}/{locationId}", (DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year, short? threshold) => GetTemperatures(resolution, measurementType, locationId, year, threshold));
app.MapGet("/reference/co2/", () => GetCarbonDioxide());
app.MapGet("/reference/enso/{index}/{resolution}", (EnsoIndex index, DataResolution resolution, string measure) => GetEnso(index, resolution, measure));
app.MapGet("/reference/enso-metadata", () => GetEnsoMetaData());

app.Run();

List<DataSet> GetTemperatures(DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year, short? threshold)
{
    switch (resolution)
    {
        case DataResolution.Daily:
            return GetDailyTemperatures(measurementType, locationId, year);
        case DataResolution.Yearly:
            return GetYearlyTemperatures(measurementType, locationId);
        case DataResolution.Weekly:
            return GetAverageTemperatures(DataResolution.Weekly, measurementType, locationId, year.Value, threshold);
        case DataResolution.Monthly:
            return GetAverageTemperatures(DataResolution.Monthly, measurementType, locationId, year.Value, threshold);
    }


    throw new NotImplementedException();
}

List<DataSet> GetAverageTemperatures(DataResolution dataResolution, MeasurementType measurementType, Guid locationId, short? year, short? threshold = 10)
{
    switch (dataResolution)
    {
        case DataResolution.Weekly:
            if (threshold < 1 || threshold > 7)
            {
                throw new ArgumentOutOfRangeException("threshold", "Threshold needs to be between 1 and 7.");
            }
            break;
        case DataResolution.Monthly:
            if (threshold < 1 || threshold > 31)
            {
                throw new ArgumentOutOfRangeException("threshold", "Threshold needs to be between 1 and 31.");
            }
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(dataResolution));
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
        else
        {
            grouping = dataset.Temperatures.GroupByMonth().ToList();
        }
        var records = new List<TemperatureRecord>();

        for (short i = 0; i < grouping.Count(); i++)
        {
            var min = grouping[i].Count(x => x.Min != null) < threshold ? null : grouping[i].Average(x => x.Min);
            var max = grouping[i].Count(x => x.Max != null) < threshold ? null : grouping[i].Average(x => x.Max);
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
            else
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

List<DataSet> GetDailyTemperatures(MeasurementType measurementType, Guid locationId, short? year)
{
    var location = Location.GetLocations(@"Locations.json").Single(x => x.Id == locationId);
    if (measurementType == MeasurementType.Adjusted)
    {
        var temperatures = AcornSatIo.ReadAdjustedTemperatures(location, year);

        var dataSet = new DataSet
        {
            Location = location,
            Resolution = DataResolution.Daily,
            Type = MeasurementType.Adjusted,
            Year = year,
            Temperatures = temperatures
        };

        return new List<DataSet> { dataSet };
    }
    else if (measurementType == MeasurementType.Unadjusted)
    {
        return AcornSatIo.ReadRawDataFile(location, year);
    }

    throw new ArgumentException(nameof(measurementType));
}

List<DataSet> GetYearlyTemperatures(MeasurementType measurementType, Guid locationId)
{
    var location = Location.GetLocations(@"Locations.json").Single(x => x.Id == locationId);

    var records = File.ReadAllLines($@"Temperature\Yearly\{measurementType}\{locationId}.csv")
        .ToList();

    records = records.Take(new Range(1, records.Count)).ToList();

    var temperatures = new List<TemperatureRecord>();

    foreach (var record in records)
    {
        var splitRow = record.Split(',');

        temperatures.Add(
            new TemperatureRecord
            {
                Year = short.Parse(splitRow[0]),
                Min = string.IsNullOrWhiteSpace(splitRow[1]) ? null : float.Parse(splitRow[1]),
                Max = string.IsNullOrWhiteSpace(splitRow[2]) ? null : float.Parse(splitRow[2]),
            }
        );
    }

    var dataSet = new DataSet
    {
        Location = location,
        Resolution = DataResolution.Yearly,
        Type = measurementType,
        Temperatures = temperatures
    };

    return new List<DataSet> { dataSet };
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