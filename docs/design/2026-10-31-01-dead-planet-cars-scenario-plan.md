# Carbon-cycle simulator — A billion cars on a dead planet

- **Date:** 2026-10-31
- **Status:** Implemented 2026-10-31 (see addendum)
- **Author:** Patrick Lea (with Claude)
- **Scope:** Scenario 2 of the simulator's carousel. A second engine (`DeadPlanet`) beside `SealedHouse`, the
  `ISimulation` interface the page runs, the car count input, and the formatting and clock speeds a planet needs.
- **Builds on:** [Carbon Cycle page — Simulation](2026-10-24-01-carbon-cycle-simulation-plan.md) (§3.5 planet energy
  balance, §11.1 first slice)
- **Branch context:** `development`

## 1. The scenario

A simple setting that's obviously not real but easy to picture. A planet the size of Earth with Earth's air and
nothing else: no sun, no ocean, no plants, no weathering. A billion cars drive at 60 km/h day and night and burn
petrol, taken as pure octane, completely. The reader sets the number of cars.

The combustors table isn't needed: the description says what a car burns, and the engine burns octane through the
Tab 1 chemistry engine (`DefaultFuels.IdealPetrol`), as the house burns methane.

## 2. Numbers

| Quantity | Value | Basis |
|---|---|---|
| Petrol per car | 4.2 L/h (1.17 mL/s) | 7 L/100 km at 60 km/h |
| Octane, all cars | 7.18 million mol/s (0.82 t/s) | 1 billion × 4.2 L/h × 703 kg/m³ ÷ 114.2 g/mol |
| CO₂ made | ~80 Gt a year | about twice today's fossil emissions (~37 Gt) |
| Air | 1.77 × 10²⁰ mol, 5.135 × 10¹⁸ kg | Earth's dry air (Trenberth and Smith 2005) |
| CO₂ rise | ~10 ppm a year | 1 ppm is ~7.8 Gt CO₂ |
| Engines stop (O₂ below 15%) | after ~3,930 years | 12.5 O₂ per octane; CO₂ is then ~3.8% |
| Engine heat | 39 TW, 0.077 W/m² | octane's heat of combustion plus the latent heat of its rained-out water |

The scenario needs the clock to run at years per second, so the speed control gains 1 week, 1 year, 10 years and
100 years per second. At 100 years per second the engines stop after about 40 seconds.

## 3. Heat to space (the investigation)

Without heat to space there's nothing to stop the warming, and with a cold sky the planet freezes. Three choices:

| Option | What happens | Verdict |
|---|---|---|
| **A. No heat to space** (an insulated planet) | The engines' heat alone warms the air by ~0.24 °C a year. Add the CO₂'s greenhouse effect with nowhere for heat to go and it runs away (~18 °C a year after a century). | Misleading: it suggests cars warm the planet with their exhaust heat. Kept as a setting (`ClimateFeedback = 0`) only. |
| **B. Cold universe, no sun** | A real Earth with no sun loses ~240 W/m², about 3,000 times the cars' heat. It cools by ~2 °C a day and freezes within months. | The cars make no difference. Not useful. |
| **C. Warm sky** (chosen) | Something unmodelled holds the planet at 15 °C at the start, as the sun does for Earth. As it warms, it loses more heat to space; CO₂ keeps some in. | Shows the real point. A few lines of code. |

Option C is the planet energy balance of simulation plan §4.3 with the sun and albedo replaced by a constant that
balances heat to space at the start:

```
C dT/dt = engine heat + F(CO₂) − λ (T − T₀)          F = 5.35 ln(C/C₀) W/m² (Myhre et al. 1998)
```

- **`λ` = 3.2 W/(m²·K)**, the Planck response alone (AR6). The planet has no water vapour, ice or clouds to feed back,
  so it warms ~1.2 °C per doubling of CO₂, not Earth's ~3 °C.
- **`C`** is the air alone: 5.135 × 10¹⁸ kg × 1,004 J/(kg·K). That makes the planet respond in ~5 weeks, so the
  temperature tracks the balance closely. There's no ocean or ground to slow it.
- **It's solved exactly over each substep** (an exponential approach to the balance), so long steps stay stable and
  the substeps are limited only by the O₂ and CO₂ change (1% each).

