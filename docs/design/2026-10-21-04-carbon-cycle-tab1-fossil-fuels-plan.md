# Carbon Cycle page — Tab 1: Fossil fuels and limestone

- **Date:** 2026-10-21
- **Status:** Proposed (draft v1.0; split out of the overview plan; combustors and upstream moved to a sub-plan 2026-10-23; ideal and real fuels added 2026-10-23)
- **Author:** Patrick Lea (with Claude)
- **Scope:** Tab 1 of the Carbon Cycle page, stage 1: combustion and calcination as chemistry, covering reactants, products and energy, including incomplete combustion and aerosols
- **Builds on:** [Carbon Cycle page — Overview](2026-10-21-01-carbon-cycle-plan.md) (purpose, tab layout, design principle, output contract, parameter system, data)
- **Branch context:** `issues/carbon-cycle`

Tab 1 covers carbon released from geological stores: fossil fuels, and carbonate rock heated to make cement and lime. It is one of three optional input tabs ([overview §2.1](2026-10-21-01-carbon-cycle-plan.md)). Land clearing is in [Tab 2](2026-10-21-05-carbon-cycle-tab2-land-agriculture-plan.md). The CO₂ from biofuels and other fast-cycle carbon is out, but their non-CO₂ emissions count ([overview §1.2](2026-10-21-01-carbon-cycle-plan.md)). Section references without "overview" are to this document.

**Two stages.** Tab 1 is built in two stages:

1. **Stage 1 (this plan): combustion in the abstract.** Fuel and oxygen go in; products and energy come out. No engine, power station or kiln. The reader sees combustion and calcination as chemistry: the balanced equations, the mass of each product, the heat released or absorbed, and what changes when combustion is incomplete or the fuel contains sulfur.
2. **Stage 2: [combustors and upstream](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md).** Where the fuel is burned (transport, power, industry, buildings), the per-passenger and per-tonne transport comparison, per-combustor emission factors, and the upstream overheads of extraction, refining and transport.

Stage 1 stands on its own. Its inputs are quantities of fuel, energy or rock, and its result goes to the simulation like any other input tab's. Stage 2 adds activities on top of the same chemistry engine and replaces some stage 1 defaults with per-combustor values (§3, §4).

---

## 1. Sub-sections

1. **Combustion (main focus)**, alongside **limestone calcination** (§2.1), with the **energy** each reaction releases or absorbs (§2.2)
2. **Incomplete combustion and tropospheric ozone** (sub-section: CO, unburnt hydrocarbons, soot, and NOx as ozone precursors)
3. **Sulfur and sulfate aerosols** (sub-section: the cooling by-product)
4. **Fuel table**
5. **Calculator** (fuel, energy or rock quantity → products and energy → an emission event for the simulation)

Every sub-section follows [overview §1.3](2026-10-21-01-carbon-cycle-plan.md). Each one opens with its Level 1 overview, a sentence or two and at most one picture or table, and the rest sits behind an expand.

## 2. Combustion chemistry content

Worked examples burn ideal fuels, one pure molecule each (§2.3), in this order (easiest first, [overview §1.3](2026-10-21-01-carbon-cycle-plan.md)):

1. **Level 1: coal** (simplified as carbon): `C + O₂ → CO₂ + heat`. One carbon atom plus oxygen from the air gives one CO₂ and releases heat.
   **Level 1, alongside coal: limestone** `CaCO₃ + heat → CaO + CO₂`. Heating rock releases CO₂ with no burning at all, and it takes in heat rather than giving it out (§2.1).
2. **Level 2: methane**: `CH₄ + 2 O₂ → CO₂ + 2 H₂O + heat`. The hydrogen burns to water too.
3. **Level 2: octane** (petrol): `2 C₈H₁₈ + 25 O₂ → 16 CO₂ + 18 H₂O + heat`.
4. **Level 2: the general rule** for any hydrocarbon:

```
CxHy + (x + y/4) O₂ → x CO₂ + (y/2) H₂O + heat
```

Every equation is shown with its products *and* its energy: the reaction display lists the mass of each product (CO₂, H₂O, and later CO, soot, SO₂) and the heat released, for the amount of fuel entered (§6).

Key teaching points. Mass gain is Level 1; the other two start with a one-line plain version at Level 1 and give the explanation at Level 2:

