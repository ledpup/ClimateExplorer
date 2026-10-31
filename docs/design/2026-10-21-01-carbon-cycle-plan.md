# Carbon Cycle page — Overview

- **Date:** 2026-10-21
- **Status:** Proposed (draft v1.1; simulation added 2026-10-24; 2026-10-27: modelled-vs-measured tab dropped, and the output tabs became views of the simulation)
- **Author:** Patrick Lea (with Claude)
- **Scope:** the Carbon Cycle page as a whole: purpose, tab layout, design principle, the input-tab contract, the parameter system and data. The input tabs and the simulation have their own plans (§1.2).
- **Branch context:** `issues/carbon-cycle`

---

## 1. Purpose

**The goal is a good understanding of radiative forcing: where each part of it comes from.** The page is a simplified, explorable model that traces human activities through emissions, chemical reactions, concentrations and radiative forcing to temperature. The reactions and the numbers flow visibly through the model. The chemistry, physics and Earth science are first-class content, not footnotes. The emphasis shifts across the page: the input tabs (1–3) are mostly chemistry and biology, Tab 5 is Earth science (reservoirs, fluxes and lifetimes), and Tabs 6 and 7 are mostly physics (molecular absorption, emission height, energy balance).

**Three ways to slice forcing.** The page connects all three:
1. **By agent**, as in the chart: what is in the atmosphere now (CO₂, CH₄, ozone, aerosols, …).
2. **By emitted species**: what was released (CO₂, CH₄, NOx, CO, VOC, SO₂, NH₃, BC, …). AR6 WG1 Fig 6.12 / SPM.2 does this. Methane's emissions-based ERF is roughly double its concentration-based value, because CH₄ also makes ozone and stratospheric water vapour and ends up as CO₂ (to verify).
3. **By activity**: what people did (burn coal, keep cattle, clear forest). This is what the input tabs build.

The input tabs produce species (view 2), the model turns species into agents (view 1), and the forcing tab can attribute each agent back to activities (view 3).

### 1.1 User experience

The interaction intent is closer to a software toy than a climate model. For example, "compare a billion people on e-scooters vs a million on luxury yachts."

### 1.2 Tabs

The page reads left to right, in two parts:
1. **Input tabs (1–3).** The reader builds activities, and each tab turns them into emissions. The tabs are optional: the reader needs at least one and can use all three (§2.1).
2. **Output tabs (4–7).** When the inputs are ready, the reader presses play and the [simulation](2026-10-24-01-carbon-cycle-simulation-plan.md) runs them through time. The output tabs are different aspects of that one run, on one clock: play, pause, fast-forward (real time, a minute per second, an hour per second, a day per second, … up to 1,000 years per second), rewind and restart. There is no separate simulation tab.

| Tab | Topic | Takes | Shows | Plan |
|---|---|---|---|---|
| 1 | Fossil fuels and limestone | User inputs (optional) | Emissions: CO₂, CH₄, CO, NOx, VOC, SO₂, BC, contrails | [Tab 1](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md); [combustors sub-plan](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md) |
| 2 | Land and agriculture | User inputs (optional) | Emissions: CO₂ (land clearing), CH₄, N₂O, NH₃, BC/OC; albedo change | [Tab 2](2026-10-21-05-carbon-cycle-tab2-land-agriculture-plan.md) (stub) |
| 3 | Waste and industrial gases | User inputs (optional) | Emissions: CH₄ (landfill, wastewater), halogenated gases, industrial N₂O | [Tab 3](2026-10-21-06-carbon-cycle-tab3-waste-industrial-gases-plan.md) (stub) |
| 4 | Overview | The run | Part of each of Tabs 5–7 for every scenario: key readouts (O₂, CO₂, temperature, flame state), a small chart from each tab, the event log | [Simulation §7.5](2026-10-24-01-carbon-cycle-simulation-plan.md) |
| 5 | Carbon, gases and rocks | The run | Gases over time; where the carbon is (air, plants, rock, ocean); CH₄, N₂O and halocarbon decay | §7; [simulation](2026-10-24-01-carbon-cycle-simulation-plan.md) |
| 6 | Radiative forcing | The run | W/m² by agent over time, attributable to species and activities | §7; [simulation](2026-10-24-01-carbon-cycle-simulation-plan.md) |
| 7 | Temperature | The run | °C response; ice | §7; [simulation](2026-10-24-01-carbon-cycle-simulation-plan.md) |

