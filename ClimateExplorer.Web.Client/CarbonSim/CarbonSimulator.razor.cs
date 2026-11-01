namespace ClimateExplorer.Web.Client.CarbonSim;

using System.Diagnostics;
using ClimateExplorer.CarbonCycle.Simulation;
using ClimateExplorer.Web.Client.CarbonSim.Common;
using ClimateExplorer.Web.Client.CarbonSim.Components;
using ClimateExplorer.Web.Client.CarbonSim.Scenarios;
using ClimateExplorer.Web.Client.Units;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The Carbon-cycle simulator. The fossil fuels tab holds a carousel of scenarios: methane burning in a sealed house
/// (simulation boxes plan §1.1) and cars on a dead planet. The active scenario's slide has its inputs; the clock runs the
/// simulation, and the floating panels show its air. A planet also gets its forcing and energy budget, now and over time.
/// </summary>
public partial class CarbonSimulator : IAsyncDisposable
{
    private const string TabsId = "carbon-sim-tabs";
    private const string FossilTab = "fossil";
    private const string SpeedId = "carbon-sim-speed";

    // A longer gap between ticks, such as while the browser tab is in the background, isn't caught up
    private const double MaxTickSeconds = 0.5;

    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(100);

    // Categorical slots in fixed order, one per gas in SimulationState.Gases. N₂ and Ar would flatten the others, and CH₄
    // is too small to see, so they start hidden; the legend shows them.
    private static readonly (string Colour, bool Hidden)[] GasStyles =
    [
        ("#2a78d6", true),
        ("#eb6834", false),
        ("#1baf7a", true),
        ("#eda100", false),
        ("#e87ba4", false),
        ("#008300", true),
    ];

    // A planet's lines. The forcing is in reds, as warming; the absorbed sunlight and heat to space stay together, so
    // one is dashed. The total forcing is dashed for the same reason: with CO₂ the only agent that changes, it's the same.
    private static readonly IReadOnlyList<TimeSeriesChartSeries> TemperatureSeries = [new("Temperature", "#d6604d")];

    private static readonly IReadOnlyList<TimeSeriesChartSeries> EnergySeries =
    [
        new("CO₂ forcing", "#d6604d"),
        new("Total forcing", "#b2182b", Dashed: true),
        new("Imbalance", "#1baf7a"),
        new("Absorbed sunlight, change", "#eda100"),
        new("Heat to space, change", "#2a78d6", Dashed: true),
    ];

    // The slides beside the active one show their scenario's own inputs at time 0
    private readonly Dictionary<Scenario, (ISimulation Simulation, SimulationState Start)> previews = [];

    private int scenarioIndex;
    private Scenario scenario = ScenarioCatalog.All[0];
    private ScenarioInputs inputs = ScenarioCatalog.All[0].Inputs;
    private bool inputInvalid;
    private ISimulation simulation = ScenarioCatalog.All[0].CreateSimulation(ScenarioCatalog.All[0].Inputs);
    private SimulationState start = default!;
    private SimulationState state = default!;
    private string selectedTab = FossilTab;
    private double secondsPerSecond = SimulationSpeed.All[0].SecondsPerSecond;
    private string? announcement;
    private TimeSeriesChart? gasChart;
    private TimeSeriesChart? temperatureChart;
    private TimeSeriesChart? energyChart;

    // The charts are reset after the render that follows a restart, once the planet charts are on the page or gone
    private bool chartsStale = true;

    // The units the temperature chart's points are in
    private IUnitFormatter chartUnits = UnitFormatter.Metric;
    private CancellationTokenSource? runCancellation;
    private Task? runTask;

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    private bool IsRunning => runCancellation is not null;

    private IReadOnlyList<TimeSeriesChartSeries> GasSeries =>
        state.Gases
            .Select((gas, i) => new TimeSeriesChartSeries(gas.Species.ToDisplayString(), GasStyles[i].Colour, GasStyles[i].Hidden))
            .ToList();

    public async ValueTask DisposeAsync()
    {
        await PauseAsync();
    }

    protected override void OnInitialized()
    {
        start = simulation.Start();
        state = start;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (chartsStale)
        {
            chartsStale = false;
            await ResetChartsAsync();
        }
        else if (Units.System != chartUnits.System)
        {
            await ConvertTemperatureChartAsync();
        }
    }

    // The unit after each value on a chart's axis, with a space where the unit setting puts one: "°C", " W/m²"
    private string AxisUnit(UnitOfMeasure unit) =>
        Units.IsTight(unit) ? Units.ShortLabel(unit) : " " + Units.ShortLabel(unit);

    private Dictionary<string, object> ScenarioParameters(CarouselSlide<Scenario> slide)
    {
        if (!slide.IsActive)
        {
            var (previewSimulation, previewStart) = Preview(slide.Item);

            return new()
            {
                [nameof(ScenarioComponent.Scenario)] = slide.Item,
                [nameof(ScenarioComponent.Inputs)] = slide.Item.Inputs,
                [nameof(ScenarioComponent.Simulation)] = previewSimulation,
                [nameof(ScenarioComponent.State)] = previewStart,
            };
        }

        return new()
        {
            [nameof(ScenarioComponent.Scenario)] = scenario,
            [nameof(ScenarioComponent.Inputs)] = inputs,
            [nameof(ScenarioComponent.InputsChanged)] = EventCallback.Factory.Create<ScenarioInputs>(this, OnInputsChangedAsync),
            [nameof(ScenarioComponent.InputInvalid)] = EventCallback.Factory.Create(this, OnInputInvalidAsync),
            [nameof(ScenarioComponent.Simulation)] = simulation,
            [nameof(ScenarioComponent.State)] = state,
        };
    }

