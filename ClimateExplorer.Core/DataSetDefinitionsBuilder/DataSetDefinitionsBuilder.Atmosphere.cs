namespace ClimateExplorer.Core;

using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public partial class DataSetDefinitionsBuilder
{
    private static List<DataSetDefinition> BuildAtmosphereDataSetDefinitions()
    {
        return
        [
            new()
            {
                Id = Guid.Parse("42c9195e-edc0-4894-97dc-923f9d5e72f0"),
                Name = "Carbon dioxide (CO₂) from the Mauna Loa Observatory",
                ShortName = "Carbon Dioxide (CO₂)",
                Description = "The carbon dioxide data on Mauna Loa constitute the longest record of direct measurements of CO₂ in the atmosphere. They were started by C. David Keeling of the Scripps Institution of Oceanography in March of 1958 at a facility of the National Oceanic and Atmospheric Administration. NOAA started its own CO₂ measurements in May of 1974, and they have run in parallel with those made by Scripps since then.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                PublisherUrl = "https://www.noaa.gov/",
                PublisherDescription = @"The Global Monitoring Laboratory (GML) of the National Oceanic and Atmospheric Administration (NOAA) conducts research that addresses three major challenges: greenhouse gas and carbon cycle feedbacks, changes in clouds, aerosols, and surface radiation, and recovery of stratospheric ozone.",
                PublisherDivision = "Global Monitoring Laboratory",
                MoreInformationUrl = "https://gml.noaa.gov/ccgg/trends/mlo.html",
                DataDownloadUrl = "https://gml.noaa.gov/webdata/ccgg/trends/co2/co2_mm_mlo.txt",
                DataDownloaderKey = "direct-http",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataType = DataType.CO2,
                        UnitOfMeasure = UnitOfMeasure.PartsPerMillion,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^\s+(?<year>\d+)\s+(?<month>\d+)\s+(?<decimalDate>\d+\.\d+)\s+(?<value>\d+\.\d+).*$",
                        DataFileSource = LooseSource(@"CO2\co2_mm_mlo.txt"),
                    },
                    new()
                    {
                        DataType = DataType.CO2Deseasoned,
                        UnitOfMeasure = UnitOfMeasure.PartsPerMillion,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^\s+(?<year>\d+)\s+(?<month>\d+)\s+(?<decimalDate>\d+\.\d+)\s+(?<co2>\d+\.\d+)\s+(?<value>\d+\.\d+).*$",
                        DataFileSource = LooseSource(@"CO2\co2_mm_mlo.txt"),
                    },
                    new()
                    {
                        DataType = DataType.CO2,
                        UnitOfMeasure = UnitOfMeasure.PartsPerMillion,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^\s*(?<year>\d+)\s+(?<month>\d+)\s+(?<day>\d+)\s+(?<decimalDate>\d+\.\d+)\s+(?<value>\d+\.\d+).*$",
                        DataDownloadUrl = "https://gml.noaa.gov/webdata/ccgg/trends/co2/co2_daily_mlo.txt",
                        DataDownloaderKey = "direct-http",
                        DataFileSource = LooseSource(@"CO2\co2_daily_mlo.txt"),
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("2debe203-cbaa-4015-977c-2f40e2782547"),
                Name = "Methane (CH₄) from a globally distributed network",
                ShortName = "Methane (CH₄)",
                Description = "The Global Monitoring Division of NOAA's Earth System Research Laboratory has measured methane since 1983 at a globally distributed network of air sampling sites. A global average is constructed by first smoothing the data for each site as a function of time, and then smoothed values for each site are plotted as a function of latitude for 48 equal time steps per year. Global means are calculated from the latitude plot at each time step.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                PublisherUrl = "https://www.noaa.gov/",
                PublisherDivision = "Global Monitoring Laboratory",
                MoreInformationUrl = "https://gml.noaa.gov/ccgg/trends_ch4/",
                DataDownloadUrl = "https://gml.noaa.gov/webdata/ccgg/trends/ch4/ch4_mm_gl.txt",
                DataDownloaderKey = "direct-http",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataType = DataType.CH4,
                        UnitOfMeasure = UnitOfMeasure.PartsPerBillion,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^\s+(?<year>\d+)\s+(?<month>\d+)\s+(?<decimalDate>\d+\.\d+)\s+(?<value>\d+\.\d+).*$",
                        DataFileSource = LooseSource(@"Methane\ch4_mm_gl.txt"),
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("6e84e743-3c77-488f-8a1c-152306c3d6f0"),
                Name = "Nitrous oxide (N₂O) from a globally distributed network",
                ShortName = "N₂O",
                Description = "The Global Monitoring Division of NOAA's Earth System Research Laboratory has measured nitrous oxide since 2001 at a globally distributed network of air sampling sites. A global average is constructed by first smoothing the data for each site as a function of time, and then smoothed values for each site are fitted as a function of latitude at 48 equally-spaced time steps per year. Global means are calculated from the latitude fits at each time step.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                PublisherUrl = "https://www.noaa.gov/",
                PublisherDivision = "Global Monitoring Laboratory",
                MoreInformationUrl = "https://gml.noaa.gov/ccgg/trends_n2o/",
                DataDownloadUrl = "https://gml.noaa.gov/webdata/ccgg/trends/n2o/n2o_mm_gl.txt",
                DataDownloaderKey = "direct-http",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataType = DataType.N2O,
                        UnitOfMeasure = UnitOfMeasure.PartsPerBillion,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^\s+(?<year>\d+)\s+(?<month>\d+)\s+(?<decimalDate>\d+\.\d+)\s+(?<value>\d+\.\d+).*$",
                        DataFileSource = LooseSource(@"NitrousOxide\n2o_mm_gl.txt"),
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("71374F06-926A-4F89-8183-B2E765DB9747"),
                Name = "Carbon dioxide emissions",
                ShortName = "CO₂ emissions",
                Description = "The Global Carbon Project (GCP) has been publishing estimates of global and national fossil CO₂ emissions since 2001. In the first instance these were simple re-publications of data from another source, but over subsequent years refinements have been made in response to feedback and identification of inaccuracies.",
                Publisher = "The Global Carbon Project",
                PublisherUrl = "https://www.globalcarbonproject.org/",
                PublisherDescription = @"The Global Carbon Project (GCP) integrates knowledge of greenhouse gases for human activities and the Earth system. Their projects include global budgets for three dominant greenhouse gases — carbon dioxide, methane, and nitrous oxide — and complementary efforts in urban, regional, cumulative, and negative emissions.",
                MoreInformationUrl = "https://zenodo.org/records/14106218",
                DataDownloadUrl = null,
                StationMetadataFileName = "Stations_Gcb.json",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.CO2Emissions,
                        UnitOfMeasure = UnitOfMeasure.MegaTonnes,
                        DataResolution = DataResolution.Yearly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^\""(?<station>\w+)\"",.*,(?<year>\d{4}),(?<value>\d+\.\d+),.*$",
                        DataFileSource = LooseSource(@"CO2Emissions\GCB2025v15_MtCO2_flat.csv"),
                        NullValue = null,
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("0ACF9042-9822-4CC4-92B5-0BC189DA8148"),
                Name = "Atmospheric Transmission of Direct Solar Radiation at Mauna Loa",
                ShortName = "Mauna Loa atmospheric transmission",
                Description = @"The clear-sky atmospheric transmission, measured at Mauna Loa, Hawaii, from 1958. This net solar irradiance (total solar irradiance minus losses through the atmosphere) is fundamental to defining the climate of the earth.

Aerosols have the greatest potential influence on the record and in general have the ability to cause both scattering and absorption but because the largest anomalies in the record are known to be due to volcanic eruptions (that produce predominantly conservative scattering aerosols), large anomalies result in net radiative cooling tendencies in the entire associated atmospheric column.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                PublisherUrl = "https://www.noaa.gov/",
                PublisherDivision = "Global Monitoring Laboratory",
                MoreInformationUrl = "https://gml.noaa.gov/grad/mloapt.html",
                DataDownloadUrl = "https://gml.noaa.gov/webdata/grad/mloapt/mauna_loa_transmission.dat",
                DataDownloaderKey = "direct-http",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.ApparentTransmission,
                        UnitOfMeasure = UnitOfMeasure.AtmosphericTransmission,
                        DataResolution = DataResolution.Monthly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<month>\w+)-(?<year>\d{4})\s+\d*\.\d*\s+(?<value>-?\d+\.\d+).*$",
                        DataFileSource = LooseSource(@"AtmosphericTransmission\mauna_loa_transmission.dat"),
                        NullValue = null,
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("489E9F1A-057F-4EA8-9C48-0C86517D08A2"),
                Name = "Southern Hemisphere ozone hole area",
                ShortName = "Ozone hole area",
                Description = @"During the Southern Hemisphere spring season (August - October) the ozone hole over the Antarctic increases in size, reaching a maximum between mid-September and mid-October.

When temperatures high up in the atmosphere (stratosphere) start to rise in late Southern Hemisphere spring, ozone depletion slows, the polar vortex weakens and finally breaks down, and by the end of December ozone levels have returned to normal.

The ozone hole area is calculated as the area with ozone values below 220 DU south of 60S.",
                Publisher = "Copernicus Atmospheric Monitoring Service (CAMS)",
                PublisherUrl = "https://www.copernicus.eu/",
                PublisherDescription = "Copernicus is the Earth observation component of the European Union's Space programme, looking at our planet and its environment.",
                MoreInformationUrl = "https://atmosphere.copernicus.eu/monitoring-ozone-layer",
                DataDownloadUrl = "https://sites.ecmwf.int/data/cams/ozone_monitoring/data/cams_ozone_monitoring_sh_ozone_area.csv",
                DataDownloaderKey = "ozone",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.OzoneHoleArea,
                        UnitOfMeasure = UnitOfMeasure.MillionSqKm,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>-?\d+\.\d+)$",
                        DataFileSource = LooseSource(@"OzoneHoleArea\cams_ozone_monitoring_sh_ozone_area_reduced.csv"),
                        NullValue = null,
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("F3F925D6-8DBD-4080-9BF3-40D98D56FBEC"),
                Name = "Southern Hemisphere ozone column",
                ShortName = "Ozone hole column",
                Description = @"Ozone values are often measured as the number of ozone molecules in a vertical column and denoted in Dobson Units (DU). During the Antarctic ozone hole period, values can be less than half the normal levels. CAMS monitors the minimum value within the ozone hole area over time as another measure of the significance of each year's event. By comparing the current state of the ozone hole with its long-term evolution, it is possible to get an indication of whether the ozone layer is being restored and how fast this process is occurring, bearing in mind that meteorological and dynamical interannual variability also impact the size and duration of the current ozone hole.",
                Publisher = "Copernicus Atmospheric Monitoring Service (CAMS)",
                PublisherUrl = "https://www.copernicus.eu/",
                MoreInformationUrl = "https://atmosphere.copernicus.eu/monitoring-ozone-layer",
                DataDownloadUrl = "https://sites.ecmwf.int/data/cams/ozone_monitoring/data/cams_ozone_monitoring_sh_ozone_minimum.csv",
                DataDownloaderKey = "ozone",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.OzoneHoleColumn,
                        UnitOfMeasure = UnitOfMeasure.DobsonUnits,
                        DataResolution = DataResolution.Daily,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}),(?<value>-?\d+\.\d+)$",
                        DataFileSource = LooseSource(@"OzoneHoleColumn\cams_ozone_monitoring_sh_ozone_minimum_reduced.csv"),
                        NullValue = null,
                    },
                ],
            },
            new()
            {
                Id = Guid.Parse("A8F34F99-0908-4BF3-8C7F-744574FFEADA"),
                Name = "Ozone Depleting Gas Index",
                ShortName = "ODGI",
                Description = @"This index is derived from NOAA’s measurements of chemicals that contain chlorine and bromine at multiple remote surface sites across the planet (see the map in Figure 1). It is defined as 100 at the peak in ozone depleting halogen abundance as determined by NOAA observations, and zero for the 1980 abundance, which corresponds to when recovery of the ozone layer might be expected based on observations in the past, all other things being constant.

Two different indices are calculated, one that is relevant for the ozone layer over Antarctica (the ODGI-A), and one that is relevant for the ozone layer at mid-latitudes (the ODGI-ML). While both indices are derived from NOAA measurements of halocarbon abundances at Earth’s surface, separate indices for these different stratospheric regions are necessary to account for the unique nature of the Antarctic stratosphere compared to the stratosphere at mid-latitudes in both hemispheres. Though an index for the Arctic stratosphere is not explicitly calculated here, it is likely that its value would lie between the mid-latitude and Antarctic ODGI in any given year.",
                Publisher = "National Oceanic and Atmospheric Administration (NOAA)",
                PublisherUrl = "https://www.noaa.gov/",
                PublisherDivision = "Global Monitoring Laboratory",
                MoreInformationUrl = "https://gml.noaa.gov/odgi/",
                DataDownloadUrl = "https://gml.noaa.gov/odgi/odgi_[station].csv",
                DataDownloaderKey = "direct-http",
                MeasurementDefinitions =
                [
                    new MeasurementDefinition
                    {
                        DataType = DataType.Ozone,
                        UnitOfMeasure = UnitOfMeasure.Odgi,
                        DataResolution = DataResolution.Yearly,
                        DataAdjustment = null,
                        DataRowRegEx = @"^(?<year>\d{4}),.*,(?<value>\d+\.\d+)$",
                        DataFileSource = LooseSource(@"ODGI\odgi_[station].csv"),
                        NullValue = null,
                    },
                ],
            },
        ];
    }
}
