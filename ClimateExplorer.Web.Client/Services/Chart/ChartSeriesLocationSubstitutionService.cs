namespace ClimateExplorer.Web.Client.Services.Chart;

using Blazorise.Snackbar;
using ClimateExplorer.Core;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.UiModel;
using static ClimateExplorer.Core.Enums;

public sealed class ChartSeriesLocationSubstitutionService : IChartSeriesLocationSubstitutionService
{
    public ChartLocationSubstitutionResult Substitute(ChartLocationSubstitutionContext context)
    {
        var location = context.Location;
        var messages = new List<SnackbarMessage>();
        var additionalCsds = new List<ChartSeriesDefinition>();
        var chartSeriesList = context.State.Series.ToList();

        foreach (var csd in chartSeriesList.ToArray())
        {
            foreach (var sss in csd.SourceSeriesSpecifications!)
            {
                if (!csd.IsLocked)
                {
                    SubstituteUnlockedSeries(context, csd, sss, location, messages);
                }
                else
                {
                    AddUnlockedDuplicateForNewLocation(context, csd, sss, location, additionalCsds);
                }
            }
        }

        var updatedSeries = chartSeriesList
            .Concat(additionalCsds)
            .ToList()
            .CreateNewListWithoutDuplicates();

        return new ChartLocationSubstitutionResult(context.State with { Series = updatedSeries }, messages);
    }

    private static void SubstituteUnlockedSeries(
        ChartLocationSubstitutionContext context,
        ChartSeriesDefinition csd,
        SourceSeriesSpecification sss,
        Location location,
        List<SnackbarMessage> messages)
    {
        if (csd.SourceSeriesSpecifications!.Length != 1 || context.Regions.Any(x => x.Id == sss.LocationId))
        {
            return;
        }

        sss.LocationId = location.Id;
        sss.LocationName = location.Name;

        var dataMatches = new List<DataSubstitute>
        {
            new()
            {
                DataType = sss.MeasurementDefinition!.DataType,
                DataAdjustment = sss.MeasurementDefinition.DataAdjustment,
            },
        };

        if (sss.MeasurementDefinition!.DataType == DataType.TempMax || sss.MeasurementDefinition!.DataType == DataType.TempMean)
        {
            dataMatches = sss.MeasurementDefinition!.DataAdjustment == DataAdjustment.Unadjusted
                ? DataSubstitute.UnadjustedTemperatureDataMatches()
                : DataSubstitute.StandardTemperatureDataMatches();
        }

        var dsd = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
            context.DataSetDefinitions,
            location.Id,
            dataMatches,
            throwIfNoMatch: false);

        if (dsd is null)
        {
            var dataType = sss.MeasurementDefinition.DataType.ToFriendlyName();
            messages.Add(new SnackbarMessage { Message = $"{dataType} data is not available at {location.FullTitle}.", Type = SnackbarColor.Warning });
            csd.DataAvailable = false;
            return;
        }

        csd.DataAvailable = true;
        sss.DataSetDefinition = dsd.DataSetDefinition!;
        UpdateMeasurementDefinition(sss);
    }

    private static void UpdateMeasurementDefinition(SourceSeriesSpecification sss)
    {
        var oldMd = sss.MeasurementDefinition!;
        var candidateMds = sss.DataSetDefinition!.MeasurementDefinitions!
            .Where(x => x.DataType == oldMd.DataType && x.DataAdjustment == oldMd.DataAdjustment)
            .ToArray();

        switch (candidateMds.Length)
        {
            case 0:
                candidateMds = sss.DataSetDefinition.MeasurementDefinitions!
                    .Where(x => x.DataType == oldMd.DataType)
                    .ToArray();

                if (candidateMds.Length == 1)
                {
                    sss.MeasurementDefinition = candidateMds.Single();
                }
                else
                {
                    var adjustedMd = candidateMds.SingleOrDefault(x => x.DataAdjustment == DataAdjustment.Adjusted);

                    if (adjustedMd is not null)
                    {
                        sss.MeasurementDefinition = adjustedMd;
                    }
                }

                break;

            case 1:
                sss.MeasurementDefinition = candidateMds.Single();
                break;

            default:
                throw new Exception("Unexpected condition: after changing location, while updating ChartSeriesDefinitions, there were multiple compatible MeasurementDefinitions for one CSD.");
        }
    }

    private static void AddUnlockedDuplicateForNewLocation(
        ChartLocationSubstitutionContext context,
        ChartSeriesDefinition csd,
        SourceSeriesSpecification sss,
        Location location,
        List<ChartSeriesDefinition> additionalCsds)
    {
        var newDsd = context.DataSetDefinitions.Single(x => x.Id == sss.DataSetDefinition!.Id);
        var newMd = newDsd.MeasurementDefinitions!
            .SingleOrDefault(x => x.DataType == sss.MeasurementDefinition!.DataType && x.DataAdjustment == sss.MeasurementDefinition.DataAdjustment);

        newMd ??= newDsd.MeasurementDefinitions!
            .SingleOrDefault(x => x.DataType == sss.MeasurementDefinition!.DataType && x.DataAdjustment == null);

        if (newMd is null)
        {
            return;
        }

        additionalCsds.Add(
            new ChartSeriesDefinition
            {
                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                SourceSeriesSpecifications =
                [
                    new SourceSeriesSpecification
                    {
                        DataSetDefinition = newDsd,
                        LocationId = location.Id,
                        LocationName = location.Name,
                        MeasurementDefinition = newMd,
                    },
                ],
                Aggregation = csd.Aggregation,
                BinGranularity = csd.BinGranularity,
                DisplayStyle = csd.DisplayStyle,
                IsLocked = false,
                ShowTrendline = csd.ShowTrendline,
                Smoothing = csd.Smoothing,
                SmoothingWindow = csd.SmoothingWindow,
                Value = csd.Value,
                Year = csd.Year,
                SeriesTransformation = csd.SeriesTransformation,
                GroupingThreshold = csd.GroupingThreshold,
                MinimumDataResolution = csd.MinimumDataResolution,
            });
    }
}
