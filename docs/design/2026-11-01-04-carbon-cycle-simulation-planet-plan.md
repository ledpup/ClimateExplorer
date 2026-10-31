# Carbon Cycle simulation — Earth settings and the carbon cycle

- **Date:** 2026-11-01 (split from the [simulation overview](2026-10-24-01-carbon-cycle-simulation-plan.md), written 2026-10-24)
- **Status:** Proposed. Not built. The dead planet ([its plan](2026-10-31-01-dead-planet-cars-scenario-plan.md)) is a planet setting with none of these processes.
- **Author:** Patrick Lea (with Claude)
- **Scope:** The Earth settings (scenarios 5 and 6) and the volcano button. Their carbon-cycle processes: the two-layer ocean, the land biosphere, weathering, volcanic outgassing, eruptions, stratospheric sulfate, methane oxidation and the loss of N₂O and halogenated gases. The planet's temperature and forcing are in the [energy balance plan](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md).
- **Builds on:** [Carbon Cycle simulation — Overview](2026-10-24-01-carbon-cycle-simulation-plan.md); [engine](2026-11-01-01-carbon-cycle-simulation-engine-plan.md); [energy balance](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md)
- **Branch context:** `development`

Section references without a plan name are to this document. "Overview" is the [Carbon Cycle page overview](2026-10-21-01-carbon-cycle-plan.md); the other simulation plans are named by their topic, as listed in the [simulation overview §3](2026-10-24-01-carbon-cycle-simulation-plan.md).

## 1. Settings

### 1.1 Basic Earth

The whole atmosphere, a two-layer ocean, a land biosphere, weathering and volcanic outgassing (§2), with a planet energy balance ([energy balance §1](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md)).

- **Today's emissions.** The brief's example input is invisible here ([scenarios §3](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md)). The preset "today's world emissions" shows:
  - CO₂ rising ~2–3 ppm/yr
  - about half of each year's emissions taken up by the ocean and land
  - warming that lags the CO₂
- **O₂ falls too,** by a tiny fraction. Readouts show the change since the start, which makes it visible ([web client §3](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md)). The real fall is measured (Scripps O₂/N₂ programme; a possible later comparison).
- **The long-term thermostat.** Over hundreds of thousands of years, weathering, sped up by the extra warmth and CO₂, brings CO₂ back towards the level where weathering balances volcanic outgassing. [Tab 1 §2.1](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md) compares the volcanic rate with today's emissions.
- **The volcano button** works here too (§1.3).

### 1.2 Earth without volcanoes

Basic Earth with background outgassing switched off. This is where the planet-scale story plays out:

1. While the emissions run, CO₂ and temperature rise.
2. Afterwards, weathering draws CO₂ down, faster because it is warm and CO₂ is high. With no volcanoes resupplying it, CO₂ keeps falling past the starting level.
3. As CO₂ falls, the planet cools and slows its own weathering. On its own this slows the decline; it would not cause a sharp fall.
4. The sharp fall comes from ice. Cooling spreads ice, ice reflects more sunlight, and below a threshold that feedback runs away to a frozen planet (a "snowball Earth"). Frozen, weathering almost stops, and the planet stays frozen. How a snowball thaws is out of scope.

So a large temperature drop with frozen water is the likely end state here. The sharp drop is ice-albedo feedback ([energy balance §1](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md)), not weathering itself. Timescale: hundreds of thousands of years (to verify in the engine), a few minutes at 1,000 years per second.

### 1.3 The volcano button

Any planet setting has an **Erupt** button, with a count (1, 10 or 100) and an eruption size.
- Each click adds an eruption event at the current simulated time ([scenarios §5](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md)). It is marked on the charts and kept in the event record ([clock §3](2026-11-01-06-carbon-cycle-simulation-clock-plan.md)). A readout counts the eruptions so far.
- Eruptions can also be starting events, so a preset can open with "100 eruptions at time 0".

An eruption does two things, on very different timescales. Provisional sizes, to verify (e.g. against the Smithsonian Global Volcanism Program and published SO₂ inventories):

| Size | Example | ~SO₂ | ~CO₂ | Main effect |
|---|---|---|---|---|
| Large | Pinatubo, 1991 | ~15–20 Mt | ~0.05 Gt (to verify) | ~0.5 °C of global cooling for ~2 years |
| Very large | Tambora, 1815 | to source (several times Pinatubo) | to source | The "year without a summer" |
| Flood basalt | Siberian Traps (end-Permian) | very large, spread over time | thousands of Gt over ~100,000 years or more (to verify) | Long warming. Modelled as raised outgassing for a period, not as a pulse |

