# Carbon Cycle page — Tab 8: Time-stepped simulation

- **Date:** 2026-10-24
- **Status:** Proposed (ideal greenhouse and volcano button added 2026-10-24)
- **Author:** Patrick Lea (with Claude)
- **Scope:** Tab 8 of the Carbon Cycle page: a box model run forward through time from a user-set initial condition, in several scenarios side by side. The simulation engine (in the model project from the [chemistry engine plan §3](2026-10-23-02-carbon-cycle-tab1-chemistry-engine-plan.md)), its scenario configurations, the daily TSI driver, and the web client pieces: render mode routing, the run loop, uPlot charts through JS interop, readouts, units and layout.
- **Builds on:** [Carbon Cycle page — Overview](2026-10-21-01-carbon-cycle-plan.md); [Tab 1 chemistry engine](2026-10-23-02-carbon-cycle-tab1-chemistry-engine-plan.md) (the burner uses its stoichiometry and heat); [US customary units toggle](2026-10-21-02-us-customary-units-plan.md) (readouts follow its setting)
- **Branch context:** `issues/carbon-cycle`

The other tabs answer "how much": a quantity of fuel goes in, and emissions, forcing and temperature come out. This tab answers "what happens, and when". The reader sets something burning, starts the clock and watches O₂ fall and CO₂ rise in a sealed house, three greenhouses and two versions of the Earth, all at once. The timescales differ by orders of magnitude. A big flame in a house goes out in about a minute, and weathering takes hundreds of thousands of years. So the reader can speed the clock from real time up to about 1,000 simulated years per second while it runs. Section references without "overview" or "Tab N" are to this document.

---

## 1. What the simulation is

- **A box model.** Each scenario is a set of reservoirs (the gas in an enclosure, liquid water, plants, rock, the ocean) holding amounts of chemical species. Processes (burning, venting, condensation, photosynthesis, weathering) move amounts between them. The model steps forward in time from an initial condition.
- **One engine, several configurations** (§3). A scenario is a choice of enclosure, a set of processes that are switched on, and their parameters. Sealed house, greenhouse and Earth are not separate models.
- **Live output.** As the simulation runs, each scenario shows number readouts and charts of the quantities it tracks (§4.1). The reader picks which quantities to show.
- **The reader controls the step** (§5.1) and can change it mid-run: from real time (1 s per s) up to about 1,000 simulated years per second.
- **The reader can intervene.** On the Earth scenarios, a button sets off volcanic eruptions, one or a hundred at a time (§3.7).
- **Cut-offs become events** (§4.4). When the flame goes out or the N₂ vent can no longer hold the pressure, the input stops. The moment is marked on every chart of that scenario.

**Levels** ([overview §1.3](2026-10-21-01-carbon-cycle-plan.md)):
- **Level 1:** one preset initial condition and the scenario panels (§3). Each panel shows a few readouts (O₂, CO₂, temperature, and whether the flame is burning) and three small charts. Play, pause, reset, a speed control and the volcano button.
- **Level 2:** the initial-condition editor, all tracked quantities with show/hide, per-scenario parameters, the events list, the accuracy control (§5.3), a fixed-step toggle that shows what a too-large step gets wrong, and a compare view (one chart per quantity, a line per scenario).
- **Level 3:** every parameter opens its popover (overview §3.3).

**Relation to the other tabs.** Tab 8 is separate, like Tab 7. It has its own initial condition and does not feed Tabs 4–6. It reuses the Tab 1 chemistry engine for what a fuel's burning consumes and produces, and the Tab 1 fuel table for which fuels can be burned. Its Earth configuration uses the same forcing and response formulas as Tabs 5 and 6 (Q21).

## 2. Initial condition

### 2.1 The input

One input, shared by every scenario, for example "burn methane at 100 L/s for 5 years":

- **Fuel:** any fuel in the [Tab 1 fuel table](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md), ideal or real, with its carbon split ([Tab 1 §3](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md)). Default: methane, complete combustion.
- **Rate:** the reader's choice, as a volume or a mass per unit time. Gas volumes are at 0 °C and 101.325 kPa, the same basis as the fuel table's gas density. This is an editable assumption, and the label states it.
- **Duration:** how long the input runs, unless a cut-off stops it first.
- **Start date:** sets the sunlight. It gives the day of year for the season and picks the TSI record value (§6.1). Default: the latest full year in the TSI record.

All scenarios start from the same input and the same starting air, as specified. Q16 asks whether the input should also scale with each enclosure, because their sizes are so different (§2.3).

### 2.2 Starting air and surroundings

- **Composition:** today's dry air, with N₂, O₂ and Ar as fixed constants, and CO₂, CH₄ and N₂O from the latest values in ClimateExplorer's own records (§6.2). Humidity is an editable assumption (default 50% relative humidity).
- **Temperature and pressure:** 101.325 kPa. Temperature defaults to 15 °C, roughly the global mean surface temperature; the Earth scenarios take their starting temperature from the global temperature record (§6.2).
- **Surroundings** for the enclosures: outside air temperature and ground temperature, each a constant (default 15 °C), and the sky (§4.3). Later, an option to use a ClimateExplorer location's temperature record and latitude (§6.2).

### 2.3 Scale check (shown on the page)

The rate is the reader's to set. The two below are only illustrations, but any single rate shows the same thing: a rate that does something visible to the Earth puts the house's flame out almost at once, and one that suits the house does nothing visible to the Earth. Numbers are approximate, to verify in the engine's tests:

| | 100 L/s of methane | A gas hob (~2.5 kW, ~0.07 L/s) |
|---|---|---|
| Heat released | ~3.6 MW, a house fire | 2.5 kW |
| Sealed 250 m³ house, O₂ from 21% to a 15% cut-off | ~70 s | ~28 h |
| CO₂ in the house at cut-off | ~3% | ~3% |
| Earth, after 5 years | CO₂ up ~0.000004 ppm; invisible | — |

For scale: human CO₂ emissions are ~37 Gt/yr, about six million times 100 L/s of methane burned to CO₂. That is why the speed range is so wide, and why Q16 asks about scaling the input to each enclosure. The presets (§2.4) give each scenario an input on a scale where something visible happens.

### 2.4 Presets

