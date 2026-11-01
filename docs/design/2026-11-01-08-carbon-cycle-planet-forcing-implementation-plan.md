# Carbon Cycle simulation — Planet energy budget and forcing: implementation

- **Date:** 2026-11-01
- **Status:** Proposed
- **Author:** Patrick Lea (with Claude)
- **Scope:** The code for the first part of the [energy balance plan](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md): sunlight and albedo settings, the energy budget, the forcing schema with CO₂, solar and albedo ERF, the dead planet moved onto them, a sunlight input, and the forcing and over-time charts. `ClimateExplorer.CarbonCycle/Simulation`, `ClimateExplorer.Web.Client/CarbonSim`, and their unit tests.
- **Builds on:** [Sunlight, forcing and the planet energy balance](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md) (the physics; "energy balance §N" below); [A billion cars on a dead planet](2026-10-31-01-dead-planet-cars-scenario-plan.md) (the code it changes); [engine](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)
- **Branch context:** `development`

Section references without a plan name are to this document. The physics, numbers and sources are in the energy balance plan; this plan says where they go in the code.

## 1. Engine (`ClimateExplorer.CarbonCycle/Simulation`)

### 1.1 New types

| Type | What it is |
|---|---|
| `PlanetEnergySettings` (record) | `SolarConstant` (W/m², default 1361), `Albedo` (0.30), `GreenhouseEffect` (`g₀`), `WaterVapourFeedback` (`γ`, per K; 0 for a dry planet). `Validate(errors)` as the other settings. Static presets: `DryPlanet` (`g₀` = 0.10, `γ` = 0). Earth's preset comes with the Earth settings. |
| `ForcingKind` (enum) | `GreenhouseGas`, `Solar`, `Albedo`. Later: `Aerosol`, `ShortLived`, `LandUse`. |
| `ForcingAgent` (record) and `ForcingAgents` (catalog) | `Id`, `Name`, `Kind`, `Source`, as `Species` and `SpeciesCatalog`. v1: `CarbonDioxide`, `Solar`, `Albedo`. |
| `RadiativeForcing` (static) | The formulas (energy balance §3): `CarbonDioxide(c, c₀)`, `Solar(s, s₀, α)`, `Albedo(s₀, α, α₀)`. Constants carry their source in a comment and in `Sources`. |
| `Forcing` and `ForcingBreakdown` (records) | `Forcing(ForcingAgent Agent, double Value)`. `ForcingBreakdown(IReadOnlyList<Forcing> Agents, double NonRadiativeHeat)` with `Total` (the agents only) and `GreenhouseGas` (the `F_GHG` of energy balance §2). |
| `EnergyBudget` (record) | `IncomingSunlight`, `Reflected`, `Absorbed`, `HeatToSpace`, `NonRadiativeHeat`, `Imbalance`, all W/m². |
| `PlanetEnergyBalance` (class) | Built from `PlanetEnergySettings` and the reference state (`S₀`, `α₀`, and the reference gases). `StartTemperature()`, `HeatToSpace(T, F_GHG)`, `Feedback(T)` (`λ`), `Budget(T, forcing)`, and `Temperature(T, forcing, heatCapacity, seconds)`, the temperature after a substep (§1.3). No state of its own. |
| `IPlanetSimulation : ISimulation` | `PlanetEnergySettings Energy`, `ForcingBreakdown Forcing(SimulationState)`, `EnergyBudget Budget(SimulationState)`, `double WarmingPerDoubling`. The page shows the forcing panels for any simulation that implements it (§2.3). |

`Ice` is added to `SpeciesCatalog` (ΔfH −291.8 kJ/mol; to verify) and `SimulationState` gains `Ice` (mol), so the cars' water can fall as frost (energy balance §5).

### 1.2 `DeadPlanet` and `DeadPlanetSettings`

- **Settings:** `StartTemperature` and `ClimateFeedback` go; `Energy` (`PlanetEnergySettings.DryPlanet`) comes in. The start temperature is computed; the feedback follows from the temperature.
- **`Start()`** sets the reference state to the start (sunlight, albedo, CO₂) and the temperature to `PlanetEnergyBalance.StartTemperature()`. So `N` = 0 at time 0, and changing the sunlight or albedo changes the start, not the forcing (energy balance §3).
- **`Step()`** computes the forcing at the start of the substep, then the temperature through `PlanetEnergyBalance.Temperature`. The engines' heat stays as `H`.
- **Water** goes to `Ice` below 0 °C, with the latent heat of deposition (vapour to ice), and to `LiquidWater` above it. `LiquidWater` stays for a planet that warms past freezing.
- **`CarbonDioxideForcing` and `EngineHeat`** are replaced by `Forcing(state)` (CO₂ is one of its agents; the engines are `NonRadiativeHeat`) and `Budget(state)`. `CarbonDioxideForcingCoefficient` goes; the formula is in `RadiativeForcing`.
- **`WarmingPerDoubling`** is `RadiativeForcing.CarbonDioxide(2, 1) / Feedback(T₀)`, for the scenario text.