- **Mass gain**: each carbon atom (12 g/mol) leaves as CO₂ (44 g/mol), so 1 kg of carbon → 3.664 kg CO₂. This is why 1 L of petrol (~0.74 kg) produces ~2.3 kg CO₂ — the oxygen comes from the air.
- **Hydrogen fraction matters**: more H per C means more energy per unit of CO₂ (methane best, coal worst). Show CO₂ per MJ alongside CO₂ per kg (§2.2).
- **Carbon vs CO₂ units**: the carbon-cycle literature uses GtC; emissions reporting uses Gt CO₂. Show both everywhere, and convert explicitly (×3.664).

### 2.1 Limestone: calcination (cement and lime)

**Main point (Level 1):** making cement means heating limestone to ~900 °C, which drives CO₂ out of the rock:

```
CaCO₃ + heat → CaO + CO₂
```

The rock gives off CO₂ without burning. The heat has to come from somewhere, usually burning coal or petroleum coke, and that adds a second CO₂ stream from the fuel. Even with no heat lost, the rock gives off more CO₂ than the fuel needed to heat it (§2.2). Globally, the rock alone gives ~1.6 Gt CO₂/yr, about 4% of the CO₂ from fossil fuels and limestone together. How the two streams split in a real kiln, with its heat losses, is in the [combustor sub-plan](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md).

**Level 2 content:**
- **Mass:** CaCO₃ (100.09 g/mol) → CO₂ (44.01), so 1 kg of pure limestone releases 0.44 kg CO₂. That's a fixed constant. The editable assumption is how much CaO ends up in the product.
- **Clinker:** ~65% CaO, so ~0.51–0.52 t CO₂ per t clinker from the rock. That's IPCC's Tier 1 default; to verify. Cement is clinker ground with gypsum and fillers, at a clinker ratio of ~0.65–0.75. Lowering that ratio is a real-world lever, so make it editable.
- **Lime** (CaO for steelmaking, construction, water treatment): the same reaction. ~0.75 t CO₂ per t high-calcium lime (IPCC default; to verify). Dolomitic lime also releases CO₂ from MgCO₃ → MgO + CO₂.
- **Recarbonation:** over decades, concrete slowly reabsorbs some CO₂ from the air (CaO + CO₂ → CaCO₃ at the surface). The Global Carbon Budget now counts this as a sink. v1 mentions it in text only, with no model.
- **Related non-combustion releases of fossil carbon** (same scope rule, [overview §1](2026-10-21-01-carbon-cycle-plan.md); Level 2 text, rows later):
  - steam methane reforming for hydrogen and ammonia, `CH₄ + 2 H₂O → CO₂ + 4 H₂`
  - iron-ore reduction, `Fe₂O₃ + 3 CO → 2 Fe + 3 CO₂`, where the CO comes from coke; the blast furnace itself is in the [combustor sub-plan](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md)

**Tie to later tabs:** this is carbon from the crust reservoir. The simulation's carbon tab (Tab 5) should compare the rates. People move carbon from geological stores to the air at ~38 Gt CO₂/yr (fossil fuels plus limestone). Volcanoes move ~0.3–0.4 Gt CO₂/yr. That's about 100 times faster (to verify).

**Model inputs (editable):** CaO content of clinker, clinker ratio of cement, lime type, and the fuel that supplies the heat (any fuel from §5). Stage 1 supplies only the reaction's own heat (§2.2), with no losses. Defaults per [overview §3.5](2026-10-21-01-carbon-cycle-plan.md).

### 2.2 Energy: heat released and heat absorbed

**Main point (Level 1):** burning gives out heat, which is why we burn fuel. Calcination takes heat in, so it has to be paired with burning. Level 1 shows one number per worked example: the heat from 1 kg of fuel, next to the CO₂ from that kilogram.

**Level 2 content.** Standard enthalpies at 25 °C, water as vapour (net / lower heating value, the basis IPCC uses). Values from standard tables, to verify:

| Reaction | Heat per mol | Heat per kg of fuel or rock | ~g CO₂ per MJ |
|---|---|---|---|
| `C + O₂ → CO₂` | −393.5 kJ | 32.8 MJ/kg C | ~112 |
| `CH₄ + 2 O₂ → CO₂ + 2 H₂O` | −802 kJ | 50.0 MJ/kg | ~55 |
| `C₈H₁₈ + 12.5 O₂ → 8 CO₂ + 9 H₂O` | −5,074 kJ | 44.4 MJ/kg | ~69 |
| `CaCO₃ → CaO + CO₂` | **+178 kJ** (absorbed) | 1.78 MJ/kg CaCO₃ taken in | — |

