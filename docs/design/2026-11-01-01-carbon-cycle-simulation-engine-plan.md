# Carbon Cycle simulation — Engine core

- **Date:** 2026-11-01 (split from the [simulation overview](2026-10-24-01-carbon-cycle-simulation-plan.md), written 2026-10-24)
- **Status:** Proposed. Partly built in the first slice and the dead planet: adaptive substeps, the limiting-reagent clamp for one reaction, and the O₂ cut-off, in two engines behind `ISimulation` (§1).
- **Author:** Patrick Lea (with Claude)
- **Scope:** `ClimateExplorer.CarbonCycle/Simulation`: the state, settings and event types, `Advance(state, dt)`, the limiting-reagent clamp, adaptive substeps, event timing, cut-offs, conservation. Pure and UI-free, in the Carbon Cycle model project ([chemistry engine plan §3](2026-10-23-02-carbon-cycle-tab1-chemistry-engine-plan.md)), alongside the chemistry catalog. It must run in the WebAssembly client.
- **Builds on:** [Carbon Cycle simulation — Overview](2026-10-24-01-carbon-cycle-simulation-plan.md); [Tab 1 chemistry engine](2026-10-23-02-carbon-cycle-tab1-chemistry-engine-plan.md)
- **Branch context:** `development`

Section references without a plan name are to this document. "Overview" is the [Carbon Cycle page overview](2026-10-21-01-carbon-cycle-plan.md); the other simulation plans are named by their topic, as listed in the [simulation overview §3](2026-10-24-01-carbon-cycle-simulation-plan.md).

## 1. One engine, many settings

```
Setting                 // what a set-up event creates
  Id, Name
  Enclosure             // Box: volume, floor area, wall area, wall type (Opaque | Glass | Ideal), U, h, G,
                        //   heat capacities, path length, sky | Planet: area, mixed-layer depth
  Processes             // the processes switched on, each with its parameters
  TemperatureModel      // BoxHeatBalance | PlanetEnergyBalance
  CutOffs               // O₂ limit, N₂ vent on/off, pressure target
  Drivers               // sunlight (TSI series, latitude), outside air, ground and sky temperatures

InputEvent
  Time                  // s since start; 0 for starting events
  Scenario              // which scenario, or all
  Kind                  // SetUp(Setting, start date) | Emission(source, species or fuel, Pulse | Rate + duration)
                        //   | StopEmission(id) | Eruption(count, size)

Scenario
  Id, Name
  StartingEvents        // the InputEvents at time 0: exactly one SetUp, then emissions and interventions

SimulationState
  Time                  // s since start
  Pools                 // mol per species per reservoir: enclosure gas, liquid water, ice, plants, rock,
                        //   ocean surface, ocean deep, stratospheric sulfate
  Temperatures          // K: gas and floor (box), or mixed layer (planet)
  ActiveEmissions       // emissions running now, with their remaining duration
  OutcomeEvents         // cut-offs and thresholds found so far
```

The six example scenarios are six `Setting` values, each run with the input tabs' emissions. A reader could make more by editing one (Level 2: "duplicate and edit"). A new setting needs new engine code only if it needs a new process. The processes are in the [boxes](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md) and [planet](2026-11-01-04-carbon-cycle-simulation-planet-plan.md) plans; the temperature models in the boxes and [energy balance](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md) plans; the events in the [scenarios plan](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md).

**What's built (2026-10-31).** Two engines, `SealedHouse` and `DeadPlanet`, behind an `ISimulation` interface (`Reaction`, `OxygenCutOff`, `Start()`, `Advance()`), with a flat `SimulationState` (one field per gas, one temperature). The [dead planet plan §5](2026-10-31-01-dead-planet-cars-scenario-plan.md) chose two engines over one, because the box and planet heat and pressure models share little; the interface lets them merge into the `Setting` model above later without the page changing.

## 2. Advancing by a step

- **One operation.** The engine's one operation is `Advance(state, dt)`: move this scenario forward by `dt` seconds of simulated time. It keeps no memory of earlier step sizes, so the step can change on every call. A new speed from the reader applies on the next tick. The speeds and the runner that picks `dt` are in the [clock plan](2026-11-01-06-carbon-cycle-simulation-clock-plan.md).
- **Input events.** An event is applied at its exact simulated time: `Advance` splits its `dt` at any event that falls inside it. An eruption is an injection into the state; an emission starts or stops a process.

## 3. No reservoir goes negative

Every process is a reaction with a rate, in mol/s of reaction. Before a substep is applied:

1. Compute each process's wanted rate.
2. For each pool, add up what all processes want to take from it in this substep. If that exceeds what's there, the pool's factor is (amount there) ÷ (amount wanted); otherwise 1.
3. Scale each process by the smallest factor among the pools it takes from: its limiting reagent.
4. Apply.

After step 3, no pool loses more than it holds, because every process drawing on it was scaled by at most that pool's factor. Inflows in the same substep are ignored, which keeps the rule conservative. Scaling a whole reaction, rather than each flux on its own, keeps every element balanced. A clamp is recorded on the state, so tests and the Level 2 accuracy readout can see when one happened.

## 4. Substeps and event timing

