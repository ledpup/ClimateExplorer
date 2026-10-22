# Carbon Cycle page — Tab 1: Fossil fuels and limestone

- **Date:** 2026-10-21
- **Status:** Proposed (draft v0.9; split out of the overview plan)
- **Author:** Patrick Lea (with Claude)
- **Scope:** Tab 1 of the Carbon Cycle page: extraction, refinement, transport, combustion and calcination
- **Builds on:** [Carbon Cycle page — Overview](2026-10-21-01-carbon-cycle-plan.md) (purpose, tab layout, design principle, output contract, parameter system, data)
- **Branch context:** `issues/carbon-cycle`

Tab 1 covers carbon released from geological stores: fossil fuels, and carbonate rock heated to make cement and lime. It is one of three optional input tabs ([overview §2.1](2026-10-21-01-carbon-cycle-plan.md)). Section references without "overview" are to this document.

---

## 1. Sub-sections

1. **Combustion (main focus)**, alongside **limestone calcination** (§2.1)
2. **Incomplete combustion and tropospheric ozone** (sub-section: CO, NMVOCs, CH₄ and NOx as ozone precursors)
3. **Sulfur and sulfate aerosols** (sub-section: the cooling by-product)
4. **Upstream: extraction, refinement, transport** (overhead factors, kept light)
5. **Where it's burned** (combustors: transport, power, industry, buildings, and a per-passenger / per-tonne transport comparison)
6. **Calculator** (inputs → result → "send to Tab 4")

Every sub-section follows [overview §1.3](2026-10-21-01-carbon-cycle-plan.md). Each one opens with its Level 1 overview, a sentence or two and at most one picture or table, and the rest sits behind an expand.

## 2. Combustion chemistry content

Worked examples, in this order (easiest first, [overview §1.3](2026-10-21-01-carbon-cycle-plan.md)):

1. **Level 1: coal** (simplified as carbon): `C + O₂ → CO₂`. One carbon atom plus oxygen from the air gives one CO₂.
   **Level 1, alongside coal: limestone** `CaCO₃ → CaO + CO₂`. Heating rock releases CO₂ with no burning at all (§2.1).
2. **Level 2: methane**: `CH₄ + 2 O₂ → CO₂ + 2 H₂O`. The hydrogen burns to water too.
3. **Level 2: octane** (petrol): `2 C₈H₁₈ + 25 O₂ → 16 CO₂ + 18 H₂O`.
4. **Level 2: the general rule** for any hydrocarbon:

```
CxHy + (x + y/4) O₂ → x CO₂ + (y/2) H₂O + heat
```

Key teaching points. Mass gain is Level 1; the other two start with a one-line plain version at Level 1 and give the explanation at Level 2:

- **Mass gain**: each carbon atom (12 g/mol) leaves as CO₂ (44 g/mol), so 1 kg of carbon → 3.664 kg CO₂. This is why 1 L of petrol (~0.74 kg) produces ~2.3 kg CO₂ — the oxygen comes from the air.
- **Hydrogen fraction matters**: more H per C means more energy per unit of CO₂ (methane best, coal worst). Show CO₂ per MJ alongside CO₂ per kg.
- **Carbon vs CO₂ units**: the carbon-cycle literature uses GtC; emissions reporting uses Gt CO₂. Show both everywhere, and convert explicitly (×3.664).

### 2.1 Limestone: calcination (cement and lime)

**Main point (Level 1):** making cement means heating limestone to ~900 °C, which drives CO₂ out of the rock:

```
CaCO₃ + heat → CaO + CO₂
```

Most of a cement kiln's CO₂ comes from the rock, not the fuel. The heat itself comes from burning coal or petroleum coke, so a cement kiln has two CO₂ streams: roughly 60% from the limestone and 40% from the fuel (to verify). Globally, the rock alone gives ~1.6 Gt CO₂/yr, about 4% of the CO₂ from fossil fuels and limestone together.

