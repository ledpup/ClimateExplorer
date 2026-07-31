namespace ClimateExplorer.Core;

using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public partial class DataSetDefinitionsBuilder
{
    private static List<DataSetDefinition> BuildGhcnDataSetDefinitions()
    {
        return
        [
            new()
            {
                Id = Guid.Parse("1DC38F20-3606-4D90-A2A0-84F93E75C964"),
                Name = "Global Historical Climatology Network monthly (GHCNm)",
                ShortName = "GHCNm",
                Description = "The Global Historical Climatology Network monthly (GHCNm) dataset provides monthly climate summaries from thousands of weather stations around the world. The initial version was developed in the early 1990s, and subsequent iterations were released in 1997, 2011, and most recently in 2018. The period of record for each summary varies by station, with the earliest observations dating to the 18th century. Some station records are purely historical and are no longer updated, but many others are still operational and provide short time delay updates that are useful for climate monitoring.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                PublisherUrl = "https://www.noaa.gov/",
                PublisherDivision = "Global Historical Climatology Network",
                MoreInformationUrl = "https://www.ncei.noaa.gov/products/land-based-station/global-historical-climatology-network-monthly",
                StationMetadataFileName = "Stations_ghcnm_adjusted.json",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMean,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Monthly,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        DataRowRegEx = @"^(?<year>\d{4}),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+)$",
                        DataFileSource = ArchiveSource("GHCNm.zip", "Temperature/Adjusted/[station].csv"),
                        NullValue = "-9999",
                        ValueAdjustment = 100.0f,
                    },
                    new()
                    {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMean,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Monthly,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        DataRowRegEx = @"^(?<year>\d{4}),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+)$",
                        DataFileSource = ArchiveSource("GHCNm.zip", "Temperature/Unadjusted/[station].csv"),
                        NullValue = "-9999",
                        ValueAdjustment = 100.0f,
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("6ABB028A-29F6-481C-837E-1FC9C8E989AF"),
                Name = "Global Historical Climatology Network (GHCN) Monthly Precipitation",
                ShortName = "GHCNmp",
                Description = "The Global Historical Climatology Network (GHCN) Monthly Precipitation, Version 4 is a collection of worldwide monthly precipitation values offering significant enhancement over the previous version 2.  It contains more values both historically and for the most recent months.  Its methods for merging records and quality control have been modernized.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                PublisherUrl = "https://www.noaa.gov/",
                PublisherDivision = "Global Historical Climatology Network",
                MoreInformationUrl = "https://www.ncei.noaa.gov/products/land-based-station/global-historical-climatology-network-monthly#tab-800",
                StationMetadataFileName = "Stations_ghcnm_adjusted.json",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataAdjustment = null,
                        DataType = DataType.Precipitation,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Monthly,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        DataRowRegEx = @"^(?<year>\d{4}),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+)$",
                        DataFileSource = ArchiveSource("GHCNm.zip", "Precipitation/[station].csv"),
                        NullValue = "-9999",
                        ValueAdjustment = 10.0f,
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("87C65C34-C689-4BA1-8061-626E4A63D401"),
                Name = "Global Historical Climatology Network daily (GHCNd)",
                ShortName = "GHCNd",
                Description = "The Global Historical Climatology Network daily (GHCNd) is an integrated database of daily climate summaries from land surface stations across the globe. GHCNd is made up of daily climate records from numerous sources that have been integrated and subjected to a common suite of quality assurance reviews.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                PublisherUrl = "https://www.noaa.gov/",
                PublisherDivision = "Global Historical Climatology Network",
                MoreInformationUrl = "https://www.ncei.noaa.gov/products/land-based-station/global-historical-climatology-network-daily",
                DataDownloadUrl = "https://www.ncei.noaa.gov/data/global-historical-climatology-network-daily/access/[station].csv",
                DataDownloaderKey = "ghcnd-station",
                StationMetadataFileName = "Stations_ghcnm_adjusted.json",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        RowDataType = RowDataType.OneValuePerRow,
                        DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}),(?<value>-?[\d+\.\d+]*),(?<tmin>-?[\d+\.\d+]*)$",
                        DataFileSource = ArchiveSource(@"GHCNd\[station].zip", "Temperature/[station].csv"),
                        NullValue = "9999",
                        ValueAdjustment = 10.0f,
                    },
                    new()
                    {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        RowDataType = RowDataType.OneValuePerRow,
                        DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}),(?<tmax>-?[\d+\.\d+]*),(?<value>-?[\d+\.\d+]*)$",
                        DataFileSource = ArchiveSource(@"GHCNd\[station].zip", "Temperature/[station].csv"),
                        NullValue = "9999",
                        ValueAdjustment = 10.0f,
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("5BBEAF4C-B459-410E-9B77-470905CB1E46"),
                Name = "Global Historical Climatology Network daily (GHCNd) precipitation",
                ShortName = "GHCNdp",
                Description = "The Global Historical Climatology Network daily (GHCNd) is an integrated database of daily climate summaries from land surface stations across the globe. GHCNd is made up of daily climate records from numerous sources that have been integrated and subjected to a common suite of quality assurance reviews.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                PublisherUrl = "https://www.noaa.gov/",
                PublisherDivision = "Global Historical Climatology Network",
                MoreInformationUrl = "https://www.ncei.noaa.gov/products/land-based-station/global-historical-climatology-network-daily",
                DataDownloadUrl = "https://www.ncei.noaa.gov/data/global-historical-climatology-network-daily/access/[station].csv",
                DataDownloaderKey = "ghcnd-station",
                StationMetadataFileName = "Stations_ghcnm_adjusted.json",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataAdjustment = null,
                        DataType = DataType.Precipitation,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Daily,
                        RowDataType = RowDataType.OneValuePerRow,
                        DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}),(?<value>[\d+\.\d+]*)$",
                        DataFileSource = ArchiveSource(@"GHCNd\[station].zip", "Precipitation/[station].csv"),
                        NullValue = "9999",
                        ValueAdjustment = 10.0f,
                    },
                ],
            },
        ];
    }
}
