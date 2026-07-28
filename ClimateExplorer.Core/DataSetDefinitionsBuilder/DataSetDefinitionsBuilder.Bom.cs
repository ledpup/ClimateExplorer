namespace ClimateExplorer.Core;

using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public partial class DataSetDefinitionsBuilder
{
    private static List<DataSetDefinition> BuildBomDataSetDefinitions()
    {
        return
        [
            new()
            {
                Id = Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321"),
                Name = "Australian Climate Observations Reference Network - Surface Air Temperature",
                ShortName = "ACORN-SAT",
                Description = "The Australian Climate Observations Reference Network - Surface Air Temperature (ACORN-SAT) data set is a homogenized daily maximum, minimum and mean temperature data set containing data from 112 locations across Australia extending from 1910 to the present.",
                Publisher = "Australian Bureau of Meteorology",
                PublisherUrl = "https://www.bom.gov.au/",
                MoreInformationUrl = "https://www.bom.gov.au/climate/data/acorn-sat/#tabs=ACORN-SAT",
                StationInfoUrl = "https://www.bom.gov.au/climate/averages/tables/cw_[station].shtml",
                LocationInfoUrl = "https://www.bom.gov.au/climate/data/acorn-sat/stations/#/[primaryStation]",
                StationMetadataFileName = "Stations_Australia_unadjusted.json",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMean,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>-?\d*\.?\d*),*$",
                        DataFileSource = ArchiveSource("ACORN-SAT.zip", "daily_tmean/tmean.[station].daily.csv"),
                    },
                    new()
                    {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>-?\d*\.?\d*),*$",
                        DataFileSource = ArchiveSource("ACORN-SAT.zip", "daily_tmax/tmax.[station].daily.csv"),
                    },
                    new()
                    {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>-?\d*\.?\d*),*$",
                        DataFileSource = ArchiveSource("ACORN-SAT.zip", "daily_tmin/tmin.[station].daily.csv"),
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E"),
                Name = "Australian Bureau of Meteorology Climate Data Online",
                ShortName = "BOM-CDO",
                Description = "Climate Data Online (CDO) provides historical daily rainfall and maximum and minimum temperature observations from Bureau of Meteorology weather stations across Australia. The period of record varies by station and weather element, with some observations dating to the mid-1800s. Data are available from both operating and closed stations, and recent observations may not yet have completed quality control.",
                Publisher = "Australian Bureau of Meteorology",
                PublisherUrl = "https://www.bom.gov.au/",
                MoreInformationUrl = "https://www.bom.gov.au/climate/data/",
                StationInfoUrl = "https://www.bom.gov.au/climate/averages/tables/cw_[station].shtml",
                LocationInfoUrl = "https://www.bom.gov.au/climate/data/acorn-sat/stations/#/[primaryStation]",
                DataDownloaderKey = "bom-station",
                StationMetadataFileName = "Stations_Australia_unadjusted.json",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMean,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}),(?<value>.*)$",
                        DataFileSource = ArchiveSource(@"BOM\[station].zip", "[station]_daily_tempmean.csv"),
                    },
                    new()
                    {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*),.*,.*$",
                        DataFileSource = ArchiveSource(@"BOM\[station].zip", "[station]_daily_tempmax.csv"),
                    },
                    new()
                    {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*),.*,.*$",
                        DataFileSource = ArchiveSource(@"BOM\[station].zip", "[station]_daily_tempmin.csv"),
                    },
                    new()
                    {
                        DataType = DataType.Precipitation,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*),.*,.*$",
                        DataFileSource = ArchiveSource(@"BOM\[station].zip", "[station]_daily_rainfall.csv"),
                    },
                    new()
                    {
                        DataType = DataType.SolarRadiation,
                        UnitOfMeasure = UnitOfMeasure.MegajoulesPerSquareMetre,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*)$",
                        DataFileSource = ArchiveSource(@"BOM\[station].zip", "[station]_daily_solarradiation.csv"),
                    },
                ],
            },
        ];
    }
}
