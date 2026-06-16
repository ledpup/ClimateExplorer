namespace ClimateExplorer.WebApi;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using Microsoft.AspNetCore.Mvc;

internal static class MetadataEndpoints
{
    public static string GetRoot()
    {
        return
            "Hello, from minimal Climate Explorer Web API!\n" +
            "\n" +
            "Operations:\n" +
            "   GET /about\n" +
            "       Returns basic API metadata\n" +
            "   GET /datasetdefinition\n" +
            "       Returns a list of dataset definitions. (E.g., ACORN-SAT)\n" +
            "   GET /location\n" +
            "       Returns a list of locations.\n" +
            "           Parameters:\n" +
            "               locationId: filter to a particular location by id (still returns an array of location, but max one entry)\n" +
            "   GET /location-by-path\n" +
            "       Returns a single location given it's path ready name\n" +
            "           Parameters:\n" +
            "               path: name of the location that has been passed to the web client. For example: hobart, cape-town-intl, birni-n-konni. These are paths in the sitemap.xml\n" +
            "   GET /country\n" +
            "       Returns a list of countries.\n" +
            "   GET /region\n" +
            "       Returns a list of regions.\n" +
            "   GET /heating-score-table\n" +
            "       A table that records the range of warming anomalies for each heating score.\n" +
            "   GET /climate-record\n" +
            "       Returns a ranked list of climate records at a specific location\n" +
            "           Parameters:\n" +
            "               locationId: location id for the climate records\n" +
            "               dataType: data type to return records for (default: TempMax)\n" +
            "               dataAdjustment: data adjustment to filter by (optional; omit for data types with no adjustment concept)\n" +
            "               ascending: if true returns lowest values first; if false returns highest values first (default: false)\n" +
            "               count: number of records to return (default: 10)\n" +
            "   GET /latest-record\n" +
            "       Returns latest daily records for a supported BOM location\n" +
            "           Parameters:\n" +
            "               locationId: location id for the latest records\n" +
            "               dataType: data type to return records for (default: TempMax)\n" +
            "               isLocationSupported: if true returns support status without downloading records\n" +
            "   POST /dataset\n" +
            "       Returns the specified data set, transformed as requested";
    }

    public static object GetAbout()
    {
        var asm = Assembly.GetExecutingAssembly();

        return
            new
            {
                Version = asm.GetName().Version.ToString(),
                BuildTimeUtc = File.GetLastWriteTimeUtc(asm.Location),
            };
    }

    public static async Task<List<DataSetDefinitionViewModel>> GetDataSetDefinitions()
    {
        var definitions = await DataSetDefinition.GetDataSetDefinitions();

        var dtos =
            definitions
            .Select(
                x =>
                new DataSetDefinitionViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    ShortName = x.ShortName,
                    MoreInformationUrl = x.MoreInformationUrl,
                    StationInfoUrl = x.StationInfoUrl,
                    LocationInfoUrl = x.LocationInfoUrl,
                    Description = x.Description,
                    Publisher = x.Publisher,
                    PublisherUrl = x.PublisherUrl,
                    LocationIds = x.DataLocationMapping?.LocationIdToDataFileMappings.Keys.ToHashSet(),
                    MeasurementDefinitions = x.MeasurementDefinitions.Select(x => x.ToViewModel()).ToList(),
                })
            .ToList();

        return dtos;
    }

    public static async Task<Dictionary<string, string>> GetCountries()
    {
        return (await Country.GetCountries(@"MetaData\countries.txt")).ToDictionary(x => x.Key, x => x.Value.Name);
    }

    public static async Task<IEnumerable<HeatingScoreRow>> GetHeatingScoreTable(
        [FromServices] ClimateExplorerApiServices services)
    {
        var result = await services.LongtermCache.Get<List<HeatingScoreRow>>(ClimateExplorerApiConstants.HeatingScoreTable);
        return result;
    }
}
