# Carbon Cycle simulation — Overview

- **Date:** 2026-10-24
- **Status:** Proposed (ideal greenhouse and volcano button added 2026-10-24; reworked 2026-10-27: the simulation runs the input tabs' emissions and is shown on output Tabs 4–7; a scenario is a list of events; rewind and the event record added; first slice implemented 2026-10-31, see §6; split into sub-plans 2026-11-01, see §3)
- **Author:** Patrick Lea (with Claude)
- **Scope:** The simulation behind the Carbon Cycle page's output tabs (4–7): a box model run forward through time from the input tabs' emissions, in one or more scenarios side by side. This overview is the index: what the simulation is, the settings, the tracked quantities, the order of work and the first slice. The detail is in the sub-plans (§3).
- **Builds on:** [Carbon Cycle page — Overview](2026-10-21-01-carbon-cycle-plan.md); [Tab 1 chemistry engine](2026-10-23-02-carbon-cycle-tab1-chemistry-engine-plan.md) (the burner uses its stoichiometry and heat); [US customary units toggle](2026-10-21-02-us-customary-units-plan.md) (readouts follow its setting)
- **Branch context:** `issues/carbon-cycle`; split on `development`

The input tabs (1–3) answer "how much": activities go in, and emissions come out. The simulation answers "what happens, and when". Once the inputs are ready, the reader presses play. The clock starts at time 0, and the output tabs (4–7) show the run as it goes: O₂ falling and CO₂ rising, carbon moving into plants, rock and the ocean, forcing building up, and temperature following. A run can hold several scenarios side by side, for example a sealed house, three greenhouses and two versions of the Earth. Their timescales differ by orders of magnitude. A big flame in a house goes out in about a minute, and weathering takes hundreds of thousands of years. So fast-forward steps the clock from real time up to about 1,000 simulated years per second, and rewind runs it back.

Section references without a plan name are to this document. "Overview" is the [Carbon Cycle page overview](2026-10-21-01-carbon-cycle-plan.md); the sub-plans are named by their topic (§3).

---

## 1. What the simulation is

- **A box model.** Each scenario is a set of reservoirs (the gas in an enclosure, liquid water, plants, rock, the ocean) holding amounts of chemical species. Processes (burning, venting, condensation, photosynthesis, weathering) move amounts between them. The model steps forward in time from time 0.
- **One engine, several settings** (§2; [engine §1](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)). A setting is a choice of enclosure, a set of processes that are switched on, and their parameters. Sealed house, greenhouse and Earth are not separate models.
- **A scenario is a list of events** ([scenarios plan](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md)). Its starting events, everything that happens at time 0, define it: its setting and the emissions from the input tabs. The reader can add events while it runs, such as a volcanic eruption ([planet §1.3](2026-11-01-04-carbon-cycle-simulation-planet-plan.md)). Every event is recorded, so a run can be saved, sent and replayed ([clock §3](2026-11-01-06-carbon-cycle-simulation-clock-plan.md)).
- **The six scenarios in §2 are examples**, a starting point. The reader can run one, a preset, several side by side, or a scenario of their own. More will be added later.
- **Live output on the output tabs** ([web client §4](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md)). Each output tab shows its aspect of every scenario in the run: an overview, carbon and gases, radiative forcing, and temperature. The reader picks which quantities to show (§4).
- **The reader controls the clock** ([clock §1](2026-11-01-06-carbon-cycle-simulation-clock-plan.md)): play, pause, fast-forward, rewind and restart. Fast-forward steps from real time up to about 1,000 simulated years per second.
- **Cut-offs become events** ([engine §5](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)). When the flame goes out or the N₂ vent can no longer hold the pressure, the emission stops. The moment is marked on every chart of that scenario.

**Levels** ([overview §1.3](2026-10-21-01-carbon-cycle-plan.md)):
- **Level 1:** one preset scenario, its readouts and charts on the output tabs, the clock controls and the volcano button. Readouts: O₂, CO₂, temperature, and whether the flame is burning.
- **Level 2:** the scenario editor (a setting and its starting events), several scenarios at once, all tracked quantities with show/hide, per-scenario parameters, the event log, the accuracy control ([engine §4](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)), a fixed-step toggle that shows what a too-large step gets wrong, and a compare view (one chart per quantity, a line per scenario).
- **Level 3:** every parameter opens its popover (overview §3.3).

**Relation to the other tabs.** The input tabs build the emission events; the simulation runs them; the output tabs show it ([overview §1.2](2026-10-21-01-carbon-cycle-plan.md)). There is no separate simulation tab. A fuel's burning goes through the Tab 1 chemistry engine, so the O₂ it uses comes out of the scenario's own air.

## 2. Settings and example scenarios

The six scenarios below are examples: each is one setting (its set-up event, [scenarios §1](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md)) run with the input tabs' emissions. They are a starting point, and more will be added. The reader can run any one of them, several side by side, or a setting of their own (Level 2: "duplicate and edit", [engine §1](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)).

| # | Scenario | Walls | Sunlight | Biology | Weathering | Temperature model | Plan |
|---|---|---|---|---|---|---|---|
| 1 | Sealed house | Opaque | None | None | None | Box | [Boxes §1.1](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md) |
| 2 | Glass greenhouse | Glass: let sunlight in, block infrared | Yes | None | None | Box | [Boxes §1.2](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md) |
| 3 | Ideal greenhouse | Ideal: hold the gas, let all radiation through | Yes | None | None | Box, with gas radiation | [Boxes §1.3](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md) |
| 4 | Ideal greenhouse with plants and weathering | Ideal | Yes | Plants | Crushed rock on the floor | Box, with gas radiation | [Boxes §1.4](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md) |
| 5 | Basic Earth | — (planet) | Yes | Land biosphere | Yes, balanced by volcanoes | Planet energy balance | [Planet §1.1](2026-11-01-04-carbon-cycle-simulation-planet-plan.md) |
| 6 | Earth without volcanoes | — (planet) | Yes | Land biosphere | Yes, with nothing to balance it; eruptions by button | Planet energy balance | [Planet §1.2](2026-11-01-04-carbon-cycle-simulation-planet-plan.md) |

All six are settings of the one engine.
- Scenarios 2 and 3 differ only in their walls. Putting them side by side isolates what the gas's own radiation does.
- Scenario 6 is scenario 5 with background outgassing switched off.
- Wall type is a parameter, so the reader can also make combinations not in the table, such as plants in a glass greenhouse.

**Built so far** (the page's carousel, `/carbon-sim`): the sealed house, and *A billion cars on a dead planet*, a planet setting not in the table: Earth's air and nothing else, held at its start temperature by a warm sky ([its plan](2026-10-31-01-dead-planet-cars-scenario-plan.md)).

## 3. Sub-plans

Each sub-plan holds the detail for its topic: settings, processes, formulas, tests, risks and open questions. **Was** lists the sections of this document, as it stood before the split, that moved there, so older references (in code comments and other plans) can be followed.

| Plan | Covers | Was |
|---|---|---|
| [Engine core](2026-11-01-01-carbon-cycle-simulation-engine-plan.md) | `Setting`/`InputEvent`/`Scenario`/`SimulationState`, `Advance`, the limiting-reagent clamp, substeps, cut-offs, conservation | §4.5, §5.1 (one operation, input events), §5.2–§5.4, §4.4 (cut-offs), §8 (clamp, conservation) |
| [Scenarios and events](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md) | Starting events, starting air, scale check, presets, events during a run, input and outcome events | §2, §4.4 (two kinds of event), §6.2 |
| [House and greenhouses](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md) | Scenarios 1–4: box processes, two-node heat balance, gas radiation, plants and weathering, the N₂ vent | §3.1–§3.4, §4.2 (box rows), §4.3 (box heat balance), §4.4 (vent), §8 (boxes) |
| [Earth settings and the carbon cycle](2026-11-01-04-carbon-cycle-simulation-planet-plan.md) | Scenarios 5–6, the volcano button: ocean, land, outgassing, eruptions, sulfate, gas lifetimes | §3.5–§3.7, §4.2 (planet rows), §8 (planet) |
| [Sunlight, forcing and the planet energy balance](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md) | The planet energy balance, forcing by agent, albedo, the daily TSI driver | §4.3 (planet energy balance), §6.1, §8 (drivers) |
| [Run clock, rewind and the event record](2026-11-01-06-carbon-cycle-simulation-clock-plan.md) | Speeds and clock controls, `SimulationRunner`, rewind, checkpoints, the record, save and open | §5.1 (speed, clock controls), §5.5, §7.2, §8 (rewind, record) |
| [Web client](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md) | Render mode, uPlot charts, units, output tab layout and accessibility, URL state | §7.1, §7.3–§7.6 |

## 4. Tracked quantities

The reader can show or hide each of these. Hidden quantities are still recorded, so showing one later shows its whole history. The last column is the output tab that shows it ([web client §4](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md)); the Overview (Tab 4) shows the Level 1 quantities.

| Quantity | 1 House | 2 Glass | 3 Ideal | 4 Ideal + plants | 5 Earth | 6 No volcanoes | Level | Tab |
|---|---|---|---|---|---|---|---|---|
| O₂ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 1 | 5 |
| CO₂ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 1 | 5 |
| Temperature (the gas; the planet's surface) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 1 | 7 |
| Flame state (burning / out, and why) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 1 | 5 |
| N₂, Ar | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 2 | 5 |
| CH₄, CO (unburnt fuel and incomplete combustion; on Earth, CH₄ also from the record) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 2 | 5 |
| Water vapour, relative humidity | ✓ | ✓ | ✓ | ✓ | — | — | 2 | 5 |
| Liquid water, ice | ✓ | ✓ | ✓ | ✓ | ice cover | ice cover | 2 | 7 |
| Floor temperature | ✓ | ✓ | ✓ | ✓ | — | — | 2 | 7 |
| Pressure | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 2 | 5 |
| N₂ vented (cumulative) | ✓ | ✓ | ✓ | ✓ | — | — | 2 | 5 |
| Fuel burned (cumulative) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 2 | 5 |
| Heat released (cumulative) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 2 | 7 |
| Sunlight absorbed | — | ✓ | ✓ | ✓ | ✓ | ✓ | 2 | 6 |
| Gas emissivity, infrared to the sky | — | — | ✓ | ✓ | — | — | 2 | 6 |
| Carbon in plants | — | — | — | ✓ | land | land | 2 | 5 |
| Carbon in carbonate rock (weathered) | — | — | — | ✓ | ✓ | ✓ | 2 | 5 |
| Ocean carbon (surface, deep) | — | — | — | — | ✓ | ✓ | 2 | 5 |
| Radiative forcing by agent, albedo | — | — | — | — | ✓ | ✓ | 1 (Earth) | 6 |
| Stratospheric sulfate, eruptions so far | — | — | — | — | ✓ | ✓ | 2 | 5 |

## 5. Work breakdown

Depends on the chemistry engine ([Tab 1 step 1](2026-10-23-02-carbon-cycle-tab1-chemistry-engine-plan.md)) for the burner, on `EmissionResult` (overview §5 step 3) for the emission events, and on the units plan for `UnitPreference`. The parameter system (overview §5 step 1) can follow: like the chemistry engine, this engine takes its assumptions as plain inputs. **Implemented** is the date a step was finished; a step the first slice (§6) only started says "partly".

| Step | Work | Plan | Implemented |
|---|---|---|---|
| 1 | Engine core: state, process and reaction-rate model, limiting-reagent clamp, adaptive substeps, event location, input events applied at their exact times, `Advance(dt)` + tests | [Engine](2026-11-01-01-carbon-cycle-simulation-engine-plan.md) | Partly (2026-10-31) |
| 2 | Box processes: burner (through the Tab 1 engine), N₂ vent and proportional leak, water phase changes, two-node heat balance with convection, wall and ground conduction, solar gain + tests | [Boxes](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md) | Partly (2026-10-31) |
| 3 | Gas radiation for ideal walls: CO₂ and water vapour emissivity correlations, sky options + tests | [Boxes](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md) | — |
| 4 | Plants and weathering; catalog additions (CH₂O, CaSiO₃, SiO₂, Mg₂SiO₄) + tests | [Boxes](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md) | — |
| 5 | Planet processes: ocean and land boxes, methane oxidation, outgassing + tests | [Planet](2026-11-01-04-carbon-cycle-simulation-planet-plan.md) | — |
| 6 | Eruptions: injection sizes, stratospheric sulfate + tests | [Planet](2026-11-01-04-carbon-cycle-simulation-planet-plan.md) | — |
| 7 | Scenarios and events: `Setting`, `InputEvent` and `Scenario` types; settings 1–6 and preset scenarios; turning each input tab's `EmissionResult` into emission events; source and record every default + tests | [Scenarios](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md), [engine](2026-11-01-01-carbon-cycle-simulation-engine-plan.md) | — |
| 8 | Energy balance and forcing: sunlight, albedo with ice, forcing by agent (CO₂, CH₄, N₂O, halogenated gases, sulfate); TSI driver: fetch the existing dataset, step averaging, insolation by latitude, extension beyond the record + tests | [Energy balance](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md) | — |
| 9 | Render mode routing: `App.razor.cs` per-route mode for the Carbon Cycle page, placeholder, forced navigation and the page's renderer check | [Web client](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md) | — |
| 10 | uPlot vendoring, the JS module and `[JSImport]` interop | [Web client](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md) | — |
| 11 | `SimulationRunner` as a scoped service: loop, budget, deltas, readout ticks, input events, eruptions + tests for its plain C# parts | [Clock](2026-11-01-06-carbon-cycle-simulation-clock-plan.md) | Partly (2026-10-31) |
| 12 | Clock and record: speed stops, fast-forward, rewind and replay over samples, checkpoints and branching, restart, the event record, save and open as JSON + tests | [Clock](2026-11-01-06-carbon-cycle-simulation-clock-plan.md) | Partly (2026-10-31) |
| 13 | UI: clock bar, volcano button, output Tabs 4–7 with their scenario panels, show/hide, event log, units, responsive layout, accessibility | [Web client](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md) | Partly (2026-10-31) |
| 14 | URL state and parameters | [Web client](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md) | — |
| 15 | Content: Level 1 text for each scenario, the greenhouse, Foote and scale points, why a simulation can rewind when chemistry can't, Level 2 notes on every simplification | Each scenario's plan | — |

The energy balance (step 8) was step 5's "energy balance with ice-albedo" and step 8's TSI driver before the split; they are one step now because the sunlight and the forcing share a schema.

## 6. First slice (implemented 2026-10-31)

A basic working simulation of scenario 1 only: methane burning in the sealed house. It touches parts of steps 1, 2, 11, 12 and 13, and completes none of them.

- **Engine** (`ClimateExplorer.CarbonCycle/Simulation`): `SealedHouse` with `Start()` and `Advance(state, dt)`. The burner goes through the Tab 1 engine (one mole of ideal natural gas). N₂ vent, instant condensation and evaporation with latent heat, the limiting-reagent clamp, adaptive substeps and the O₂ cut-off landed on by interpolation. Argon added to the catalog; dry air is `DryAir`, with argon as the remainder. Tests: `SealedHouseTests`.
- **Simplified against the plan:** one lumped temperature for the air and contents (not the two-node balance), losing heat through the walls to an outside at the start temperature (20 °C); CO₂ and CH₄ start values are fixed, not from the records; no N₂ cut-off, emission duration, events, rewind or record.
- **UI** (`ClimateExplorer.Web.Client/CarbonSim`, `/carbon-sim`): the three input-tab strip with Tabs 2 and 3 disabled; Tab 1 has the burner rate, the equation and the reactants and products so far. A clock bar (play/pause, restart, speed stops up to 1 day/s) with the loop in the page, not a `SimulationRunner` service. The output tabs are not built: their place is taken by two draggable `FloatingPanel`s, the gas table with readouts and a Chart.js gas chart (not uPlot). No per-route render mode.
- **Units (2026-10-31):** new Core units `Kilograms`, `CubicMetres`, `Kilopascals`, `GasLitresPerSecond` and `LiquidLitresPerSecond`, converted by `UnitFormatter` to lb, ft³, psi, ft³/h and US gal/h. The engine stays SI. The house volume, pressure, masses, burner rate and its hint follow the setting; the burner rate is typed in the reader's units and turned back into metric by `IUnitFormatter.FromDisplay`. Gas flow is ft³/h, not the ft³/min the plan first had, because US gas appliances and meters are rated per hour (a hob burns about 9 ft³/h), and liquid flow is gal/h for the same reason (oil-burner nozzles), not gal/s. Nothing uses `LiquidLitresPerSecond` yet. Energy (MJ), moles and the chart's shares are unchanged ([web client §3](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md)).
- **Scenario carousel (2026-10-31):** Tab 1's panel is a carousel of scenarios (`CarbonSim/Common/Carousel`, generic), and each slide is a whole scenario ([scenarios §4](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md)): its description, its inputs (the burner rate, `BurnerRateInput`) and the reactants and products so far. A `Scenario` (`CarbonSim/Scenarios`) has a title, a component derived from `ScenarioComponent`, its `SealedHouseSettings` and a `ScenarioInputs` with one member per input tab (only `FossilFuels` so far). The page owns the inputs and the run: the active slide gets the live ones, and the slides beside it their scenario's own inputs at time 0. Bringing a scenario in replaces the reader's inputs with its own and restarts the run. `ScenarioCatalog` has the sealed house plus two blank placeholders. On fullscreen the active slide keeps the full content width; the arrows and the faded neighbours sit outside it in the page margin, out to the browser's edges (clipped by `html:has(.carousel-viewport)` in `app.css`), so they show only as far as the margin allows. On tablet and mobile one slide shows, with the arrows in its top corners.
- **Dead planet (2026-10-31):** scenario 2 of the carousel is now a billion cars burning octane in a dead planet's air, with a second engine (`DeadPlanet`) behind an `ISimulation` interface the page runs; `HouseState` is renamed `SimulationState`, and a `Scenario` takes a simulation factory in place of `SealedHouseSettings`. See [A billion cars on a dead planet](2026-10-31-01-dead-planet-cars-scenario-plan.md).

## 7. Open questions

Question numbers are shared across the Carbon Cycle plans. Each open question is in the sub-plan it belongs to.

| Question | Plan |
|---|---|
| Q16 Scale the emissions to each enclosure? | [Scenarios §8](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md) |
| ~~Q17~~ Resolved: scenario 3 is the ideal greenhouse, scenario 4 adds plants and weathering, and Earth without volcanoes is scenario 6, with a volcano button | — |
| Q18 Prerender the input tabs? | [Web client §7](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md) |
| Q19 TSI outside 1978–2025 | [Energy balance §4](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md) |
| Q20 Relight when plants restore the O₂? | [Boxes §7](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md) |
| ~~Q21~~ Resolved: the output tabs (4–7) are views of this simulation, so the page has one Earth model and no separate impulse-response model | — |
| Q22 Different emissions per scenario? | [Scenarios §8](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md) |
| Q23 Adding an event after rewinding | [Clock §6](2026-11-01-06-carbon-cycle-simulation-clock-plan.md) |

## Summary

The simulation runs the input tabs' emissions forward through time in one or more scenarios, from a sealed house to the whole Earth, on one clock from real time to 1,000 years per second, with rewind and a replayable event record. This overview holds the settings, the tracked quantities and the order of work; seven sub-plans hold the detail. The split itself (2026-11-01) moved text and fixed references; it changed no design decision, apart from merging the energy balance and the TSI driver into one step (§5).

### Change size

Rough totals of the sub-plans' estimates. They overlap where two plans change the same file (the engines, the page), so the changed-file count is an upper bound.

- **Projects:** `ClimateExplorer.CarbonCycle`, `ClimateExplorer.Web.Client`, `ClimateExplorer.Web`, `ClimateExplorer.CarbonCycle.UnitTests`, `ClimateExplorer.Web.UnitTests`, `ClimateExplorer.Web.CarbonSim.UnitTests`

| Plan | Added | Changed | Removed |
|---|---|---|---|
| Engine core | 8 files, +1,000 | 5 files, ~300 | — |
| Scenarios and events | 3 files, +350 | 5 files, ~130 | — |
| House and greenhouses | 15 files, +1,450 | 6 files, ~180 | — |
| Earth settings and the carbon cycle | 13 files, +1,250 | 2 files, ~30 | — |
| Sunlight, forcing and the energy balance | 8 files, +960 | 4 files, ~110 | — |
| Run clock, rewind and the record | 10 files, +1,150 | 6 files, ~150 | — |
| Web client | 21 files, +1,860 | 7 files, ~180 | 4 files, −310 |
| **Total** | **78 files, +8,020** | **≤35 files, ~1,080** | **4 files, −310** |

About 2,300 of the added lines are tests.
