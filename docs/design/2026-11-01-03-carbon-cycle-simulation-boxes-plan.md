# Carbon Cycle simulation — House and greenhouses

- **Date:** 2026-11-01 (split from the [simulation overview](2026-10-24-01-carbon-cycle-simulation-plan.md), written 2026-10-24)
- **Status:** Proposed. Partly built: the sealed house (§1.1) in the first slice, with one lumped temperature, not the two-node balance (§3).
- **Author:** Patrick Lea (with Claude)
- **Scope:** The box settings (scenarios 1–4): sealed house, glass greenhouse, ideal greenhouse, ideal greenhouse with plants and weathering. Their processes (burner, N₂ vent, water phase changes, plants, weathering, heat through walls and ground), the two-node heat balance, gas radiation for ideal walls, and the N₂ vent.
- **Builds on:** [Carbon Cycle simulation — Overview](2026-10-24-01-carbon-cycle-simulation-plan.md); [engine](2026-11-01-01-carbon-cycle-simulation-engine-plan.md); [Tab 1 chemistry engine](2026-10-23-02-carbon-cycle-tab1-chemistry-engine-plan.md) (the burner uses its stoichiometry and heat)
- **Branch context:** `development`

Section references without a plan name are to this document. "Overview" is the [Carbon Cycle page overview](2026-10-21-01-carbon-cycle-plan.md); the other simulation plans are named by their topic, as listed in the [simulation overview §3](2026-10-24-01-carbon-cycle-simulation-plan.md).

## 1. Settings

**Every box is sealed for gas** apart from its N₂ vent (§4). Each box exchanges heat with its floor and the ground beneath. Opaque and glass walls also conduct heat to the outside air (§3). Without that, the house would heat without limit. Ideal walls conduct none by default, so heat leaves an ideal greenhouse only by radiation and through the ground.

- Scenarios 2 and 3 differ only in their walls. Putting them side by side isolates what the gas's own radiation does.
- Wall type is a parameter, so the reader can also make combinations not in the list, such as plants in a glass greenhouse.

### 1.1 Sealed house

No gas exchange, no sunlight. The burner uses up the house's O₂ until the flame goes out at the O₂ cut-off ([engine §5](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)). Teaching points:
- How fast it happens at each rate ([scenarios §3](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md)).
- Air stops being safe for people long before the flame goes out. Level 2 reference lines show the O₂ and CO₂ limits for breathing (to source, e.g. O₂ below 19.5%, CO₂ above 0.5% for a working day). In incomplete combustion, CO is the bigger danger.
- Water from the flame condenses on the walls, and the readout shows the house's pressure dropping below the outside's (§4).

### 1.2 Glass greenhouse

As the house, plus sunlight through the glass. The sun warms it by day and the walls cool it by night.

**Key teaching point:** the CO₂ the flame adds has no measurable warming effect inside a glass greenhouse.
- A greenhouse stays warm because the glass stops the warm air rising away. CO₂ absorbing infrared plays no real part in it.
- The glass is already opaque to infrared, so the gas's infrared only goes back and forth between the gas and the glass.
- R. W. Wood showed this in 1909 (to cite).

Scenario 3 takes the glass away to see what the gas does on its own. The planet's greenhouse effect needs a tall atmosphere that gets colder with height, and it is in the Earth settings ([planet plan](2026-11-01-04-carbon-cycle-simulation-planet-plan.md), [energy balance plan](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md)).

### 1.3 Ideal greenhouse

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

A later Level 2 preset, *Foote's cylinders*, would shrink scenario 2 to her cylinders and run one full of CO₂ beside one of air. It needs one addition to the model: gas absorbing sunlight (§3).

### 1.4 Ideal greenhouse with plants and weathering

Scenario 3, plus plants and a floor of crushed silicate rock.

- **Plants** draw CO₂ down into stored carbon and put O₂ back (`CO₂ + H₂O → CH₂O + O₂`). In a sealed greenhouse they stop at the *CO₂ compensation point*, where uptake equals their own respiration: ~40–50 ppm for most crops (C3 plants; to verify). Real growers add CO₂ to sealed greenhouses for this reason. In daylight, a 100 m² crop can roughly keep up with a gas hob's CO₂ (both a few mmol/s; to verify), but not with 100 L/s.
- **Silicate weathering** turns CO₂ into carbonate rock (net `CaSiO₃ + CO₂ → CaCO₃ + SiO₂`). Its rate depends on temperature and CO₂ (§2). On a greenhouse floor this is *enhanced weathering*, a real carbon-removal method that spreads crushed basalt on fields. It acts over years, not minutes.