- **Where the energy comes from:** the bonds formed (C=O in CO₂, O–H in water) are stronger than the bonds broken (C–C, C–H, O=O). The difference comes out as heat.
- **Why hydrogen helps:** burning hydrogen to water releases energy without making CO₂. Methane gets about half its heat from its hydrogen, so it makes about half the CO₂ per MJ of pure carbon. The CO₂-per-MJ column of the fuel table (§5) is this point applied to every fuel.
- **Checks against IPCC:** the methane and octane rows land within a few per cent of the IPCC emission factors (56.1 and 69.3 g CO₂/MJ, §5). Pure carbon (~112) sits above real coals (~95–101), because coal also holds some hydrogen. The page shows this comparison, so its chemistry has a published number to check against.
- **Gross vs net heating value:** if the water vapour condenses, it gives back its latent heat (methane: −890 kJ/mol instead of −802). Fuel data uses the net value; condensing boilers recover some of the difference. One line of text.
- **Heat for calcination:** 1 kg of rock CO₂ needs ~4.0 MJ (178 kJ per 44 g). Supplying that from carbon at ~112 g CO₂/MJ adds at least ~0.45 kg of fuel CO₂. This is the floor with no heat lost. A real kiln also heats the material to ~1,450 °C to form clinker, and loses heat. Its real fuel use is in the [combustor sub-plan](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md).
- **Incomplete combustion releases less heat** (§3): carbon burned only to CO gives out about 28% of the heat of burning it to CO₂.
- **Water vapour product:** combustion also makes water (methane: 2.25 kg H₂O per kg). Show it as a product. Emitted at ground level it has no lasting climate effect, because the water cycle removes it within days. The page says so in one line. (Water vapour at cruise altitude, as contrails, is in the combustor sub-plan and Tab 6.)

### 2.3 Ideal and real fuels

**Main point (Level 1):** the equations above burn *ideal* fuels, one pure molecule each: coal as pure carbon, natural gas as methane, petrol as octane. Fuels in the real world are mixtures, and the page uses the real fuel for every number it sends to the simulation.

- **Ideal fuel:** one molecule. Its equation is clean, and its products and heat follow from the chemistry with nothing to look up. Fixed, not editable. The worked examples (§2) and the incomplete-combustion series (§3) use ideal fuels.
- **Real fuel:** a mixture of parts, each with a mass fraction. Petrol is alkanes centred on octane, plus aromatics (such as toluene), sometimes ethanol, and a trace of sulfur. Diesel and jet fuel are similar, centred on cetane and dodecane. Natural gas is mostly methane, with some ethane, CO₂ and N₂. Coal, coke and heavy fuel oil have no molecules to list, so they are written as their elements (carbon, hydrogen, oxygen, nitrogen, sulfur) plus moisture and ash, as received. Real fuels fill the fuel table (§5) and the calculator (§6). The composition and heating value are editable assumptions.
- **The switch belongs to the fuel, not the combustor.** A fuel's composition is the same whatever burns it. The [combustor sub-plan](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md) takes a real fuel and decides how completely it burns, how much NOx it makes and whether its sulfur is scrubbed.
- **Level 2 shows the two side by side.** For example, 1 kg of coal as pure carbon gives 3.66 kg CO₂ and 32.8 MJ. Real bituminous coal gives ~2.4–2.7 kg CO₂ and ~26–28 MJ, because water, ash, oxygen and hydrogen make up part of its mass. For petrol, the real blend has a lower H:C than octane because of its aromatics.
- **Heat:** an ideal fuel's heat comes from the chemistry (§2.2). A real fuel's comes from its net calorific value (IPCC first, §5). Where a real fuel's parts are all molecules, Level 2 also shows the heat added up from its parts, as a check on the composition.
- **Sulfur is only in real fuels** (§4). No ideal fuel contains any.
- **Biofuel blends:** a part can be fast-cycle carbon, such as the ethanol in E10 petrol or the biodiesel in B7 diesel. Its CO₂ is not counted ([overview §1.2](2026-10-21-01-carbon-cycle-plan.md)), but its CO, soot and other by-products are. Rows come later; the fuel model supports them from the start.

The engine design for both kinds of fuel is in the [chemistry engine plan §5](2026-10-23-02-carbon-cycle-tab1-chemistry-engine-plan.md).

## 3. Incomplete combustion and tropospheric ozone (sub-section)

**Main point:** combustion by-products are ozone precursors, and tropospheric ozone (O₃) is a greenhouse gas. Incomplete combustion barely changes the carbon budget, because CO becomes CO₂ within months. What it adds is ozone formation, soot, and less heat from the same fuel.

**Level 1** is the main point in plain words: "When fuel burns without enough oxygen, some carbon leaves as carbon monoxide or soot instead of CO₂, and less heat comes out. Burning also gives off gases that, in sunlight, make ozone near the ground. Ozone is a short-lived greenhouse gas and a pollutant." Everything below is Level 2.