**What the reader sees:** the engines' heat is worth ~0.02 °C. The CO₂ they add is worth ~2 °C after a century and
~8 °C by the time the engines stop. The slide shows both as W/m² side by side ("Heat kept in by the added CO₂",
"Heat from the engines"); after a century it's ~6.6 against ~0.08.

## 4. Other simplifications

- **Water rains out at once.** The air starts dry (a dead planet has no ocean) and stays dry, so the cars' water
  vapour has no greenhouse effect. Its latent heat goes into the air.
- **Pressure is the weight of the air** (mass × g ÷ area), 98.7 kPa at the start, Earth's mean surface pressure. Each
  octane burnt takes 48 g out of the air (12.5 O₂ in, 8 CO₂ out, the water rained out), so it falls slightly.
- **One temperature for the whole atmosphere.** No lapse rate, no stratosphere.
- **Engines stop at the same 15% O₂ cut-off as the house's flame** (simulation plan §4.4). Real engines would lose
  power and run rich before that.

## 5. Design

- **`ISimulation`** (`Reaction`, `OxygenCutOff`, `Start()`, `Advance()`) is what the page runs. `SealedHouse` and
  `DeadPlanet` implement it. `HouseState` is renamed `SimulationState` and `MethaneBurnt` is `FuelBurnt`; the fuel is
  the reaction's first reactant.
- This is two engines, not the plan's one engine with many settings (§4.5). The box and planet heat and pressure
  models share little, and the interface lets them merge later without the page changing.
- **`Scenario`** takes a `CreateSimulation(ScenarioInputs)` factory instead of `SealedHouseSettings`.
  `ScenarioComponent.Simulation` replaces `House`; each slide casts it to its own engine.
- **`FossilFuelInputs.Cars`** beside `MethaneBurnerRate`; **`CarCountInput`** takes a whole number from 1 to a trillion.
- **`ReactionSummary`** takes the scenario's own totals as child content: the house shows condensed water and vented
  N₂, the planet rained-out water and the two heat fluxes.
- **Shared text** no longer says "house": "Gases in the air", "Time since the start".
- **`CarbonSimFormat.Number`** gives a trillion and up as a power of ten ("1.77 × 10²⁰"); **`Duration`** goes to
  years ("12 yr 45 d", "3,934 yr"); the chart's time axis to years.

## Summary

Scenario 2 replaces a placeholder with a billion cars burning octane in a dead planet's air. A warm sky holds the
planet at 15 °C to begin with; as it warms it loses heat to space, and the CO₂ the cars add keeps some in. CO₂ rises
~10 ppm a year, the temperature ~2 °C a century, and the engines stop at 15% O₂ after ~3,900 years. The engines' own
heat is ~1% of the CO₂'s effect.

### Change size

- **Projects:** `ClimateExplorer.CarbonCycle`, `ClimateExplorer.Web.Client`, `ClimateExplorer.CarbonCycle.UnitTests`,
  `ClimateExplorer.Web.CarbonSim.UnitTests`

| Area | Added | Changed | Removed |
|---|---|---|---|
| Engine: `DeadPlanet`, `DeadPlanetSettings`, `ISimulation` | 3 files, +250 | — | — |
| Engine: `SealedHouse`, `SimulationState` (renamed) | — | 2 files, ~10 | — |
| Web: `DeadPlanetScenario`, `CarCountInput` | 6 files, +190 | — | — |
| Web: page, scenarios, summary, table, chart, format, speeds | — | 15 files, ~150 | — |
| Tests: `DeadPlanetTests` | 1 file, +170 | — | — |
| Tests: format, catalog, house | — | 3 files, ~50 | — |
| **Total** | **10 files, +610** | **20 files, ~210** | — |

## Addendum — implementation notes

Implemented as above on 2026-10-31. Follow-ups:

- **The house at the fastest speeds.** The house's wall-loss step limit (5,000 s) and its 10,000-substep cap mean that
  at 100 years per second the house's clock falls behind (it advances ~1.6 years a tick, not 10). Nothing changes in
  the house by then, so only the time readout shows it. Solving its heat loss exactly, as the planet does, would fix it.
- **Earth-like feedbacks.** A Level 2 option could use Earth's `λ` (~1.2 W/(m²·K), 3 °C per doubling) to show what
  water vapour, ice and clouds add.