**Level 2 content:**
- **Mass:** CaCO₃ (100.09 g/mol) → CO₂ (44.01), so 1 kg of pure limestone releases 0.44 kg CO₂. That's a fixed constant. The editable assumption is how much CaO ends up in the product.
- **Clinker:** ~65% CaO, so ~0.51–0.52 t CO₂ per t clinker from the rock. That's IPCC's Tier 1 default; to verify. Cement is clinker ground with gypsum and fillers, at a clinker ratio of ~0.65–0.75. Lowering that ratio is a real-world lever, so make it editable.
- **Lime** (CaO for steelmaking, construction, water treatment): the same reaction. ~0.75 t CO₂ per t high-calcium lime (IPCC default; to verify). Dolomitic lime also releases CO₂ from MgCO₃ → MgO + CO₂.
- **Recarbonation:** over decades, concrete slowly reabsorbs some CO₂ from the air (CaO + CO₂ → CaCO₃ at the surface). The Global Carbon Budget now counts this as a sink. v1 mentions it in text only, with no model.
- **Related non-combustion releases of fossil carbon** (same scope rule, [overview §1](2026-10-21-01-carbon-cycle-plan.md); Level 2 text, rows later):
  - steam methane reforming for hydrogen and ammonia, `CH₄ + 2 H₂O → CO₂ + 4 H₂`
  - iron-ore reduction in blast furnaces, `Fe₂O₃ + 3 CO → 2 Fe + 3 CO₂`, which is part of the blast-furnace row's coke use

**Tie to later tabs:** this is carbon from the crust reservoir. Tab 4 should compare the rates. People move carbon from geological stores to the air at ~38 Gt CO₂/yr (fossil fuels plus limestone). Volcanoes move ~0.3–0.4 Gt CO₂/yr. That's about 100 times faster (to verify).

**Model inputs (editable):** CaO content of clinker, clinker ratio of cement, lime type, and the kiln fuel and fuel energy per t clinker (§6.3). Defaults per [overview §3.5](2026-10-21-01-carbon-cycle-plan.md).

## 3. Incomplete combustion and tropospheric ozone (sub-section)

**Main point:** combustion by-products are ozone precursors, and tropospheric ozone (O₃) is a greenhouse gas. Incomplete combustion barely changes the carbon budget, because CO becomes CO₂ within months. What it adds is ozone formation.

**Level 1** is the main point in plain words: "Engines and fires also give off gases that, in sunlight, make ozone near the ground. Ozone is a short-lived greenhouse gas and a pollutant." Everything below is Level 2.

**Precursors and where they come from:**

| Precursor | Source | Role in ozone formation | ~Atmospheric lifetime |
|---|---|---|---|
| CO | Incomplete combustion | Fuel: oxidised by OH, driving the cycle | ~1–2 months |
| Unburnt hydrocarbons (NMVOCs) | Incomplete combustion, evaporation | Fuel: oxidised by OH, driving the cycle | hours–weeks |
| CH₄ | Fugitive emissions (§8), unburnt gas slip | Fuel: slow, global background ozone source | ~10 years |
| NOx (NO + NO₂) | High-temperature combustion | Catalyst: needed to turn the oxidation into O₃ | ~days |

- **Correctness note:** NOx is *not* an incomplete-combustion product. It comes from hot, oxygen-rich combustion, where N₂ from the air reacts with O₂ (plus nitrogen in the fuel itself). Cleaner, hotter combustion can mean *less* CO but *more* NOx. The page should frame this subsection as "combustion by-products", with incomplete combustion as one source among them.
- Keep the existing pieces: soot/black carbon is noted and deferred to Tab 5; example equation `2 CH₄ + 3 O₂ → 2 CO + 4 H₂O`.

**Chemistry to show (CO as the worked example):**

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

**Model inputs (editable, per combustor):**
- Combustion completeness: fraction of carbon leaving as CO₂ versus CO and unburnt HC. Default ~99%+ for modern engines, lower for e.g. open burning.
- Emission factors for CO, NOx and NMVOC, in g per kg of fuel. Defaults to be sourced per [overview §3.5](2026-10-21-01-carbon-cycle-plan.md) (IPCC first; the EMEP/EEA Guidebook where IPCC has no Tier 1 factor).
- Precursor masses are carried to Tab 5 for ozone forcing. CO's carbon is still counted in the carbon total sent to Tab 4.

## 4. Sulfur and sulfate aerosols (sub-section)

**Main point:** sulfur in fuel is the main by-product that *cools* the climate. It works in the opposite direction to CO₂ and ozone and, globally, masks a sizeable part of the warming. Unlike CO₂, its effect stops within weeks of the emissions stopping. That makes it the counterweight in the story: cleaning up sulfur improves air quality and acid rain, and it also removes some of that cooling.

**Level 1** is that main point plus one picture: a satellite image of ship tracks. The chemistry, inputs and sulfur table are Level 2.

**Chemistry to show:**

```
S (in fuel) + O₂        → SO₂                    (in the flame)
SO₂ + OH (+M)           → HOSO₂                  (gas phase)
HOSO₂ + O₂              → HO₂ + SO₃
SO₃ + H₂O               → H₂SO₄                  → sulfate aerosol
SO₂ + H₂O₂ (in cloud)   → H₂SO₄                  (aqueous phase, usually the larger route)
```