**Scenarios.** A run holds one or more scenarios side by side. A scenario is a list of events at time 0: its setting (a sealed house, a greenhouse, the Earth, …) and the input tabs' emissions. The reader can add events as it runs, such as a volcanic eruption, and every event is recorded so the run can be saved or sent to someone ([simulation §2, §5.5](2026-10-24-01-carbon-cycle-simulation-plan.md)). The six scenarios in the simulation plan are examples to start from; more will be added later.

Not a personal carbon-footprint calculator and not a consumer tool. It must work equally for "one truck driving 1,000 km" and "a nation's annual coal burn".

**Educational, not predictive.** The model uses real chemistry and physics, but rough, incomplete information about the world and a simplified Earth system. It is for understanding where forcing comes from, not for reproducing the observed record, so the page does not compare its output with measured CO₂ or temperature.

**Global scope.** Defaults, examples and presets are not tied to any one country. Where a "typical" vehicle or plant is needed, pick what is most common worldwide by count or by emissions. That usually means what is used in China, India and the rest of Asia and Africa, not the Western fleet (e.g. a 100–125 cc motorcycle and a diesel bus, not a large petrol sedan).

**What counts.** Every human-caused change in a forcing agent counts. Solar does not. For CO₂, the question is where the carbon was stored:
- **Geological stores (Tab 1):** fossil fuels, and carbonate rock (limestone) heated to make cement and lime. Without people, that carbon would come back to the atmosphere only over millions of years, through volcanoes and weathering in the slow carbon cycle. Whether the source is a "fuel" doesn't matter.
- **Land clearing (Tab 2):** cleared forest that doesn't regrow moves carbon from the land store to the air for good. It counts.
- **Fast-cycle carbon:** firewood, biofuels and crop residue from land that regrows, and the carbon in cattle feed or rotting food. This was taken from the air recently, so its **CO₂ is not counted**, and the page says why. Its **CH₄, N₂O, CO, NOx, black carbon and aerosols *do* count**. A cow's methane is recycled carbon, but while it is methane it traps far more heat than the CO₂ it came from.

**Experiments, not inventories.** The page models chemistry and physics. It is not an emissions inventory. Its main use is thought experiments built from a few basic tools: an activity, a combustor or process, a count, and a population. For example: "everyone in India rides a motorbike 10 km a day for a year", "run 20 GW of subcritical coal plants for a year at 60%", "make a year of China's cement". National datasets supply scale (population, §4.1) and reference figures to compare against. They are not what drives the page, so no fleet-by-country dataset is needed.

### 1.3 Design principle: simple first, detail on demand

**This applies to every tab and every section of the page.** All the detail in this plan is on the page, but the first view is simple. A reader who wants more drills in to find it. Three levels:

| Level | What the reader sees | Example (fuels) |
|---|---|---|
| **1. Overview** (default) | Plain words, a short list, one easy example, a few numbers. Nothing a non-specialist would need to look up. | 6 everyday fuels: coal, natural gas, petrol, diesel, jet fuel, ship fuel. The coal reaction `C + O₂ → CO₂`. |
| **2. Detail** (expand) | Full lists, every column, harder examples, reaction chains, caveats and simplifications. | All fuel rows incl. lignite and anthracite; the methane, then octane, reactions; H:C, density, sulfur. |
| **3. Source** (click a number) | The parameter popover and Assumptions panel (§3.3): value, unit, source, edit, reset. | "IPCC 2006 Vol 2 Table 1.4, 74,100 kg CO₂/TJ, editable". |

Rules:
- **Order from easiest to hardest** within any section: coal before methane before octane, cars and buses before blast furnaces, CO₂ before ozone chemistry.
- **No unexplained jargon at Level 1.** Use the plain name ("brown coal", "ship fuel") or give the term a short hover definition (`DelayedTooltip`). Terms like anthracite, NMVOC, H:C, tonne-km and NCV appear first at Level 2.
- **Short lists at Level 1, full lists at Level 2.** A short-list row may stand for a group: "Coal" at Level 1 expands into its ranks at Level 2.
- **Level 1 must work alone.** The calculator gives a sensible answer from Level 1 inputs only, and the story reads end to end without expanding anything.
- **Every Level 1 number is clickable**, opening its Level 3 popover. A reader can check or edit a number without first expanding Level 2.
- **Expansion state is remembered** per section, locally and in the shared URL, alongside edited parameters.
- Build with the existing common components: `Collapsible` for expanding sections, `InfoPanel` for side explanations, and `DelayedTooltip` for term definitions.

---

## 2. Input tabs

