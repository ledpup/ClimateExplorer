using AcornSat.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
                        Call /temperature/{temperatureType}/{locationId} for temperature records at locationId.");
app.MapGet("/location", () => Location.GetLocations(@"Locations.json"));
app.MapGet("/temperature/{temperatureType}/{locationId}", (string temperatureType, string locationId) => GetTemperatureData(temperatureType, locationId));
app.MapGet("/reference/co2/", () => GetCarbonDioxide());
app.MapGet("/reference/meiv2/{measure}", (string measure) => GetMeiV2(measure));

app.Run();

List<YearlyAverageTemps> GetTemperatureData(string temperatureType, string locationId)
{
    var records = File.ReadAllLines($@"Temperature\{temperatureType}\{locationId}.csv")
        .ToList();

    records = records.Take(new Range(1, records.Count)).ToList();

    var list = new List<YearlyAverageTemps>();

    foreach (var record in records)
    {
        var splitRow = record.Split(',');

        list.Add(
            new YearlyAverageTemps
            {
                Year = int.Parse(splitRow[0]),
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
                Year = int.Parse(splitRow[0]),
                Value = float.Parse(splitRow[1]),
            }
        );
    }
    return list;
}

List<ReferenceData> GetMeiV2(string measure)
{
    var records = File.ReadAllLines($@"Reference\meiv2.data").ToList();
    records = records.Take(new Range(1, 44)).ToList();

    var regEx = new Regex(@"^(\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)");

    var list = new List<ReferenceData>();
    foreach (var record in records)
    {
        var groups = regEx.Match(record).Groups;

        var values = new List<float>();
        for (var i = 2; i < groups.Count; i++)
        {
            if (groups[i].Value != "-999.00")
            {
                var value = float.Parse(groups[i].Value);
                values.Add(value);
            }
        }

        list.Add(
            new ReferenceData
            {
                Year = int.Parse(groups[1].Value),
                Value = measure == "median" 
                                        ? values.Median() 
                                        : (measure == "mode" ? values.Mode() : values.Average()),
            }
        );
    }
    
    return list;
}