- Mass gain again: 1 kg S → 2 kg SO₂ (32 → 64 g/mol). This is a fixed constant.
- The aerosol droplets scatter sunlight (direct effect) and seed more numerous, smaller cloud droplets, giving brighter clouds (indirect effect).
- They last days to a couple of weeks, and the effect is regional, strongest downwind of sources.
- Side effects to mention: acid rain, and fine-particle health impacts, which are why sulfur has been regulated.

**Teaching examples:**
- **Ship tracks:** lines of bright cloud along shipping lanes, visible in satellite images. They are a direct view of the indirect effect.
- **The 2020 shipping fuel change:** the IMO global cap cut bunker fuel sulfur from ~3.5% to 0.5%. That reduced the cooling over busy shipping lanes. This is useful as a preset ("bunker fuel before / after 2020").
- **Coal power scrubbers:** flue-gas desulfurisation removes most of the SO₂ before it leaves the stack.

**Model inputs (editable):**
- Sulfur content per fuel (mass fraction). This becomes a new fuel-table column (§5).
- SO₂ removal efficiency per combustor (scrubbers on power stations, exhaust scrubbers on ships). Default 0% for vehicles.
- SO₂ mass is carried to Tab 5 as a negative forcing that tracks the emission rate, the same short-lived treatment as ozone.

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

**Sorting (Q1, resolved):** the table is sortable on any column. Default sort: CO₂ per unit of energy, highest first. That puts coal at the top, matching the coal-first chemistry, and natural gas at the bottom, which is the main teaching point of this table.

The full (Level 2) table follows. Values are **provisional starting points to verify** using the source order in [overview §3.5](2026-10-21-01-carbon-cycle-plan.md). IPCC comes first; the IPCC defaults are listed after this table.

| Fuel | Representative composition | ~H:C (atomic) | ~C mass fraction | ~kg CO₂ / kg | ~Density (kg/L) | ~kg CO₂ / L (or m³) |
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
- Four coal ranks at Level 2, in order of rank: lignite, sub-bituminous, bituminous, anthracite. Sub-bituminous is included (Q2) because Indonesia, the largest thermal-coal exporter, ships mostly sub-bituminous coal, and much of the US and Indian coal burn is sub-bituminous or high-ash.
- Level 2 names lead with the plain name, e.g. "Anthracite (hard coal, almost pure carbon)" and "Lignite (brown coal)".
- Coal values are *as received*; moisture and ash dominate the spread. Show a range plus an editable default.
- Natural gas is mostly methane; real pipeline gas has ethane/CO₂/N₂. Row is "Methane (natural gas)" with an editable purity.
- H:C for liquid fuels: the representative molecule gives one value, the real blend is lower because of aromatics. Show the real-blend value as the editable default and the molecule's value as the fixed reference.
- H:C is the column that explains *why* CO₂ per MJ differs. The sort order runs almost exactly opposite to CO₂/MJ.

**IPCC defaults (primary source for this table).** IPCC 2006 Guidelines Vol 2 Ch 1 (the 2019 Refinement leaves these in place) gives, per fuel, a net calorific value, a carbon content (kg C/GJ) and a CO₂ emission factor (kg CO₂/TJ), each with a 95% range. The page's editable carbon mass fraction default is **carbon content × NCV**. For diesel that is 20.2 kg C/GJ × 43.0 MJ/kg ≈ 0.87. The CO₂-per-MJ column is the IPCC emission factor itself, so the page's chemistry has a published number to check against. Values from memory, to verify against the tables:

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
- IPCC assumes an oxidation factor of 1 (all carbon ends up as CO₂). That matches this page's accounting rule ([overview §2.3](2026-10-21-01-carbon-cycle-plan.md)).
- **Rows to add for global coverage:** *kerosene* (cooking and lighting in South Asia and Africa; nearly the same as jet fuel), *petroleum coke* (a major cement-kiln fuel, especially in India), *coke* (blast furnaces), and *CNG*. CNG is the methane row with a "compressed, in kg" unit, used by auto-rickshaws, buses and cars in India, Pakistan and Iran.

**Columns** (Level 1 shows Name, Image and the three CO₂ yield columns; "Show all columns" reveals the rest; per-column visibility toggles come later):