The clamp prevents negatives but not inaccuracy. At 1,000 yr/s and 60 ticks a second, each tick is ~17 years, while a big flame in a house lasts about a minute. So `Advance` splits its `dt` into substeps:
- **Size:** the largest substep in which no pool changes by more than a set fraction (default 1%, the Level 2 accuracy control) and no temperature changes by more than a set amount. When fluxes are small against their pools, substeps grow to the whole `dt`. Examples: a house after its flame is out, or an Earth near balance.
- **Event timing:** if a cut-off threshold would be crossed inside a substep, the substep is shortened to land on the crossing (linear estimate, then one refinement). The event's time is therefore right whatever step the reader chose.
- **Fixed-step toggle (Level 2):** switches substeps off, so the reader can watch a big step get the answer wrong. The clamp still applies.
- **Budget:** each scenario has a cap on substeps per tick ([clock §2](2026-11-01-06-carbon-cycle-simulation-clock-plan.md)). If it's hit, the runner slows the clock for all scenarios together rather than skipping ahead.

The integrator is forward Euler on these adaptive substeps. It's easy to explain and, with the 1% rule, accurate enough. The tests (§7) check convergence as the fraction shrinks. A process with a closed-form solution over a substep may use it instead; the dead planet's temperature does, so its long steps stay stable.

## 5. Cut-offs

A burning emission stops at the first of:
1. **O₂ cut-off:** O₂ falls below the combustion limit. Default ~15% by volume (to verify; flammability data put methane's limiting O₂ at ~12% in N₂, higher in CO₂). It is an editable assumption, with a note that the real limit rises with CO₂ and falls with temperature.
2. **N₂ cut-off:** the vent has no N₂ left, so the pressure can no longer be held ([boxes §4](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md)). This happens when the enclosure gets hot. At constant pressure, heating the gas from 20 °C to ~850 °C would mean venting about three-quarters of it, nearly all of the N₂. The enclosure's heat capacity decides whether it gets that hot.
3. **Duration:** the emission event's own end.

A cut-off is permanent by default, as specified. Q20 ([boxes §7](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md)) asks about relighting when plants restore the O₂. Cut-offs are outcome events ([scenarios §6](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md)).

## 6. Conservation and reproducibility

- **Elements:** totals (C, H, O, N, Ar, S, Ca, Si) in a closed box are constant, apart from what the vent removes, which is counted. The planet's carbon and sulfur change only through outgassing, eruptions, weathering and fall-out, all counted.
- **Energy:** the heat in, out and stored add up, within the integrator's error.
- **Reproducibility:** the same settings, the same sequence of `dt` values and the same input events give identical results. A replayed record ([clock §3](2026-11-01-06-carbon-cycle-simulation-clock-plan.md)) has different tick sizes, so it matches within the accuracy setting; its events still land at their exact times.

## 7. Tests

Engine tests live in `ClimateExplorer.CarbonCycle.UnitTests`, named `MethodName_StateUnderTest_ExpectedBehavior`. Known answers are approximate and get tolerances.

**Clamp and stepping**
- No pool goes negative for any `dt`, including steps far larger than the burn (randomised configurations and steps).
- A limited reaction keeps its stoichiometry (O₂ and CH₄ used in a 2:1 ratio when O₂ runs out).
- `Advance` accepts a different `dt` on every call; two half-steps match one whole step to within the accuracy setting.
- Results converge as the accuracy fraction shrinks.
- The O₂ cut-off time is the same, within tolerance, at real-time speed and at 1,000 yr/s.
- An input event inside a step lands at its exact time, whatever the step size.

**Conservation**
- Each element's total in a sealed box is constant, apart from the counted vent outflow.
- Energy in = energy out + change in stored heat, within tolerance.
- The same settings, `dt` sequence and input events give identical results.

## 8. Risks

- **WebAssembly speed.** .NET on WebAssembly is interpreted unless compiled ahead of time. Six scenarios with ~15 processes each should fit the budget with adaptive substeps, but this needs measuring with the real engine. Fallbacks, in order: AOT for the client, coarser accuracy at high speed, then a Web Worker.
- **Stiffness at high speed.** A flame lasting a minute and weathering lasting a million years in one run make a stiff system. Adaptive substeps and event location handle the cut-offs. A scenario whose fast process never settles can still hit the substep cap: for example, day and night at a 1-hour step. The runner then slows the clock, as intended.

## Summary

The engine that every setting runs on: a state of pools and temperatures, processes as reactions with rates, a limiting-reagent clamp so no pool goes negative, adaptive substeps so the answer doesn't depend on the reader's step, and cut-offs landed on at their exact times. Today's two engines (`SealedHouse`, `DeadPlanet`) already do a single-reaction version of this; the work here generalises it to many processes and pools and moves both engines onto it.

### Change size

Rough, from the split; refine when this plan is next revised.

- **Projects:** `ClimateExplorer.CarbonCycle`, `ClimateExplorer.CarbonCycle.UnitTests`

| Area | Added | Changed | Removed |
|---|---|---|---|
| Engine: settings, events, pools, processes, clamp, substeps | 6 files, +600 | — | — |
| Engine: `SealedHouse`, `DeadPlanet`, `SimulationState` onto the shared core | — | 3 files, ~250 | — |
| Tests: clamp, stepping, conservation | 2 files, +400 | 2 files, ~50 | — |
| **Total** | **8 files, +1,000** | **5 files, ~300** | — |
