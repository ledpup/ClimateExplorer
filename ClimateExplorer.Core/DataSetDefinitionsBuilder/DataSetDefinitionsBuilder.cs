namespace ClimateExplorer.Core;

using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public partial class DataSetDefinitionsBuilder
{
    public static List<DataSetDefinition> BuildDataSetDefinitions()
    {
        var dataSetDefinitions = new List<DataSetDefinition>();

        dataSetDefinitions.AddRange(BuildBomDataSetDefinitions());
        dataSetDefinitions.AddRange(BuildGhcnDataSetDefinitions());
        dataSetDefinitions.AddRange(BuildAtmosphereDataSetDefinitions());
        dataSetDefinitions.AddRange(BuildOtherDataSetDefinitions());

        return dataSetDefinitions;
    }

    private static List<DataSetDefinition> BuildOtherDataSetDefinitions()
    {
        return
        [
            new()
            {
                Id = Guid.Parse("bfbaa69b-c10d-4de3-a78c-1ed6ff307327"),
                Name = "Niño 3.4",
                ShortName = "Niño 3.4",
                Description = @"The Niño 3.4 index is calculated as a 3-month running average of sea surface temperature measurements around the equator in the East Pacific (5 deg N to 5 deg C, 170 deg W to 120 deg W), and then expressed as an anomaly (i.e. difference from the average).

                    Niño 3.4 conditions of +0.4 deg C or higher are considered El Niño, and -0.4 deg C or lower are considered La Niña.",
                MoreInformationUrl = "https://psl.noaa.gov/data/timeseries/month/",
                DataDownloadUrl = "https://psl.noaa.gov/data/timeseries/month/data/nino34.long.anom.data",
                DataDownloaderKey = "direct-http",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataType = DataType.Nino34,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsiusAnomaly,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        DataFileSource = LooseSource(@"Nino34\nino34.long.anom.data.txt"),
                        DataRowRegEx = @"^\s*(?<year>\d{4})\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                        NullValue = "-99.99",
                    },
                ],
            },
            new()
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
                MoreInformationUrl = "https://www.bom.gov.au/climate/enso",
                DataDownloadUrl = "https://psl.noaa.gov/gcos_wgsp/Timeseries/Data/dmi.had.long.data",
                DataDownloaderKey = "direct-http",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataType = DataType.IOD,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsiusAnomaly,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        DataRowRegEx = @"^\s*(?<year>\d{4})\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)$",
                        DataFileSource = LooseSource(@"IOD\dmi.had.long.data.txt"),
                        NullValue = "-9999",
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("4EA1E30B-AF74-4BE8-B55D-C28764CF384E"),
                Name = "Arctic sea ice extent",
                ShortName = "Arctic sea ice extent",
                Description = "The daily Sea Ice Index provides a quick look at Arctic-wide changes in sea ice. It provides consistently processed daily ice extent and concentration images and data since 1979.",
                MoreInformationUrl = "https://nsidc.org/data/seaice_index/",
                DataDownloadUrl = "https://masie_web.apps.nsidc.org/pub/DATASETS/NOAA/G02135/north/daily/data/N_seaice_extent_daily_v4.0.csv",
                DataDownloaderKey = "direct-http",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataType = DataType.SeaIceExtent,
                        UnitOfMeasure = UnitOfMeasure.MillionSqKm,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d+),\s+(?<month>\d+),\s+(?<day>\d+),\s+(?<value>\d+\.\d+).*$",
                        DataFileSource = LooseSource(@"ArcticSeaIce\N_seaice_extent_daily_v4.0.csv"),
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("EC8AF0AC-215F-4D9C-9770-CC24EE24FBC7"),
                Name = "Antarctic sea ice extent",
                ShortName = "Antarctic sea ice extent",
                Description = "The daily Sea Ice Index provides a quick look at Antarctic-wide changes in sea ice. It provides consistently processed daily ice extent and concentration images and data since 1979.",
                MoreInformationUrl = "https://nsidc.org/data/seaice_index/",
                DataDownloadUrl = "https://masie_web.apps.nsidc.org/pub/DATASETS/NOAA/G02135/south/daily/data/S_seaice_extent_daily_v4.0.csv",
                DataDownloaderKey = "direct-http",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataType = DataType.SeaIceExtent,
                        UnitOfMeasure = UnitOfMeasure.MillionSqKm,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d+),\s+(?<month>\d+),\s+(?<day>\d+),\s+(?<value>\d+\.\d+).*$",
                        DataFileSource = LooseSource(@"AntarcticSeaIce\S_seaice_extent_daily_v4.0.csv"),
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("6484A7F8-43BC-4B16-8C4D-9168F8D6699C"),
                Name = "Greenland ice melt area",
                ShortName = "Greenland ice melt",
                Description = "Greenland ice melt area since 1979",
                MoreInformationUrl = "https://nsidc.org/greenland-today",
                DataDownloadUrl = null,
                DataDownloaderKey = "greenland-melt",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataType = DataType.IceMeltArea,
                        UnitOfMeasure = UnitOfMeasure.SqKm,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>\d*).*$",
                        DataFileSource = LooseSource(@"Greenland\greenland-melt-area.csv"),
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("0561CF7E-83F2-4617-AC61-4962A0E95093"),
                Name = "Hadley Centre Central England Temperature",
                ShortName = "HadCET",
                Description = @"The Hadley Centre Central England Temperature (HadCET) dataset provides daily and monthly mean, minimum and maximum temperatures representative of a roughly triangular area enclosed by Lancashire, London and Bristol.