1. **Name**
2. **Image** — friendly illustration (petrol can, gas flame/cylinder, cartoon coal lumps, jet, ship tank…)
3. **Composition** — formula / representative molecule (petrol centred on octane) plus a one-line note
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
- Fixed: molecular formula of the representative molecule, molar masses, stoichiometry.
- Editable: carbon mass fraction (for mixtures/coal), density, net calorific value, sulfur content, upstream overhead.

**Image style:** one consistent set — flat SVG illustrations, same palette, stroke weight, perspective and canvas size; light/dark-theme friendly. Chemical diagrams likewise one consistent SVG style (consider rendering from SMILES at build time so they're uniform, e.g. via RDKit, then hand-check).

## 6. Combustor table ("where the carbon is burned")

Scope (Q5, resolved): transport, power stations, and the other large fossil-fuel combustors: industry, buildings, and oil and gas operations. Rows are grouped by sector. The approximate global split shows where the carbon actually goes, and the page should open on it so the table isn't read as "mostly cars".

**Global energy-related CO₂ by sector (approx., ~37 Gt CO₂/yr; IEA; to verify):**

| Sector | ~Share | Main combustors |
|---|---|---|
| Electricity and heat | ~40% | Coal and gas power stations, district-heating boilers |
| Industry | ~25% | Blast furnaces, cement kilns, industrial boilers, brick kilns, chemicals |
| Transport | ~22% | Road ~75% of this; aviation and shipping ~10% each; rail ~1% |
| Buildings (direct) | ~8% | Cooking and space heating |
| Other energy industry | ~5% | Refinery own use, gas flaring, oil and gas field equipment |

**Level 1 short list ([overview §1.3](2026-10-21-01-carbon-cycle-plan.md)):** the sector split above, plus about ten plain-named combustors: bicycle (the zero-fuel baseline), motorbike, car, bus, truck, train, plane, ship, coal power station, gas power station, steelworks, cement kiln, and home cooking and heating. Each one stands for its group in the Level 2 tables below. For example, "Truck" defaults to the heavy truck and expands to the light, medium and heavy rows. Level 1 columns: name, image, fuel, and CO₂ per km (or per MWh, or per tonne of product). Efficiency, capacity, emission factors and SO₂ removal are Level 2.

Defaults in the tables below are **provisional placeholders to be sourced** ([overview §3.5](2026-10-21-01-carbon-cycle-plan.md)). Every row has: default fuel (changeable to any compatible fuel), fuel-use or efficiency input, combustion completeness, CO / NOx / NMVOC emission factors (§3; these vary far more by combustor than by fuel), and SO₂ removal efficiency (§4).

### 6.1 Transport: passenger

Representative vehicles are chosen by global count, not Western fleets. Transport rows also carry capacity and load factor (§6.4).

| Combustor | Representative example | Default fuel | ~Fuel use per vehicle-km | ~Capacity | ~Typical load |
|---|---|---|---|---|---|
| Bicycle | Steel utility bicycle (the world's most numerous vehicle; China, India, the Netherlands) | None | 0 (carbon is all fixed cost, §6.4) | 1 (2 with a carrier) | ~1 |
| Electric two-wheeler | E-bike / e-scooter (several hundred million in China; growing in India and Vietnam; count to verify) | Grid electricity | ~1.5–3 kWh/100 km | 1–2 | ~1.2 |
| Motorcycle / scooter | 100–125 cc commuter bike (the most numerous motor vehicle in India and South-East Asia) | Petrol | ~2 L/100 km | 2 | ~1.3–2 |
| Three-wheeler (auto-rickshaw) | CNG auto-rickshaw (India, Pakistan, Bangladesh) | CNG (methane), also LPG / petrol / diesel | ~3 kg CNG/100 km | 3–4 (often more) | ~2 |
| Small car | 1.0–1.5 L hatchback / compact (largest segments in India and China) | Petrol | ~5–6 L/100 km | 4–5 | ~1.5 |
| Large car / SUV | Mid-size SUV (fastest-growing segment worldwide, incl. China) | Petrol | ~8–10 L/100 km | 5 | ~1.5 |
| Car (diesel) | Compact diesel | Diesel | ~5–6 L/100 km | 5 | ~1.5 |
| City bus | 12 m diesel or CNG bus | Diesel / CNG | ~30–45 L/100 km | ~60–90 incl. standing | ~20–60, very place-dependent |
| Intercity coach | 45–55 seat coach | Diesel | ~25–30 L/100 km | ~50 | ~30–45 |
| Passenger train (electric) | Metro, suburban, intercity and high-speed (most passenger rail in China, India, Europe and Japan) | Grid electricity | kWh per train-km, to source | ~1,000–2,000+ | high in India and China |
| Passenger train (diesel) | Diesel-hauled long-distance train (Africa, parts of the Americas, regional lines elsewhere; India's broad-gauge network is now almost all electric, to verify) | Diesel | per train-km, to source | ~500–1,500 | to source |
| Airliner (narrow-body) | A320 / 737 class | Jet fuel | ~3–4 L/km | ~180 | ~80–85% |
| Airliner (wide-body) | 787 / A350 / 777 class | Jet fuel | ~8–10 L/km | ~300 | ~80% |
| Private jet *(outlier)* | Large-cabin business jet (G650 / Global 7500 class) | Jet fuel | ~1.5–2.5 L/km | ~13–19 | 1 (see below) |
| Superyacht *(outlier)* | 70–100 m motor yacht | Marine diesel | ~10–40 L/km at cruise, plus generators at anchor; to source | 12 guests (the usual regulatory cap) | 1 (see below) |

**Outliers.** The private jet and the superyacht are not common vehicles. They are in as extremes, labelled as such, and are Level 2 only: on the Level 1 linear chart they would flatten every other bar. Their default load is 1, the owner. Crew are not passengers, as with the airliner's pilots and cabin crew. The load is editable. For the jet, occupied flights often carry 2–4 passengers, but a large share of flights (~40% reported, to source) are empty positioning legs, so ~1–2 averaged over all km. Most of a superyacht's fuel is burned by generators while it sits at anchor, not per km, so it also gets a per-year preset (§7).

**Excluded: electric cars and electric buses (Q11).** Electric modes are in only where they give a step change per passenger-km. On today's world-average grid (~470 g CO₂/kWh), a mid-size electric SUV at ~0.2 kWh/km emits ~95 g CO₂/km at the power station. A small petrol car emits ~130 g/km at the tailpipe. A small EV emits about half the SUV's figure, and an e-bus about a third less than a diesel bus. These are real gains, but they are not the order-of-magnitude gap between a car and a bus, train, bicycle or e-bike. An electric car also still needs a city built around cars: roads and parking take a large share of urban land (~25–35% in many cities, over half in some US downtowns; to source). That is an environmental and social cost outside this page's scope. The comparison view carries a footnote saying all this.

### 6.2 Transport: freight

| Combustor | Representative example | Default fuel | ~Fuel use per vehicle-km | ~Payload capacity | Load factor notes |
|---|---|---|---|---|---|
| Light commercial vehicle | ~1 t mini-truck (e.g. the small pickups and mini-trucks common in India and China) | Diesel | ~7–9 L/100 km | ~1 t | Includes empty running |
| Medium rigid truck | ~10 t payload | Diesel | ~20–25 L/100 km | ~10 t | Empty running ~20–30% of km |
| Heavy truck (tractor-trailer) | ~25 t payload | Diesel | ~30–35 L/100 km | ~25 t | As above |
| Freight train (electric) | Electric bulk / container train (most freight rail in China and India) | Grid electricity | kWh per train-km, to source | ~2,000–5,000 t | Often empty one way (coal, ore) |
| Freight train (diesel) | Diesel-hauled bulk / container train (North America, Russia's unelectrified lines, Africa, Australia) | Diesel | per train-km, to source | ~2,000–10,000+ t | Often empty one way (coal, ore) |
| Container ship | ~8,000–14,000 TEU | Bunker fuel | per day at speed, to source (IMO) | ~100,000+ t | Slow steaming matters a lot |
| Bulk carrier | Panamax / Capesize | Bunker fuel | to source (IMO) | ~70,000–180,000 t | Returns in ballast |
| Oil tanker | VLCC | Bunker fuel | to source (IMO) | ~300,000 t | Returns in ballast |
| Air freighter | 777F / 747-8F class | Jet fuel | ~9–11 L/km | ~100 t | ~60–70% |

Aviation shows CO₂ only here. Contrails and NOx at altitude add a substantial non-CO₂ effect. That is a Tab 5 topic; the comparison view should carry a footnote saying so.

### 6.3 Stationary combustors

| Combustor | Default fuel | Efficiency input (editable) | ~Default | Activity unit |
|---|---|---|---|---|
| Coal power station (subcritical) | Bituminous / sub-bituminous coal | thermal efficiency | ~33–38% (much of the Indian and older Chinese fleets) | MWh |
| Coal power station (supercritical / ultra-supercritical) | Bituminous coal | thermal efficiency | ~40–47% (most new Chinese capacity) | MWh |
| Lignite power station | Lignite | thermal efficiency | ~30–40% | MWh |
| Gas power station (open cycle) | Natural gas | thermal efficiency | ~35–40% | MWh |
| Gas power station (combined cycle) | Natural gas | thermal efficiency | ~55–60% | MWh |
| Oil / diesel generator sets | Diesel / residual fuel oil | thermal efficiency | ~30–40% (backup power in India, Nigeria, the Middle East, islands) | MWh |
| District-heating / CHP boiler | Coal / gas | heat efficiency | ~70–90% | MWh heat |
| Blast furnace (steel) | Coke (from coking coal) | GJ or t coke per t hot metal | to source | t steel |
| Cement kiln | Coal / petroleum coke, **plus limestone** | GJ per t clinker; CaO fraction; clinker ratio | ~3–3.5 GJ/t; ~65%; ~0.7 | t clinker or t cement |
| Lime kiln | Coal / gas, **plus limestone** | GJ per t lime; lime type | to source | t lime |
| Brick kiln | Coal | GJ per 1,000 bricks | to source (South Asia; also large black-carbon source) | 1,000 bricks |
| Industrial boiler | Coal / gas / oil | boiler efficiency | ~75–90% | MWh steam |
| Residential cooking stove | LPG / kerosene / coal | fuel per household-year | to source (LPG is now dominant in urban India) | household-year |
| Residential / commercial space heating | Gas / coal / heating oil | fuel per household-year | to source (coal stoves in northern China; gas in Europe, Russia, North America) | household-year |
| Gas flare | Natural gas | flaring combustion efficiency | ~95–98% (unburnt slip feeds §3) | bcm flared |
| Off-road diesel | Diesel | L per operating hour | to source (tractors, irrigation pumpsets in India, mining haul trucks, construction plant) | operating hour |

Other combustors, noted but not given rows in v1:
- **Waste incineration:** plastics are fossil carbon, so burning them is fossil CO₂. Paper and food waste are biogenic.
- **Fishing vessels and inland waterway barges:** cover with the ship rows and an edited size.
- **Traditional biomass and crop-residue burning:** large in South Asia and Africa, but not fossil carbon. Mention it as out of scope, because it is a big source of the same CO and black-carbon by-products.

Cement and lime kilns report two lines, **fuel CO₂** and **rock (calcination) CO₂**, so the reader sees that most of the kiln's CO₂ comes from the limestone (§2.1).

### 6.4 Moving people and goods: per passenger-km and per tonne-km

Resolved (Q7): included. The point is to compare **which modes move the most for the least carbon**. It is not a personal footprint.

Transport rows carry:
- **Capacity:** seats (or standing capacity), or payload tonnes.
- **Load factor:** occupancy or payload utilisation, including empty return trips. Editable, with a "typical" default (§6.1–6.2) and a one-click "full" option. This shows how much load factor matters: a crowded bus or train against a half-empty one, or a laden ship against one returning in ballast.
- **Fuel use per vehicle-km.** v1 treats this as independent of load, and the page says so. A full truck burns more than an empty one; a later version could make fuel use rise linearly from empty to full.
- **Fixed cost (manufacture and maintenance):** kg CO₂ per vehicle over its life, and lifetime vehicle-km. Dividing one by the other gives g CO₂ per vehicle-km. This is still fossil carbon from this tab: the steelworks, aluminium smelters (via their power stations) and factories that built the vehicle. Infrastructure (roads, track, airports, ports) is excluded.

Fixed-cost defaults (provisional, to source):

| Vehicle | ~Manufacture + maintenance | ~Lifetime | ~g CO₂ per vehicle-km |
|---|---|---|---|
| Steel bicycle | ~100–150 kg | ~20,000 km | ~5–8 |
| Aluminium bicycle | higher: aluminium is ~8–16 kg CO₂/kg against steel's ~2 | ~20,000 km | to source |
| Small car | ~5–8 t | ~200,000 km | ~25–40 |
| Airliner, ship, train | small relative to fuel (~1–3%) | to source | to source |

LCA sources report these figures as CO₂e. Most of it is energy-related CO₂, so v1 treats it as CO₂ and says so.

Derived outputs: g CO₂ per vehicle-km, per **passenger-km**, and per **tonne-km**.

**Comparison view:**
- Level 1: the short-list modes at typical load, on two simple charts, with the plain labels "CO₂ per passenger per km" and "CO₂ per tonne of goods per km". Full load, the full vehicle list, log scale and the well-to-wheel toggle are Level 2.
- Two ranked bar charts, one for passenger modes (g CO₂/pkm) and one for freight modes (g CO₂/tkm). Never put both on one axis.
- Both charts offer a log scale. Freight spans ~2 orders of magnitude (bulk ship ~3–8, rail ~15–30, heavy truck ~60–120, light commercial ~300+, air freight ~500–1,000 g CO₂/tkm). Passenger spans ~4 (bicycle ~5–8, airliner ~80–100, private jet ~5,000, superyacht tens of thousands of g CO₂/pkm). All order of magnitude, to verify.
- Each bar shows "typical" and "full" load, so the reader sees the load-factor range, not one number.
- Basis: combustion only (tank-to-wheel) by default. A toggle adds upstream (§8) to give well-to-wheel, which is the basis used by the GLEC Framework / ISO 14083 for freight. For electric rows, "combustion" is at the power station.
- Fixed cost is shown by default as a lighter stacked segment on each bar, with a toggle to hide it. Without it the bicycle would be a zero bar, which a log axis can't draw.

**Allocation rule:** mixed passenger and freight vehicles (belly cargo on airliners, ro-pax ferries) allocate everything to the primary use in v1. The ICAO calculator and GLEC have allocation methods; note them as a refinement.

**Electric modes (Q11, resolved):** in where they are a step change per passenger-km or are the common form of the mode: e-bikes and e-scooters, and electric passenger and freight trains (metro, suburban, intercity and high-speed share one row; their energy per passenger-km is similar). Electric cars and buses are out (§6.1). An electric row has energy use (kWh per vehicle-km) and a grid intensity (g CO₂/kWh). The grid intensity comes from a power-station row or a chosen mix, so the carbon comes from the power station and stays inside this tab's combustion story.

Grid-intensity choices (to source): world average (~470 g/kWh; ~60% fossil, ~32% renewable, ~9% nuclear, Ember 2024), China (~550), India (~700), coal-only (~1,000), gas combined-cycle (~350–400), low-carbon (~20–50). The coal-only choice teaches the main point: grid mix decides the result. An electric motor is ~90% efficient, but a coal station is ~33–40%, and transmission and charging lose more on top. Coal also emits ~30% more CO₂ per unit of energy than diesel. On a coal-only grid, electric rail is therefore roughly level with diesel rail. Its advantages there are regenerative braking and not carrying an engine and fuel. On today's average grid it is roughly a third to a half lower, and on a low-carbon grid it is an order of magnitude lower.

## 7. Calculator: input modes and scale

The same engine must cover 10⁰–10¹³ kg.

Level 1 ([overview §1.3](2026-10-21-01-carbon-cycle-plan.md)): presets plus the Activity mode with short-list combustors, and a result in auto-scaled units with one relatability anchor. Other input modes, the emission profile, the breakdown and scientific notation are Level 2.

Input modes:

1. **Activity**: combustor × distance (km), output (MWh), product (t clinker, t steel) or operating time × count
2. **Transport task**: passengers × km or tonnes × km, plus a mode. The vehicle count comes from capacity and load factor (§6.4), and the vehicle count is shown.
3. **Fuel quantity**: amount of a fuel directly (L, kL, ML, GL, kg, t, Mt, m³, bcm)
4. **Presets**: "a heavy truck driving 1,000 km", "a Delhi–Mumbai flight", "one container ship crossing Shanghai–Rotterdam", "move 1 Mt of iron ore 1,000 km by ship / rail / truck", "one year of a 1 GW subcritical coal plant", "one year of a superyacht" (cruising plus generators at anchor; published estimates are thousands of tonnes of CO₂ a year, to source), "1 tonne of cement", "a year of China's cement production", "China's annual coal combustion", "global annual fossil emissions" (presets are just parameter sets, i.e. ready-made thought experiments, [overview §1](2026-10-21-01-carbon-cycle-plan.md))
   - **Population-scaled presets** (from OWID data, [overview §4](2026-10-21-01-carbon-cycle-plan.md)): any activity × a country's population for a chosen year, e.g. "everyone in India rides a motorcycle 10 km a day for a year". Shown as per-person activity × population, so the scale-up is visible.
5. *(optional)* **Energy**: MJ/PJ/EJ of a fuel

Emission profile:
- **Single pulse (default)**: the whole amount is released at t = 0. Tab 1 deals only in quantity; time first appears in Tab 4.
- **Constant rate (option)**: the same amount repeated each year for N years, e.g. "this nation's coal burn, every year for 30 years". This is a small Tab 1 control (on/off and N). Tab 4 handles it by summing pulses, so it costs little to support.

Output display:
- kg → t → kt → Mt → Gt with SI prefixes; scientific notation toggle
- Always both **CO₂** and **C**
- Relatability anchors at scale: equivalent ppm of atmospheric CO₂ if all stayed airborne (1 ppm ≈ 2.12 GtC ≈ 7.8 Gt CO₂); fraction of current global annual emissions
- Breakdown: combustion vs upstream overhead

Numeric type: `double` is sufficient (range and precision are far beyond need); keep calculation in SI base units (kg, J, m) internally, convert only at the UI edge.

## 8. Upstream overheads

- Per-fuel editable factors: extraction, refining, transport, each as % of combustion CO₂ (or kg CO₂ per unit fuel).
- **Fugitive methane** (leaks from gas and coal) is a large upstream effect but is CH₄, not CO₂. Show it as a separate line, never merged into CO₂. Its mass is carried to Tab 5 twice over: as a greenhouse gas in its own right, and as an ozone precursor (§3). Editable leak rate per fuel (% of throughput for gas; m³ CH₄ per tonne for coal).

## 9. Output to Tabs 4 and 5

Tab 1 fills the shared `EmissionResult` ([overview §2.2](2026-10-21-01-carbon-cycle-plan.md)):

- `CarbonMassKg`: carbon from combustion, calcination and upstream, including the carbon in CO, NMVOC and fossil CH₄ ([overview §2.3](2026-10-21-01-carbon-cycle-plan.md)).
- `Breakdown`: combustion, calcination (rock), upstream, vehicle fixed cost (§6.4), incomplete-combustion products.
- `NonCo2`: fossil CH₄ (fugitive leaks and unburnt slip, §8), CO, NMVOC, NOx, SO₂ (§4), black carbon.
- `Other`: aircraft-km, for contrails in Tab 5.
- `Source`: Tab 1 plus the activity (combustor or preset).

---

## 10. Work breakdown

The parameter system and page shell are cross-cutting ([overview §5](2026-10-21-01-carbon-cycle-plan.md)).

1. Calculation engine + unit tests
2. Source and verify all fuel and combustor defaults; record citations
3. Fuel table UI (sortable, default sort CO₂ per unit energy; units toggle; Level 1 short list and columns, "show all" for Level 2; per-column visibility controls later)
4. Combustor table UI, grouped by sector (transport passenger, transport freight, stationary)
5. Transport comparison view: g CO₂/pkm and g CO₂/tkm rankings, typical vs full load, log scale
6. Calculator UI: input modes (incl. transport task), unit scaling, presets, results breakdown
7. Chemistry content: equations (coal → methane → octane), mass-gain explainer, incomplete combustion and ozone formation sub-section, plus Level 1 plain-language text and term definitions for every section
8. Illustration set (fuels and combustors) and chemical diagrams — single style
9. Hand-off to Tab 4 (placeholder Tab 4 that displays the received result)

---

## 11. Open questions

Question numbers are shared across the Carbon Cycle plans.

- ~~Q1~~ Resolved: the table is sortable on any column; default sort is CO₂ per unit energy, highest first (§5).
- ~~Q2~~ Resolved: sub-bituminous coal is a Level 2 row (§5).
- ~~Q3~~ Resolved: single pulse by default, constant rate over N years as an option (§7).
- ~~Q4~~ Resolved: fugitive methane is in v1 as a separate line, feeding Tab 5 as a GHG and an ozone precursor (§8).
- ~~Q5~~ Resolved: transport (globally common vehicles), power stations, industry, buildings and gas flaring are all in (§6).
- ~~Q7~~ Resolved: per passenger-km and per tonne-km are in, with a mode-comparison view (§6.4).
- ~~Q11~~ Resolved: electric modes are in only where they give a step change or are the usual form of the mode: e-two-wheelers and electric passenger and freight rail. Electric cars and buses are out, with a footnote. Bicycle, private jet and superyacht are added. Transport rows gain a fixed-cost (manufacture and maintenance) column (§6.1, §6.4).
- ~~Q12~~ Resolved: limestone calcination is in, at Level 1 next to coal. Tab 1's scope rule is carbon released from geological stores (fossil fuels and carbonate rock). Land clearing is in [Tab 2](2026-10-21-05-carbon-cycle-tab2-land-agriculture-plan.md). The CO₂ from biofuels and other fast-cycle carbon is out, but their non-CO₂ emissions count ([overview §1.2](2026-10-21-01-carbon-cycle-plan.md), §2.1).
