namespace ClimateExplorer.Web.Client.Components.Chart.Trend;

using System.Globalization;
using Blazorise;
using ClimateExplorer.Web.Client.UiModel.Trends;
using Microsoft.AspNetCore.Components;

/// <summary>
/// One trend's worth of controls - regression type, period, predict-until, and its own "About this
/// trend" panel. Extracted out of <c>ChartSeriesView</c> so the same block can be instantiated once
/// per trend on a series (up to three), and, in a later stage, reused from the "Add data set" modal.
/// </summary>
public partial class ChartSeriesTrendControls
{
    private Validation? predictionYearsValidation;
    private ChartTrendPanel? trendPanel;
    private string predictionYearsText = string.Empty;

    /// <summary>The trend request this instance edits - one entry in a series' <c>Trends</c> list.</summary>
    [Parameter]
    public ChartSeriesTrendRequest? Request { get; set; }

    /// <summary>
    /// The fitted result for this specific request, or null when the build hasn't produced it yet.
    /// The dropdown can only offer periods that came back statistically significant, which is why
    /// the control needs the result rather than just the request.
    /// </summary>
    [Parameter]
    public ChartSeriesTrend? Trend { get; set; }

    /// <summary>"Trend 2" etc., shown only when the parent series has more than one trend.</summary>
    [Parameter]
    public string? SlotLabel { get; set; }

    [Parameter]
    public bool CanRemove { get; set; } = true;

    [Parameter]
    public EventCallback OnChanged { get; set; }

    [Parameter]
    public EventCallback OnRemove { get; set; }

    private IReadOnlyList<TrendWindow> SelectableTrendPeriods => Trend?.SignificantWindows ?? [];

    private TrendWindow SelectedTrendPeriod =>
        Request?.TrendPeriod is { } period && SelectableTrendPeriods.Contains(period)
            ? period
            : SelectableTrendPeriods[0];

    /// <summary>
    /// What the disabled dropdown says when there is nothing to select - either the trends haven't
    /// been fitted yet, or none of the periods cleared the significance threshold.
    /// </summary>
    private string UnavailableTrendPeriodValue => Trend is null
        ? "Calculating…"
        : Trend.UnavailableReason is not null
            ? "Not enough data"
            : "No significant trend";

    /// <summary>
    /// The year this trend's projection is measured forward from. See
    /// <see cref="ChartSeriesTrendRequest.TrendPredictionYears"/> - the projection is always a count
    /// of years past the end of the real data, shown and edited here as a calendar year for
    /// readability. Once the trend has been fitted, that's the series' own last data year;
    /// beforehand (the field is disabled in that case, but still needs something to display) it
    /// falls back to the current year as the closest available estimate.
    /// </summary>
    private int PredictUntilAnchorYear => Trend?.LastDataYear ?? DateTime.Now.Year;

    /// <summary>
    /// A fixed target year (typically set by a preset) is shown clamped into the same 1-100-year
    /// range as the duration field - the same clamp
    /// <c>ChartSeriesTrendCalculator</c> applies when it resolves the projection - so what's
    /// displayed always matches what's drawn. See
    /// <see cref="ChartSeriesTrendRequest.TrendPredictionTargetYear"/>.
    /// </summary>
    private int PredictUntilYear =>
        Request?.TrendPredictionTargetYear is { } targetYear
            ? PredictUntilAnchorYear + TrendPredictionRange.Clamp(targetYear - PredictUntilAnchorYear)
            : PredictUntilAnchorYear + (Request?.TrendPredictionYears ?? TrendPredictionRange.Default);

    private string PredictUntilTooltip =>
        $"The calendar year to project the trend forward to ({PredictUntilAnchorYear + TrendPredictionRange.Minimum}-{PredictUntilAnchorYear + TrendPredictionRange.Maximum})";

    private string PredictUntilValidationMessage =>
        $"Enter a year from {PredictUntilAnchorYear + TrendPredictionRange.Minimum} to {PredictUntilAnchorYear + TrendPredictionRange.Maximum}";

    private string AboutButtonLabel => SlotLabel is null ? "About this trend" : $"About this trend ({SlotLabel})";

    private string RemoveButtonLabel => SlotLabel is null ? "Remove this trend" : $"Remove this trend ({SlotLabel})";

    protected override void OnParametersSet()
    {
        if (Request is null)
        {
            return;
        }

        // Keep the text box in step with the request, including when a value arriving from a URL
        // has been clamped into range, or the anchor year has just become known (Trend went from
        // null to a real result). The input commits on blur rather than per keystroke, so this
        // doesn't fight with typing.
        if (!int.TryParse(predictionYearsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var current)
            || current != PredictUntilYear)
        {
            predictionYearsText = PredictUntilYear.ToString(CultureInfo.InvariantCulture);
        }
    }

    private async Task OnRegressionTypeChanged(TrendRegressionType regressionType)
    {
        Request!.RegressionType = regressionType;

        await OnChanged.InvokeAsync();
    }

    private async Task OnTrendPeriodChanged(TrendWindow window)
    {
        Request!.TrendPeriod = window;

        await OnChanged.InvokeAsync();
    }

    private async Task OnPredictionYearsChanged(string value)
    {
        predictionYearsText = value;

        if (predictionYearsValidation is null || predictionYearsValidation.Validate() != ValidationStatus.Success)
        {
            return;
        }

        var targetYear = int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        var years = targetYear - PredictUntilAnchorYear;

        if (years == Request!.TrendPredictionYears && Request.TrendPredictionTargetYear is null)
        {
            return;
        }

        // A hand-edited value is anchored to "now" like every other interactive control, not
        // pinned to a fixed calendar year the way a preset's TrendPredictionTargetYear is - so
        // editing this box always reverts to the duration-based field, even if a fixed target was
        // what produced the value currently showing.
        Request.TrendPredictionTargetYear = null;

        // Only the projected points depend on this - the regression and its significance don't - so
        // the chart just rebuilds; the user is never re-told about a period they can't select.
        Request.TrendPredictionYears = years;

        await OnChanged.InvokeAsync();
    }

    private void ValidatePredictionYears(ValidatorEventArgs e)
    {
        var text = Convert.ToString(e.Value);

        e.Status = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var targetYear)
            && TrendPredictionRange.IsValid(targetYear - PredictUntilAnchorYear)
                ? ValidationStatus.Success
                : ValidationStatus.Error;
    }

    private async Task OnAboutTrendClicked()
    {
        await trendPanel!.Show();
    }

    private async Task OnRemoveClicked()
    {
        await OnRemove.InvokeAsync();
    }
}
