using ClimateExplorer.Core.Model;
using System.Text.Json;
using System.Text.Json.Serialization;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Data.Misc;

internal class DataSetDefinitionsBuilder
{
    public static List<DataSetDefinition> BuildDataSetDefinitions()
    {
        var dataSetDefinitions = new List<DataSetDefinition>
        {
            new() {
                Id = Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321"),
                Name = "ACORN-SAT",
                Description = "The Australian Climate Observations Reference Network - Surface Air Temperature data set is a homogenized daily maximum and minimum temperature data set containing data from 112 locations across Australia extending from 1910 to the present.",
                Publisher = "Australian Bureau of Meteorology",
                PublisherUrl = "http://www.bom.gov.au/",
                MoreInformationUrl = "http://www.bom.gov.au/climate/data/acorn-sat/#tabs=Data-and-networks",
                StationInfoUrl = "http://www.bom.gov.au/climate/averages/tables/cw_[station].shtml",
                LocationInfoUrl = "http://www.bom.gov.au/climate/data/acorn-sat/stations/#/[primaryStation]",
                MeasurementDefinitions =
                [
                    new() {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>-?\d*\.?\d*),*$",
                        FolderName = @"Temperature\ACORN-SAT\daily_tmax",
                        FileNameFormat = "tmax.[station].daily.csv",
                    },
                    new() {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>-?\d*\.?\d*),*$",
                        FolderName = @"Temperature\ACORN-SAT\daily_tmin",
                        FileNameFormat = "tmin.[station].daily.csv",
                    },
                ],
            },
            new() {
                Id = Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E"),
                Name = "Bureau of Meteorology",
                Description = null,
                Publisher = "Australian Bureau of Meteorology",
                PublisherUrl = "http://www.bom.gov.au/",
                //MoreInformationUrl = "http://www.bom.gov.au/climate/data/acorn-sat/#tabs=Data-and-networks",
                StationInfoUrl = "http://www.bom.gov.au/climate/averages/tables/cw_[station].shtml",
                LocationInfoUrl = "http://www.bom.gov.au/climate/data/acorn-sat/stations/#/[primaryStation]",
                MeasurementDefinitions =
                [
                    new() {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*),.*,.*$",
                        FolderName = @"Temperature\BOM\daily_tempmax",
                        FileNameFormat = "[station]_daily_tempmax.csv",
                    },
                    new() {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*),.*,.*$",
                        FolderName = @"Temperature\BOM\daily_tempmin",
                        FileNameFormat = "[station]_daily_tempmin.csv",
                    },
                    new() {
                        DataType = DataType.Precipitation,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*),.*,.*$",
                        FolderName = @"Precipitation\BOM",
                        FileNameFormat = "[station]_daily_rainfall.csv",
                    },
                    new() {
                        DataType = DataType.SolarRadiation,
                        UnitOfMeasure = UnitOfMeasure.MegajoulesPerSquareMetre,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*)$",
                        FolderName = @"Solar\BOM",
                        FileNameFormat = "[station]_daily_solarradiation.csv",
                    },
                ],
            },
            new() {
                Id = Guid.Parse("647b6a05-43e4-48e0-a43e-04ae81a74653"),
                Name = "RAIA",
                Description = "This ACORN-SAT dataset includes homogenised monthly data from the Remote Australian Islands and Antarctica network of 8 locations, which provide ground-based temperature records.",
                Publisher = "Australian Bureau of Meteorology",
                PublisherUrl = "http://www.bom.gov.au/",
                MoreInformationUrl = "http://www.bom.gov.au/climate/data/acorn-sat/#tabs=Data-and-networks",
                StationInfoUrl = "http://www.bom.gov.au/climate/averages/tables/cw_[station].shtml",
                MeasurementDefinitions =
                [
                    new() {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Monthly,
                        DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$",
                        FolderName = @"Temperature\RAIA\Monthly\adjusted\maxT",
                        FileNameFormat = "acorn.ria.maxT.[station].monthly.txt",
                        NullValue = "99999.9",
                    },
                    new() {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Monthly,
                        DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$",
                        FolderName = @"Temperature\RAIA\Monthly\adjusted\minT",
                        FileNameFormat = "acorn.ria.minT.[station].monthly.txt",
                        NullValue = "99999.9",
                    },
                ],
            },




            new() {
                Id = Guid.Parse("7522E8EC-E743-4CB0-BC65-6E9F202FC824"),
                Name = "NIWA 7-stations series adjusted",
                Description = "NIWA's long-running 'seven-station' series shows NZ's average annual temperature has increased by about 1 °C over the past 100 years.",
                Publisher = "National Institute of Water and Atmospheric Research (NIWA)",
                PublisherUrl = "http://www.niwa.co.nz/",
                MoreInformationUrl = "https://niwa.co.nz/seven-stations",
                MeasurementDefinitions =
                [
                    new() {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$",
                        FolderName = @"Temperature\NIWA",
                        FileNameFormat = "[station]_temperature.csv",
                        NullValue = "-",
                    },
                    new() {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<tmax>-?[\d+\.\d+]*),-?\d*,(?<value>-?[\d+\.\d+]*),-?\d*,.*,D$",
                        FolderName = @"Temperature\NIWA",
                        FileNameFormat = "[station]_temperature.csv",
                        NullValue = "-",
                    },
                ],
            },
            new() {
                Id = Guid.Parse("534950DC-EDA4-4DB5-8816-3705358F1797"),
                Name = "NIWA 7-stations series unadjusted",
                Description = "NIWA's long-running 'seven-station' series shows NZ's average annual temperature has increased by about 1 °C over the past 100 years.",
                Publisher = "National Institute of Water and Atmospheric Research (NIWA)",
                PublisherUrl = "http://www.niwa.co.nz/",
                MoreInformationUrl = "https://niwa.co.nz/seven-stations",
                MeasurementDefinitions =
                [
                    new() {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$",
                        FolderName = @"Temperature\NIWA",
                        FileNameFormat = "[station]_temperature.csv",
                        NullValue = "-",
                    },
                    new() {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<tmax>-?[\d+\.\d+]*),-?\d*,(?<value>-?[\d+\.\d+]*),-?\d*,.*,D$",
                        FolderName = @"Temperature\NIWA",
                        FileNameFormat = "[station]_temperature.csv",
                        NullValue = "-",
                    },
                    new() {
                        DataType = DataType.Precipitation,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),.*,D$",
                        FolderName = @"Precipitation\NIWA",
                        FileNameFormat = "[station]_rainfall.csv",
                        NullValue = "-",
                    },
                ],
            },




            new() {
                Id = Guid.Parse("88e52edd-3c67-484a-b614-91070037d47a"),
                Name = "NIWA 11-stations series",
                Description = "The National Institute of Water and Atmospheric Research (NIWA) eleven-station series are New Zealand temperature trends from a set of eleven climate stations with no significant site changes since the 1930s.",
                Publisher = "National Institute of Water and Atmospheric Research (NIWA)",
                PublisherUrl = "http://www.niwa.co.nz/",
                MoreInformationUrl = "https://niwa.co.nz/our-science/climate/information-and-resources/nz-temp-record/temperature-trends-from-raw-data",
                MeasurementDefinitions =
                [
                    new() {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$",
                        FolderName = @"Temperature\NIWA",
                        FileNameFormat = "[station]_temperature.csv",
                        NullValue = "-",
                    },
                    new() {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<tmax>-?[\d+\.\d+]*),-?\d*,(?<value>-?[\d+\.\d+]*),-?\d*,.*,D$",
                        FolderName = @"Temperature\NIWA",
                        FileNameFormat = "[station]_temperature.csv",
                        NullValue = "-",
                    },
                    new() {
                        DataType = DataType.Precipitation,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),.*,D$",
                        FolderName = @"Precipitation\NIWA",
                        FileNameFormat = "[station]_rainfall.csv",
                        NullValue = "-",
                    },
                ],
            },


            new() {
                Id = Guid.Parse("1DC38F20-3606-4D90-A2A0-84F93E75C964"),
                Name = "Global Historical Climatology Network monthly (GHCNm)",
                Description = "The Global Historical Climatology Network monthly (GHCNm) dataset provides monthly climate summaries from thousands of weather stations around the world. The initial version was developed in the early 1990s, and subsequent iterations were released in 1997, 2011, and most recently in 2018. The period of record for each summary varies by station, with the earliest observations dating to the 18th century. Some station records are purely historical and are no longer updated, but many others are still operational and provide short time delay updates that are useful for climate monitoring.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                PublisherUrl = "https://www.noaa.gov/",
                MoreInformationUrl = "https://www.ncei.noaa.gov/products/land-based-station/global-historical-climatology-network-monthly",
                MeasurementDefinitions =
                [
                    new() {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMean,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Monthly,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        DataRowRegEx = @"^(?<year>\d{4}),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+)$",
                        FolderName = @"Temperature\GHCNm\Adjusted",
                        FileNameFormat = "[station].csv",
                        NullValue = "-9999",
                        ValueAdjustment = 100.0f,
                    },
                    new() {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMean,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Monthly,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        DataRowRegEx = @"^(?<year>\d{4}),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+)$",
                        FolderName = @"Temperature\GHCNm\Unadjusted",
                        FileNameFormat = "[station].csv",
                        NullValue = "-9999",
                        ValueAdjustment = 100.0f,
                    },
                ],
            },

            new() {
                Id = Guid.Parse("6ABB028A-29F6-481C-837E-1FC9C8E989AF"),
                Name = "Global Historical Climatology Network (GHCN) Monthly Precipitation",
                Description = "The Global Historical Climatology Network (GHCN) Monthly Precipitation, Version 4 is a collection of worldwide monthly precipitation values offering significant enhancement over the previous version 2.  It contains more values both historically and for the most recent months.  Its methods for merging records and quality control have been modernized.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                PublisherUrl = "https://www.noaa.gov/",
                MoreInformationUrl = "https://www.ncei.noaa.gov/products/land-based-station/global-historical-climatology-network-monthly#tab-800",
                MeasurementDefinitions =
                [
                    new() {
                        DataAdjustment = null,
                        DataType = DataType.Precipitation,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Monthly,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        DataRowRegEx = @"^(?<year>\d{4}),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+),(-?\d+)$",
                        FolderName = @"Precipitation\GHCNm",
                        FileNameFormat = "[station].csv",
                        NullValue = "-9999",
                        ValueAdjustment = 10.0f,
                    },
                ]
            },

            new() {
                Id = Guid.Parse("ffd5f5e2-d8df-4779-a7f4-f5d148505033"),
                Name = "Multivariate ENSO index (MEI)",
                ShortName = "MEI.v2",
                Description = "The MEI combines both oceanic and atmospheric variables to form a single index assessment of ENSO. It is an Empirical Orthogonal Function (EOF) of five different variables (sea level pressure (SLP), sea surface temperature (SST), zonal and meridional components of the surface wind, and outgoing longwave radiation (OLR)) over the tropical Pacific basin (30°S-30°N and 100°E-70°W).",
                Publisher = "US National Oceanic and Atmospheric Administration's Physical Sciences Laboratory (NOAA PSL)",
                PublisherUrl = "https://psl.noaa.gov/",
                MoreInformationUrl = "https://psl.noaa.gov/enso/mei/",
                MeasurementDefinitions =
                [
                    new() {
                        DataType = DataType.MEIv2,
                        UnitOfMeasure = UnitOfMeasure.EnsoIndex,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        FolderName = @"Ocean\ENSO",
                        FileNameFormat = "meiv2.data.txt",
                        DataRowRegEx = @"^\s*(?<year>\d{4})\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                        NullValue = "-999.00"
                    },
                ],
                DataDownloadUrl = "https://psl.noaa.gov/enso/mei/data/meiv2.data",
            },
            new() {
                Id = Guid.Parse("c31270fa-b207-4d8f-b68e-4995698f1a4d"),
                Name = "Southern Oscillation Index (SOI)",
                ShortName = "SOI",
                Description = @"The Southern Oscillation Index is calculated based on atmospheric pressure difference between Tahiti and Darwin. Higher values of the SOI indicate that Tahiti has higher atmospheric pressure, relative to its typical value, than Darwin does relative to its typical value.

                    High values of the SOI correlate with cold waters in the eastern tropical Pacific.",
                MoreInformationUrl = "https://www.ncdc.noaa.gov/teleconnections/enso/soi",
                MeasurementDefinitions =
                [
                    new() {
                        DataType = DataType.SOI,
                        UnitOfMeasure = UnitOfMeasure.EnsoIndex,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        FolderName = @"Ocean\ENSO",
                        FileNameFormat = "soi.long.data.txt",
                        DataRowRegEx = @"^\s*(?<year>\d{4})\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                        NullValue = "-99.99"
                    },
                ],
                DataDownloadUrl = "https://psl.noaa.gov/gcos_wgsp/Timeseries/Data/soi.long.data",
            },
            new() {
                Id = Guid.Parse("1042147a-8625-4ee7-bb5a-f0f17795c393"),
                Name = "Oceanic Niño Index (ONI)",
                ShortName = "ONI",
                Description = @"The Oceanic Niño Index (ONI) is calculated from 3-month running averages of sea surface temperature measurements from the same area as Niño 3.4 (around the equator in the East Pacific, 5 deg N to 5 deg C, 170 deg W to 120 deg W), and then expressed as an anomaly (i.e. difference from a 30 year rolling average).

                    ONI conditions of +0.5 deg C or higher are considered El Niño, and -0.5 deg C or lower are considered La Niña. El Niño or La Niña conditions must prevail for at least five consecutive months to be considered an El Niño or La Niña event.",
                MoreInformationUrl = "https://www.climate.gov/news-features/understanding-climate/climate-variability-oceanic-ni%C3%B1o-index",
                MeasurementDefinitions =
                [
                    new() {
                        DataType = DataType.ONI,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsiusAnomaly,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        FolderName = @"Ocean\ENSO",
                        FileNameFormat = "oni.data.txt",
                        DataRowRegEx = @"^\s*(?<year>\d{4})\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                        NullValue = "-99.9"
                    },
                ],
                DataDownloadUrl = "https://psl.noaa.gov/data/correlation/oni.data",
            },
            new() {
                Id = Guid.Parse("bfbaa69b-c10d-4de3-a78c-1ed6ff307327"),
                Name = "Niño 3.4",
                ShortName = "Niño 3.4",
                Description = @"The Niño 3.4 index is calculated as a 3-month running average of sea surface temperature measurements around the equator in the East Pacific (5 deg N to 5 deg C, 170 deg W to 120 deg W), and then expressed as an anomaly (i.e. difference from the average).

                    Niño 3.4 conditions of +0.4 deg C or higher are considered El Niño, and -0.4 deg C or lower are considered La Niña.",
                MoreInformationUrl = "https://climatedataguide.ucar.edu/climate-data/nino-sst-indices-nino-12-3-34-4-oni-and-tni",
                MeasurementDefinitions =
                [
                    new() {
                        DataType = DataType.Nino34,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsiusAnomaly,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        FolderName = @"Ocean\ENSO",
                        FileNameFormat = "nino34.long.anom.data.txt",
                        DataRowRegEx = @"^\s*(?<year>\d{4})\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                        NullValue = "-99.99"
                    },
                ],
                DataDownloadUrl = "https://psl.noaa.gov/gcos_wgsp/Timeseries/Data/nino34.long.anom.data",
            },





            new() {
                Id = Guid.Parse("42c9195e-edc0-4894-97dc-923f9d5e72f0"),
                Name = "Carbon dioxide (CO₂) from the Mauna Loa Observatory",
                ShortName = "Carbon Dioxide (CO₂)",
                Description = "The carbon dioxide data on Mauna Loa constitute the longest record of direct measurements of CO2 in the atmosphere. They were started by C. David Keeling of the Scripps Institution of Oceanography in March of 1958 at a facility of the National Oceanic and Atmospheric Administration. NOAA started its own CO2 measurements in May of 1974, and they have run in parallel with those made by Scripps since then.",
                MoreInformationUrl = "https://gml.noaa.gov/ccgg/trends/mlo.html",
                DataDownloadUrl = "https://gml.noaa.gov/webdata/ccgg/trends/co2/co2_mm_mlo.txt",
                MeasurementDefinitions =
                [
                    new() {
                        DataType = DataType.CO2,
                        UnitOfMeasure = UnitOfMeasure.PartsPerMillion,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^\s+(?<year>\d+)\s+(?<month>\d+)\s+(?<decimalDate>\d+\.\d+)\s+(?<value>\d+\.\d+).*$",
                        FolderName = @"Atmosphere",
                        FileNameFormat = "co2_mm_mlo.txt",
                    },
                ],
            },
            new() {
                Id = Guid.Parse("2debe203-cbaa-4015-977c-2f40e2782547"),
                Name = "Methane (CH₄) from a globally distributed network",
                ShortName = "Methane (CH₄)",
                Description = "The Global Monitoring Division of NOAA's Earth System Research Laboratory has measured methane since 1983 at a globally distributed network of air sampling sites. A global average is constructed by first smoothing the data for each site as a function of time, and then smoothed values for each site are plotted as a function of latitude for 48 equal time steps per year. Global means are calculated from the latitude plot at each time step.",
                MoreInformationUrl = "https://gml.noaa.gov/ccgg/trends_ch4/",
                DataDownloadUrl = "https://gml.noaa.gov/webdata/ccgg/trends/ch4/ch4_mm_gl.txt",
                MeasurementDefinitions =
                [
                    new() {
                        DataType = DataType.CH4,
                        UnitOfMeasure = UnitOfMeasure.PartsPerBillion,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^\s+(?<year>\d+)\s+(?<month>\d+)\s+(?<decimalDate>\d+\.\d+)\s+(?<value>\d+\.\d+).*$",
                        FolderName = @"Atmosphere",
                        FileNameFormat = "ch4_mm_gl.txt",
                    },
                ],
            },
            new() {
                Id = Guid.Parse("6e84e743-3c77-488f-8a1c-152306c3d6f0"),
                Name = "Nitrous oxide (N₂O) from a globally distributed network",
                ShortName = "N₂O",
                Description = "The Global Monitoring Division of NOAA's Earth System Research Laboratory has measured nitrous oxide since 2001 at a globally distributed network of air sampling sites. A global average is constructed by first smoothing the data for each site as a function of time, and then smoothed values for each site are fitted as a function of latitude at 48 equally-spaced time steps per year. Global means are calculated from the latitude fits at each time step.",
                MoreInformationUrl = "https://gml.noaa.gov/ccgg/trends_n2o/",
                DataDownloadUrl = "https://gml.noaa.gov/webdata/ccgg/trends/n2o/n2o_mm_gl.txt",
                MeasurementDefinitions =
                [
                    new() {
                        DataType = DataType.N2O,
                        UnitOfMeasure = UnitOfMeasure.PartsPerBillion,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^\s+(?<year>\d+)\s+(?<month>\d+)\s+(?<decimalDate>\d+\.\d+)\s+(?<value>\d+\.\d+).*$",
                        FolderName = @"Atmosphere",
                        FileNameFormat = "n2o_mm_gl.txt",
                    },
                ],
            },





            new() {
                Id = Guid.Parse("a3841b12-2dd4-424b-a96e-c35ddba66efc"),
                Name = "Indian Ocean Dipole",
                ShortName = "IOD",
                Description = @"Indian Ocean Dipole (IOD) events are driven by changes in the tropical Indian Ocean. Sustained changes in the difference between normal sea surface temperatures in the tropical western and eastern Indian Ocean are what characterise IOD events.

        The IOD is commonly measured by an index (sometimes referred to as the Dipole Mode Index, or DMI) that is the difference between sea surface temperature (SST) anomalies in two regions of the tropical Indian Ocean (see map above):

        IOD west: 50°E to 70°E and 10°S to 10°N
        IOD east: 90°E to 110°E and 10°S to 0°S

        A positive IOD period is characterised by cooler than average water in the tropical eastern Indian Ocean and warmer than average water in the tropical western Indian Ocean. Conversely, a negative IOD period is characterised by warmer than average water in the tropical eastern Indian Ocean and cooler than average water in the tropical western Indian Ocean.

        For monitoring the IOD, Australian climatologists consider sustained values above +0.4 °C as typical of a positive IOD, and values below −0.4 °C as typical of a negative IOD.",
                MoreInformationUrl = "http://www.bom.gov.au/climate/enso/indices/about.shtml",
                DataDownloadUrl = "https://psl.noaa.gov/gcos_wgsp/Timeseries/Data/dmi.had.long.data",
                MeasurementDefinitions =
                [
                    new() {
                        DataType = DataType.IOD,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsiusAnomaly,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        DataRowRegEx = @"^\s*(?<year>\d{4})\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                        FolderName = @"Ocean\IOD",
                        FileNameFormat = "dmi.had.long.data.txt",
                        NullValue = "-9999"
                    },
                ],
            },


            new() {
                Id = Guid.Parse("4EA1E30B-AF74-4BE8-B55D-C28764CF384E"),
                Name = "Arctic sea ice extent",
                ShortName = "Arctic sea ice extent",
                Description = "The daily Sea Ice Index provides a quick look at Arctic-wide changes in sea ice. It provides consistently processed daily ice extent and concentration images and data since 1979.",
                MoreInformationUrl = "https://nsidc.org/data/seaice_index/",
                DataDownloadUrl = "https://masie_web.apps.nsidc.org/pub/DATASETS/NOAA/G02135/north/daily/data/N_seaice_extent_daily_v3.0.csv",
                MeasurementDefinitions =
                [
                    new() {
                        DataType = DataType.SeaIceExtent,
                        UnitOfMeasure = UnitOfMeasure.MillionSqKm,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d+),\s+(?<month>\d+),\s+(?<day>\d+),\s+(?<value>\d+\.\d+).*$",
                        FolderName = @"Ice",
                        FileNameFormat = "N_seaice_extent_daily_v3.0.csv",
                    },
                ],
            },
            new() {
                Id = Guid.Parse("EC8AF0AC-215F-4D9C-9770-CC24EE24FBC7"),
                Name = "Antarctic sea ice extent",
                ShortName = "Antarctic sea ice extent",
                Description = "The daily Sea Ice Index provides a quick look at Antarctic-wide changes in sea ice. It provides consistently processed daily ice extent and concentration images and data since 1979.",
                MoreInformationUrl = "https://nsidc.org/data/seaice_index/",
                DataDownloadUrl = "https://masie_web.apps.nsidc.org/pub/DATASETS/NOAA/G02135/south/daily/data/S_seaice_extent_daily_v3.0.csv",
                MeasurementDefinitions =
                [
                    new() {
                        DataType = DataType.SeaIceExtent,
                        UnitOfMeasure = UnitOfMeasure.MillionSqKm,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d+),\s+(?<month>\d+),\s+(?<day>\d+),\s+(?<value>\d+\.\d+).*$",
                        FolderName = @"Ice",
                        FileNameFormat = "S_seaice_extent_daily_v3.0.csv",
                    },
                ],
            },
            new() {
                Id = Guid.Parse("6484A7F8-43BC-4B16-8C4D-9168F8D6699C"),
                Name = "Greenland ice melt area",
                ShortName = "Greenland ice melt",
                Description = "Greenland ice melt area since 1979",
                MoreInformationUrl = "https://nsidc.org/greenland-today",
                DataDownloadUrl = "https://nsidc.org/api/greenland/melt_area/{year}",
                MeasurementDefinitions =
                [
                    new() {
                        DataType = DataType.IceMeltArea,
                        UnitOfMeasure = UnitOfMeasure.SqKm,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>\d*).*$",
                        FolderName = @"Ice",
                        FileNameFormat = "greenland-melt-area.csv",
                    },
                ],
            },



            new() {
                Id = Guid.Parse("0561CF7E-83F2-4617-AC61-4962A0E95093"),
                Name = "Hadley Centre",
                Description = @"The Met Office Hadley Centre is one of the UK's foremost climate change research centres.

Our aim is to provide climate science and services to people and organisations, so they can make better decisions to stay safe and thrive. We do this by working with partners around the globe, carrying out world leading research.",
                Publisher = "Met Office",
                PublisherUrl = "https://www.metoffice.gov.uk/",
                MoreInformationUrl = "https://www.metoffice.gov.uk/hadobs/",
                StationInfoUrl = "https://www.metoffice.gov.uk/hadobs/",
                DataDownloadUrl = "https://www.metoffice.gov.uk/hadobs/hadcet/data/download.html",
                MeasurementDefinitions =
                [
                    new() {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})\s+(?<value>-?[\d+\.\d+]*)$",
                        FolderName = @"Temperature\Met",
                        FileNameFormat = "maxtemp_daily_totals.txt",
                    },
                    new() {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})\s+(?<value>-?[\d+\.\d+]*)$",
                        FolderName = @"Temperature\Met",
                        FileNameFormat = "mintemp_daily_totals.txt",
                    },
                    new() {
                        DataType = DataType.Precipitation,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})\s+(?<value>-?[\d+\.\d+]*)$",
                        FolderName = @"Precipitation\Met",
                        FileNameFormat = "HadCEP_daily_totals.txt",
                    },
                ],
            },


            new() {
                Id = Guid.Parse("E2D9A74B-3C30-4332-8B22-26BB14A0BDC7"),
                Name = "Sunspot number",
                ShortName = "Sunspot number",
                Description = "Sunspot number since 1818. On July 1st, 2015, the sunspot number series has been replaced by a new improved version (version 2.0) that includes several corrections of past inhomogeneities in the time series.",
                Publisher = "WDC-SILSO, Royal Observatory of Belgium, Brussels",
                MoreInformationUrl = "https://sidc.be/SILSO/newdataset",
                DataDownloadUrl = "https://sidc.be/SILSO/DATA/SN_d_tot_V2.0.txt",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.SunspotNumber,
                        UnitOfMeasure = UnitOfMeasure.Sn,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})\s+(?<month>\d+)\s(?<day>\d{2})\s\d{4}\.\d{3}\s+(?<value>-?\d+).*$",
                        FolderName = @"Solar",
                        FileNameFormat = "SN_d_tot_V2.0.txt",
                        NullValue = "-1",
                    },
                ],
            },
            new() {
                Id = Guid.Parse("E45293F9-B7AC-4874-9544-25E006B6B998"),
                Name = "Total solar irradiance",
                ShortName = "TSI",
                Description = "The Solar Irradiance Climate Data Record (CDR) includes a composite observational record of total solar irradiance (TSI) constructed from space-based radiometer composite records between 1978 and 2014 and Total Irradiance Monitor (TIM) observations after the launch of the SOlar Radiation and Climate  Experiment (SORCE). The SORCE TIM record ended Feb 25, 2020. The TSIS-1 TIM record began Jan 11, 2018.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                MoreInformationUrl = "https://sidc.be/SILSO/newdataset",
                DataDownloadUrl = "https://www.ncei.noaa.gov/data/total-solar-irradiance/access/ancillary-data/tsi-ssi_v02r01_observed-tsi-composite_s19780101_e20230930_c20231010.txt",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.SolarRadiation,
                        UnitOfMeasure = UnitOfMeasure.WattsPerSquareMetre,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),+(?<value>-?\d+\.*\d*),.*$$",
                        FolderName = @"Solar",
                        FileNameFormat = "tsi-ssi_v02r01_observed-tsi-composite.txt",
                        NullValue = "-99.0",
                    },
                ],
            },



            new() {
                Id = Guid.Parse("E61C6279-EDF4-461B-BDD1-0724D21F42F3"),
                Name = "Global temperature anomaly",
                ShortName = "Global temp",
                Description = "The NOAA Merged Land Ocean Global Surface Temperature Analysis (NOAAGlobalTemp, formerly known as MLOST) combines long-term sea surface (water) temperature (SST) and land surface (air) temperature datasets to create a complete, accurate depiction of global temperature trends. The dataset is used to support climate monitoring activities such as the Monthly Global Climate Assessment, and also provides input data for a number of climate models.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                MoreInformationUrl = "https://www.ncei.noaa.gov/products/land-based-station/noaa-global-temp",
                DataDownloadUrl = "aravg.mon.land.90S.90N.v5.1.0.[year][month].asc",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.TempMean,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsiusAnomaly,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})\s+(?<month>\d+)\s+(?<value>-?\d+\.\d+).*$",
                        FolderName = @"Temperature\NOAAGlobalTemp",
                        FileNameFormat = "aravg.mon.[station].v5.1.0.202311.asc",
                        NullValue = null,
                    },
                ],
            },

        };

        var options = new JsonSerializerOptions
        { 
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        Directory.CreateDirectory("Output");

        File.WriteAllText(@"Output\DataSetDefinitions.json", JsonSerializer.Serialize(dataSetDefinitions, options));

        return dataSetDefinitions;
    }
}