    private (ISimulation Simulation, SimulationState Start) Preview(Scenario item)
    {
        if (!previews.TryGetValue(item, out var preview))
        {
            var previewSimulation = item.CreateSimulation(item.Inputs);
            preview = (previewSimulation, previewSimulation.Start());
            previews[item] = preview;
        }

        return preview;
    }

    // A scenario brings its own inputs (simulation scenarios plan §4), replacing the reader's, so the run goes back to
    // time 0 and pauses.
    private async Task OnScenarioChangedAsync(int index)
    {
        scenarioIndex = index;
        scenario = ScenarioCatalog.All[index];
        await OnInputsChangedAsync(scenario.Inputs);
    }

    // Changing an input changes the starting events, so the run goes back to time 0 and pauses (simulation clock plan §1).
    private async Task OnInputsChangedAsync(ScenarioInputs value)
    {
        inputs = value;
        inputInvalid = false;
        simulation = scenario.CreateSimulation(inputs);
        await RestartAsync();
    }

    private async Task OnInputInvalidAsync()
    {
        inputInvalid = true;
        await PauseAsync();
    }

    private void OnSelectedTabChanged(string name)
    {
        selectedTab = name;
    }

    private void OnSpeedChanged(double value)
    {
        secondsPerSecond = value;
    }

    private async Task TogglePlayAsync()
    {
        if (IsRunning)
        {
            await PauseAsync();
        }
        else
        {
            runCancellation = new CancellationTokenSource();
            runTask = RunAsync(runCancellation.Token);
        }
    }

    private async Task RestartAsync()
    {
        await PauseAsync();
        start = simulation.Start();
        state = start;
        announcement = null;
        chartsStale = true;
    }

    private async Task PauseAsync()
    {
        if (runCancellation is null)
        {
            return;
        }

        runCancellation.Cancel();

        if (runTask is not null)
        {
            await runTask;
        }

        runCancellation.Dispose();
        runCancellation = null;
        runTask = null;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TickInterval);
        var last = Stopwatch.GetTimestamp();

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var now = Stopwatch.GetTimestamp();
                var realSeconds = Math.Min(Stopwatch.GetElapsedTime(last, now).TotalSeconds, MaxTickSeconds);
                last = now;

                var wasBurning = state.FlameOn;
                state = simulation.Advance(state, realSeconds * secondsPerSecond);

                if (wasBurning && !state.FlameOn)
                {
                    announcement = $"Flame out at {CarbonSimFormat.Duration(state.FlameOutTime ?? state.Time)}: oxygen below {CarbonSimFormat.Share(simulation.OxygenCutOff)}.";
                }

                await AppendChartPointsAsync();
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ResetChartsAsync()
    {
        // The planet charts are only on the page while the simulation is a planet; otherwise their references are stale
        if (simulation is not IPlanetSimulation)
        {
            temperatureChart = null;
            energyChart = null;
        }

        chartUnits = Units;

        TimeSeriesChart?[] charts = [gasChart, temperatureChart, energyChart];

        foreach (var chart in charts.OfType<TimeSeriesChart>())
        {
            await chart.ResetAsync();
        }

        await AppendChartPointsAsync();
    }

    // The reader changed the unit setting: the temperatures already drawn move to the new units, as a line v × scale +
    // offset from the old display units to the new
    private async Task ConvertTemperatureChartAsync()
    {
        double Convert(double value) =>
            Units.ToDisplay(Measure.Level(chartUnits.FromDisplay(value, UnitOfMeasure.DegreesCelsius), UnitOfMeasure.DegreesCelsius));

        var offset = Convert(0);
        var scale = Convert(1) - offset;
        chartUnits = Units;

        if (temperatureChart is not null)
        {
            await temperatureChart.ConvertAsync(scale, offset);
        }
    }

    private async Task AppendChartPointsAsync()
    {
        if (gasChart is not null)
        {
            await gasChart.AppendAsync(state.Time, state.Gases.Select(gas => 100 * gas.Moles / state.GasMoles).ToList());
        }

        if (simulation is not IPlanetSimulation planet || temperatureChart is null || energyChart is null)
        {
            return;
        }

        await temperatureChart.AppendAsync(
            state.Time,
            [chartUnits.ToDisplay(Measure.Level(state.Temperature - 273.15, UnitOfMeasure.DegreesCelsius))]);

        var forcing = planet.Forcing(state);
        var budget = planet.Budget(state);
        var startBudget = planet.Budget(start);

        // In the order of EnergySeries
        await energyChart.AppendAsync(
            state.Time,
            [
                forcing.Of(ForcingAgents.CarbonDioxide),
                forcing.Total,
                budget.Imbalance,
                budget.Absorbed - startBudget.Absorbed,
                budget.HeatToSpace - startBudget.HeatToSpace,
            ]);
    }
}
