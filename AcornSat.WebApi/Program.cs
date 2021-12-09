using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

app.MapGet("/", () => "Hello, from minimal ACORN-SAT Web API!\r\nCall /location for list of locations.\r\nCall /temperature/{locationId} for temperature records at locationId.");
app.MapGet("/location", () => Location.GetLocations(@"Locations.csv"));
app.MapGet("/temperature/{locationId}", (string locationId) => GetTemperatureData(locationId));

app.Run();


List<YearlyAverageTemps> GetTemperatureData(string locationId)
{
    var records = File.ReadAllLines($@"Data\{locationId}.csv")
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