**The chemistry of incomplete combustion.** Shown as a series that runs from plenty of oxygen to too little, using methane, with the heat released per mol of CH₄:

```
CH₄ + 2   O₂ → CO₂ + 2 H₂O        −802 kJ   (complete)
CH₄ + 1.5 O₂ → CO  + 2 H₂O        −519 kJ   (carbon monoxide)
CH₄ +     O₂ → C   + 2 H₂O        −408 kJ   (soot)
CH₄                                  0 kJ   (unburnt slip)
```

and for carbon itself:

```
C  + ½ O₂ → CO       −110.5 kJ
CO + ½ O₂ → CO₂      −283.0 kJ
```

- The carbon has to end up somewhere: as CO₂, CO, soot (black carbon), or unburnt fuel. The page balances each equation and shows the split.
- **Black carbon (soot)** is an aerosol. It is the carbon that never reacted, formed in fuel-rich parts of a flame. It absorbs sunlight (warming) and darkens snow and ice where it lands. Its chemistry is shown here; its forcing is in Tab 6. It falls out of the air within days to weeks rather than oxidising to CO₂, so its carbon is not added to the carbon total sent to the simulation (to confirm against [overview §2.3](2026-10-21-01-carbon-cycle-plan.md)).
- Example equation `2 CH₄ + 3 O₂ → 2 CO + 4 H₂O`.

**Precursors and where they come from:**

| Precursor | Source | Role in ozone formation | ~Atmospheric lifetime |
|---|---|---|---|
| CO | Incomplete combustion | Fuel: oxidised by OH, driving the cycle | ~1–2 months |
| Unburnt hydrocarbons (NMVOCs) | Incomplete combustion, evaporation | Fuel: oxidised by OH, driving the cycle | hours–weeks |
| CH₄ | Unburnt gas slip; fugitive leaks ([combustor sub-plan](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md)) | Fuel: slow, global background ozone source | ~10 years |
| NOx (NO + NO₂) | High-temperature combustion | Catalyst: needed to turn the oxidation into O₃ | ~days |

**NOx is not an incomplete-combustion product.** It comes from hot, oxygen-rich combustion, where N₂ from the air reacts with O₂ (plus nitrogen in the fuel itself):

```
N₂ + O₂ + heat → 2 NO          +180.6 kJ   (absorbs heat; needs a hot flame)
O  + N₂        → NO + N                    (Zeldovich mechanism)
N  + O₂        → NO + O
2 NO + O₂      → 2 NO₂                     (in the air afterwards)
```

The reaction takes in heat, so it runs only at flame temperatures, and faster the hotter the flame. Cleaner, hotter combustion can therefore mean *less* CO but *more* NOx. The page frames this sub-section as "combustion by-products", with incomplete combustion as one source among them.

**Chemistry of ozone formation (CO as the worked example):**

```
CO  + OH        → CO₂ + H
H   + O₂ (+M)   → HO₂
HO₂ + NO        → OH  + NO₂
NO₂ + sunlight  → NO  + O
O   + O₂ (+M)   → O₃
-----------------------------------
net:  CO + 2 O₂ → CO₂ + O₃      (NOx and OH regenerated)
```

The same pattern with CH₄ and the NMVOCs produces more O₃ per molecule. Teaching points: sunlight drives it, NOx acts as a catalyst, ozone is short-lived (weeks), and its effect is regional rather than well mixed.

**Simplifications to state openly on the page:**
- Ozone production depends nonlinearly on the NOx and VOC levels already present. v1 uses an editable "ozone yield per unit precursor" factor rather than chemistry.
- NOx also generates OH, which shortens methane's lifetime, an indirect *cooling* effect. Mention it in the text; don't model it in v1.
- CO competes for OH, which lengthens methane's lifetime, an indirect warming effect. Also text only in v1.

**Model inputs (editable):**
- **Carbon split:** the fraction of the fuel's carbon leaving as CO₂, CO, soot and unburnt fuel. Default 100% CO₂, which matches IPCC's oxidation factor of 1 (§5). The reader moves it to see the products change, the equation rebalance and the heat fall. Presets illustrate it: a clean gas flame, a smouldering coal fire, a smoky diesel flame.
- The masses of CO, soot and unburnt hydrocarbon follow from the split by stoichiometry, with no emission factors: CO is 28/12 of its carbon, soot is its carbon, and unburnt hydrocarbon has the fuel's own composition.
- **NOx formed**, g per kg of fuel, with one generic default per fuel, sourced per [overview §3.5](2026-10-21-01-carbon-cycle-plan.md) (IPCC first; the EMEP/EEA Guidebook where IPCC has no Tier 1 factor). NOx depends mostly on flame temperature, which belongs to the combustor, so the page labels this default as a rough average. The combustor sub-plan replaces the split and the NOx default with per-combustor values.
- Precursor masses are carried to the simulation for ozone forcing (Tab 6). CO's carbon is still counted in the carbon total sent to the simulation.