Tabs 1–3 are the input tabs. Each one turns activities into emissions; the simulation runs those emissions through time, and the output tabs (4–7) show concentrations, forcing and temperature.

### 2.1 Each input tab is optional

- The user can use any one, two or all three input tabs. The simulation needs at least one.
- An input tab is **in use** once it holds at least one activity (an entered input or a preset). Clearing its inputs takes it out of use.
- Each in-use tab has an **"Include in model"** toggle beside its result, on by default. It switches the tab's contribution off without losing its inputs, so the reader can compare, for example, fossil fuels alone against fossil fuels plus land clearing.
- The tab strip marks which input tabs are in use and included (a badge with an accessible name such as "In use, included").
- Each activity on an included tab becomes an emission event at time 0 in the simulation ([simulation §2.1](2026-10-24-01-carbon-cycle-simulation-plan.md)). Each output chart and table says which input tabs it draws on ("From: Fossil fuels and limestone; Land and agriculture"), and attribution (§1.1, view 3) lists only those tabs.
- With no input tab in use, the output tabs show an empty state rather than blank charts or zeros: a short explanation, a link to each input tab, and one starter preset per tab.
- No input tab depends on another. Links between them (fertiliser production in Tab 1, its N₂O in Tab 2; plastics incineration in Tab 1, landfill in Tab 3) are explanatory text, not data dependencies.
- Tabs can use different emission profiles (one a single pulse, another a constant rate for a duration). Each is its own emission event, so mixing them works.
- Changing an input tab or its include toggle changes the starting events, so the run goes back to time 0 and pauses ([simulation §5.1](2026-10-24-01-carbon-cycle-simulation-plan.md)).
- Which tabs are in use and included is part of the shared URL state, alongside the inputs, edited parameters and the run's event record (§3.3).
- Later: page-level presets that fill several input tabs at once, e.g. "the world's emissions in 2022" across all three.

### 2.2 Output contract to the simulation

All input tabs produce the same shape. The simulation turns each result into an emission event and keeps track of which tab and activity each part came from, so forcing can be attributed back to activities (§1.1).

```
EmissionResult
  CarbonMassKg          // net C added to the atmosphere + ocean + land system from geological stores or land clearing (kg C)
  Co2MassKg             // derived, for display
  Profile               // Pulse (default) | ConstantRate(period, duration); masses are per pulse, or per period
                        //   (e.g. per year for 30 years, per second for 2 hours)
  Breakdown             // combustion, calcination (rock), land clearing, upstream, incomplete-combustion products
  NonCo2                // masses for the gases and forcing: CH₄ (fossil | biogenic), N₂O, CO, NMVOC, NOx (as NO₂), SO₂, NH₃,
                        //   black carbon, organic carbon, halogenated gases (by species)
  Other                 // aircraft-km (contrails), land area converted (albedo)
  Source                // tab + activity, for attribution
  ParameterSnapshot     // which parameter values produced this
```

The simulation's carbon and gas processes take the carbon mass, the long-lived gases and the profile. A fuel burned in an enclosure also draws its O₂ from the enclosure's air, through the Tab 1 chemistry engine. The forcing additionally takes the short-lived species for ozone, aerosol, contrail and albedo forcing.

### 2.3 Carbon accounting rules

- Carbon in CO, NMVOC and **fossil** CH₄ is included in `CarbonMassKg`, since it all ends up as CO₂. It is also reported in `NonCo2` for its short-term effects. The UI must label this clearly so it isn't read as double counting.
- Carbon in **biogenic** CH₄ (cattle, rice, manure, landfill) is *not* added to `CarbonMassKg`. It was taken from the air recently, and it returns there as CO₂ when the CH₄ breaks down. The CH₄ itself still counts in full for forcing (§1.2, "What counts").

### 2.4 Results display

Every input tab's result uses the same display: auto-scaled units (kg → t → kt → Mt → Gt), both CO₂ and C, and the relatability anchors. [Tab 1 §6](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md) specifies it; Tabs 2 and 3 reuse it.

---

## 3. Parameter system (cross-cutting)

### 3.1 Two kinds of number

- **Constants** — physically fixed, read-only, shown with source: atomic masses (C 12.011, H 1.008, O 15.999), molecular masses (CH₄, H₂O, CO₂, O₂, …), CO₂:C ratio 3.664, stoichiometric coefficients.
- **Assumptions** — estimates, editable: densities, carbon fractions of mixtures and coal, calorific values, vehicle efficiencies, completeness, upstream overheads, later-tab model parameters.