The monthly mean series begins in 1659 and is the longest instrumental temperature record in the world, while daily mean temperatures begin in 1772 and minimum and maximum temperature series begin in 1878.",
                Publisher = "Met Office",
                PublisherUrl = "https://www.metoffice.gov.uk/",
                MoreInformationUrl = "https://www.metoffice.gov.uk/hadobs/",
                StationInfoUrl = "https://www.metoffice.gov.uk/hadobs/",
                DataDownloaderKey = "direct-http",
                StationMetadataFileName = "Stations_UK_HadObs.json",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMean,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Monthly,
                        DataDownloadUrl = "https://www.metoffice.gov.uk/hadobs/hadcet/data/meantemp_monthly_totals.txt",
                        RowDataType = RowDataType.TwelveMonthsPerRow,
                        DataRowRegEx = @"^\s*(?<year>\d{4})\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+)\s+(-?\d+\.?\d+).*$",
                        DataFileSource = LooseSource(@"Met\meantemp_monthly_totals.txt"),
                        NullValue = "-99.9",
                    },
                    new()
                    {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMean,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataDownloadUrl = "https://www.metoffice.gov.uk/hadobs/hadcet/data/meantemp_daily_totals.txt",
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})\s+(?<value>-?[\d+\.\d+]*)$",
                        DataFileSource = LooseSource(@"Met\meantemp_daily_totals.txt"),
                    },
                    new()
                    {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataDownloadUrl = "https://www.metoffice.gov.uk/hadobs/hadcet/data/maxtemp_daily_totals.txt",
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})\s+(?<value>-?[\d+\.\d+]*)$",
                        DataFileSource = LooseSource(@"Met\maxtemp_daily_totals.txt"),
                    },
                    new()
                    {
                        DataAdjustment = DataAdjustment.Adjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        DataDownloadUrl = "https://www.metoffice.gov.uk/hadobs/hadcet/data/mintemp_daily_totals.txt",
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})\s+(?<value>-?[\d+\.\d+]*)$",
                        DataFileSource = LooseSource(@"Met\mintemp_daily_totals.txt"),
                    },
                    new()
                    {
                        DataType = DataType.Precipitation,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Daily,
                        DataDownloadUrl = "https://www.metoffice.gov.uk/hadobs/hadukp/data/daily/HadCEP_daily_totals.txt",
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})\s+(?<value>-?[\d+\.\d+]*)$",
                        DataFileSource = LooseSource(@"Met\HadCEP_daily_totals.txt"),
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("E2D9A74B-3C30-4332-8B22-26BB14A0BDC7"),
                Name = "Sunspot number",
                ShortName = "Sunspot number",
                Description = "Sunspot number since 1818. On July 1st, 2015, the sunspot number series has been replaced by a new improved version (version 2.0) that includes several corrections of past inhomogeneities in the time series.",
                Publisher = "WDC-SILSO, Royal Observatory of Belgium, Brussels",
                MoreInformationUrl = "https://sidc.be/SILSO/newdataset",
                DataDownloadUrl = "https://sidc.be/SILSO/DATA/SN_d_tot_V2.0.txt",
                DataDownloaderKey = "direct-http",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.SunspotNumber,
                        UnitOfMeasure = UnitOfMeasure.Sn,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})\s+(?<month>\d+)\s(?<day>\d{2})\s\d{4}\.\d{3}\s+(?<value>-?\d+).*$",
                        DataFileSource = LooseSource(@"Sunspots\SN_d_tot_V2.0.txt"),
                        NullValue = "-1",
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("E45293F9-B7AC-4874-9544-25E006B6B998"),
                Name = "Total solar irradiance",
                ShortName = "TSI",
                Description = "The Solar Irradiance Climate Data Record (CDR) includes a composite observational record of total solar irradiance (TSI) constructed from space-based radiometer composite records between 1978 and 2014 and Total Irradiance Monitor (TIM) observations after the launch of the SOlar Radiation and Climate Experiment (SORCE). The SORCE TIM record ended Feb 25, 2020. The TSIS-1 TIM record began Jan 11, 2018.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                MoreInformationUrl = "https://www.ncei.noaa.gov/products/climate-data-records/total-solar-irradiance",
                DataDownloadUrl = "https://www.ncei.noaa.gov/data/total-solar-irradiance/access/ancillary-data/tsi-ssi_v03r00_observed-tsi-composite_s19780101_e20250630_c20250917.txt",
                DataDownloaderKey = "direct-http",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.SolarRadiation,
                        UnitOfMeasure = UnitOfMeasure.WattsPerSquareMetre,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),+(?<value>-?\d+\.*\d*),.*$$",
                        DataFileSource = LooseSource(@"TSI\tsi-ssi_v02r01_observed-tsi-composite.txt"),
                        NullValue = "-99.000000",
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("E61C6279-EDF4-461B-BDD1-0724D21F42F3"),
                Name = "Global temperature anomaly",
                ShortName = "Global temp",
                Description = @"The NOAA Merged Land Ocean Global Surface Temperature Analysis (NOAAGlobalTemp, formerly known as MLOST) combines long-term sea surface (water) temperature (SST) and land surface (air) temperature datasets to create a complete, accurate depiction of global temperature trends. The dataset is used to support climate monitoring activities such as the Monthly Global Climate Assessment, and also provides input data for a number of climate models.

