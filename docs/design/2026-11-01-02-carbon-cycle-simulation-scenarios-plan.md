# Carbon Cycle simulation — Scenarios and events

- **Date:** 2026-11-01 (split from the [simulation overview](2026-10-24-01-carbon-cycle-simulation-plan.md), written 2026-10-24)
- **Status:** Proposed. Partly built: the scenario carousel and `Scenario`/`ScenarioInputs` (2026-10-31) are presets with their own inputs (§4); there are no events yet.
- **Author:** Patrick Lea (with Claude)
- **Scope:** What a scenario is: its starting events (set-up, emissions, interventions), the starting air, the scale check, preset scenarios, events added during a run, and input against outcome events. Turning each input tab's `EmissionResult` into emission events.
- **Builds on:** [Carbon Cycle simulation — Overview](2026-10-24-01-carbon-cycle-simulation-plan.md); [engine](2026-11-01-01-carbon-cycle-simulation-engine-plan.md) (the event types, §1)
- **Branch context:** `development`

Section references without a plan name are to this document. "Overview" is the [Carbon Cycle page overview](2026-10-21-01-carbon-cycle-plan.md); the other simulation plans are named by their topic, as listed in the [simulation overview §3](2026-10-24-01-carbon-cycle-simulation-plan.md).

## 1. Starting events

A scenario is the list of events at time 0:

- **Set-up** (one per scenario, only at time 0): the setting ([simulation overview §2](2026-10-24-01-carbon-cycle-simulation-plan.md)), with its size and processes, and the **start date**. The start date sets the sunlight: it gives the day of year for the season and picks the TSI record value ([energy balance §2](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md)). Default: the latest full year in the TSI record.
- **Emissions:** one per activity on the included input tabs ([overview §2.1](2026-10-21-01-carbon-cycle-plan.md)), for example "burn methane at 100 L/s for 5 years". Each is a *pulse* (all at once) or a *constant rate for a duration*, as set on its input tab.
  - A fuel goes through the burner ([boxes §2](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md)): any fuel in the [Tab 1 fuel table](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md), ideal or real, with its carbon split ([Tab 1 §3](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md)). Its O₂ comes from the scenario's air.
  - A Tab 2 or Tab 3 activity (cattle, rice, landfill, refrigerant leaks) puts its species straight into the air.
  - A rate is a volume or a mass per unit time. Gas volumes are at 0 °C and 101.325 kPa, the same basis as the fuel table's gas density. This is an editable assumption, and the label states it.
  - An emission runs for its duration unless a cut-off stops it first ([engine §5](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)).
- **Interventions,** such as eruptions ([planet §1.3](2026-11-01-04-carbon-cycle-simulation-planet-plan.md)), can also be starting events.

By default every scenario in a run takes the same emission events from the input tabs, and the same starting air, so their results can be compared. Q16 asks whether the emissions should also scale with each enclosure, because their sizes are so different (§3). Q22 asks whether scenarios in one run can have different emissions.

## 2. Starting air and surroundings

- **Composition:** today's dry air, with N₂, O₂ and Ar as fixed constants, and CO₂, CH₄ and N₂O from the latest values in ClimateExplorer's own records (below). Humidity is an editable assumption (default 50% relative humidity).
- **Temperature and pressure:** 101.325 kPa. Temperature defaults to 15 °C, roughly the global mean surface temperature; the Earth scenarios take their starting temperature from the global temperature record (below).
- **Surroundings** for the enclosures: outside air temperature and ground temperature, each a constant (default 15 °C), and the sky ([boxes §3](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md)). Later, an option to use a ClimateExplorer location's temperature record and latitude.

**ClimateExplorer data used** (the daily TSI is in the [energy balance plan §2](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md)):

| Dataset (existing) | Used for |
|---|---|
| CO₂ from the Mauna Loa Observatory | Starting CO₂ (latest annual mean) |
| Methane (CH₄), global network | Starting CH₄ |
| Nitrous oxide (N₂O), global network | Starting N₂O for the Earth forcing |
| Global temperature anomaly (NOAAGlobalTemp) | Earth starting temperature (anomaly + an absolute baseline, to source) |
| CO₂ emissions (Global Carbon Budget) | Rate for the "today's world emissions" preset |
| A location's temperature record (later) | Outside temperature and latitude for the enclosures |

## 3. Scale check (shown on the page)

The rate comes from the input tabs. The two below are only illustrations, but any single rate shows the same thing: a rate that does something visible to the Earth puts the house's flame out almost at once, and one that suits the house does nothing visible to the Earth. Numbers are approximate, to verify in the engine's tests:

| | 100 L/s of methane | A gas hob (~2.5 kW, ~0.07 L/s) |
|---|---|---|
| Heat released | ~3.6 MW, a house fire | 2.5 kW |
| Sealed 250 m³ house, O₂ from 21% to a 15% cut-off | ~70 s | ~28 h |
| CO₂ in the house at cut-off | ~3% | ~3% |
| Earth, after 5 years | CO₂ up ~0.000004 ppm; invisible | — |

For scale: human CO₂ emissions are ~37 Gt/yr, about six million times 100 L/s of methane burned to CO₂. That is why the speed range is so wide, and why Q16 asks about scaling the emissions to each enclosure. The presets (§4) give each scenario an input on a scale where something visible happens.

## 4. Preset scenarios

A preset is a list of starting events plus a parameter set. Its emissions fill the input tabs, as the input tabs' own presets do, so the reader can see and edit them before pressing play.

- "A gas hob left on in a sealed house"
- "Burn methane at 100 L/s for 5 years", in all six settings (the brief's example)
- "A coal fire in a sealed house" (solid fuel, mass rate; no change in moles, [boxes §4](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md))
- "Today's world emissions on Earth, for 100 years" (rate from the CO₂ emissions dataset, §2)
- "A one-off pulse on Earth without volcanoes, then wait a million years" (to show weathering)
- Later, Level 2: "Foote's cylinders, 1856" ([boxes §1.3](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md))

**Built (2026-10-31):** the carousel's scenarios are presets of this kind: each `Scenario` has a simulation factory and a `ScenarioInputs` with one member per input tab. Bringing a scenario in replaces the reader's inputs with its own and restarts the run ([simulation overview §6](2026-10-24-01-carbon-cycle-simulation-plan.md)).

## 5. Events during a run

The reader can add events while the run plays or is paused. Each takes effect at the current simulated time, applies to the scenario it names (or to all), and goes into the event record ([clock §3](2026-11-01-06-carbon-cycle-simulation-clock-plan.md)):

- an eruption, one or many ([planet §1.3](2026-11-01-04-carbon-cycle-simulation-planet-plan.md))
- Level 2: start a new emission ("light a second fire") or stop one early ("turn the hob off")
- later: other interventions, as scenarios are added

Adding an event after rewinding is covered in [clock §3](2026-11-01-06-carbon-cycle-simulation-clock-plan.md).

## 6. Input and outcome events

*Input events* are the reader's: set-up, emissions and interventions (§1, §5). They make up the event record ([clock §3](2026-11-01-06-carbon-cycle-simulation-clock-plan.md)). *Outcome events* are what the engine finds as it runs: cut-offs and thresholds ([engine §5](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)). They are not stored, because replaying the input events finds them again. Both kinds are marked on every chart of their scenario (a vertical line with a short label) and listed in the event log, with their simulated time:
- emission started, emission ended, O₂ cut-off, N₂ cut-off, eruptions
- Level 2: water starts condensing, water starts freezing, CO₂ reaches the plants' compensation point, breathing limits crossed ([boxes §1.1](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md)), the planet tips into the ice state

## 7. Tests

- Each input tab's emission becomes one emission event with the same mass and profile.
- A preset's starting events fill the input tabs with its emissions, and the run starts from them.

## 8. Open questions

Question numbers are shared across the Carbon Cycle plans.

- **Q16** The input tabs set one rate, but any single rate shared by every scenario either puts the house's flame out at once or does nothing visible to the Earth (§3). Recommended: keep the shared emissions by default. Add a per-scenario "scale to this enclosure" toggle (rate per tonne of enclosed gas) and per-scenario presets.
- **Q22** In a run with several scenarios, do they all take the input tabs' emissions (recommended default, so they compare like for like), or can each have its own? If each can, the input tabs need an "Editing: ‹scenario›" selector, and the emissions move from the input tabs into each scenario's starting events.

## Summary

A scenario is a list of events at time 0: one set-up (the setting and a start date) and the input tabs' emissions, with interventions such as eruptions. Presets bundle a list of starting events with parameters; the reader can add events as it runs. Input events are recorded; outcome events (cut-offs, thresholds) are found again on replay.

### Change size

Rough, from the split; refine when this plan is next revised.

- **Projects:** `ClimateExplorer.CarbonCycle`, `ClimateExplorer.Web.Client`, the unit test projects

| Area | Added | Changed | Removed |
|---|---|---|---|
| Engine: emission events from `EmissionResult`, starting air from the records | 2 files, +200 | 1 file, ~30 | — |
| Web: presets as starting events, `Scenario`/`ScenarioInputs` | — | 4 files, ~100 | — |
| Tests | 1 file, +150 | — | — |
| **Total** | **3 files, +350** | **5 files, ~130** | — |