## 4. Sulfur and sulfate aerosols (sub-section)

**Main point:** sulfur in fuel is the main by-product that *cools* the climate. It works in the opposite direction to CO₂ and ozone and, globally, masks a sizeable part of the warming. Unlike CO₂, its effect stops within weeks of the emissions stopping. That makes it the counterweight in the story: cleaning up sulfur improves air quality and acid rain, and it also removes some of that cooling.

**Level 1** is that main point plus one picture: a satellite image of ship tracks. The chemistry, inputs and sulfur table are Level 2.

**Chemistry to show:**

```
S (in fuel) + O₂        → SO₂          −296.8 kJ  (in the flame)
SO₂ + OH (+M)           → HOSO₂                   (gas phase)
HOSO₂ + O₂              → HO₂ + SO₃
SO₃ + H₂O               → H₂SO₄                   → sulfate aerosol
SO₂ + H₂O₂ (in cloud)   → H₂SO₄                   (aqueous phase, usually the larger route)
```

- Mass gain again: 1 kg S → 2 kg SO₂ (32 → 64 g/mol). This is a fixed constant. Burning sulfur also releases heat (~9.3 MJ/kg S), a small share of the fuel's total.
- The aerosol droplets scatter sunlight (direct effect) and seed more numerous, smaller cloud droplets, giving brighter clouds (indirect effect).
- They last days to a couple of weeks, and the effect is regional, strongest downwind of sources.
- Side effects to mention: acid rain, and fine-particle health impacts, which are why sulfur has been regulated.

**Teaching examples:**
- **Ship tracks:** lines of bright cloud along shipping lanes, visible in satellite images. They are a direct view of the indirect effect.
- **The 2020 shipping fuel change:** the IMO global cap cut bunker fuel sulfur from ~3.5% to 0.5%. That reduced the cooling over busy shipping lanes. This is useful as a preset ("1 t of bunker fuel before / after 2020"), since it is purely a change in the fuel.

**Model inputs (editable):**
- Sulfur content per real fuel (mass fraction; one of its parts, §2.3). Ideal fuels contain none. This is a fuel-table column (§5).
- All of the fuel's sulfur leaves as SO₂ in stage 1. Removing SO₂ before it leaves the stack (scrubbers) is equipment, so it is in the [combustor sub-plan](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md).
- SO₂ mass is carried to the simulation as a negative forcing (Tab 6) that tracks the emission rate, the same short-lived treatment as ozone.

Provisional sulfur contents (to verify):

| Fuel | ~Sulfur content |
|---|---|
| Methane / natural gas | ~0 (H₂S removed at processing; trace odorant) |
| LPG | very low, <0.02% |
| Petrol | ~10 ppm in ultra-low-sulfur markets; higher elsewhere |
| Diesel | ~10 ppm (ULSD); much higher in some markets |
| Jet fuel | spec max 0.3%; typically ~0.03–0.08% |
| Bunker fuel (HFO) | ≤0.5% global cap since 2020 (≤0.1% in emission control areas); ~2.5–3.5% before |
| Lignite | ~0.3–2.5%, highly variable |
| Sub-bituminous coal | ~0.2–1% (Indonesian and US Powder River Basin coal are low-sulfur) |
| Bituminous coal | ~0.5–3% |
| Anthracite | ~0.5–1% |

## 5. Fuel table

**Level 1 short list (6 rows):**

| Short-list row | Stands for (Level 2 rows) | Default when used as one row |
|---|---|---|
| Coal | Brown coal (lignite), sub-bituminous coal, black coal (bituminous), anthracite; plus coke and petroleum coke | Bituminous, as received (to revisit: a burn-weighted world-average coal may be better) |
| Natural gas | Methane / natural gas, CNG, LPG | Natural gas |
| Petrol | Petrol | Petrol |
| Diesel | Diesel | Diesel |
| Jet fuel | Jet fuel, kerosene | Jet fuel |
| Ship fuel | Heavy fuel oil (bunker fuel) | HFO |

Level 1 columns: Name, Image, CO₂ per kg, CO₂ per litre (per m³ for gas), CO₂ per unit of energy. All other columns are Level 2 ("Show all columns").