### 1.3 Stepping the temperature

`σT⁴` makes heat to space non-linear, so the exact exponential solution the dead planet uses today no longer holds as is. Each substep linearises about its starting temperature, with `λ = Feedback(T)`, and relaxes towards that substep's balance:

```
T_eq = T + N(T) / λ
T'   = T_eq + (T − T_eq) e^(−λ t / C)
```

This stays stable at any step, as today's does. A substep limit of 0.5 K of change keeps the linearisation accurate; the O₂ and CO₂ limits (1% each) stay. On the dead planet the air responds in ~5 weeks (`C/λ`), so at the faster speeds the temperature lands on the balance each substep.

### 1.4 Order and compatibility

`SealedHouse` doesn't change. `ISimulation` doesn't change; `IPlanetSimulation` extends it. Nothing else uses the removed members apart from `DeadPlanetScenario` and the tests, which change with them.

## 2. Web (`ClimateExplorer.Web.Client/CarbonSim`)

### 2.1 Inputs

- **`PlanetInputs`** (record): `SolarConstant` and `Albedo`. `ScenarioInputs` gains `Planet` beside `FossilFuels`. Its comment changes from "one member per input tab" to "one member per input tab, and the setting's own inputs".
- **`ScenarioCatalog`:** the dead planet's factory maps `Planet` into `DeadPlanetSettings.Energy`. Both sunlight and albedo flow through, so a later scenario or input can set the albedo with no engine change.
- **`SunlightInput`** (new; `.razor`, `.razor.cs`, `.razor.css`, as `CarCountInput`): a `TextInput` in W/m² with the hint "Today's sun: 1,361 W/m²". It accepts 1 to 100,000 W/m², and the parent validates as `CarCountInput` does. W/m² doesn't convert between unit systems ([web client §3](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md)). Shown on the dead planet's slide, under the car count.
- **Albedo has no control on screen.** It's in `PlanetInputs` and the settings, and a test checks it reaches the engine.

### 2.2 The dead planet's slide

- **Text:** the "warm sky" paragraph is replaced. The sun warms the planet, it starts where the heat it sends to space balances the sunlight it absorbs, and that's about −12 °C because dry air with a little CO₂ holds in little heat. The CO₂ the cars add holds in more, so it warms until it's in balance again. It warms ~1 °C per doubling, less than Earth, because it has no water vapour. The cars' water falls as frost. The albedo is Earth's, though the dry planet has no clouds. Numbers in the text come from the engine (start temperature, warming per doubling), not literals.
- **Summary list** (`ReactionSummary` child content): water fallen out (as frost or rain), "Heat kept in by the added CO₂" (CO₂ ERF) and "Heat from the engines" stay; the energy budget readouts go to the forcing panel (§2.3).

### 2.3 Forcing panel and charts

Two new `FloatingPanel`s on the page, shown while the active scenario's simulation is an `IPlanetSimulation`.

**"Radiative forcing"** (energy balance §7, view 1, and the readouts):
- **`ForcingChart`** (new; `.razor`, `.razor.cs`, `.razor.css`, `.razor.js`): a Chart.js horizontal bar chart (`indexAxis: 'y'`). There's a bar per built agent and one for the total, plus "Engines' heat (not a forcing)" below a divider. Warming bars are red and cooling bars blue, and the total is darker. The x-axis is W/m², with zero always in view.
  - **Why Chart.js:** it's already vendored (4.6.1) and handles categorical bars well. uPlot isn't vendored yet, and it's built for streaming time series, not bars. When the [web client plan §2](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md) brings in uPlot for the time series, this bar chart stays on Chart.js.
  - Its JS module has `create`, `update(canvas, labels, values)` and `destroy`, as `GasChart.razor.js`. It updates at the readout rate, not per tick.
- **Readouts** under it: temperature, absorbed sunlight, heat to space, imbalance, total ERF, warming per doubling of CO₂. The imbalance shows to 0.01 W/m², so the reader sees it stay small while the CO₂ ERF grows.
- **Accessibility:** the chart has an accessible name, and the readouts plus a visually hidden table of the bars' values are its text alternative.

**"Over time"** (energy balance §7, views 2 and 3):
- **`GasChart` becomes `TimeSeriesChart`**, with parameters for the y-axis title and unit and for the series. `GasChartSeries` becomes `TimeSeriesChartSeries`. The gas panel uses it unchanged in behaviour.
- The panel holds two `TimeSeriesChart`s. **Temperature** (°C or °F, through `UnitPreference`). **Forcing and energy** (W/m²): CO₂ ERF, total ERF, the imbalance, and absorbed sunlight against heat to space as change since the start. As change, the two energy lines sit on the same scale as the forcing, and they stay together near zero while the forcing climbs.
- Points are appended per tick, as the gas chart's are.

**Page** (`CarbonSimulator.razor`, `.razor.cs`, `.razor.css`): the two panels, the series lists, and the pushes on reset, tick and readout. Placement as the other panels, with their own default positions.

## 3. Tests

