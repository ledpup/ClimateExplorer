namespace ClimateExplorer.Web.Client.Components.Chart.DataSetBrowser;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.UiModel;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Data set browser for data sets specific to a location: its own observations, and (when a previous
/// location is available) data sets comparing the current location against it.
/// </summary>
public partial class LocalDataSetBrowser
{
    [Parameter]
    [EditorRequired]
    public IEnumerable<DataSetDefinitionViewModel>? DataSetDefinitions { get; set; }

    [Parameter]
    public EventCallback<DataSetLibraryEntry> OnAddDataSet { get; set; }

    [Parameter]
    [EditorRequired]
    public Location? CurrentLocation { get; set; }

    [Parameter]
    public Location? PreviousLocation { get; set; }

    [Parameter]
    public IReadOnlyList<ChartSeriesDefinition>? ChartSeriesList { get; set; }

    [Parameter]
    public IReadOnlyList<SeriesWithData>? SeriesWithData { get; set; }

    [Parameter]
    public EventCallback OnTrendsChanged { get; set; }

    private List<DataSetLibraryFolder>? RootFolders { get; set; }

    /// <summary>
    /// Chart series eligible for "add a trend to an existing series" from this tab: on the chart,
    /// tied to a location rather than a region (see <see cref="ChartSeriesDefinition.IsGlobalSeries"/>
    /// - global series belong on the Global tab instead), with data, and plotted against a year axis
    /// (trends need a linear year x-axis). Not filtered by the three-trend cap - a series already at
    /// the cap stays pickable so its existing trends remain visible and editable (including
    /// removable, to free up a slot); <see cref="AddTrendSection"/> hides its own "Add trend" button
    /// per-series once that series reaches the cap.
    /// </summary>
    private List<ChartSeriesDefinition> LocalTrendEligibleSeries =>
        ChartSeriesList?
            .Where(x => x.DataAvailable && !x.IsGlobalSeries && x.BinGranularity == BinGranularities.ByYear)
            .ToList()
        ?? [];

    private bool ShowAdjustedDataSets { get; set; } = true;

    private DataAdjustment SelectedDataAdjustment => ShowAdjustedDataSets ? DataAdjustment.Adjusted : DataAdjustment.Unadjusted;

    protected override void OnParametersSet()
    {
        RootFolders = [];

        if (DataSetDefinitions is null || CurrentLocation is null)
        {
            return;
        }

        var currentLocationFolder =
            new DataSetLibraryFolder()
                {
                    Name = CurrentLocation.Name + " observations",
                    DataSets = [],
                };

        var measurements = DataSetDefinitionViewModel.GetMeasurementsForLocation(DataSetDefinitions!, CurrentLocation.Id);

        foreach (var measurement in measurements)
        {
            var dsd = measurement.Item1;
            var md = measurement.Item2;

            currentLocationFolder.DataSets.Add(
                new DataSetLibraryEntry()
                    {
                        SourceSeriesSpecifications =
                        [
                            new DataSetLibraryEntry.SourceSeriesSpecification
                            {
                                DataType = md.DataType,
                                DataAdjustment = md.DataAdjustment,
                                SourceDataSetId = dsd.Id,
                                LocationId = CurrentLocation.Id,
                                LocationName = CurrentLocation.Name,
                            },
                        ],
                        SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                        Name = BuildDataSetName(md, dsd),
                    });
        }

        // Daily temperature range
        var bestAvailableTempMax = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions!, CurrentLocation.Id, DataSubstitute.DailyMaxTemperatureDataMatches(), throwIfNoMatch: false);
        var bestAvailableTempMin = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions!, CurrentLocation.Id, DataSubstitute.DailyMinTemperatureDataMatches(), throwIfNoMatch: false);

        if (bestAvailableTempMax != null && bestAvailableTempMin != null)
        {
            currentLocationFolder.DataSets.Add(
                new DataSetLibraryEntry()
                    {
                        SourceSeriesSpecifications =
                        [
                            new DataSetLibraryEntry.SourceSeriesSpecification
                            {
                                DataType = DataType.TempMax,
                                DataAdjustment = bestAvailableTempMax.MeasurementDefinition!.DataAdjustment,
                                SourceDataSetId = bestAvailableTempMax.DataSetDefinition!.Id,
                                LocationId = CurrentLocation.Id,
                                LocationName = CurrentLocation.Name,
                            },
                            new DataSetLibraryEntry.SourceSeriesSpecification
                            {
                                DataType = DataType.TempMin,
                                DataAdjustment = bestAvailableTempMin.MeasurementDefinition!.DataAdjustment,
                                SourceDataSetId = bestAvailableTempMin.DataSetDefinition!.Id,
                                LocationId = CurrentLocation.Id,
                                LocationName = CurrentLocation.Name,
                            },
                        ],
                        SeriesDerivationType = SeriesDerivationTypes.DifferenceBetweenTwoSeries,
                        Name = "Daily temperature range",
                    });

            if (!measurements.Any(x => x.Item2.DataType == DataType.TempMean))
            {
                currentLocationFolder.DataSets.Add(
                    new DataSetLibraryEntry()
                    {
                        SourceSeriesSpecifications =
                        [
                            new DataSetLibraryEntry.SourceSeriesSpecification
                            {
                                DataType = DataType.TempMax,
                                DataAdjustment = bestAvailableTempMax.MeasurementDefinition!.DataAdjustment,
                                SourceDataSetId = bestAvailableTempMax.DataSetDefinition!.Id,
                                LocationId = CurrentLocation.Id,
                                LocationName = CurrentLocation.Name,
                            },
                            new DataSetLibraryEntry.SourceSeriesSpecification
                            {
                                DataType = DataType.TempMin,
                                DataAdjustment = bestAvailableTempMin.MeasurementDefinition!.DataAdjustment,
                                SourceDataSetId = bestAvailableTempMin.DataSetDefinition!.Id,
                                LocationId = CurrentLocation.Id,
                                LocationName = CurrentLocation.Name,
                            },
                        ],
                        SeriesDerivationType = SeriesDerivationTypes.AverageOfMultipleSeries,
                        Name = "Average of maximum and minimum temperatures",
                    });
            }
        }

        currentLocationFolder.DataSets = [.. currentLocationFolder.DataSets.Where(ds => MatchesAdjustmentFilter(ds, SelectedDataAdjustment))];

        RootFolders.Add(currentLocationFolder);

        if (PreviousLocation != null && PreviousLocation.Id != CurrentLocation.Id)
        {
            var measurementsForPreviousLocation = DataSetDefinitionViewModel.GetMeasurementsForLocation(DataSetDefinitions!, PreviousLocation.Id);

            var comparisonFolder =
                new DataSetLibraryFolder()
                {
                    Name = $"{CurrentLocation.Name} relative to {PreviousLocation.Name}",
                    DataSets = [],
                };

            foreach (var measurementAtCurrentLocation in measurements)
            {
                // Look for a matching measurement at new site
                var bestMatchingMeasurementAtPreviousLocation =
                    measurementsForPreviousLocation
                    .SingleOrDefault(x => x.Item2.DataType == measurementAtCurrentLocation.Item2.DataType
                                    && x.Item2.DataAdjustment == measurementAtCurrentLocation.Item2.DataAdjustment
                                    && x.Item2.DataResolution == measurementAtCurrentLocation.Item2.DataResolution);

                if (bestMatchingMeasurementAtPreviousLocation != null)
                {
                    comparisonFolder.DataSets.Add(
                        new DataSetLibraryEntry()
                        {
                            SourceSeriesSpecifications =
                            [
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        DataType = bestMatchingMeasurementAtPreviousLocation.Item2.DataType,
                                        DataAdjustment = bestMatchingMeasurementAtPreviousLocation.Item2.DataAdjustment,
                                        SourceDataSetId = bestMatchingMeasurementAtPreviousLocation.Item1.Id,
                                        LocationId = PreviousLocation.Id,
                                        LocationName = PreviousLocation.Name,
                                    },
                                    new DataSetLibraryEntry.SourceSeriesSpecification
                                    {
                                        DataType = measurementAtCurrentLocation.Item2.DataType,
                                        DataAdjustment = measurementAtCurrentLocation.Item2.DataAdjustment,
                                        SourceDataSetId = measurementAtCurrentLocation.Item1.Id,
                                        LocationId = CurrentLocation.Id,
                                        LocationName = CurrentLocation.Name,
                                    },
                            ],
                            SeriesDerivationType = SeriesDerivationTypes.DifferenceBetweenTwoSeries,
                            Name = BuildDataSetName(measurementAtCurrentLocation.Item2, measurementAtCurrentLocation.Item1),
                        });
                }
            }

            comparisonFolder.DataSets = [.. comparisonFolder.DataSets.Where(ds => MatchesAdjustmentFilter(ds, SelectedDataAdjustment))];

            if (comparisonFolder.DataSets.Count > 0)
            {
                RootFolders.Add(comparisonFolder);
            }
        }
    }

    private static string BuildDataSetName(MeasurementDefinitionViewModel md, DataSetDefinitionViewModel dsd)
    {
        List<string> segments = [];

        switch (md.DataType)
        {
            case DataType.TempMax:
                segments.Add("Temperature");
                segments.Add($"{md.DataResolution} maximum");

                if (md.DataAdjustment != null)
                {
                    segments.Add(md.DataAdjustment.ToString()!);
                }

                break;

            case DataType.TempMin:
                segments.Add("Temperature");
                segments.Add($"{md.DataResolution} minimum");

                if (md.DataAdjustment != null)
                {
                    segments.Add(md.DataAdjustment.ToString()!);
                }

                break;

            case DataType.TempMean:
                segments.Add("Temperature");
                segments.Add($"{md.DataResolution} mean");

                if (md.DataAdjustment != null)
                {
                    segments.Add(md.DataAdjustment.ToString()!);
                }

                break;

            case DataType.Precipitation:
                segments.Add("Precipitation");
                segments.Add(md.DataResolution.ToString());
                break;

            case DataType.SolarRadiation:
                segments.Add("Solar radiation");
                segments.Add(md.DataResolution.ToString());
                break;

            case DataType.SeaIceExtent:
                segments.Add("Sea ice extent");
                break;
        }

        return string.Join(" | ", segments);
    }

    /// <summary>
    /// A data set entry matches the adjustment filter if none of its source series have an adjustment
    /// concept (e.g. CO₂, precipitation), or if every source series that does have one matches the
    /// requested adjustment.
    /// </summary>
    private static bool MatchesAdjustmentFilter(DataSetLibraryEntry entry, DataAdjustment adjustment)
    {
        return entry.SourceSeriesSpecifications is null
            || entry.SourceSeriesSpecifications.All(sss => sss.DataAdjustment is null || sss.DataAdjustment == adjustment);
    }

    private void OnAdjustedChanged(bool value)
    {
        ShowAdjustedDataSets = value;
        OnParametersSet();
    }
}