**Sorting:** the table is sortable on any column. Default sort: CO₂ per unit of energy, highest first. That puts coal at the top, matching the coal-first chemistry, and natural gas at the bottom, which is the main teaching point of this table.

Every row is a real fuel (§2.3). Its ideal fuel appears beside it at Level 2. The full (Level 2) table follows. Values are **provisional starting points to verify** using the source order in [overview §3.5](2026-10-21-01-carbon-cycle-plan.md). IPCC comes first; the IPCC defaults are listed after this table.

| Fuel | Ideal fuel (representative molecule) | ~H:C (atomic) | ~C mass fraction | ~kg CO₂ / kg | ~Density (kg/L) | ~kg CO₂ / L (or m³) |
|---|---|---|---|---|---|---|
| Lignite (brown coal) | variable, high moisture | ~0.8–1.0 | 25–35% | ~1.0–1.3 | solid | — |
| Sub-bituminous coal | variable, moderate moisture | ~0.8–0.9 | ~45–55% | ~1.7–2.0 | solid | — |
| Bituminous (black) coal | variable | ~0.7–0.9 | 60–80% | ~2.2–2.9 | solid | — |
| Methane / natural gas | CH₄ | 4.0 | 75% | 2.75 | 0.72 kg/m³ (0 °C) | ~1.9–2.0 per m³ |
| LPG | C₃H₈ / C₄H₁₀ mix | 2.5–2.67 | ~82% | ~3.0 | ~0.51–0.58 | ~1.6 |
| Petrol (gasoline) | ~C₈H₁₈ (octane) | 2.25 (octane); ~1.8–2.0 real blend | ~85% | ~3.1 | ~0.74 | ~2.3 |
| Jet fuel (Jet A-1 kerosene) | ~C₁₂H₂₆ | 2.17 (dodecane); ~1.9–2.0 real | ~86% | ~3.16 | ~0.80 | ~2.5 |
| Diesel | ~C₁₂–C₁₆ (cetane C₁₆H₃₄) | 2.125 (cetane); ~1.8–1.9 real | ~86% | ~3.2 | ~0.835 | ~2.7 |
| Bunker fuel (HFO) | heavy residual, ~C₂₀+ | ~1.4–1.6 | ~85–87% | ~3.11 (IMO) | ~0.98 | ~3.1 |
| Anthracite | mostly C | ~0.3–0.5 | 85–92% | ~3.0–3.3 | solid | — |

Notes:
- Four coal ranks at Level 2, in order of rank: lignite, sub-bituminous, bituminous, anthracite. Sub-bituminous is included because Indonesia, the largest thermal-coal exporter, ships mostly sub-bituminous coal, and much of the US and Indian coal burn is sub-bituminous or high-ash.
- Level 2 names lead with the plain name, e.g. "Anthracite (hard coal, almost pure carbon)" and "Lignite (brown coal)".
- Coal values are *as received*; moisture and ash dominate the spread. Show a range plus an editable default.
- Natural gas is mostly methane; real pipeline gas has ethane/CO₂/N₂. Row is "Methane (natural gas)" with an editable purity.
- H:C for liquid fuels: the ideal fuel's molecule gives one value, the real blend is lower because of aromatics. The real-blend value is derived from the real fuel's parts (§2.3), and the molecule's value is the fixed reference.
- H:C is the column that explains *why* CO₂ per MJ differs (§2.2). The sort order runs almost exactly opposite to CO₂/MJ.

**IPCC defaults (primary source for this table).** IPCC 2006 Guidelines Vol 2 Ch 1 (the 2019 Refinement leaves these in place) gives, per fuel, a net calorific value, a carbon content (kg C/GJ) and a CO₂ emission factor (kg CO₂/TJ), each with a 95% range. Each real fuel's default parts are chosen so that its derived carbon mass fraction equals **carbon content × NCV**. For diesel that is 20.2 kg C/GJ × 43.0 MJ/kg ≈ 0.87. The CO₂-per-MJ column is the IPCC emission factor itself, so the page's chemistry (§2.2) has a published number to check against. Values from memory, to verify against the tables:

