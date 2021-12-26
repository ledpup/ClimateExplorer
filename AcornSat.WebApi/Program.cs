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
app.MapGet("/temperature/yearly/{measurementType}/{locationId}/", (MeasurementType measurementType, Guid locationId) => GetYearlyTemperatures(measurementType, locationId));
app.MapGet("/temperature/daily/{measurementType}/{locationId}/{year}", (MeasurementType measurementType, Guid locationId, int year) => GetDailyTemperatures(measurementType, locationId, year));
app.MapGet("/reference/co2/", () => GetCarbonDioxide());
app.MapGet("/reference/enso/{index}/{resolution}", (EnsoIndex index, DataResolution resolution, string measure) => GetEnso(index, resolution, measure));
app.MapGet("/reference/enso-metadata", () => GetEnsoMetaData());

app.Run();

List<DailyTemperatureRecord> GetDailyTemperatures(MeasurementType measurementType, Guid locationId, int year)
{
    var location = Location.GetLocations(@"Locations.json").Single(x => x.Id == locationId);
    if (measurementType == MeasurementType.Adjusted)
    {
        return AcornSatIo.ReadAdjustedTemperatures(location, year);
    }
    else if (measurementType == MeasurementType.Unadjusted)
    {
        return AcornSatIo.ReadAdjustedTemperatures(location, year);
    }

    throw new ArgumentException(nameof(measurementType));
}

List<YearlyAverageTemps> GetYearlyTemperatures(MeasurementType measurementType, Guid locationId)
{
    var records = File.ReadAllLines($@"Temperature\Yearly\{measurementType}\{locationId}.csv")
        .ToList();

    records = records.Take(new Range(1, records.Count)).ToList();

    var list = new List<YearlyAverageTemps>();

    foreach (var record in records)
    {
        var splitRow = record.Split(',');

        list.Add(
            new YearlyAverageTemps
            {
                Year = short.Parse(splitRow[0]),
                Min = string.IsNullOrWhiteSpace(splitRow[1]) ? null : double.Parse(splitRow[1]),
                Max = string.IsNullOrWhiteSpace(splitRow[2]) ? null : double.Parse(splitRow[2]),
            }
        );
    }

    return list;
}

List<ReferenceData> GetCarbonDioxide()
{
    var records = File.ReadAllLines($@"Reference\co2_annmean_mlo.csv").ToList();
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
        new EnsoMetaData { Index = EnsoIndex.Mei, ElNinoOrientation = 1, Name = "MEI.v2", Url = "https://psl.noaa.gov/enso/mei/data/meiv2.data" },
        new EnsoMetaData { Index = EnsoIndex.Oni, ElNinoOrientation = 1, Name = "ONI", Url ="https://psl.noaa.gov/data/correlation/oni.data" },
        new EnsoMetaData { Index = EnsoIndex.Soi, ElNinoOrientation = -1, Name = "OSI", Url = "https://psl.noaa.gov/gcos_wgsp/Timeseries/Data/soi.long.data" },
    };

    return ensos;
}

List<ReferenceData> GetEnso(EnsoIndex index, DataResolution resolution, string measure)
{
    List<string> records = null;
    switch (index)
    {
        case EnsoIndex.Mei:
            records = File.ReadAllLines($@"Reference\ENSO\meiv2.data").ToList();
            break;
        case EnsoIndex.Oni:
            records = File.ReadAllLines($@"Reference\ENSO\oni.data.txt").ToList();
            break;
        case EnsoIndex.Soi:
            records = File.ReadAllLines($@"Reference\ENSO\soi.long.data.txt").ToList();
            break;
    }
    records = records.ToList();

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