- **SO₂** becomes sulfate aerosol in the stratosphere within weeks and falls out over a year or two (§2). While it's there, it reflects sunlight. This is the same sulfate cooling as [Tab 1 §4](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md), but high up, where it lasts longer. Its forcing is in the [energy balance plan §1](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md).
- **CO₂** from an eruption is small. A hundred Pinatubos add ~5 Gt CO₂, about 0.6 ppm and well under a year of today's human emissions (~37 Gt).

**The teaching point, in Earth without volcanoes.** A hundred eruptions at once cool the planet for a few years, and can push a cold planet over the threshold into the ice state.

At high speed an eruption's cooling lasts a few milliseconds of real time. The substeps still resolve it ([engine §4](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)), and the chart's decimation keeps each bucket's minimum ([web client §2](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md)), so the dip shows.

## 2. Processes

As in the [boxes plan §2](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md), each process is a reaction or transfer with a rate law, and the values are provisional, to verify. The burner, direct emissions, photosynthesis, respiration and silicate weathering are in that table; on a planet, photosynthesis and respiration are the land biosphere, and weathering is reduced by ice cover.

| Process | Reaction or transfer | Rate | Scenarios |
|---|---|---|---|
| Volcanic outgassing | crust → CO₂ | constant, ~0.3–0.4 Gt CO₂/yr; by default equal to preindustrial weathering | 5 |
| Eruption | crust → SO₂ (stratosphere) + CO₂ | a pulse per click, sized by §1.3 | 5–6 |
| Sulfate formation | SO₂ → sulfate aerosol (net of the [Tab 1 §4](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md) chain) | SO₂ ÷ ~1 month (to verify) | 5–6 |
| Sulfate fall-out | stratospheric sulfate → surface | sulfate ÷ ~1 year (to verify) | 5–6 |
| Air–sea exchange | CO₂ (air) ⇌ surface-ocean carbon | towards a buffered equilibrium (Revelle factor ~10), timescale ~1 yr | 5–6 |
| Surface–deep ocean mixing | surface ⇌ deep | timescale of centuries to a millennium | 5–6 |
| Methane oxidation | CH₄ + 2 O₂ → CO₂ + 2 H₂O | CH₄ ÷ lifetime (~9–12 yr) | 5–6 |
| N₂O and halogenated-gas loss | each → removed (stratospheric breakdown) | amount ÷ lifetime (N₂O ~110 yr; halogenated gases per species, AR6) | 5–6 |

Solar gain and outgoing longwave for the planet are the energy balance ([energy balance §1](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md)).

## 3. Tests

- With outgassing on, long runs return to where weathering equals outgassing.
- Earth without volcanoes: CO₂ declines below the start; with ice-albedo on, a cold enough state stays frozen.
- An eruption's SO₂ gives a cooling that peaks within months and decays at the sulfate lifetime. 100 eruptions inject exactly 100 times one. An eruption between ticks takes effect at the right time.
- Today's emissions on Basic Earth: about half of each year's CO₂ is taken up by the ocean and land.

## 4. Risks

- **A fixed Revelle factor** is wrong for large perturbations.
- **Sulfate forcing** grows less than in proportion at very large loads (particles grow and fall faster), which matters for 100 eruptions at once. It needs a saturating form.
- **The log CO₂ forcing near zero** and **the ice-albedo tuning** matter most here; they are in the [energy balance plan §5](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md).

Each of these gets a Level 2 note.

## Summary

Two Earth settings, with and without volcanic outgassing, and a volcano button. A two-layer ocean, a land biosphere, weathering and outgassing move carbon between air, ocean, plants and rock; eruptions add short-lived stratospheric sulfate and a little CO₂. The teaching points are the ocean and land taking up about half of each year's emissions, the slow weathering thermostat, and how a planet with no volcanoes can draw down its CO₂ until it freezes.

### Change size

Rough, from the split; refine when this plan is next revised.

- **Projects:** `ClimateExplorer.CarbonCycle`, `ClimateExplorer.Web.Client`, the unit test projects

| Area | Added | Changed | Removed |
|---|---|---|---|
| Engine: ocean, land, outgassing, methane oxidation, gas lifetimes | 4 files, +450 | — | — |
| Engine: eruptions and stratospheric sulfate | 2 files, +150 | — | — |
| Web: two Earth scenarios, volcano button | 5 files, +300 | 2 files, ~30 | — |
| Tests | 2 files, +350 | — | — |
| **Total** | **13 files, +1,250** | **2 files, ~30** | — |