- "A gas hob left on in a sealed house"
- "Burn methane at 100 L/s for 5 years" (the brief's example)
- "A coal fire" (solid fuel, mass rate; no change in moles, §4.4)
- "Today's world emissions, for 100 years" (rate from the CO₂ emissions dataset, §6.2)
- "A one-off pulse, then wait a million years" (to show weathering)
- Later, Level 2: "Foote's cylinders, 1856" (§3.3)

A preset is a parameter set, as on the other tabs.

## 3. Scenarios

| # | Scenario | Walls | Sunlight | Biology | Weathering | Temperature model |
|---|---|---|---|---|---|---|
| 1 | Sealed house | Opaque | None | None | None | Box |
| 2 | Glass greenhouse | Glass: let sunlight in, block infrared | Yes | None | None | Box |
| 3 | Ideal greenhouse | Ideal: hold the gas, let all radiation through | Yes | None | None | Box, with gas radiation |
| 4 | Ideal greenhouse with plants and weathering | Ideal | Yes | Plants | Crushed rock on the floor | Box, with gas radiation |
| 5 | Basic Earth | — (planet) | Yes | Land biosphere | Yes, balanced by volcanoes | Planet energy balance |
| 6 | Earth without volcanoes | — (planet) | Yes | Land biosphere | Yes, with nothing to balance it; eruptions by button | Planet energy balance |

All six are configurations of the one engine (§4.5).
- Scenarios 2 and 3 differ only in their walls. Putting them side by side isolates what the gas's own radiation does.
- Scenario 6 is scenario 5 with background outgassing switched off.
- Wall type is a parameter, so the reader can also make combinations not in the table, such as plants in a glass greenhouse.

**Every box is sealed for gas** apart from its N₂ vent (§4.4). Each box exchanges heat with its floor and the ground beneath. Opaque and glass walls also conduct heat to the outside air (§4.3). Without that, the house would heat without limit. Ideal walls conduct none by default, so heat leaves an ideal greenhouse only by radiation and through the ground.

### 3.1 Sealed house

No gas exchange, no sunlight. The burner uses up the house's O₂ until the flame goes out at the O₂ cut-off (§4.4). Teaching points:
- How fast it happens at each rate (§2.3).
- Air stops being safe for people long before the flame goes out. Level 2 reference lines show the O₂ and CO₂ limits for breathing (to source, e.g. O₂ below 19.5%, CO₂ above 0.5% for a working day). In incomplete combustion, CO is the bigger danger.
- Water from the flame condenses on the walls, and the readout shows the house's pressure dropping below the outside's (§4.4).

### 3.2 Glass greenhouse

As the house, plus sunlight through the glass. The sun warms it by day and the walls cool it by night.

**Key teaching point:** the CO₂ the flame adds has no measurable warming effect inside a glass greenhouse.
- A greenhouse stays warm because the glass stops the warm air rising away. CO₂ absorbing infrared plays no real part in it.
- The glass is already opaque to infrared, so the gas's infrared only goes back and forth between the gas and the glass.
- R. W. Wood showed this in 1909 (to cite).

Scenario 3 takes the glass away to see what the gas does on its own. The planet's greenhouse effect needs a tall atmosphere that gets colder with height, and it is in scenarios 5 and 6.

### 3.3 Ideal greenhouse

As the glass greenhouse, but with **ideal walls**: they hold the gas in and let every wavelength through, sunlight and infrared alike. By default they conduct no heat. The glass is no longer a factor, so whatever the CO₂ does is the gas's own effect.

- **Infrared crosses the gas.** The sunlit floor warms and gives off infrared. Without glass, that infrared crosses the gas on its way to the sky. CO₂ and water vapour absorb part of it and give off their own, half of it back down.
- **Diminishing returns.** The more CO₂ there is, the more the gas absorbs. Each extra amount adds less, because the centre of the CO₂ band is already absorbing nearly all it can. That is the same logarithmic pattern as the Earth's CO₂ forcing (overview §7), seen in a box.
- **The direction depends on which is warmer.** When the sun has warmed the floor more than the gas, CO₂ sends some of the floor's heat back and keeps it warmer. When the flame has made the gas hotter than the floor, CO₂ helps the gas lose heat to the sky. The second case is why added CO₂ cools the stratosphere (Level 2 note).
- **The effect is small**, because a few metres of gas is a thin layer. Under a real sky it is smaller still. The atmosphere above is already opaque at the centre of the CO₂ band, so in that band the sky looks almost as warm as the air.
- **The sky is a parameter:** *real sky* (the default: an effective sky temperature, to source) or *open to space* (as if there were no atmosphere above; this gives the largest effect).
- **Computed, not assumed.** The engine computes the size of the effect at today's CO₂ and at the flame's CO₂, and the page shows it next to the glass greenhouse.

**Foote's experiment (1856).** Eunice Foote put two glass cylinders in sunlight, each with a thermometer: one filled with "carbonic acid gas" (CO₂), the other with ordinary air. The CO₂ cylinder got hotter (to ~125 °F against ~106 °F, from ~80 °F; to verify against her paper) and took longer to cool. She suggested that an atmosphere with more CO₂ would make the Earth warmer.
- She was right, but her apparatus couldn't show the mechanism. Her cylinders were glass, which blocks the thermal infrared that the greenhouse effect works through.
- What she measured was mostly CO₂ absorbing the near-infrared part of sunlight directly, plus differences in how the two gases hold and conduct heat.
- Tyndall's measurements from 1859 were the first to show gases absorbing thermal infrared.

A later Level 2 preset, *Foote's cylinders*, would shrink scenario 2 to her cylinders and run one full of CO₂ beside one of air. It needs one addition to the model: gas absorbing sunlight (§4.3).

### 3.4 Ideal greenhouse with plants and weathering

Scenario 3, plus plants and a floor of crushed silicate rock.

- **Plants** draw CO₂ down into stored carbon and put O₂ back (`CO₂ + H₂O → CH₂O + O₂`). In a sealed greenhouse they stop at the *CO₂ compensation point*, where uptake equals their own respiration: ~40–50 ppm for most crops (C3 plants; to verify). Real growers add CO₂ to sealed greenhouses for this reason. In daylight, a 100 m² crop can roughly keep up with a gas hob's CO₂ (both a few mmol/s; to verify), but not with 100 L/s.
- **Silicate weathering** turns CO₂ into carbonate rock (net `CaSiO₃ + CO₂ → CaCO₃ + SiO₂`). Its rate depends on temperature and CO₂ (§4.2). On a greenhouse floor this is *enhanced weathering*, a real carbon-removal method that spreads crushed basalt on fields. It acts over years, not minutes.

**What the reader will see:**
- **CO₂** rises while the burner runs, then falls: fast through the plants and slowly through weathering. It ends below the starting level, at the plants' compensation point.
- **Temperature** mostly follows the flame, the sun and the sky. On top of that, the gas's own radiation (§3.3) adds a small warming as CO₂ rises and a small cooling as it is drawn down. Compared with scenario 3, which has the same walls and no plants, the difference is what the plants and the rock do through CO₂.
- **Frozen water** needs the floor to go below 0 °C. That depends on the sky, the ground and the season far more than on CO₂.

The planet-scale version of the same story (rise, draw-down, freeze) is scenario 6.

### 3.5 Basic Earth

The whole atmosphere, a two-layer ocean, a land biosphere, weathering and volcanic outgassing (§4.2), with a planet energy balance (§4.3).

- **Today's emissions.** The brief's example input is invisible here (§2.3). The preset "today's world emissions" shows:
  - CO₂ rising ~2–3 ppm/yr
  - about half of each year's emissions taken up by the ocean and land
  - warming that lags the CO₂
- **O₂ falls too,** by a tiny fraction. Readouts show the change since the start, which makes it visible (§7.4). The real fall is measured (Scripps O₂/N₂ programme; a possible later comparison).
- **The long-term thermostat.** Over hundreds of thousands of years, weathering, sped up by the extra warmth and CO₂, brings CO₂ back towards the level where weathering balances volcanic outgassing. [Tab 1 §2.1](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md) compares the volcanic rate with today's emissions.
- **The volcano button** works here too (§3.7).

### 3.6 Earth without volcanoes

Scenario 5 with background outgassing switched off. This is where the planet-scale story plays out:

1. While the input runs, CO₂ and temperature rise.
2. Afterwards, weathering draws CO₂ down, faster because it is warm and CO₂ is high. With no volcanoes resupplying it, CO₂ keeps falling past the starting level.
3. As CO₂ falls, the planet cools and slows its own weathering. On its own this slows the decline; it would not cause a sharp fall.
4. The sharp fall comes from ice. Cooling spreads ice, ice reflects more sunlight, and below a threshold that feedback runs away to a frozen planet (a "snowball Earth"). Frozen, weathering almost stops, and the planet stays frozen. How a snowball thaws is out of scope.

So a large temperature drop with frozen water is the likely end state here. The sharp drop is ice-albedo feedback, not weathering itself. Timescale: hundreds of thousands of years (to verify in the engine), a few minutes at 1,000 years per second.

### 3.7 The volcano button

Scenarios 5 and 6 have an **Erupt** button, with a count (1, 10 or 100) and an eruption size.
- Each click injects that many eruptions at the current simulated time and marks an event on the charts. A readout counts the eruptions so far.

An eruption does two things, on very different timescales. Provisional sizes, to verify (e.g. against the Smithsonian Global Volcanism Program and published SO₂ inventories):

| Size | Example | ~SO₂ | ~CO₂ | Main effect |
|---|---|---|---|---|
| Large | Pinatubo, 1991 | ~15–20 Mt | ~0.05 Gt (to verify) | ~0.5 °C of global cooling for ~2 years |
| Very large | Tambora, 1815 | to source (several times Pinatubo) | to source | The "year without a summer" |
| Flood basalt | Siberian Traps (end-Permian) | very large, spread over time | thousands of Gt over ~100,000 years or more (to verify) | Long warming. Modelled as raised outgassing for a period, not as a pulse |

- **SO₂** becomes sulfate aerosol in the stratosphere within weeks and falls out over a year or two (§4.2). While it's there, it reflects sunlight. This is the same sulfate cooling as [Tab 1 §4](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md), but high up, where it lasts longer.
- **CO₂** from an eruption is small. A hundred Pinatubos add ~5 Gt CO₂, about 0.6 ppm and well under a year of today's human emissions (~37 Gt).

**The teaching point, in scenario 6.** A hundred eruptions at once cool the planet for a few years, and can push a cold planet over the threshold into the ice state.

At high speed an eruption's cooling lasts a few milliseconds of real time. The substeps still resolve it (§5.3), and the chart's decimation keeps each bucket's minimum (§7.3), so the dip shows.

## 4. Model

### 4.1 Tracked quantities

The reader can show or hide each of these. Hidden quantities are still recorded, so showing one later shows its whole history.

| Quantity | 1 House | 2 Glass | 3 Ideal | 4 Ideal + plants | 5 Earth | 6 No volcanoes | Level |
|---|---|---|---|---|---|---|---|
| O₂ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 1 |
| CO₂ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 1 |
| Temperature (the gas; the planet's surface) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 1 |
| Flame state (burning / out, and why) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 1 |
| N₂, Ar | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 2 |
| CH₄, CO (unburnt fuel and incomplete combustion; on Earth, CH₄ also from the record) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 2 |
| Water vapour, relative humidity | ✓ | ✓ | ✓ | ✓ | — | — | 2 |
| Liquid water, ice | ✓ | ✓ | ✓ | ✓ | ice cover | ice cover | 2 |
| Floor temperature | ✓ | ✓ | ✓ | ✓ | — | — | 2 |
| Pressure | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 2 |
| N₂ vented (cumulative) | ✓ | ✓ | ✓ | ✓ | — | — | 2 |
| Fuel burned, heat released (cumulative) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 2 |
| Sunlight absorbed | — | ✓ | ✓ | ✓ | ✓ | ✓ | 2 |
| Gas emissivity, infrared to the sky | — | — | ✓ | ✓ | — | — | 2 |
| Carbon in plants | — | — | — | ✓ | land | land | 2 |
| Carbon in carbonate rock (weathered) | — | — | — | ✓ | ✓ | ✓ | 2 |
| Ocean carbon (surface, deep) | — | — | — | — | ✓ | ✓ | 2 |
| Radiative forcing, albedo | — | — | — | — | ✓ | ✓ | 2 |
| Stratospheric sulfate, eruptions so far | — | — | — | — | ✓ | ✓ | 2 |

### 4.2 Processes

Each process is a reaction (or a transfer) between reservoirs, with a rate law. Reactions come from the chemistry catalog, so they are balanced and carry their heat. All rates and constants are editable assumptions with sources ([overview §3](2026-10-21-01-carbon-cycle-plan.md)); the values here are provisional, to verify.

| Process | Reaction or transfer | Rate | Scenarios |
|---|---|---|---|
| Burner | fuel + O₂ → products, by the Tab 1 engine at the input's carbon split | the input rate, while burning (§4.4) | all |
| N₂ vent | N₂ (enclosure) → outside | whatever holds the pressure at its target; removes only | 1–4 |
| Condensation / evaporation | H₂O(g) ⇌ H₂O(l) | relaxes vapour towards saturation (Magnus formula) with a short time constant | 1–4 |
| Freezing / melting | H₂O(l) ⇌ H₂O(s) | at 0 °C, limited by the heat available | 1–4 |
| Photosynthesis | CO₂ + H₂O → CH₂O + O₂ | light × efficiency × CO₂ response (zero at the compensation point) × temperature response | 4; land in 5–6 |
| Plant respiration and decay | CH₂O + O₂ → CO₂ + H₂O | stored carbon × base rate × Q₁₀ temperature factor (Q₁₀ ~2); needs O₂ | 4; land in 5–6 |
| Silicate weathering | CaSiO₃ + CO₂ → CaCO₃ + SiO₂ (net) | W₀ × (pCO₂ / pCO₂,ref)^β × exp((T − T_ref) / T_e) (Walker, Hays and Kasting 1981; β ~0.3–0.5, T_e ~10–13 K); × rock area in a box; reduced by ice cover on a planet | 4–6 |
| Volcanic outgassing | crust → CO₂ | constant, ~0.3–0.4 Gt CO₂/yr; by default equal to preindustrial weathering | 5 |
| Eruption | crust → SO₂ (stratosphere) + CO₂ | a pulse per click, sized by §3.7 | 5–6 |
| Sulfate formation | SO₂ → sulfate aerosol (net of the [Tab 1 §4](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md) chain) | SO₂ ÷ ~1 month (to verify) | 5–6 |
| Sulfate fall-out | stratospheric sulfate → surface | sulfate ÷ ~1 year (to verify) | 5–6 |
| Air–sea exchange | CO₂ (air) ⇌ surface-ocean carbon | towards a buffered equilibrium (Revelle factor ~10), timescale ~1 yr | 5–6 |
| Surface–deep ocean mixing | surface ⇌ deep | timescale of centuries to a millennium | 5–6 |
| Methane oxidation | CH₄ + 2 O₂ → CO₂ + 2 H₂O | CH₄ ÷ lifetime (~9–12 yr) | 5–6 |
| Floor–gas convection | floor ⇌ gas | h × floor area × (T_floor − T_gas) | 1–4 |
| Heat through walls | gas → outside air | U × wall area × (T_gas − T_outside); U = 0 by default for ideal walls | 1–4 |
| Heat into the ground | floor ⇌ ground | G × floor area × (T_floor − T_ground) | 1–4 |
| Gas radiation | floor ⇌ gas ⇌ sky, infrared (§4.3) | from the gas emissivity | 3–4 |
| Solar gain | sun → floor or planet | insolation × transmittances × area (§4.3, §6.1) | 2–6 |
| Outgoing longwave | planet → space | OLR₀ + λ (T − T_ref) − F(CO₂, CH₄, sulfate) | 5–6 |

Photosynthesis in scenario 4 adds the chemistry catalog's first biology species, CH₂O, as the stand-in for stored plant carbon. Weathering adds silicate and carbonate solids: CaSiO₃ (wollastonite, standing for silicate rock) and SiO₂. Forsterite (Mg₂SiO₄, olivine) is a Level 2 alternative for enhanced weathering.

### 4.3 Temperature

**Box heat balance (scenarios 1–4).** Two temperatures: the gas, and the floor with the box's contents. The floor holds most of the heat capacity and absorbs the sunlight.

```
gas:   C_gas   dT_gas/dt   = flame heat + h·A_floor·(T_floor − T_gas) − U·A_walls·(T_gas − T_outside) + latent heat + gas radiation
floor: C_floor dT_floor/dt = solar gain − h·A_floor·(T_floor − T_gas) − G·A_floor·(T_floor − T_ground) + floor radiation
```

- **Defaults** are sized for a house and for a greenhouse (to source). The gas's own heat capacity is small: at 100 L/s, heating only the air would take it up by hundreds of kelvins in a minute.
- **Flame heat** is the Tab 1 engine's heat for the fuel burned at its carbon split. It goes into the gas.
- **Latent heat:** condensing and freezing release heat (+); evaporating and melting take it in (−).
- **Solar gain** onto the floor = insolation at the greenhouse's latitude (§6.1) × atmospheric transmittance (default an all-sky average, to source) × wall transmittance (glass ~0.8–0.9; ideal 1) × floor area.
- **Radiation depends on the walls.**
  - **Opaque and glass walls** are opaque to infrared and sit at about the gas temperature. The gas's infrared only goes back and forth inside, so there is no net radiation term. This is the §3.2 point.
  - **Ideal walls:** the floor, the gas and the sky exchange infrared (floor taken as black in the infrared; editable emissivity at Level 2):

```
gas radiation   = ε_gas · σ · (T_floor⁴ + T_sky⁴ − 2 T_gas⁴) · A_floor
floor radiation = −σ · (T_floor⁴ − ε_gas · T_gas⁴ − (1 − ε_gas) · T_sky⁴) · A_floor
```

  - **Gas emissivity** `ε_gas` comes from the standard engineering correlations for CO₂ and water vapour (Hottel; Leckner 1972, to verify). It depends on each gas's partial pressure × path length and on the gas temperature, with a correction for where their bands overlap.
    - Path length is the greenhouse's mean beam length, about its height (editable).
    - CH₄ is left out of `ε_gas` in v1.
    - This is a grey-gas simplification of real band absorption, and Level 2 says so.
  - **T_sky** is the real sky (default: an effective clear-sky temperature, to source) or open to space (~3 K).
  - **Absorption of sunlight by the gas** is left out of v1. The Foote's cylinders preset would need it (§3.3).

**Planet energy balance (scenarios 5–6).** A zero-dimensional energy balance on the ocean mixed layer:

```
C dT/dt = (TSI / 4) × (1 − α(T)) − [OLR₀ + λ (T − T_ref) − F(CO₂, CH₄, sulfate)]
```

- **Forcing `F`:**
  - CO₂ and CH₄ use the simple published forms that Tab 5 will use: logarithmic in CO₂ (5.35 ln(C/C₀) W/m² as the starting point), square-root in CH₄ (overview §7). If Tab 5 adopts newer forms (AR6), this tab uses them too.
  - Stratospheric sulfate adds a negative forcing in proportion to its burden (coefficient to source; Pinatubo's peak was a few W/m²).
- **`λ`** is set so that doubling CO₂ gives an editable equilibrium warming (default ~3 °C, the AR6 best estimate). The ice-albedo part is taken out of `λ` and modelled through `α(T)` instead.
- **`α(T)`** is a smooth step from an ice-free albedo (~0.3) to an ice-covered one (~0.6) over a temperature range, in the style of Budyko and Sellers. This is what allows a snowball state (§3.6).
- **`C`** is a ~100 m ocean mixed layer. A deep-ocean heat layer is left out of v1, so the warming responds faster than it really does. It comes with Tab 6's two-layer response (Q21).
- **`T_ref` and `OLR₀`** are calibrated so that preindustrial CO₂ is in balance at the preindustrial temperature.

### 4.4 Pressure, the N₂ vent and cut-offs

**The N₂ vent (scenarios 1–4).** Each enclosure holds its pressure at the target (default 101.325 kPa) by venting N₂ as gas is added or the gas heats up.
- **It only removes.** If the pressure falls below the target, the enclosure stays below it, and the readout shows the deficit. This happens with methane at room temperature: each mole of methane burned adds one mole of gas net (the fuel itself), but its two moles of water condense. A coal fire changes no moles (`C + O₂ → CO₂`), so only heating moves its pressure.
- **It removes only N₂.** That's a thought-experiment device that keeps the chemistry easy to read. The page says so, because of one side effect: heating pushes N₂ out and *raises* the O₂ fraction. A real leak would remove all gases in proportion. A Level 2 "proportional leak" option shows the difference.

**Cut-offs.** The input stops at the first of:
1. **O₂ cut-off:** O₂ falls below the combustion limit. Default ~15% by volume (to verify; flammability data put methane's limiting O₂ at ~12% in N₂, higher in CO₂). It is an editable assumption, with a note that the real limit rises with CO₂ and falls with temperature.
2. **N₂ cut-off:** the vent has no N₂ left, so the pressure can no longer be held. This happens when the enclosure gets hot. At constant pressure, heating the gas from 20 °C to ~850 °C would mean venting about three-quarters of it, nearly all of the N₂. The enclosure's heat capacity decides whether it gets that hot.
3. **Duration:** the input's own end.

A cut-off is permanent by default, as specified. Q20 asks about relighting when plants restore the O₂.

**Events** are marked on every chart of their scenario (a vertical line with a short label) and listed in the scenario's event log, with their simulated time:
- input started, input ended, O₂ cut-off, N₂ cut-off, eruptions
- Level 2: water starts condensing, water starts freezing, CO₂ reaches the plants' compensation point, breathing limits crossed (§3.1), the planet tips into the ice state

### 4.5 One engine, many configurations

```
ScenarioConfiguration
  Id, Name
  Enclosure             // Box: volume, floor area, wall area, wall type (Opaque | Glass | Ideal), U, h, G,
                        //   heat capacities, path length, sky | Planet: area, mixed-layer depth
  Processes             // the §4.2 processes switched on, each with its parameters
  TemperatureModel      // BoxHeatBalance | PlanetEnergyBalance
  CutOffs               // O₂ limit, N₂ vent on/off, pressure target
  Drivers               // sunlight (TSI series, latitude), outside air, ground and sky temperatures

SimulationState
  Time                  // s since start
  Pools                 // mol per species per reservoir: enclosure gas, liquid water, ice, plants, rock,
                        //   ocean surface, ocean deep, stratospheric sulfate
  Temperatures          // K: gas and floor (box), or mixed layer (planet)
  InputActive, Events   // events include the reader's eruptions
```

Scenarios 1–6 are six `ScenarioConfiguration` values. A reader could make more by editing one (Level 2: "duplicate and edit"). A new scenario needs new engine code only if it needs a new process.

## 5. Simulation engine

Pure and UI-free, in the Carbon Cycle model project ([chemistry engine plan §3](2026-10-23-02-carbon-cycle-tab1-chemistry-engine-plan.md)), alongside the chemistry catalog. It must run in the WebAssembly client.

### 5.1 Advancing by the reader's step

- **One operation.** The engine's one operation is `Advance(state, dt)`: move this scenario forward by `dt` seconds of simulated time. It keeps no memory of earlier step sizes, so the step can change on every call. A new speed from the reader applies on the next tick.
- **Speed.** The reader sets the speed: simulated time per real second, on a log scale with stops at real time, 1 min/s, 1 h/s, 1 day/s, 1 week/s, 1 month/s, 1 yr/s, 10, 100 and 1,000 yr/s. Each tick's step is speed × the real time since the last tick (§7.2). The readout shows the step in use.
- **Interventions.** An eruption is an injection into the state, applied at the start of the next `Advance` at the current simulated time and recorded as an event.
- **Controls:** pause, resume and reset. When paused, "step once" and "run to the next event".

### 5.2 No reservoir goes negative

Every process is a reaction with a rate, in mol/s of reaction. Before a substep is applied:

1. Compute each process's wanted rate.
2. For each pool, add up what all processes want to take from it in this substep. If that exceeds what's there, the pool's factor is (amount there) ÷ (amount wanted); otherwise 1.
3. Scale each process by the smallest factor among the pools it takes from: its limiting reagent.
4. Apply.

After step 3, no pool loses more than it holds, because every process drawing on it was scaled by at most that pool's factor. Inflows in the same substep are ignored, which keeps the rule conservative. Scaling a whole reaction, rather than each flux on its own, keeps every element balanced. A clamp is recorded on the state, so tests and the Level 2 accuracy readout can see when one happened.

### 5.3 Substeps and event timing

The clamp prevents negatives but not inaccuracy. At 1,000 yr/s and 60 ticks a second, each tick is ~17 years, while a big flame in a house lasts about a minute. So `Advance` splits its `dt` into substeps:
- **Size:** the largest substep in which no pool changes by more than a set fraction (default 1%, the Level 2 accuracy control) and no temperature changes by more than a set amount. When fluxes are small against their pools, substeps grow to the whole `dt`. Examples: a house after its flame is out, or an Earth near balance.
- **Event timing:** if a cut-off threshold would be crossed inside a substep, the substep is shortened to land on the crossing (linear estimate, then one refinement). The event's time is therefore right whatever step the reader chose.
- **Fixed-step toggle (Level 2):** switches substeps off, so the reader can watch a big step get the answer wrong. The clamp still applies.
- **Budget:** each scenario has a cap on substeps per tick (§7.2). If it's hit, the runner slows the clock for all scenarios together rather than skipping ahead.

The integrator is forward Euler on these adaptive substeps. It's easy to explain and, with the 1% rule, accurate enough. The tests (§8) check convergence as the fraction shrinks.

### 5.4 Conservation and reproducibility

- **Elements:** totals (C, H, O, N, Ar, S, Ca, Si) in a closed box are constant, apart from what the vent removes, which is counted. The planet's carbon and sulfur change only through outgassing, eruptions, weathering and fall-out, all counted.
- **Energy:** the heat in, out and stored add up, within the integrator's error.
- **Reproducibility:** the same configuration, the same sequence of `dt` values and the same eruptions give identical results.

## 6. Real-world inputs

### 6.1 Daily total solar irradiance

**Already in ClimateExplorer.** The "Total solar irradiance" dataset (`E45293F9-B7AC-4874-9544-25E006B6B998`) is the NOAA NCEI Total Solar Irradiance CDR observed composite: daily, in W/m², from late 1978 to 2025-06-30. No new ingestion is needed. The local file name still says `v02r01`, while the download URL and the file's header and end date are `v03r00`. Renaming it is a one-line tidy-up, out of scope here.

Use:
- **Greenhouses (2–4):** insolation at the greenhouse's latitude (default to choose; editable) from the day's TSI, the solar declination and the hour angle.
  - At steps under about an hour it follows the time of day, so day and night show. At longer steps it uses the standard daily-mean insolation formula.
  - TSI is reported at 1 AU, so the Earth–Sun distance factor (±3.4% over the year) is applied.
- **Earth (5–6):** TSI ÷ 4, the global-mean top-of-atmosphere sunlight.
- **Averaging over a step:** at long steps, the driver is the mean TSI over the step, computed from prefix sums, so it costs the same at any step size.
- **Outside the record** (Q19): by default, loop the last whole solar cycles in the record (cycles 22–24, ~1986–2019). The record's mean is an alternative. At 1,000 yr/s, the 0.1% solar cycle is invisible anyway.
- **Loading:** the client fetches the series (~17,000 values) once, through the existing WebApi dataset endpoint, and caches it for the session.

### 6.2 Other ClimateExplorer data

| Dataset (existing) | Used for |
|---|---|
| CO₂ from the Mauna Loa Observatory | Starting CO₂ (latest annual mean) |
| Methane (CH₄), global network | Starting CH₄ |
| Nitrous oxide (N₂O), global network | Starting N₂O for the Earth forcing |
| Global temperature anomaly (NOAAGlobalTemp) | Earth starting temperature (anomaly + an absolute baseline, to source) |
| CO₂ emissions (Global Carbon Budget) | Rate for the "today's world emissions" preset |
| A location's temperature record (later) | Outside temperature and latitude for the enclosures |

## 7. Technical approach (web client)

### 7.1 Render mode: WebAssembly, no prerender

The simulation loop runs in the browser. On the server over SignalR, every tick and chart update would cross the network.

- **A page-level `@rendermode` won't work here.** `App.razor` gives `<Routes>` (and `<HeadOutlet>`) a global `InteractiveAuto`. A page inside an already-interactive tree can't switch to another interactive mode.
- **Instead, `App.razor` chooses the mode per request.** A new `App.razor.cs` code-behind (per AGENTS.md) reads the cascaded `HttpContext`. It returns `new InteractiveWebAssemblyRenderMode(prerender: false)` for the simulation route and `InteractiveAuto` for everything else, and `<Routes>` and `<HeadOutlet>` both use it.
- **Arriving by in-app navigation** wouldn't re-run `App.razor`, so the reader could land on the simulation in a Server-mode session. Two guards stop this:
  - Links to the simulation route use a full page load (`forceLoad: true`).
  - The page checks `RendererInfo.Name` on first render, and reloads itself if it isn't running on WebAssembly.
- **Scope:** only the simulation tab's own route (e.g. `/carbon-cycle/simulation`) is forced. The other tabs are mostly reading, and they keep prerendered HTML for first paint and search engines. Q18 asks whether to force the whole page instead.
- **First visit:** with no prerender, nothing shows until WebAssembly starts. `App.razor` renders a static "Loading the simulation…" placeholder for this route. The page title isn't in the first HTML, which is acceptable for this route.

### 7.2 The loop: sim rate apart from redraw rate

One `SimulationRunner` (client, `UiLogic`) owns all the scenarios and one timer:

| Activity | Rate | What it does |
|---|---|---|
| Sim tick | ~60 Hz (`PeriodicTimer`, 16 ms) | Measures real time since the last tick (`Stopwatch`), works out `dt` from the speed, applies any pending eruptions, and calls `Advance` on every scenario with the same `dt`, so they stay in sync. Records a sample per scenario, plus one at every event. |
| Chart push | 10–15 Hz | For each chart, one interop call carrying only the points added since its last push (§7.3). No `StateHasChanged`. |
| Readouts | 4–5 Hz | Copies the latest values to the readout components and calls `StateHasChanged` on them alone. Events also trigger one, so the flame-out shows at once. |

- **Budget:** a tick spends at most ~8 ms on stepping. Past that, the runner reduces the speed actually achieved, for all scenarios together, and the speed readout shows "requested / achieved".
- **Background tab:** browsers slow timers in hidden tabs. A tick's `dt` is capped at the step for ~250 ms of real time, so returning to the tab doesn't cause one giant step. The sim keeps running at the reduced rate.
- **Only the readouts re-render.** Chart components render once and return `ShouldRender() => false`; data reaches them only through JS. The page itself doesn't re-render on ticks.
- **Single thread:** WebAssembly .NET runs on the browser's main thread, so stepping, interop and rendering share it. Multithreaded WebAssembly is not relied on. If the budget proves too tight, the options are ahead-of-time compilation, then a Web Worker (§9).
- **Disposal:** leaving the page stops the timer and destroys the charts.

### 7.3 Charts: uPlot through a thin JS module

uPlot is small (~50 KB), MIT-licensed, and draws hundreds of thousands of points on canvas. The site's current charts (Blazorise.Charts, which is Chart.js) send whole datasets on update, which is too slow for streaming at this rate.

- **Asset:** a pinned uPlot 1.6.x release, vendored into `ClimateExplorer.Web.Client/wwwroot/lib/uplot/` with its licence. No CDN.
- **Module:** a collocated `SimulationChart.razor.js`, like the existing `ReconnectModal.razor.js`. It holds the series arrays, and .NET never keeps a copy of a chart's history. Its API is minimal:
  - `create(element, options)` returns a handle. Options set the series, axes and colours, and the 0.75rem minimum canvas font (AGENTS.md).
  - `append(handle, points)` adds the new points.
  - Small helpers beside those two: `setVisible(handle, series, on)`, `setUnits(handle, series, scale, offset)` (§7.4), `annotate(handle, time, label)`, `reset(handle)` and `destroy(handle)`.
- **Interop:** `[JSImport]`, available because the route is WebAssembly-only.
  - The delta is passed as a `Span<double>` marshalled as a `MemoryView`: the time column, then each series, flattened. This avoids JSON.
  - The call is synchronous, and the module copies the values before returning, because a `MemoryView` over a span is valid only during the call.
- **Deltas only:** the runner keeps a per-chart buffer of points since the last push and clears it after each push.
- **Long runs:** when a series passes a cap (default 20,000 points), the module halves its resolution. It keeps every event point and each bucket's minimum and maximum, so spikes and dips survive.
- **Annotations:** cut-offs, eruptions and other events are drawn by a uPlot draw hook as vertical lines with labels. uPlot has no built-in annotations.
- **Time axis:** linear or log (Level 2 toggle). Log suits side-by-side runs where the house is over in a minute and the Earth runs for millennia.
- **Layout per scenario panel:** a stack of small charts, one per group of shown quantities, each auto-scaling.
  - Level 1 shows O₂, CO₂ and temperature.
  - Level 2 adds major gases (N₂, O₂, Ar), trace gases (CO₂, CH₄, CO; log axis option), water, pressure, and stores (plants, rock, ocean, sulfate).
- **Compare view (Level 2, later):** one chart per quantity, with a line per scenario, plotted as change since the start, because the scenarios' absolute values differ by orders of magnitude.

### 7.4 Units

Readouts and chart axes follow the site's unit setting from the [US customary units plan](2026-10-21-02-us-customary-units-plan.md) (`UnitPreference`). That plan isn't built yet, so it is a dependency. The site's setting is US customary, not imperial. That plan notes the difference matters once volume and mass units appear, and they appear here: a US gallon, not an imperial one.

- **Converted:** temperature (°C ↔ °F), enclosure volume (m³ ↔ ft³), liquid fuel flow (L/s ↔ US gal/s), gas flow (L/s ↔ ft³/min, the US unit for gas), masses at human scale (kg ↔ lb), areas (m² ↔ ft²), pressure (kPa ↔ psi).
- **Unchanged, as in the units plan:** mole fractions (%, ppm, ppb), W/m², energy and power (J, W), and large masses (t, Mt, Gt).
- **Where conversion happens:**
  - The engine works in SI throughout, and readouts convert at display.
  - Charts receive SI, and the JS module applies each series' display transform (scale and offset).
  - When the setting changes, `setUnits` re-transforms the stored series in place, so no history is resent.
- **Readout format:** value, plus the change since the start, each auto-scaled (e.g. "O₂ 20.946% (−0.4 ppm)"). A tiny change on the Earth stays visible next to a large one in the house.

### 7.5 Layout and accessibility

- **Fullscreen:** the scenario panels in a grid, three across (two rows for six). **Tablet:** two across. **Mobile:** one panel at a time, chosen with Tabs, plus a compact table of every scenario's key readouts and flame state. The reader can hide scenarios, and the rest fill the space.
- **Controls** use the common components (`ClimateButton`, `DropdownButton`, `Collapsible`, `InfoPanel`, `DelayedTooltip`) and the Blazorise `Select`/`Tabs` styling.
  - The speed control is a slider with an accessible name and a text value ("1 year per second").
  - The volcano button's accessible name says what it does and where ("Erupt 10 volcanoes on Basic Earth").
- **Charts:** each has an accessible name and a text alternative (its latest values and the event log).
- **Announcements:** readouts are not live regions, since five updates a second would flood a screen reader. Events, including eruptions, are announced once, through a polite live region ("Sealed house: flame out at 1 min 9 s, O₂ below 15%").
- **Text size:** nothing below 0.75rem, including uPlot's canvas axis labels and legends.

### 7.6 Parameters and URL state

- Every scenario parameter, cut-off and rate constant is an Assumption with an id (e.g. `sim.house.volume`, `sim.cutoff.o2`, `sim.weathering.beta`, `sim.eruption.pinatubo.so2`) in the overview's parameter system.
- The URL holds the initial condition, which scenarios and quantities are shown, and edited parameters. A shared link reproduces the setup, not a run in progress. Speed and eruptions are not stored.

## 8. Tests

Engine tests live in the model's unit test project, named `MethodName_StateUnderTest_ExpectedBehavior`. Known answers are approximate and get tolerances.

**Clamp and stepping**
- No pool goes negative for any `dt`, including steps far larger than the burn (randomised configurations and steps).
- A limited reaction keeps its stoichiometry (O₂ and CH₄ used in a 2:1 ratio when O₂ runs out).
- `Advance` accepts a different `dt` on every call; two half-steps match one whole step to within the accuracy setting.
- Results converge as the accuracy fraction shrinks.
- The O₂ cut-off time is the same, within tolerance, at real-time speed and at 1,000 yr/s.

**Conservation**
- Each element's total in a sealed box is constant, apart from the counted vent outflow.
- Energy in = energy out + change in stored heat, within tolerance.
- The same configuration, `dt` sequence and eruptions give identical results.

**Boxes**
- Sealed 250 m³ house, methane at 100 L/s: O₂ cut-off at ~70 s; gas hob: ~28 h (§2.3).
- Coal fire: no change in moles. Methane fire with condensing water: the pressure falls below target and the vent stays closed.
- The N₂ cut-off occurs in a box with a small heat capacity and a large burner, and not in the default house.
- Opaque and glass walls: CO₂ changes the box temperature only through the heat the flame released.
- Ideal walls with `ε_gas` = 0: the floor radiates straight to the sky.
- `ε_gas` rises with CO₂ partial pressure, by less for each doubling.
- Warm floor, cooler gas: more CO₂ gives a warmer floor. Hot gas, cooler floor: more CO₂ cools the gas faster.
- Plants alone in sealed light draw CO₂ down to the compensation point and no further.
- The weathering rate rises with CO₂ and temperature, per its formula.

**Planet**
- Doubling CO₂ gives the configured equilibrium warming. With outgassing on, long runs return to where weathering equals outgassing.
- Earth without volcanoes: CO₂ declines below the start; with ice-albedo on, a cold enough state stays frozen.
- An eruption's SO₂ gives a cooling that peaks within months and decays at the sulfate lifetime. 100 eruptions inject exactly 100 times one. An eruption between ticks takes effect at the right time.

**Drivers**
- TSI: the mean over a long step equals the record mean over that span.
- The daily-mean insolation formula matches a published value at a test latitude and date.

Client pieces (runner rates, delta buffering, decimation) are tested where they are plain C#. The JS module and charts aren't covered by browser tests (AGENTS.md).

## 9. Risks

- **WebAssembly speed.** .NET on WebAssembly is interpreted unless compiled ahead of time. Six scenarios with ~15 processes each should fit the budget with adaptive substeps, but this needs measuring with the real engine. Fallbacks, in order: AOT for the client, coarser accuracy at high speed, then a Web Worker.
- **Stiffness at high speed.** A flame lasting a minute and weathering lasting a million years in one run make a stiff system. Adaptive substeps and event location handle the cut-offs. A scenario whose fast process never settles can still hit the substep cap: for example, day and night at a 1-hour step. The runner then slows the clock, as intended.
- **Simple formulas outside their range:**
  - The logarithmic CO₂ forcing is fitted for roughly 180–1,000+ ppm and fails as CO₂ nears zero (scenario 6). The engine needs a floor or a different form at very low CO₂.
  - A fixed Revelle factor is wrong for large perturbations.
  - The ice-albedo step is a tuning choice that decides when a snowball happens.
  - Sulfate forcing grows less than in proportion at very large loads (particles grow and fall faster), which matters for 100 eruptions at once. It needs a saturating form.

  Each of these gets a Level 2 note.
- **Grey-gas radiation in the ideal greenhouse.** Emissivity correlations are a simplification of band absorption. The result is sensitive to the chosen sky temperature. The page shows the sky choice next to the result.
- **The N₂-only vent** is unphysical (§4.4). The page says so, and the proportional-leak option is there to compare.
- **Readers mixing up box and planet physics.** Each panel names its temperature model, and the greenhouse teaching points (§3.2, §3.3) are Level 1 text.
- **No prerender** on the simulation route means a blank first paint on a cold visit, covered by the placeholder (§7.1).
- **Memory** for very long runs is bounded by the decimation cap (§7.3).

## 10. Open questions

Question numbers are shared across the Carbon Cycle plans.

- **Q16** The rate is the reader's to set, but any single rate shared by every scenario either puts the house's flame out at once or does nothing visible to the Earth (§2.3). Recommended: keep the shared input by default. Add a per-scenario "scale to this enclosure" toggle (rate per tonne of enclosed gas) and per-scenario presets.
- ~~Q17~~ Resolved: scenario 3 is the ideal greenhouse (walls hold the gas and let all radiation through, §3.3), scenario 4 adds plants and weathering to it, and Earth without volcanoes is scenario 6, with a volcano button (§3.6, §3.7).
- **Q18** Force WebAssembly for the simulation route only (recommended, §7.1), or for the whole Carbon Cycle page? The whole page would lose prerendering for its reading-heavy tabs.
- **Q19** TSI outside 1978–2025: loop the last whole solar cycles (recommended), or hold the mean? And should runs that start before 1978 (e.g. from 1750, as Tab 7 may) bring in the CDR's modelled TSI (NRLTSI2: daily from 1882, annual from 1610)? That would be a new dataset.
- **Q20** After an O₂ cut-off, plants in scenario 4 can bring the O₂ back. Keep the cut-off permanent (as specified, the default), or offer a Level 2 "relight when O₂ recovers" option?
- **Q21** Should Tabs 4–6 use this engine's Earth configuration (carbon boxes, forcing, energy balance) rather than a separate impulse-response model? Recommended: yes, so the page has one Earth model. Tab 4's numbers would then be what this tab shows with weathering off, and Tab 6's two-layer response would replace §4.3's single layer here too.

## 11. Work breakdown

Depends on the chemistry engine ([Tab 1 step 1](2026-10-23-02-carbon-cycle-tab1-chemistry-engine-plan.md)) for the burner, and on the units plan for `UnitPreference`. The parameter system (overview §5 step 1) can follow: like the chemistry engine, this engine takes its assumptions as plain inputs.

1. Engine core: state, process and reaction-rate model, limiting-reagent clamp, adaptive substeps, event location, injections, `Advance(dt)` (§5) + tests
2. Box processes: burner (through the Tab 1 engine), N₂ vent and proportional leak, water phase changes, two-node heat balance with convection, wall and ground conduction, solar gain (§4.2–§4.4) + tests
3. Gas radiation for ideal walls: CO₂ and water vapour emissivity correlations, sky options (§4.3) + tests
4. Plants and weathering; catalog additions (CH₂O, CaSiO₃, SiO₂, Mg₂SiO₄) (§4.2) + tests
5. Planet processes: ocean and land boxes, methane oxidation, outgassing, energy balance with ice-albedo (§4.2, §4.3) + tests
6. Eruptions: injection sizes, stratospheric sulfate and its forcing (§3.7) + tests
7. Scenario configurations 1–6 and presets; source and record every default (§3, §2.4)
8. TSI driver: fetch the existing dataset, step averaging, insolation by latitude, extension beyond the record (§6.1) + tests
9. Render mode routing: `App.razor.cs` per-route mode, placeholder, forced navigation and the page's renderer check (§7.1)
10. uPlot vendoring, the JS module and `[JSImport]` interop (§7.3)
11. `SimulationRunner`: loop, budget, deltas, readout ticks, events, eruptions (§7.2) + tests for its plain C# parts
12. UI: controls, volcano button, scenario panels, show/hide, event log, units, responsive layout, accessibility (§7.4, §7.5)
13. URL state and parameters (§7.6)
14. Content: Level 1 text for each scenario, the greenhouse, Foote and scale points, Level 2 notes on every simplification