The reference period used to calculate the anomalies is 1971–2000.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                MoreInformationUrl = "https://www.ncei.noaa.gov/products/land-based-station/noaa-global-temp",
                DataDownloadUrl = null,
                DataDownloaderKey = "noaa-global-temperature",
                StationMetadataFileName = "Stations_NoaaGlobalTemp.json",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.TempMean,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsiusAnomaly,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})\s+(?<month>\d+)\s+(?<value>-?\d+\.\d+).*$",
                        DataFileSource = LooseSource(@"NOAAGlobalTemp\aravg.mon.[station].v6.0.0.asc"),
                        NullValue = null,
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("F478726F-7A7C-4B73-B942-1644857D442D"),
                Name = "Mean sea level",
                ShortName = "Sea level",
                Description = @"Data are from TOPEX/Poseidon (T/P), Jason-1, Jason-2, and Jason-3, which have monitored the same ground track since 1992.

Only altimetry measurements between 66°S and 66°N have been processed. An inverted barometer has been applied to the time series. The estimates of sea level rise do not include glacial isostatic adjustment effects on the geoid, which are modeled to be +0.2 to +0.5 mm/year when globally averaged.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                PublisherUrl = "https://www.noaa.gov/",
                MoreInformationUrl = "https://www.star.nesdis.noaa.gov/socd/lsa/SeaLevelRise/LSA_SLR_timeseries_global.php",
                DataDownloadUrl = "https://www.star.nesdis.noaa.gov/socd/lsa/SeaLevelRise/slr/slr_sla_gbl_free_ref_90.csv",
                DataDownloaderKey = "sea-level",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.SeaLevel,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>-?\d+\.\d+)$",
                        DataFileSource = LooseSource(@"SeaLevel\slr_sla_gbl_free_ref_90_reduced.csv"),
                        NullValue = null,
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("8FD9692D-8A0E-4591-89BD-0FE77783CC4D"),
                Name = "Atlantic Multidecadal Oscillation",
                ShortName = "AMO",
                Description = @"The Atlantic Multi-decadal Oscillation (AMO) has been identified as a coherent mode of natural variability occurring in the North Atlantic Ocean with an estimated period of 60-80 years.

