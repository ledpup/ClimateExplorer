namespace ClimateExplorer.Web.Client.Components.Chart.DataSetBrowser;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Data set browser for global (non-location-specific) data sets, e.g. atmosphere, ocean, cryosphere,
/// solar and southern hemisphere indices.
/// </summary>
public partial class GlobalDataSetBrowser
{
    [Parameter]
    public IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    [Parameter]
    public EventCallback<DataSetLibraryEntry> OnAddDataSet { get; set; }

    [Parameter]
    public IReadOnlyList<ChartSeriesDefinition>? ChartSeriesList { get; set; }

    [Parameter]
    public IReadOnlyList<SeriesWithData>? SeriesWithData { get; set; }

    [Parameter]
    public EventCallback OnTrendsChanged { get; set; }

    private List<DataSetLibraryFolder>? RootFolders { get; set; }

    /// <summary>
    /// Chart series eligible for "add a trend to an existing series" from this tab: on the chart,
    /// tied to a region rather than a location (see <see cref="ChartSeriesDefinition.IsGlobalSeries"/>
    /// - location-tied series belong on the Local tab instead), with data, plotted against a year
    /// axis (trends need a linear year x-axis), and not already at the three-trend cap.
    /// </summary>
    private List<ChartSeriesDefinition> GlobalTrendEligibleSeries =>
        ChartSeriesList?
            .Where(x => x.DataAvailable && x.IsGlobalSeries && x.BinGranularity == BinGranularities.ByYear && x.Trends.Count < ChartSeriesDefinition.MaxTrends)
            .ToList()
        ?? [];

    protected override void OnParametersSet()
    {
        RootFolders = [];

        if (DataSetDefinitions is null)
        {
            return;
        }

        RootFolders.AddRange(
            [
                new DataSetLibraryFolder
                {
                    Name = "Atmosphere",
                    DataSets =
                        [
                            new DataSetLibraryEntry()
                            {
                                Name = "CO₂ (Carbon Dioxide) | parts per million",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.Atmosphere),
                                        SourceDataSetId = Guid.Parse("42c9195e-edc0-4894-97dc-923f9d5e72f0"),
                                        DataType = DataType.CO2,
                                    },
                                ],
                            },
                            new DataSetLibraryEntry()
                            {
                                Name = "CH₄ (Methane) | parts per billion",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.Atmosphere),
                                        SourceDataSetId = Guid.Parse("2debe203-cbaa-4015-977c-2f40e2782547"),
                                        DataType = DataType.CH4,
                                    },
                                ],
                            },
                            new DataSetLibraryEntry()
                            {
                                Name = "N₂O (Nitrous Oxide) | parts per billion",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.Atmosphere),
                                        SourceDataSetId = Guid.Parse("6e84e743-3c77-488f-8a1c-152306c3d6f0"),
                                        DataType = DataType.N2O,
                                    },
                                ],
                            },
                            new DataSetLibraryEntry()
                            {
                                Name = "Reported CO₂ emissions | megatonnes",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.Atmosphere),
                                        SourceDataSetId = Guid.Parse("71374f06-926a-4f89-8183-b2e765db9747"),
                                        DataType = DataType.CO2Emissions,
                                    },
                                ],
                            },
                            new DataSetLibraryEntry()
                            {
                                Name = "Apparent atmospheric transmission",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.Atmosphere),
                                        SourceDataSetId = Guid.Parse("0ACF9042-9822-4CC4-92B5-0BC189DA8148"),
                                        DataType = DataType.ApparentTransmission,
                                    },
                                ],
                            },
                        ],
                },
                new DataSetLibraryFolder
                {
                    Name = "Ocean",
                    DataSets =
                        [
                            new DataSetLibraryEntry()
                            {
                                Name = "Niño 3.4",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.Ocean),
                                        SourceDataSetId = Guid.Parse("bfbaa69b-c10d-4de3-a78c-1ed6ff307327"),
                                        DataType = DataType.Nino34,
                                    },
                                ],
                            },
                            new DataSetLibraryEntry()
                            {
                                Name = "Indian Ocean Dipole (IOD)",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.Ocean),
                                        SourceDataSetId = Guid.Parse("a3841b12-2dd4-424b-a96e-c35ddba66efc"),
                                        DataType = DataType.IOD,
                                    },
                                ],
                            },
                        ],
                },
                new DataSetLibraryFolder
                {
                    Name = "Cryosphere",
                    DataSets =
                        [
                            new DataSetLibraryEntry()
                            {
                                Name = "Antarctic sea ice extent",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.Antarctic),
                                        SourceDataSetId = Guid.Parse("EC8AF0AC-215F-4D9C-9770-CC24EE24FBC7"),
                                        DataType = DataType.SeaIceExtent,
                                    },
                                ],
                            },
                            new DataSetLibraryEntry()
                            {
                                Name = "Arctic sea ice extent",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.Arctic),
                                        SourceDataSetId = Guid.Parse("4EA1E30B-AF74-4BE8-B55D-C28764CF384E"),
                                        DataType = DataType.SeaIceExtent,
                                    },
                                ],
                            },
                            new DataSetLibraryEntry()
                            {
                                Name = "Greenland ice melt area",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.Greenland),
                                        SourceDataSetId = Guid.Parse("6484a7f8-43bc-4b16-8c4d-9168f8d6699c"),
                                        DataType = DataType.IceMeltArea,
                                    },
                                ],
                            },
                        ],
                },
                new DataSetLibraryFolder
                {
                    Name = "Solar",
                    DataSets =
                        [
                            new DataSetLibraryEntry()
                            {
                                Name = "Sunspot number",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.Sun),
                                        SourceDataSetId = Guid.Parse("E2D9A74B-3C30-4332-8B22-26BB14A0BDC7"),
                                        DataType = DataType.SunspotNumber,
                                    },
                                ],
                            },
                            new DataSetLibraryEntry()
                            {
                                Name = "Total solar irradiance",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.Sun),
                                        SourceDataSetId = Guid.Parse("E45293F9-B7AC-4874-9544-25E006B6B998"),
                                        DataType = DataType.SolarRadiation,
                                    },
                                ],
                            },
                        ],
                },
                new DataSetLibraryFolder
                {
                    Name = "Southern hemisphere",
                    DataSets =
                        [
                            new DataSetLibraryEntry()
                            {
                                Name = "Ozone Hole area",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.SouthernHemi),
                                        SourceDataSetId = Guid.Parse("489E9F1A-057F-4EA8-9C48-0C86517D08A2"),
                                        DataType = DataType.OzoneHoleArea,
                                    },
                                ],
                            },
                            new DataSetLibraryEntry()
                            {
                                Name = "Ozone Hole column",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.SouthernHemi),
                                        SourceDataSetId = Guid.Parse("F3F925D6-8DBD-4080-9BF3-40D98D56FBEC"),
                                        DataType = DataType.OzoneHoleColumn,
                                    },
                                ],
                            },
                            new DataSetLibraryEntry()
                            {
                                Name = "Ozone Depleting Gas Index",
                                SourceSeriesSpecifications =
                                [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        LocationId = Region.RegionId(Region.SouthernHemi),
                                        SourceDataSetId = Guid.Parse("A8F34F99-0908-4BF3-8C7F-744574FFEADA"),
                                        DataType = DataType.Ozone,
                                    },
                                ],
                            },
                        ],
                },
            ]);
    }
}
