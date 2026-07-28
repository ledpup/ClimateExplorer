namespace ClimateExplorer.Core;

using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public partial class DataSetDefinitionsBuilder
{
    private static List<DataSetDefinition> BuildOceanDataSetDefinitions()
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
}