This AMO time-series is from NOAA/NCEI, using the ERSSTV5 dataset. It is a sea-surface temperature anomaly (SSTA) North Atlantic 0-60N",
                PublisherUrl = "https://www.noaa.gov/",
                MoreInformationUrl = "https://psl.noaa.gov/data/timeseries/AMO/",
                DataDownloadUrl = "https://www1.ncdc.noaa.gov/pub/data/cmb/ersst/v5/index/ersst.v5.amo.dat",
                DataDownloaderKey = "direct-http",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.Amo,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsiusAnomaly,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})\s+(?<month>\d+)\s+(?<value>-?\d+\.\d+)$",
                        DataFileSource = LooseSource(@"AMO\ersst.v5.amo.dat"),
                        NullValue = null,
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("6859B806-12FD-4161-B0B6-3E4DA984B731"),
                Name = "Ocean acidity",
                ShortName = "Ocean pH",
                Description = @"Station ALOHA (22°45'N, 158°00'W) is a deep water (~4,800 m) location approximately 100 km north of the Hawaiian Island of Oahu. The Hawaii Ocean Time-series (HOT) surface CO₂ system data product (HOT_surface_CO2.txt) is created after taking cruises to the station (every 1-2 months) for measurements, beginning in 1988.

The mean seawater pH is calculated from the mean seawater dissolved inorganic carbon (DIC - equal to the total CO₂) concentration and mean seawater total alkalinity (TA) at 25 °C, on the total scale.",
                PublisherUrl = "https://www.noaa.gov/",
                MoreInformationUrl = "https://hahana.soest.hawaii.edu/hot/",
                DataDownloadUrl = "https://hahana.soest.hawaii.edu/hot/hotco2/HOT_surface_CO2.txt",
                DataDownloaderKey = "ocean-acidity",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.OceanAcidity,
                        UnitOfMeasure = UnitOfMeasure.Ph,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4}),(?<month>\d+),(?<value>-?\d+\.\d+)$",
                        DataFileSource = LooseSource(@"OceanAcidity\HOT_surface_CO2_reduced.csv"),
                        NullValue = null,
                    },
                ],
            },
        ];
    }

    private static DataFileSourceDefinition LooseSource(string filePathFormat)
    {
        return new DataFileSourceDefinition
        {
            FilePathFormat = filePathFormat,
        };
    }

    private static DataFileSourceDefinition ArchiveSource(string filePathFormat, string archiveEntryPathFormat)
    {
        return new DataFileSourceDefinition
        {
            FilePathFormat = filePathFormat,
            ArchiveEntryPathFormat = archiveEntryPathFormat,
        };
    }
}