**What the reader will see:**
- **CO₂** rises while the burner runs, then falls: fast through the plants and slowly through weathering. It ends below the starting level, at the plants' compensation point.
- **Temperature** mostly follows the flame, the sun and the sky. On top of that, the gas's own radiation (§1.3) adds a small warming as CO₂ rises and a small cooling as it is drawn down. Compared with scenario 3, which has the same walls and no plants, the difference is what the plants and the rock do through CO₂.
- **Frozen water** needs the floor to go below 0 °C. That depends on the sky, the ground and the season far more than on CO₂.

The planet-scale version of the same story (rise, draw-down, freeze) is Earth without volcanoes ([planet §1.2](2026-11-01-04-carbon-cycle-simulation-planet-plan.md)).

## 2. Processes

Each process is a reaction (or a transfer) between reservoirs, with a rate law. Reactions come from the chemistry catalog, so they are balanced and carry their heat. All rates and constants are editable assumptions with sources ([overview §3](2026-10-21-01-carbon-cycle-plan.md)); the values here are provisional, to verify. The planet's processes are in the [planet plan §2](2026-11-01-04-carbon-cycle-simulation-planet-plan.md).

| Process | Reaction or transfer | Rate | Scenarios |
|---|---|---|---|
| Burner | fuel + O₂ → products, by the Tab 1 engine at the emission's carbon split | the emission event's rate, while burning ([engine §5](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)) | all |
| Direct emission | species from a Tab 2 or Tab 3 activity → air | the emission event's rate | all |
| N₂ vent | N₂ (enclosure) → outside | whatever holds the pressure at its target; removes only | 1–4 |
| Condensation / evaporation | H₂O(g) ⇌ H₂O(l) | relaxes vapour towards saturation (Magnus formula) with a short time constant | 1–4 |
| Freezing / melting | H₂O(l) ⇌ H₂O(s) | at 0 °C, limited by the heat available | 1–4 |
| Photosynthesis | CO₂ + H₂O → CH₂O + O₂ | light × efficiency × CO₂ response (zero at the compensation point) × temperature response | 4; land in 5–6 |
| Plant respiration and decay | CH₂O + O₂ → CO₂ + H₂O | stored carbon × base rate × Q₁₀ temperature factor (Q₁₀ ~2); needs O₂ | 4; land in 5–6 |
| Silicate weathering | CaSiO₃ + CO₂ → CaCO₃ + SiO₂ (net) | W₀ × (pCO₂ / pCO₂,ref)^β × exp((T − T_ref) / T_e) (Walker, Hays and Kasting 1981; β ~0.3–0.5, T_e ~10–13 K); × rock area in a box; reduced by ice cover on a planet | 4–6 |
| Floor–gas convection | floor ⇌ gas | h × floor area × (T_floor − T_gas) | 1–4 |
| Heat through walls | gas → outside air | U × wall area × (T_gas − T_outside); U = 0 by default for ideal walls | 1–4 |
| Heat into the ground | floor ⇌ ground | G × floor area × (T_floor − T_ground) | 1–4 |
| Gas radiation | floor ⇌ gas ⇌ sky, infrared (§3) | from the gas emissivity | 3–4 |
| Solar gain | sun → floor | insolation × transmittances × area (§3; insolation from the [energy balance plan §2](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md)) | 2–4 |

Photosynthesis in scenario 4 adds the chemistry catalog's first biology species, CH₂O, as the stand-in for stored plant carbon. Weathering adds silicate and carbonate solids: CaSiO₃ (wollastonite, standing for silicate rock) and SiO₂. Forsterite (Mg₂SiO₄, olivine) is a Level 2 alternative for enhanced weathering.

## 3. Heat balance

Two temperatures: the gas, and the floor with the box's contents. The floor holds most of the heat capacity and absorbs the sunlight.

```
gas:   C_gas   dT_gas/dt   = flame heat + h·A_floor·(T_floor − T_gas) − U·A_walls·(T_gas − T_outside) + latent heat + gas radiation
floor: C_floor dT_floor/dt = solar gain − h·A_floor·(T_floor − T_gas) − G·A_floor·(T_floor − T_ground) + floor radiation
```