### 3.2 Parameter record

```
Id                 // stable key, e.g. "fuel.diesel.density"
Label, Description
Kind               // Constant | Assumption
Value, DefaultValue
Unit               // with allowed display units
Min, Max           // validation / sensible bounds
Source             // citation + URL
Notes              // e.g. "as received", "at 15 °C"
Group / Tab        // for filtering
```

### 3.3 Viewing and editing

- Each value on the page is clickable → small popover: value, unit, source, edit (if Assumption), reset.
- A full **"Assumptions" panel** per tab: table of all parameters, filterable, shows modified values highlighted, "reset all".
- Constants appear in the same views, visibly locked.
- Edited state is shareable: encode modified parameters only in the URL; optionally also persist locally.
- Validation: reject out-of-range or non-physical input (e.g. carbon fraction > 1).

### 3.4 Engine design

- Pure calculation library, no UI dependencies, unit-tested. Inputs = parameter set + user inputs, output = `EmissionResult`.
- Tests: stoichiometry checks (C mass in = C mass out), known-answer tests for each fuel's kg CO₂/L, scale tests (1 L vs 10¹² L give linear results).
- Known-answer tests against IPCC: the page's computed kg CO₂/TJ for each fuel (from carbon fraction and NCV) lands within the IPCC 95% range.

### 3.5 Source order for defaults

Resolved (Q6): the latest IPCC material wins; other sources fill the gaps IPCC leaves. No national (e.g. Australian) inventory factors as defaults. Each parameter's `Source` records which tier it came from.

1. **IPCC.** The 2006 Guidelines as amended by the **2019 Refinement**, which is the latest inventory methodology. Vol 2 (Energy) covers fuel NCV, carbon content and CO₂ factors, Tier 1 CH₄ / N₂O factors for stationary and mobile combustion, and fugitive emissions (Ch 4). The IPCC Emission Factor Database (EFDB) adds to it. **AR6** (WG1) is used for the physical constants and later-tab parameters (forcing, lifetimes, GWPs), and **AR6 WG3** for the sector split in the [Tab 1 combustor sub-plan §2](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md).
2. **Sector bodies, where IPCC gives no number** (vehicle fuel use, occupancy, plant efficiency, density, sulfur):
   - Shipping: the latest IMO GHG Study (Fourth, 2020, unless a newer one has been published).
   - Aviation: the ICAO Carbon Emissions Calculator methodology (fuel per km by aircraft type, load factors, per-passenger allocation).
   - Freight per tonne-km: the GLEC Framework (Smart Freight Centre) / ISO 14083.
   - Road vehicles: ICCT (global fleet fuel economy, with India and China coverage). IEA for fleet mix.
   - Power and industry: IEA (plant efficiencies, sector CO₂, cement and steel intensities). IEA / UIC Railway Handbook for rail.
   - CO / NOx / NMVOC: the EMEP/EEA Guidebook, where IPCC has no Tier 1 factor. It is Europe-derived, so flag it where a combustor's Asian or African reality differs (e.g. two-stroke engines, unregulated diesel).
   - Fuel specs (density, sulfur): standards (ISO 8217 for marine fuel; ASTM / EN / BIS / GB national fuel specs), recorded as a range.
3. **Peer-reviewed literature**, only where tiers 1–2 have nothing, e.g. brick kilns.

---

## 4. Data

### 4.1 Emissions and population: OWID CO₂ dataset

