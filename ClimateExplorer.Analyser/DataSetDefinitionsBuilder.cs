using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static ClimateExplorer.Core.Enums;

namespace ClimateExplorer.Analyser;

internal class DataSetDefinitionsBuilder
{
    public static List<DataSetDefinition> BuildDataSetDefinitions()
    {
        var dataSetDefinitions = new List<DataSetDefinition>
        {
            new DataSetDefinition
            {
                Id = Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321"),
                Name = "ACORN-SAT",
                Description = "The Australian Climate Observations Reference Network - Surface Air Temperature data set is a homogenized daily maximum and minimum temperature data set containing data from 112 locations across Australia extending from 1910 to the present.",
                Publisher = "Australian Bureau of Meteorology",
                PublisherUrl = "http://www.bom.gov.au/",
                MoreInformationUrl = "http://www.bom.gov.au/climate/data/acorn-sat/#tabs=Data-and-networks",
                StationInfoUrl = "http://www.bom.gov.au/climate/averages/tables/cw_[station].shtml",
                LocationInfoUrl = "http://www.bom.gov.au/climate/data/acorn-sat/stations/#/[primaryStation]",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataCategory = DataCategory.Temperature,
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>-?\d*\.?\d*),*$",
                        FolderName = @"Temperature\ACORN-SAT\Daily\adjusted\daily_tmax",
                        FileNameFormat = "tmax.[station].daily.csv",
                        PreferredColour = 0,
                    },
                    new MeasurementDefinition
                    {
                        DataCategory = DataCategory.Temperature,
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>-?\d*\.?\d*),*$",
                        FolderName = @"Temperature\ACORN-SAT\Daily\adjusted\daily_tmin",
                        FileNameFormat = "tmin.[station].daily.csv",
                        PreferredColour = 1,
                    },
                },
            },
            new DataSetDefinition
            {
                Id = Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E"),
                Name = "Bureau of Meteorology",
                Description = null,
                Publisher = "Australian Bureau of Meteorology",
                PublisherUrl = "http://www.bom.gov.au/",
                //MoreInformationUrl = "http://www.bom.gov.au/climate/data/acorn-sat/#tabs=Data-and-networks",
                StationInfoUrl = "http://www.bom.gov.au/climate/averages/tables/cw_[station].shtml",
                LocationInfoUrl = "http://www.bom.gov.au/climate/data/acorn-sat/stations/#/[primaryStation]",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataCategory = DataCategory.Temperature,
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*),.*,.*$",
                        FolderName = @"Temperature\ACORN-SAT\Daily\raw-data\daily_tempmax",
                        FileNameFormat = "[station]_daily_tempmax.csv",
                        PreferredColour = 2,
                    },
                    new MeasurementDefinition
                    {
                        DataCategory = DataCategory.Temperature,
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*),.*,.*$",
                        FolderName = @"Temperature\ACORN-SAT\Daily\raw-data\daily_tempmin",
                        FileNameFormat = "[station]_daily_tempmin.csv",
                        PreferredColour = 3,
                    },
                    new MeasurementDefinition
                    {
                        DataType = DataType.Rainfall,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*),.*,.*$",
                        FolderName = @"Rainfall\ACORN-SAT\Daily\raw-data\daily_rainfall",
                        FileNameFormat = "[station]_daily_rainfall.csv",
                        PreferredColour = 1,
                    },
                    new MeasurementDefinition
                    {
                        DataType = DataType.SolarRadiation,
                        UnitOfMeasure = UnitOfMeasure.MegajoulesPerSquareMetre,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<productCode>.+),(?<station>\d{6}),(?<year>\d{4}),(?<month>\d{2}),(?<day>\d{2}),(?<value>.*)$",
                        FolderName = @"SolarRadiation\ACORN-SAT\Daily\daily_solarradiation",
                        FileNameFormat = "[station]_daily_solarradiation.csv",
                        PreferredColour = 0,
                    },
                },
            },
            new DataSetDefinition
            {
                Id = Guid.Parse("647b6a05-43e4-48e0-a43e-04ae81a74653"),
                Name = "RAIA",
                Description = "This ACORN-SAT dataset includes homogenised monthly data from the Remote Australian Islands and Antarctica network of 8 locations, which provide ground-based temperature records.",
                Publisher = "Australian Bureau of Meteorology",
                PublisherUrl = "http://www.bom.gov.au/",
                MoreInformationUrl = "http://www.bom.gov.au/climate/data/acorn-sat/#tabs=Data-and-networks",
                StationInfoUrl = "http://www.bom.gov.au/climate/averages/tables/cw_[station].shtml",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataCategory = DataCategory.Temperature,
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Monthly,
                        DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$",
                        FolderName = @"Temperature\RAIA\Monthly\adjusted\maxT",
                        FileNameFormat = "acorn.ria.maxT.[station].monthly.txt",
                        NullValue = "99999.9",
                        PreferredColour = 0,
                    },
                    new MeasurementDefinition
                    {
                        DataCategory = DataCategory.Temperature,
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Monthly,
                        DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<value>-?\d+\.\d+)$",
                        FolderName = @"Temperature\RAIA\Monthly\adjusted\minT",
                        FileNameFormat = "acorn.ria.minT.[station].monthly.txt",
                        NullValue = "99999.9",
                        PreferredColour = 1,
                    },
                },
            },




            new DataSetDefinition
            {
                Id = Guid.Parse("7522E8EC-E743-4CB0-BC65-6E9F202FC824"),
                Name = "NIWA 7-stations series adjusted",
                Description = "NIWA's long-running 'seven-station' series shows NZ's average annual temperature has increased by about 1 °C over the past 100 years.",
                Publisher = "National Institute of Water and Atmospheric Research (NIWA)",
                PublisherUrl = "http://www.niwa.co.nz/",
                MoreInformationUrl = "https://niwa.co.nz/seven-stations",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataCategory = DataCategory.Temperature,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$",
                        FolderName = @"Temperature\NIWA\Daily\raw-data",
                        FileNameFormat = "[station].csv",
                        NullValue = "-",
                        PreferredColour = 2,
                    },
                    new MeasurementDefinition
                    {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataCategory = DataCategory.Temperature,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<tmax>-?[\d+\.\d+]*),-?\d*,(?<value>-?[\d+\.\d+]*),-?\d*,.*,D$",
                        FolderName = @"Temperature\NIWA\Daily\raw-data",
                        FileNameFormat = "[station].csv",
                        NullValue = "-",
                        PreferredColour = 3,
                    },
                },
            },
            new DataSetDefinition
            {
                Id = Guid.Parse("534950DC-EDA4-4DB5-8816-3705358F1797"),
                Name = "NIWA 7-stations series unadjusted",
                Description = "NIWA's long-running 'seven-station' series shows NZ's average annual temperature has increased by about 1 °C over the past 100 years.",
                Publisher = "National Institute of Water and Atmospheric Research (NIWA)",
                PublisherUrl = "http://www.niwa.co.nz/",
                MoreInformationUrl = "https://niwa.co.nz/seven-stations",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataCategory = DataCategory.Temperature,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$",
                        FolderName = @"Temperature\NIWA\Daily\raw-data",
                        FileNameFormat = "[station].csv",
                        NullValue = "-",
                        PreferredColour = 2,
                    },
                    new MeasurementDefinition
                    {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataCategory = DataCategory.Temperature,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<tmax>-?[\d+\.\d+]*),-?\d*,(?<value>-?[\d+\.\d+]*),-?\d*,.*,D$",
                        FolderName = @"Temperature\NIWA\Daily\raw-data",
                        FileNameFormat = "[station].csv",
                        NullValue = "-",
                        PreferredColour = 3,
                    },
                    new MeasurementDefinition
                    {
                        DataType = DataType.Rainfall,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),.*,D$",
                        FolderName = @"Rainfall\NIWA\Daily",
                        FileNameFormat = "[station]_rainfall.csv",
                        NullValue = "-",
                        PreferredColour = 1,
                    },
                },
            },




            new DataSetDefinition
            {
                Id = Guid.Parse("88e52edd-3c67-484a-b614-91070037d47a"),
                Name = "NIWA 11-stations series",
                Description = "The National Institute of Water and Atmospheric Research (NIWA) eleven-station series are New Zealand temperature trends from a set of eleven climate stations with no significant site changes since the 1930s.",
                Publisher = "National Institute of Water and Atmospheric Research (NIWA)",
                PublisherUrl = "http://www.niwa.co.nz/",
                MoreInformationUrl = "https://niwa.co.nz/our-science/climate/information-and-resources/nz-temp-record/temperature-trends-from-raw-data",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataCategory = DataCategory.Temperature,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),-?\d*,(?<tmin>-?[\d+\.\d+]*),-?\d*,.*,D$",
                        FolderName = @"Temperature\NIWA\Daily\raw-data",
                        FileNameFormat = "[station].csv",
                        NullValue = "-",
                        PreferredColour = 2,
                    },
                    new MeasurementDefinition
                    {
                        DataCategory = DataCategory.Temperature,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<tmax>-?[\d+\.\d+]*),-?\d*,(?<value>-?[\d+\.\d+]*),-?\d*,.*,D$",
                        FolderName = @"Temperature\NIWA\Daily\raw-data",
                        FileNameFormat = "[station].csv",
                        NullValue = "-",
                        PreferredColour = 3,
                    },
                    new MeasurementDefinition
                    {
                        DataType = DataType.Rainfall,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Daily,
                        DataRowRegEx = @"^(?<station>\d+),(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}):\d+,(?<value>-?[\d+\.\d+]*),.*,D$",
                        FolderName = @"Rainfall\NIWA\Daily",
                        FileNameFormat = "[station]_rainfall.csv",
                        NullValue = "-",
                        PreferredColour = 1,
                    },
                },
            },




            new DataSetDefinition
            {
                Id = Guid.Parse("ffd5f5e2-d8df-4779-a7f4-f5d148505033"),
                Name = "Multivariate ENSO index (MEI)",
                ShortName = "MEI.v2",
                Description = "The MEI combines both oceanic and atmospheric variables to form a single index assessment of ENSO. It is an Empirical Orthogonal Function (EOF) of five different variables (sea level pressure (SLP), sea surface temperature (SST), zonal and meridional components of the surface wind, and outgoing longwave radiation (OLR)) over the tropical Pacific basin (30°S-30°N and 100°E-70°W).",
                Publisher = "US National Oceanic and Atmospheric Administration's Physical Sciences Laboratory (NOAA PSL)",
                PublisherUrl = "https://psl.noaa.gov/",
                MoreInformationUrl = "https://psl.noaa.gov/enso/mei/",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataCategory = DataCategory.Enso,
                        DataType = DataType.MEIv2,
                        UnitOfMeasure = UnitOfMeasure.EnsoIndex,
                        DataResolution = DataResolution.Monthly,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        FolderName = @"Reference\ENSO",
                        FileNameFormat = "meiv2.data.txt",
                        DataRowRegEx = @"^\s*(\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                        NullValue = "-999.00"
                    },
                },
                DataDownloadUrl = "https://psl.noaa.gov/enso/mei/data/meiv2.data",
            },
            new DataSetDefinition
            {
                Id = Guid.Parse("c31270fa-b207-4d8f-b68e-4995698f1a4d"),
                Name = "Southern Oscillation Index (ISOI)",
                ShortName = "SOI",
                Description = "TBC",
                MoreInformationUrl = "https://www.ncdc.noaa.gov/teleconnections/enso/soi",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataCategory = DataCategory.Enso,
                        DataType = DataType.SOI,
                        UnitOfMeasure = UnitOfMeasure.EnsoIndex,
                        DataResolution = DataResolution.Monthly,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        FolderName = @"Reference\ENSO",
                        FileNameFormat = "soi.long.data.txt",
                        DataRowRegEx = @"^\s*(\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                        NullValue = "-99.99"
                    },
                },
                DataDownloadUrl = "https://psl.noaa.gov/gcos_wgsp/Timeseries/Data/soi.long.data",
            },
            new DataSetDefinition
            {
                Id = Guid.Parse("1042147a-8625-4ee7-bb5a-f0f17795c393"),
                Name = "Oceanic Niño Index (ONI)",
                ShortName = "ONI",
                Description = "TBC",
                MoreInformationUrl = "https://www.climate.gov/news-features/understanding-climate/climate-variability-oceanic-ni%C3%B1o-index",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataCategory = DataCategory.Enso,
                        DataType = DataType.ONI,
                        UnitOfMeasure = UnitOfMeasure.EnsoIndex,
                        DataResolution = DataResolution.Monthly,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        FolderName = @"Reference\ENSO",
                        FileNameFormat = "oni.data.txt",
                        DataRowRegEx = @"^\s*(\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                        NullValue = "-99.9"
                    },
                },
            },
            new DataSetDefinition
            {
                Id = Guid.Parse("bfbaa69b-c10d-4de3-a78c-1ed6ff307327"),
                Name = "Niño 3.4",
                ShortName = "Niño 3.4",
                Description = "TBC",
                MoreInformationUrl = "https://climatedataguide.ucar.edu/climate-data/nino-sst-indices-nino-12-3-34-4-oni-and-tni",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataCategory = DataCategory.Enso,
                        DataType = DataType.Nino34,
                        UnitOfMeasure = UnitOfMeasure.EnsoIndex,
                        DataResolution = DataResolution.Monthly,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        FolderName = @"Reference\ENSO",
                        FileNameFormat = "nino34.long.anom.data.txt",
                        DataRowRegEx = @"^\s*(\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                        NullValue = "-99.99"
                    },
                },
            },





            new DataSetDefinition
            {
                Id = Guid.Parse("42c9195e-edc0-4894-97dc-923f9d5e72f0"),
                Name = "Carbon dioxide (CO₂) from the Mauna Loa Observatory",
                ShortName = "Carbon Dioxide (CO₂)",
                Description = "The carbon dioxide data on Mauna Loa constitute the longest record of direct measurements of CO2 in the atmosphere. They were started by C. David Keeling of the Scripps Institution of Oceanography in March of 1958 at a facility of the National Oceanic and Atmospheric Administration [Keeling, 1976]. NOAA started its own CO2 measurements in May of 1974, and they have run in parallel with those made by Scripps since then [Thoning, 1989].",
                MoreInformationUrl = "https://gml.noaa.gov/ccgg/trends/mlo.html",
                DataDownloadUrl = "https://gml.noaa.gov/webdata/ccgg/trends/co2/co2_mm_mlo.txt",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataType = DataType.CO2,
                        UnitOfMeasure = UnitOfMeasure.PartsPerMillion,
                        DataResolution = DataResolution.Monthly,
                        DataRowRegEx = @"^\s+(?<year>\d+)\s+(?<month>\d+)\s+(?<decimalDate>\d+\.\d+)\s+(?<value>\d+\.\d+).*$",
                        FolderName = @"Reference\CO2\Monthly",
                        FileNameFormat = "co2_mm_mlo.txt",
                        PreferredColour = 4
                    },
                },
            },
            new DataSetDefinition
            {
                Id = Guid.Parse("2debe203-cbaa-4015-977c-2f40e2782547"),
                Name = "Methane (CH₄) from a globally distributed network",
                ShortName = "Methane (CH₄)",
                Description = "The Global Monitoring Division of NOAA's Earth System Research Laboratory has measured methane since 1983 at a globally distributed network of air sampling sites (Dlugokencky et al., 1994). A global average is constructed by first smoothing the data for each site as a function of time, and then smoothed values for each site are plotted as a function of latitude for 48 equal time steps per year. Global means are calculated from the latitude plot at each time step (Masarie and Tans, 1995).",
                MoreInformationUrl = "https://gml.noaa.gov/ccgg/trends_ch4/",
                DataDownloadUrl = "https://gml.noaa.gov/webdata/ccgg/trends/ch4/ch4_mm_gl.txt",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataType = DataType.CH4,
                        UnitOfMeasure = UnitOfMeasure.PartsPerBillion,
                        DataResolution = DataResolution.Monthly,
                        DataRowRegEx = @"^\s+(?<year>\d+)\s+(?<month>\d+)\s+(?<decimalDate>\d+\.\d+)\s+(?<value>\d+\.\d+).*$",
                        FolderName = @"Reference\CH4\Monthly",
                        FileNameFormat = "ch4_mm_gl.txt",
                        PreferredColour = 4
                    },
                },
            },
            new DataSetDefinition
            {
                Id = Guid.Parse("6e84e743-3c77-488f-8a1c-152306c3d6f0"),
                Name = "Nitrous oxide (N₂O) from a globally distributed network",
                ShortName = "N₂O",
                Description = "The Global Monitoring Division of NOAA's Earth System Research Laboratory has measured nitrous oxide since 1997 at a globally distributed network of air sampling sites (Dlugokencky et al., 1994). A global average is constructed by first smoothing the data for each site as a function of time, and then smoothed values for each site are fitted as a function of latitude at 48 equally-spaced time steps per year. Global means are calculated from the latitude fits at each time step (Masarie and Tans, 1995).",
                MoreInformationUrl = "https://gml.noaa.gov/ccgg/trends_n2o/",
                DataDownloadUrl = "https://gml.noaa.gov/webdata/ccgg/trends/n2o/n2o_mm_gl.txt",
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataType = DataType.N2O,
                        UnitOfMeasure = UnitOfMeasure.PartsPerBillion,
                        DataResolution = DataResolution.Monthly,
                        DataRowRegEx = @"^\s+(?<year>\d+)\s+(?<month>\d+)\s+(?<decimalDate>\d+\.\d+)\s+(?<value>\d+\.\d+).*$",
                        FolderName = @"Reference\N2O\Monthly",
                        FileNameFormat = "n2o_mm_gl.txt",
                        PreferredColour = 4
                    },
                },
            },





            new DataSetDefinition
            {
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
                MeasurementDefinitions = new List<MeasurementDefinition>
                {
                    new MeasurementDefinition
                    {
                        DataType = DataType.IOD,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsiusAnomaly,
                        DataResolution = DataResolution.Monthly,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        DataRowRegEx = @"^\s*(\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                        FolderName = @"Reference\IOD",
                        FileNameFormat = "dmi.had.long.data.txt",
                        NullValue = "-9999"
                    },
                },
            }
        };

        var options = new JsonSerializerOptions
        { 
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        Directory.CreateDirectory("Output");

        File.WriteAllText(@"Output\DataSetDefinitions.json", JsonSerializer.Serialize(dataSetDefinitions, options));

        return dataSetDefinitions;
    }
}