**`ClimateExplorer.CarbonCycle.UnitTests`**
- `RadiativeForcingTests`: CO₂ is 3.93 W/m² per doubling and 0 at the reference; solar is `ΔS/4 × (1 − α)` and 0 at constant sunlight; albedo is 0 at `α₀`; solar plus albedo equals the change in absorbed sunlight.
- `PlanetEnergyBalanceTests`: the start temperature matches the closed form and is in balance; more sunlight, less albedo or more `g₀` gives a warmer start; a constant forcing settles where `N` = 0; `Feedback` is `4·OLR/T` with `γ` = 0; one large step and many small ones agree within 0.02 K.
- `DeadPlanetTests` (rewritten where the warm sky was):
  - The default starts at ~261 K with `N` = 0.
  - After a century, CO₂ ERF is ~7.0 W/m² and the planet is ~1.9 K warmer.
  - When the engines stop, it's ~268 K.
  - The engines' heat is in `NonRadiativeHeat` and not in `Total`.
  - The water goes to `Ice` below 0 °C.
  - With no cars, it stays at the start temperature.
- `ValidationTests`: `PlanetEnergySettings` rejects a non-positive solar constant, and an albedo or `g₀` outside 0–1.

**`ClimateExplorer.Web.CarbonSim.UnitTests`**
- `ScenarioCatalogTests`: the dead planet's `PlanetInputs` (sunlight and albedo) reach `DeadPlanetSettings.Energy`.
- Sunlight input parsing and range, where it's plain C#.

`ClimateExplorer.Web.UnitTests` (CSS class usage) runs over the new scoped CSS unchanged.

## 4. Order

Each step builds and passes the unit tests on its own.

1. **Engine:** §1, with its tests. `DeadPlanetScenario` switches to `Forcing`/`Budget`, and its text gets the new physics (§2.2).
2. **Inputs:** `PlanetInputs`, `SunlightInput`, the catalog mapping (§2.1).
3. **Forcing panel:** `ForcingChart` and the readouts (§2.3).
4. **Over time:** `TimeSeriesChart` and the two charts (§2.3).

On landing, update the energy balance plan's status, the [simulation overview §5](2026-10-24-01-carbon-cycle-simulation-plan.md) (step 8, partly), and the dead planet plan (an addendum note that the warm sky is replaced).

## 5. Risks

- **The dead planet's numbers change.** Its start moves from 15 °C to about −12 °C and its warming per doubling from ~1.2 °C to ~1.1 °C, so its tests and text change together. `g₀` is provisional (energy balance §10).
- **CO₂ beyond the formula's range** (Q24): v1 extrapolates. The readout mark for "beyond the formula's range" is left to the step that settles Q24.
- **Chart.js streaming cost.** Two more time-series charts appended per tick. At the current tick rate and the gas chart's point counts, this should be fine; if it isn't, push at the readout rate instead, as the bar chart does.

## Summary

Adds the forcing schema and the energy budget to the engine. That means `PlanetEnergySettings`, a `ForcingAgent` catalog with CO₂, solar and albedo, `RadiativeForcing`, `ForcingBreakdown`, `EnergyBudget`, `PlanetEnergyBalance` and an `IPlanetSimulation` interface. The dead planet moves onto them: it's warmed by the sun, starts in balance at its own temperature, and reports forcing by agent. On the page, the reader gets a sunlight input (albedo is carried but not shown) and a radiative forcing panel with a Chart.js bar chart and energy readouts. An over-time panel charts temperature and forcing, built from the gas chart made generic.

### Change size

- **Projects:** `ClimateExplorer.CarbonCycle`, `ClimateExplorer.Web.Client`, `ClimateExplorer.CarbonCycle.UnitTests`, `ClimateExplorer.Web.CarbonSim.UnitTests`. Not changed: `SealedHouse`, `ClimateExplorer.WebApi`, `ClimateExplorer.Core`.

| Area | Added | Changed | Removed |
|---|---|---|---|
| Engine: settings, forcing agents and formulas, breakdown, budget, energy balance, `IPlanetSimulation` | 8 files, +450 | — | — |
| Engine: `DeadPlanet`, `DeadPlanetSettings`, `SimulationState`, `SpeciesCatalog`, `Sources` | — | 5 files, ~175 | — |
| Web: `PlanetInputs`, `SunlightInput` | 4 files, +140 | 2 files (`ScenarioInputs`, `ScenarioCatalog`), ~20 | — |
| Web: `ForcingChart` and the forcing panel | 4 files, +250 | — | — |
| Web: `GasChart` → `TimeSeriesChart` (renamed), over-time panel | — | 5 files, ~80 | — |
| Web: page, `DeadPlanetScenario` | — | 5 files, ~200 | — |
| Tests: `RadiativeForcingTests`, `PlanetEnergyBalanceTests` | 2 files, +300 | — | — |
| Tests: `DeadPlanetTests`, `ValidationTests`, `ScenarioCatalogTests` | — | 3 files, ~200 | — |
| **Total** | **18 files, +1,140** | **20 files, ~675** | — |

The renamed chart's 4 files are counted as changed; its own lines barely change.
