namespace ClimateExplorer.Web.Client.CarbonSim;

using System.Diagnostics;
using System.Globalization;
using ClimateExplorer.CarbonCycle.Simulation;
using ClimateExplorer.Web.Client.CarbonSim.Components;
using ClimateExplorer.Web.Client.Units;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// The Carbon-cycle simulator: one scenario so far, methane burning in a sealed house (simulation plan §3.1). The
/// fossil fuels tab sets the burner; the clock runs the simulation, and the floating panels show the house's air.
/// </summary>
public partial class CarbonSimulator : IAsyncDisposable
{
    private const string TabsId = "carbon-sim-tabs";
    private const string FossilTab = "fossil";
    private const string BurnerRateId = "carbon-sim-burner-rate";
    private const string SpeedId = "carbon-sim-speed";
    private const UnitOfMeasure BurnerRateUnit = UnitOfMeasure.GasLitresPerSecond;

    // In litres per second
    private const double MaxBurnerRate = 10_000;
    private const double HouseFireRate = 100;
    private const double HobRate = 0.07;

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

    private SealedHouse house = new(SealedHouseSettings.Default);
    private HouseState start = default!;
    private HouseState state = default!;
    private string selectedTab = FossilTab;
    private string? burnerRateText;
    private string? burnerRateError;
    private int secondsPerSecond = SimulationSpeed.All[0].SecondsPerSecond;
    private string? announcement;
    private IUnitFormatter? burnerRateUnits;
    private GasChart? gasChart;
    private CancellationTokenSource? runCancellation;
    private Task? runTask;

    [CascadingParameter]
    public IUnitFormatter Units { get; set; } = UnitFormatter.Metric;

    private bool IsRunning => runCancellation is not null;

    private string HouseVolumeText => CarbonSimFormat.Quantity(Units, Measure.Level(house.Settings.Volume, UnitOfMeasure.CubicMetres));

    private string StartTemperatureText => Units.Format(Measure.Level(house.Settings.StartTemperature - 273.15, UnitOfMeasure.DegreesCelsius));

    private string HouseFireRateText => CarbonSimFormat.Approximate(Units, Measure.Level(HouseFireRate, BurnerRateUnit));

    private string HobRateText => CarbonSimFormat.Approximate(Units, Measure.Level(HobRate, BurnerRateUnit));

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

    // The burner rate is typed in the reader's units, so a change of units rewrites it from the rate in use. That
    // also drops any invalid text, which was typed in the old units.
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(Units, burnerRateUnits))
        {
            return;
        }

        burnerRateUnits = Units;
        burnerRateText = CarbonSimFormat.Number(Units.ToDisplay(Measure.Level(house.Settings.BurnerRate * 1000, BurnerRateUnit)));
        burnerRateError = null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ResetChartAsync();
        }
    }

    private void OnSelectedTabChanged(string name)
    {
        selectedTab = name;
    }

    private void OnSpeedChanged(int value)
    {
        secondsPerSecond = value;
    }

    // Changing the burner changes the starting events, so the run goes back to time 0 and pauses (simulation plan §5.1).
    private async Task OnBurnerRateChangedAsync(string? text)
    {
        burnerRateText = text;

        var parsed = double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value);
        var litresPerSecond = Units.FromDisplay(value, BurnerRateUnit);

        if (!parsed
            || !double.IsFinite(litresPerSecond)
            || litresPerSecond <= 0
            || litresPerSecond > MaxBurnerRate)
        {
            burnerRateError = $"Enter a rate more than 0 and up to {CarbonSimFormat.Quantity(Units, Measure.Level(MaxBurnerRate, BurnerRateUnit))}.";
            await PauseAsync();
            return;
        }

        burnerRateError = null;
        house = new SealedHouse(SealedHouseSettings.Default with { BurnerRate = litresPerSecond / 1000 });
        await RestartAsync();
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