| IPCC fuel | NCV (MJ/kg) | kg CO₂/TJ (= mg/kJ) | → kg CO₂/kg |
|---|---|---|---|
| Natural gas | 48.0 | 56,100 | 2.69 |
| LPG | 47.3 | 63,100 | 2.98 |
| Motor gasoline | 44.3 | 69,300 | 3.07 |
| Jet kerosene | 44.1 | 71,500 | 3.15 |
| Other kerosene | 43.8 | 71,900 | 3.15 |
| Gas/diesel oil | 43.0 | 74,100 | 3.19 |
| Residual fuel oil | 40.4 | 77,400 | 3.13 |
| Petroleum coke | 32.5 | 97,500 | 3.17 |
| Anthracite | 26.7 | 98,300 | 2.62 |
| Coking / other bituminous coal | 28.2 / 25.8 | 94,600 | 2.67 / 2.44 |
| Sub-bituminous coal | 18.9 | 96,100 | 1.82 |
| Lignite | 11.9 | 101,000 | 1.20 |
| Coke oven coke | 28.2 | 107,000 | 3.02 |

- **Discrepancy to resolve:** the fuel table's anthracite range (~3.0–3.3 kg CO₂/kg) is on a near dry, ash-free basis. IPCC's as-received default comes to ~2.6. Use the as-received basis throughout, to match the other coals, and say so.
- IPCC assumes an oxidation factor of 1 (all carbon ends up as CO₂). That matches this page's accounting rule ([overview §2.3](2026-10-21-01-carbon-cycle-plan.md)) and the default carbon split (§3).
- **Rows to add for global coverage:** *kerosene* (cooking and lighting in South Asia and Africa; nearly the same as jet fuel), *petroleum coke* (a major cement-kiln fuel, especially in India), *coke* (blast furnaces), and *CNG*. CNG is the methane row with a "compressed, in kg" unit, used by auto-rickshaws, buses and cars in India, Pakistan and Iran.

**Columns** (Level 1 shows Name, Image and the three CO₂ yield columns; "Show all columns" reveals the rest; per-column visibility toggles come later):

1. **Name**
2. **Image** — friendly illustration (petrol can, gas flame/cylinder, cartoon coal lumps, jet, ship tank…)
3. **Composition** — the ideal fuel's molecule (petrol centred on octane) and the real fuel's parts (petrol: alkanes, aromatics, sulfur), plus a one-line note
4. **Structure** — short text description of the molecular structure (see below)
5. **Chemical diagram** — structural/skeletal formula of the representative molecule (coal shown as a simplified aromatic-ring fragment)
6. **Approx. H:C ratio** — atomic ratio
7. **Carbon mass fraction** — kg C per kg fuel
8. **Density** — kg/L (kg/m³ for gas)
9. **Net calorific value** — MJ/kg
10. **Sulfur content** — mass fraction (§4)
11. **CO₂ yield per kg**
12. **CO₂ yield per L** (per m³ for gas)
13. **CO₂ yield per MJ**

Starting text for the Structure column:

| Fuel | Structure |
|---|---|
| Methane | Single carbon bonded to four hydrogens (tetrahedral); the smallest hydrocarbon |
| LPG | Short straight chains of 3–4 carbons (propane, butane) |
| Petrol | Mix of C5–C10 straight and branched chains plus some aromatic rings; octane (C8) is the reference |
| Jet fuel | C8–C16 chains, with some ring (cycloalkane) and aromatic molecules |
| Diesel | Longer C10–C22 chains plus aromatics; cetane (C16) is the reference |
| Bunker fuel | Heavy refinery residue: large, multi-ring aromatic molecules and asphaltenes, C20+ |
| Lignite | Loose network of small aromatic clusters with a lot of oxygen, bound water and mineral matter |
| Sub-bituminous coal | Between lignite and bituminous: somewhat larger aromatic clusters, still fairly moist (~15–30% water) |
| Bituminous coal | Larger, more tightly linked aromatic ring clusters; less oxygen and water |
| Anthracite | Highly condensed, nearly graphite-like stacked aromatic sheets; almost pure carbon |

Fixed vs editable per fuel:
- Fixed: the ideal fuel (its molecule, molar mass, equation and heat), molar masses, stoichiometry, standard enthalpies of the worked-example reactions.
- Editable: the real fuel's parts (e.g. aromatics share, sulfur content, moisture and ash for coal), density, net calorific value, NOx default (§3). Carbon mass fraction and H:C are derived from the parts, so they can't disagree. The combustor sub-plan adds the upstream overhead.