Source: [owid/co2-data](https://github.com/owid/co2-data), one row per country per year, in CSV, XLSX or JSON (split by country). Preferred over using the Global Carbon Budget directly because it bundles population with emissions.

Columns of interest (confirmed in the codebook):
- `iso_code`, `country`, `year`, `population`
- `co2` (Mt CO₂/yr, excluding land-use change), `co2_per_capita`
- by source: `coal_co2`, `oil_co2`, `gas_co2`, `flaring_co2`, `cement_co2`
- `land_use_change_co2`, `methane`, `primary_energy_consumption`

Uses:
- Population-scaled presets ([Tab 1 §6](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md); activity versions in the [combustor sub-plan §5](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md)).
- National presets: "China's coal CO₂ in 2024" as a ready-made quantity, for any country.
- Reference anchors in the results: "this is X% of the world's (or a chosen country's) annual fossil CO₂".
- Gas flaring: `flaring_co2` gives a national check for the gas-flare combustor ([Tab 1 combustor sub-plan §2.3](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md)).

Notes:
- OWID's CO₂ figures *are* Global Carbon Budget numbers, processed and converted at 3.664. The two are the same source, so cite both.
- They're inventory estimates, not measurements, and the UI should say so.
- OWID gives CO₂ by fuel, not fuel quantities. A national preset entered as "coal CO₂" skips the combustion chemistry. To keep the chemistry in play, back-calculate fuel mass with the page's own emission factor, and show that step ("≈ N Mt of bituminous coal at the current carbon fraction"). An alternative is to drive presets from `primary_energy_consumption` through the energy input mode.
- Updated about annually. Pin a version or snapshot date in the parameter source field.

### 4.2 ClimateExplorer data

Likely needs (to confirm):
- The simulation: daily total solar irradiance (NOAA NCEI TSI CDR, already in ClimateExplorer) as the sunlight for the greenhouse and Earth scenarios; latest CO₂, CH₄ and N₂O and the global temperature anomaly as starting values; Global Carbon Budget CO₂ emissions for a preset ([simulation §6](2026-10-24-01-carbon-cycle-simulation-plan.md)).

---

---

## 5. Work breakdown (cross-cutting)

Per-tab work is in each input tab's plan, and the output tabs' work is in the [simulation plan §11](2026-10-24-01-carbon-cycle-simulation-plan.md) (§1.2).

1. Parameter system: record type, registry, constants set, edit popover, assumptions panel, URL state (incl. expansion state, §1.3)
2. Page shell: the seven tabs (three input, four output), input-tab in-use state and "Include in model" toggles, tab-strip badges, the empty state for the output tabs, and URL state for all of these (§2.1)
3. `EmissionResult` type, one emission event per activity on the included input tabs, and attribution back to tab and activity (§2.2, §2.3)
4. Shared results display (§2.4)
5. OWID CO₂ dataset ingestion (§4.1)

---

## 6. Open questions

Question numbers are shared across the Carbon Cycle plans. Tab-specific questions are in each tab's plan, and the simulation's (which cover the output tabs) are Q16–Q23 in the [simulation plan](2026-10-24-01-carbon-cycle-simulation-plan.md).

- ~~Q6~~ Resolved: latest IPCC first, then sector bodies, then literature; no country-specific defaults (§3.5).
- **Q8** Ozone and sulfate in the forcing (Tab 6): a simple editable yield factor per precursor, or published per-precursor forcing coefficients (e.g. AR6 emissions-based ERF)? The second is more defensible, but it attributes forcing to global emissions rather than computing ozone or aerosol. Now that the page's purpose is attribution (§1.1), leaning towards the published coefficients (AR6 Ch 6; Thornhill et al. 2021) for the short-lived agents, with the reactions shown as the explanation of where each coefficient comes from.
- ~~Q9, Q10~~ Dropped with the modelled-vs-measured tab, which is out of scope (§1.2).
- ~~Q13~~ Resolved: "Waste and industrial gases" is a third input tab ([Tab 3 plan](2026-10-21-06-carbon-cycle-tab3-waste-industrial-gases-plan.md)).
- **Q14** Natural sources that respond to warming, such as wetland methane and permafrost: leave out (the page models human forcing only), or add as a Level 2 feedback process in the simulation, shown on Tab 5? Wetlands are the largest single methane source, and their emissions are rising.
- ~~Q15~~ Resolved: every input tab is optional; the user needs at least one and can use all three (§2.1).

---

## 7. Output tabs

Tabs 4–7 are views of the one [simulation](2026-10-24-01-carbon-cycle-simulation-plan.md) run, on its shared clock. Each shows its aspect of every scenario in the run, and the simulation plan (§4, §7.5) holds their detail. This section sets what each must explain. Level 1 examples (§1.3):
- Tab 5: "of each tonne of CO₂ released, roughly 40% is still in the air after a century, and about a fifth after a thousand years". On an Earth scenario the reader watches this happen.
- Tab 6: the §8 forcing chart at the current simulated time, where each bar can be clicked to show its sources. "More CO₂ traps more heat"; the logarithmic relation is at Level 2.

Scope:
- **Tab 4 — Overview:** part of each of Tabs 5–7 for every scenario: the key readouts, one small chart from each, and the event log.
- **Tab 5 — Carbon, gases and rocks:**
  - CO₂: reservoirs (atmosphere, land biosphere and soil, ocean surface and deep, crust as limestone and kerogen), the simulation's boxes. The airborne fraction over time; a summary table at years, centuries and millennia.
  - Other long-lived gases: single-lifetime decay. CH₄ ~12 yr (perturbation lifetime), N₂O ~110 yr, halogenated gases per species (to verify against AR6).
  - Shows where fossil and biogenic CH₄ end up: both become CO₂, but only fossil CH₄ adds net carbon.
  - In enclosures: O₂, CO₂ and the other gases, the flame, pressure, plants and rock.
- **Tab 6 — Radiative forcing:** concentration → forcing by agent, then attribution back to species and activities (§1.1):
  - CO₂: logarithmic relation.
  - CH₄ and N₂O: square-root relations, with their spectral overlap.
  - Halogenated gases: linear (radiative efficiency × concentration).
  - Short-lived agents (ozone, aerosol–radiation, aerosol–cloud, stratospheric water vapour, contrails, black carbon on snow): from emission rates via per-species coefficients (Q8). Their forcing tracks the emission rate, not a cumulative total.
  - Land-use albedo: from area converted.
  - Solar and volcanic: not human forcing (§1.2, "What counts"), so not attributed to activities. The simulation's sunlight and the reader's eruptions still show their effect on the Earth scenarios.
  - In enclosures: sunlight absorbed, and the gas's own infrared in the ideal greenhouses.
- **Tab 7 — Temperature:** forcing → transient response through the simulation's planet energy balance, and the equilibrium it heads for. A two-layer response (mixed layer and deep ocean) is planned to replace the single layer. In enclosures: the gas and floor temperatures, water and ice.


### 8. The target: explain every bar of the forcing chart

The reference is the effective radiative forcing (ERF) chart from *Indicators of Global Climate Change 2022* (Forster et al., ESSD 15, 2295–2327, 2023; [figure](https://en.wikipedia.org/wiki/Radiative_forcing#/media/File:ESSD_Radiative_Forcing_1750_to_2022.png)). The page should let a reader point at any bar and follow it back to the activities and reactions behind it. The column "Input tab" names the input tabs whose activities make each agent.

| Agent | ERF 1750–2022 (W/m²) | Where it comes from (approx. shares, to verify) | Input tab |
|---|---|---|---|
| CO₂ | +2.25 | Fossil fuels and limestone (~2/3 of cumulative emissions since 1750); land clearing (~1/3) | 1, 2 |
| CH₄ | +0.56 | Agriculture and waste ~60% of human emissions (cattle and other ruminants, rice paddies, manure, landfills); fossil fuels ~1/3 (gas leaks, coal mines, oil); biomass burning the rest | 1, 2, 3 |
| N₂O | +0.22 | Agriculture ~2/3 of human emissions (fertiliser, manure); also industry (nitric and adipic acid), combustion, wastewater | 2 (some 1, 3) |
| Halogenated gases | +0.41 | CFCs and HCFCs (old refrigerants, foams, spray cans: phased out under the Montreal Protocol and declining slowly, lifetimes ~50–100 yr); HFCs (replacement refrigerants, rising; Kigali Amendment); SF₆ (electrical switchgear); PFCs (aluminium smelting) | 3 |
| Ozone | +0.48 [0.24 to 0.72] | Tropospheric ozone made from CH₄, NOx, CO and VOCs (+), minus stratospheric ozone destroyed by CFCs (−) | 1, 2, 3 |
| Stratospheric water vapour | +0.05 (AR6) | Methane oxidised in the stratosphere (`CH₄ + 2 O₂ → CO₂ + 2 H₂O` overall). Aircraft water vapour is a tiny part | via CH₄ |
| Contrails and contrail cirrus | +0.05 [0.02 to 0.09] | Aircraft | 1 |
| Black carbon on snow | +0.06 [0.00 to 0.14] | Soot from diesel, coal and biomass burning, settling on snow and ice | 1, 2 |
| Land use (albedo, irrigation) | −0.20 (AR6) | Dark forest cleared to lighter cropland and pasture reflects more sunlight; irrigation | 2 |
| Aerosol–radiation | −0.21 [−0.42 to 0.00] | Sulfate (SO₂ from coal, oil, ships); nitrate (NOx, plus ammonia from agriculture); organic carbon. Black carbon partly offsets (+) | 1, 2 |
| Aerosol–cloud | −0.77 [−1.33 to −0.23] | The same aerosols seeding brighter clouds. Largest uncertainty on the chart | 1, 2 |
| **Total anthropogenic** | **+2.91 [2.19 to 3.63]** | | |
| Solar, volcanic | ~0.01; episodic | Natural. A footnote only | — |