- **Defaults** are sized for a house and for a greenhouse (to source). The gas's own heat capacity is small: at 100 L/s, heating only the air would take it up by hundreds of kelvins in a minute.
- **Flame heat** is the Tab 1 engine's heat for the fuel burned at its carbon split. It goes into the gas.
- **Latent heat:** condensing and freezing release heat (+); evaporating and melting take it in (−).
- **Solar gain** onto the floor = insolation at the greenhouse's latitude ([energy balance §2](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md)) × atmospheric transmittance (default an all-sky average, to source) × wall transmittance (glass ~0.8–0.9; ideal 1) × floor area.
- **Radiation depends on the walls.**
  - **Opaque and glass walls** are opaque to infrared and sit at about the gas temperature. The gas's infrared only goes back and forth inside, so there is no net radiation term. This is the §1.2 point.
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
  - **Absorption of sunlight by the gas** is left out of v1. The Foote's cylinders preset would need it (§1.3).

**Built (first slice):** one lumped temperature for the air and the house's contents, losing heat through the walls to an outside at the start temperature (20 °C).

## 4. Pressure and the N₂ vent

Each enclosure holds its pressure at the target (default 101.325 kPa) by venting N₂ as gas is added or the gas heats up.
- **It only removes.** If the pressure falls below the target, the enclosure stays below it, and the readout shows the deficit. This happens with methane at room temperature: each mole of methane burned adds one mole of gas net (the fuel itself), but its two moles of water condense. A coal fire changes no moles (`C + O₂ → CO₂`), so only heating moves its pressure.
- **It removes only N₂.** That's a thought-experiment device that keeps the chemistry easy to read. The page says so, because of one side effect: heating pushes N₂ out and *raises* the O₂ fraction. A real leak would remove all gases in proportion. A Level 2 "proportional leak" option shows the difference.
- **When the vent runs out of N₂**, the pressure can no longer be held, and the burner stops: the N₂ cut-off ([engine §5](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)).

## 5. Tests

- Sealed 250 m³ house, methane at 100 L/s: O₂ cut-off at ~70 s; gas hob: ~28 h ([scenarios §3](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md)).
- Coal fire: no change in moles. Methane fire with condensing water: the pressure falls below target and the vent stays closed.
- The N₂ cut-off occurs in a box with a small heat capacity and a large burner, and not in the default house.
- Opaque and glass walls: CO₂ changes the box temperature only through the heat the flame released.
- Ideal walls with `ε_gas` = 0: the floor radiates straight to the sky.
- `ε_gas` rises with CO₂ partial pressure, by less for each doubling.
- Warm floor, cooler gas: more CO₂ gives a warmer floor. Hot gas, cooler floor: more CO₂ cools the gas faster.
- Plants alone in sealed light draw CO₂ down to the compensation point and no further.
- The weathering rate rises with CO₂ and temperature, per its formula.

## 6. Risks

- **Grey-gas radiation in the ideal greenhouse.** Emissivity correlations are a simplification of band absorption. The result is sensitive to the chosen sky temperature. The page shows the sky choice next to the result.
- **The N₂-only vent** is unphysical (§4). The page says so, and the proportional-leak option is there to compare.
- **Readers mixing up box and planet physics.** Each panel names its temperature model, and the greenhouse teaching points (§1.2, §1.3) are Level 1 text.

## 7. Open questions

- **Q20** After an O₂ cut-off, plants in scenario 4 can bring the O₂ back. Keep the cut-off permanent (as specified, the default), or offer a Level 2 "relight when O₂ recovers" option?

## Summary

The four box settings, from a sealed house to an ideal greenhouse with plants and crushed rock. A two-node heat balance (gas and floor) with walls, ground and, for ideal walls, the gas's own infrared exchange with the floor and sky. The N₂ vent holds the pressure. The teaching points are how fast a flame uses up a room's O₂, why glass (not CO₂) keeps a greenhouse warm, and what the gas does on its own once the glass is gone.

### Change size

Rough, from the split; refine when this plan is next revised.

- **Projects:** `ClimateExplorer.CarbonCycle`, `ClimateExplorer.Web.Client`, the unit test projects

| Area | Added | Changed | Removed |
|---|---|---|---|
| Engine: two-node heat balance, walls, ground, vent options | 2 files, +250 | 2 files, ~100 | — |
| Engine: gas emissivity, sky | 2 files, +200 | — | — |
| Engine: plants and weathering; catalog species (CH₂O, CaSiO₃, SiO₂, Mg₂SiO₄) | 2 files, +250 | 2 files, ~40 | — |
| Web: three greenhouse scenarios | 6 files, +300 | 1 file, ~10 | — |
| Tests | 3 files, +450 | 1 file, ~30 | — |
| **Total** | **15 files, +1,450** | **6 files, ~180** | — |