**Image style:** one consistent set — flat SVG illustrations, same palette, stroke weight, perspective and canvas size; light/dark-theme friendly. Chemical diagrams likewise one consistent SVG style (consider rendering from SMILES at build time so they're uniform, e.g. via RDKit, then hand-check).

## 6. Calculator: input modes and scale

The same engine must cover 10⁰–10¹³ kg.

Level 1 ([overview §1.3](2026-10-21-01-carbon-cycle-plan.md)): presets plus the fuel-quantity mode with short-list fuels, and a result in auto-scaled units with one relatability anchor. Other input modes, the emission profile, the carbon split, the breakdown and scientific notation are Level 2.

Input modes:

1. **Fuel quantity**: amount of a fuel directly (L, kL, ML, GL, kg, t, Mt, m³, bcm)
2. **Energy**: the heat wanted (MJ, GJ, PJ, EJ) and the fuel that supplies it. This puts the fuels side by side on the same footing: "1 GJ of heat from coal, from gas, from diesel".
3. **Rock or product quantity**: t limestone, t clinker, t cement (via the clinker ratio) or t lime, with the fuel that supplies the heat (§2.1, §2.2)
4. **Presets**: "1 L of petrol", "1 t of coal", "1 GJ of heat from coal vs gas", "1 t of cement", "1 t of bunker fuel before / after 2020", "a clean flame vs a smouldering fire" (the carbon split, §3), "a year of China's cement production", "China's annual coal combustion", "global annual fossil emissions" (presets are just parameter sets, i.e. ready-made thought experiments, [overview §1](2026-10-21-01-carbon-cycle-plan.md))
   - **Population-scaled presets** (from OWID data, [overview §4](2026-10-21-01-carbon-cycle-plan.md)): any quantity × a country's population for a chosen year, e.g. "everyone in India burns 1 L of petrol a week for a year". Shown as per-person quantity × population, so the scale-up is visible.

The [combustor sub-plan](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md) adds the activity and transport-task modes and activity presets ("a heavy truck driving 1,000 km").

Emission profile:
- **Single pulse (default)**: the whole amount is released at t = 0. Tab 1 deals only in quantity; time first appears in the simulation, where the pulse is an emission event at time 0.
- **Constant rate (option)**: the same amount every period for a duration, e.g. "this nation's coal burn, every year for 30 years" or "a gas hob, per second for 2 hours". This is a small Tab 1 control (on/off, period and duration). The simulation runs it as one emission event at that rate, so it costs little to support.

Output display:
- kg → t → kt → Mt → Gt with SI prefixes; scientific notation toggle
- Always both **CO₂** and **C**
- **Products and energy**: the balanced equation for the chosen fuel, with the mass of each product (CO₂, H₂O, CO, soot, unburnt fuel, SO₂, NOx) and the heat released or absorbed (MJ → GJ → PJ → EJ)
- Relatability anchors at scale: equivalent ppm of atmospheric CO₂ if all stayed airborne (1 ppm ≈ 2.12 GtC ≈ 7.8 Gt CO₂); fraction of current global annual emissions
- Breakdown: combustion vs calcination (rock)

Numeric type: `double` is sufficient (range and precision are far beyond need); keep calculation in SI base units (kg, J, m) internally, convert only at the UI edge.

## 7. Output to the simulation

Tab 1 fills the shared `EmissionResult` ([overview §2.2](2026-10-21-01-carbon-cycle-plan.md)):

- `CarbonMassKg`: carbon from combustion and calcination, including the carbon in CO, NMVOC and unburnt CH₄ ([overview §2.3](2026-10-21-01-carbon-cycle-plan.md)).
- `Breakdown`: combustion, calcination (rock), incomplete-combustion products.
- `NonCo2`: unburnt CH₄, CO, NMVOC, NOx, SO₂ (§4), black carbon (§3).
- `Source`: Tab 1 plus the fuel, rock or preset.

The combustor sub-plan adds upstream, vehicle fixed cost, fugitive CH₄ and aircraft-km.

---

## 8. Work breakdown

The parameter system and page shell are cross-cutting ([overview §5](2026-10-21-01-carbon-cycle-plan.md)). Combustor and upstream work is in the [combustor sub-plan](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md).

1. Calculation engine + unit tests: stoichiometry, product masses, heat released or absorbed, carbon split, calcination ([technical plan](2026-10-23-02-carbon-cycle-tab1-chemistry-engine-plan.md))
2. Source and verify all fuel defaults and reaction enthalpies; record citations
3. Fuel table UI (sortable, default sort CO₂ per unit energy; units toggle; Level 1 short list and columns, "show all" for Level 2; per-column visibility controls later)
4. Reaction display: balanced equation, products and energy for the chosen fuel and carbon split
5. Calculator UI: input modes (fuel, energy, rock or product), unit scaling, presets, results breakdown
6. Chemistry content: equations (coal → methane → octane), mass-gain and energy explainers, calcination, incomplete combustion and ozone formation, sulfur and sulfate aerosols, plus Level 1 plain-language text and term definitions for every section
7. Fuel illustration set and chemical diagrams — single style
8. Hand-off to the simulation (a placeholder output tab that shows the received result as an emission event)
