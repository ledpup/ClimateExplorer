namespace ClimateExplorer.Web.Client.CarbonSim;

using System.Diagnostics;
using ClimateExplorer.CarbonCycle.Simulation;
using ClimateExplorer.Web.Client.CarbonSim.Common;
using ClimateExplorer.Web.Client.CarbonSim.Components;
using ClimateExplorer.Web.Client.CarbonSim.Scenarios;
using ClimateExplorer.Web.Client.Units;
using Microsoft.AspNetCore.Components;

/// <summary>
/// The Carbon-cycle simulator. The fossil fuels tab holds a carousel of scenarios; so far the only real one is
/// methane burning in a sealed house (simulation plan §3.1). The active scenario's slide has its inputs; the clock
/// runs the simulation, and the floating panels show the house's air.
/// </summary>
public partial class CarbonSimulator : IAsyncDisposable
{
    private const string TabsId = "carbon-sim-tabs";
    private const string FossilTab = "fossil";
    private const string SpeedId = "carbon-sim-speed";

    // A longer gap between ticks, such as while the browser tab is in the background, isn't caught up
    private const double MaxTickSeconds = 0.5;

    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(100);

    // Categorical slots in fixed order, one per gas in HouseState.Gases. N₂ and Ar would flatten the others, and CH₄
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

    // The slides beside the active one show their scenario's own inputs at time 0
    private readonly Dictionary<Scenario, (SealedHouse House, HouseState Start)> previews = [];

    private int scenarioIndex;
    private Scenario scenario = ScenarioCatalog.All[0];
    private ScenarioInputs inputs = ScenarioCatalog.All[0].Inputs;
    private bool inputInvalid;
    private SealedHouse house = HouseFor(ScenarioCatalog.All[0], ScenarioCatalog.All[0].Inputs);
    private HouseState start = default!;
    private HouseState state = default!;
    private string selectedTab = FossilTab;
    private int secondsPerSecond = SimulationSpeed.All[0].SecondsPerSecond;
    private string? announcement;
    private GasChart? gasChart;
    private CancellationTokenSource? runCancellation;
    private Task? runTask;

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    private bool IsRunning => runCancellation is not null;

    private IReadOnlyList<GasChartSeries> ChartSeries =>
        state.Gases
            .Select((gas, i) => new GasChartSeries(gas.Species.ToDisplayString(), GasStyles[i].Colour, GasStyles[i].Hidden))
            .ToList();

    public async ValueTask DisposeAsync()
    {
        await PauseAsync();
    }

    protected override void OnInitialized()
    {
        start = house.Start();
        state = start;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ResetChartAsync();
        }
    }

    // Inputs without fossil fuels leave the burner off
    private static SealedHouse HouseFor(Scenario scenario, ScenarioInputs inputs) =>
        new(scenario.Setting with { BurnerRate = inputs.FossilFuels?.MethaneBurnerRate ?? 0 });

    private Dictionary<string, object> ScenarioParameters(CarouselSlide<Scenario> slide)
    {
        if (!slide.IsActive)
        {
            var (previewHouse, previewStart) = Preview(slide.Item);

            return new()
            {
                [nameof(ScenarioComponent.Scenario)] = slide.Item,
                [nameof(ScenarioComponent.Inputs)] = slide.Item.Inputs,
                [nameof(ScenarioComponent.House)] = previewHouse,
                [nameof(ScenarioComponent.State)] = previewStart,
            };
        }

        return new()
        {
            [nameof(ScenarioComponent.Scenario)] = scenario,
            [nameof(ScenarioComponent.Inputs)] = inputs,
            [nameof(ScenarioComponent.InputsChanged)] = EventCallback.Factory.Create<ScenarioInputs>(this, OnInputsChangedAsync),
            [nameof(ScenarioComponent.InputInvalid)] = EventCallback.Factory.Create(this, OnInputInvalidAsync),
            [nameof(ScenarioComponent.House)] = house,
            [nameof(ScenarioComponent.State)] = state,
        };
    }

    private (SealedHouse House, HouseState Start) Preview(Scenario item)
    {
        if (!previews.TryGetValue(item, out var preview))
        {
            var previewHouse = HouseFor(item, item.Inputs);
            preview = (previewHouse, previewHouse.Start());
            previews[item] = preview;
        }

        return preview;
    }

    // A scenario brings its own inputs (simulation plan §2.4), replacing the reader's, so the run goes back to time 0
    // and pauses.
    private async Task OnScenarioChangedAsync(int index)
    {
        scenarioIndex = index;
        scenario = ScenarioCatalog.All[index];
        await OnInputsChangedAsync(scenario.Inputs);
    }

    // Changing an input changes the starting events, so the run goes back to time 0 and pauses (simulation plan §5.1).
    private async Task OnInputsChangedAsync(ScenarioInputs value)
    {
        inputs = value;
        inputInvalid = false;
        house = HouseFor(scenario, inputs);
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

    private void OnSpeedChanged(int value)
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
        start = house.Start();
        state = start;
        announcement = null;
        await ResetChartAsync();
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
                state = house.Advance(state, realSeconds * secondsPerSecond);

                if (wasBurning && !state.FlameOn)
                {
                    announcement = $"Flame out at {CarbonSimFormat.Duration(state.FlameOutTime ?? state.Time)}: oxygen below {CarbonSimFormat.Share(house.Settings.OxygenCutOff)}.";
                }

                await AppendChartPointAsync();
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ResetChartAsync()
    {
        if (gasChart is null)
        {
            return;
        }

        await gasChart.ResetAsync();
        await AppendChartPointAsync();
    }

    private Task AppendChartPointAsync() =>
        gasChart?.AppendAsync(state.Time, state.Gases.Select(gas => 100 * gas.Moles / state.GasMoles).ToList())
        ?? Task.CompletedTask;
}
