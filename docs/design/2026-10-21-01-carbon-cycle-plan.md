# Carbon Cycle page — Plan

Status: draft v0.6 (21 Oct 2026). Scope of this plan: **Tab 1 (Combustion)** and the **parameter system**. Tabs 2–4 are stubbed only to define the hand-off contract.

---

## 1. Purpose

A simplified, explorable calculator and model of the carbon cycle that explains *why* burning fossil fuels heats the planet. The chemistry, physics and Earth science are first-class content, not footnotes. The emphasis shifts across the page: Tab 1 is mostly chemistry (fuels, combustion, by-products), Tab 2 is Earth science (reservoirs and fluxes), and Tab 3 is mostly physics (molecular absorption, emission height, energy balance). The page reads left to right:

| Tab | Topic | Takes | Produces |
|---|---|---|---|
| 1 | Fossil fuels and limestone → CO₂ | User inputs | Carbon released (mass, and optionally a time profile) |
| 2 | Carbon circulation | Tab 1 output | Carbon in atmosphere / land / ocean / crust over time |
| 3 | Radiative forcing | Tab 2 atmospheric CO₂ | W/m² |
| 4 | Temperature | Tab 3 forcing | °C response |
| 5 | Modelled vs measured | Tabs 1–4 run on historical emissions | Model output overlaid on observed records |

Not a personal carbon-footprint calculator and not a consumer tool. It must work equally for "one truck driving 1,000 km" and "a nation's annual coal burn".

**Global scope.** Defaults, examples and presets are not tied to any one country. Where a "typical" vehicle or plant is needed, pick what is most common worldwide by count or by emissions. That usually means what is used in China, India and the rest of Asia and Africa, not the Western fleet (e.g. a 100–125 cc motorcycle and a diesel bus, not a large petrol sedan).

**Which carbon counts.** The page counts carbon that people release from **geological stores**: fossil fuels, and carbonate rock (limestone) heated to make cement and lime. Without people, that carbon would come back to the atmosphere only over millions of years, through volcanoes and weathering in the slow carbon cycle. Burning wood is different: that carbon was taken from the air recently and returns to the fast cycle. So land clearing, biofuels and biomass burning are out of scope, and the page says why. Whether the source is a "fuel" doesn't matter; what matters is where the carbon was stored.

**Experiments, not inventories.** The page models chemistry and physics. It is not an emissions inventory. Its main use is thought experiments built from a few basic tools: an activity, a combustor or process, a count, and a population. For example: "everyone in India rides a motorbike 10 km a day for a year", "run 20 GW of subcritical coal plants for a year at 60%", "make a year of China's cement". National datasets supply scale (population, §4.1) and reference figures to compare against. They are not what drives the page, so no fleet-by-country dataset is needed.

### 1.1 Design principle: simple first, detail on demand

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

## 2. Tab 1 — Fossil fuels and limestone: extraction, refinement, transport, combustion and calcination

### 2.1 Sub-sections

1. **Combustion (main focus)**, alongside **limestone calcination** (§2.2.1)
2. **Incomplete combustion and tropospheric ozone** (sub-section: CO, NMVOCs, CH₄ and NOx as ozone precursors)
3. **Sulfur and sulfate aerosols** (sub-section: the cooling by-product)
4. **Upstream: extraction, refinement, transport** (overhead factors, kept light)
5. **Where it's burned** (combustors: transport, power, industry, buildings, and a per-passenger / per-tonne transport comparison)
6. **Calculator** (inputs → result → "send to Tab 2")

Every sub-section follows §1.1. Each one opens with its Level 1 overview, a sentence or two and at most one picture or table, and the rest sits behind an expand.

### 2.2 Combustion chemistry content

Worked examples, in this order (easiest first, §1.1):

1. **Level 1: coal** (simplified as carbon): `C + O₂ → CO₂`. One carbon atom plus oxygen from the air gives one CO₂.
   **Level 1, alongside coal: limestone** `CaCO₃ → CaO + CO₂`. Heating rock releases CO₂ with no burning at all (§2.2.1).
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

### 2.2.1 Limestone: calcination (cement and lime)

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
- **Related non-combustion releases of fossil carbon** (same scope rule, §1; Level 2 text, rows later):
  - steam methane reforming for hydrogen and ammonia, `CH₄ + 2 H₂O → CO₂ + 4 H₂`
  - iron-ore reduction in blast furnaces, `Fe₂O₃ + 3 CO → 2 Fe + 3 CO₂`, which is part of the blast-furnace row's coke use

**Tie to later tabs:** this is carbon from the crust reservoir. Tab 2 should compare the rates. People move carbon from geological stores to the air at ~38 Gt CO₂/yr (fossil fuels plus limestone). Volcanoes move ~0.3–0.4 Gt CO₂/yr. That's about 100 times faster (to verify).

**Model inputs (editable):** CaO content of clinker, clinker ratio of cement, lime type, and the kiln fuel and fuel energy per t clinker (§2.6.3). Defaults per §3.5.

### 2.3 Incomplete combustion and tropospheric ozone (sub-section)

**Main point:** combustion by-products are ozone precursors, and tropospheric ozone (O₃) is a greenhouse gas. Incomplete combustion barely changes the carbon budget, because CO becomes CO₂ within months. What it adds is ozone formation.

**Level 1** is the main point in plain words: "Engines and fires also give off gases that, in sunlight, make ozone near the ground. Ozone is a short-lived greenhouse gas and a pollutant." Everything below is Level 2.

**Precursors and where they come from:**

| Precursor | Source | Role in ozone formation | ~Atmospheric lifetime |
|---|---|---|---|
| CO | Incomplete combustion | Fuel: oxidised by OH, driving the cycle | ~1–2 months |
| Unburnt hydrocarbons (NMVOCs) | Incomplete combustion, evaporation | Fuel: oxidised by OH, driving the cycle | hours–weeks |
| CH₄ | Fugitive emissions (§2.8), unburnt gas slip | Fuel: slow, global background ozone source | ~10 years |
| NOx (NO + NO₂) | High-temperature combustion | Catalyst: needed to turn the oxidation into O₃ | ~days |

- **Correctness note:** NOx is *not* an incomplete-combustion product. It comes from hot, oxygen-rich combustion, where N₂ from the air reacts with O₂ (plus nitrogen in the fuel itself). Cleaner, hotter combustion can mean *less* CO but *more* NOx. The page should frame this subsection as "combustion by-products", with incomplete combustion as one source among them.
- Keep the existing pieces: soot/black carbon is noted and deferred to Tab 3; example equation `2 CH₄ + 3 O₂ → 2 CO + 4 H₂O`.

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
- Emission factors for CO, NOx and NMVOC, in g per kg of fuel. Defaults to be sourced per §3.5 (IPCC first; the EMEP/EEA Guidebook where IPCC has no Tier 1 factor).
- Precursor masses are carried to Tab 3 for ozone forcing. CO's carbon is still counted in the carbon total sent to Tab 2.

### 2.4 Sulfur and sulfate aerosols (sub-section)

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
- Sulfur content per fuel (mass fraction). This becomes a new fuel-table column (§2.5).
- SO₂ removal efficiency per combustor (scrubbers on power stations, exhaust scrubbers on ships). Default 0% for vehicles.
- SO₂ mass is carried to Tab 3 as a negative forcing that tracks the emission rate, the same short-lived treatment as ozone.

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

### 2.5 Fuel table

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

The full (Level 2) table follows. Values are **provisional starting points to verify** using the source order in §3.5. IPCC comes first; the IPCC defaults are listed after this table.

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
- IPCC assumes an oxidation factor of 1 (all carbon ends up as CO₂). That matches this page's accounting rule (§2.9).
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
10. **Sulfur content** — mass fraction (§2.4)
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

### 2.6 Combustor table ("where the carbon is burned")

Scope (Q5, resolved): transport, power stations, and the other large fossil-fuel combustors: industry, buildings, and oil and gas operations. Rows are grouped by sector. The approximate global split shows where the carbon actually goes, and the page should open on it so the table isn't read as "mostly cars".

**Global energy-related CO₂ by sector (approx., ~37 Gt CO₂/yr; IEA; to verify):**

| Sector | ~Share | Main combustors |
|---|---|---|
| Electricity and heat | ~40% | Coal and gas power stations, district-heating boilers |
| Industry | ~25% | Blast furnaces, cement kilns, industrial boilers, brick kilns, chemicals |
| Transport | ~22% | Road ~75% of this; aviation and shipping ~10% each; rail ~1% |
| Buildings (direct) | ~8% | Cooking and space heating |
| Other energy industry | ~5% | Refinery own use, gas flaring, oil and gas field equipment |

**Level 1 short list (§1.1):** the sector split above, plus about ten plain-named combustors: motorbike, car, bus, truck, train, plane, ship, coal power station, gas power station, steelworks, cement kiln, and home cooking and heating. Each one stands for its group in the Level 2 tables below. For example, "Truck" defaults to the heavy truck and expands to the light, medium and heavy rows. Level 1 columns: name, image, fuel, and CO₂ per km (or per MWh, or per tonne of product). Efficiency, capacity, emission factors and SO₂ removal are Level 2.

Defaults in the tables below are **provisional placeholders to be sourced** (§3.5). Every row has: default fuel (changeable to any compatible fuel), fuel-use or efficiency input, combustion completeness, CO / NOx / NMVOC emission factors (§2.3; these vary far more by combustor than by fuel), and SO₂ removal efficiency (§2.4).

#### 2.6.1 Transport: passenger

Representative vehicles are chosen by global count, not Western fleets. Transport rows also carry capacity and load factor (§2.6.4).

| Combustor | Representative example | Default fuel | ~Fuel use per vehicle-km | ~Capacity | ~Typical load |
|---|---|---|---|---|---|
| Motorcycle / scooter | 100–125 cc commuter bike (the most numerous motor vehicle in India and South-East Asia) | Petrol | ~2 L/100 km | 2 | ~1.3–2 |
| Three-wheeler (auto-rickshaw) | CNG auto-rickshaw (India, Pakistan, Bangladesh) | CNG (methane), also LPG / petrol / diesel | ~3 kg CNG/100 km | 3–4 (often more) | ~2 |
| Small car | 1.0–1.5 L hatchback / compact (largest segments in India and China) | Petrol | ~5–6 L/100 km | 4–5 | ~1.5 |
| Large car / SUV | Mid-size SUV (fastest-growing segment worldwide, incl. China) | Petrol | ~8–10 L/100 km | 5 | ~1.5 |
| Car (diesel) | Compact diesel | Diesel | ~5–6 L/100 km | 5 | ~1.5 |
| City bus | 12 m diesel or CNG bus | Diesel / CNG | ~30–45 L/100 km | ~60–90 incl. standing | ~20–60, very place-dependent |
| Intercity coach | 45–55 seat coach | Diesel | ~25–30 L/100 km | ~50 | ~30–45 |
| Passenger train (diesel) | Diesel-hauled long-distance train | Diesel | per train-km, to source | ~1,000–2,000+ | high in India and China |
| Airliner (narrow-body) | A320 / 737 class | Jet fuel | ~3–4 L/km | ~180 | ~80–85% |
| Airliner (wide-body) | 787 / A350 / 777 class | Jet fuel | ~8–10 L/km | ~300 | ~80% |

#### 2.6.2 Transport: freight

| Combustor | Representative example | Default fuel | ~Fuel use per vehicle-km | ~Payload capacity | Load factor notes |
|---|---|---|---|---|---|
| Light commercial vehicle | ~1 t mini-truck (e.g. the small pickups and mini-trucks common in India and China) | Diesel | ~7–9 L/100 km | ~1 t | Includes empty running |
| Medium rigid truck | ~10 t payload | Diesel | ~20–25 L/100 km | ~10 t | Empty running ~20–30% of km |
| Heavy truck (tractor-trailer) | ~25 t payload | Diesel | ~30–35 L/100 km | ~25 t | As above |
| Freight train (diesel) | Diesel-hauled bulk / container train | Diesel | per train-km, to source | ~2,000–5,000 t | Often empty one way (coal, ore) |
| Container ship | ~8,000–14,000 TEU | Bunker fuel | per day at speed, to source (IMO) | ~100,000+ t | Slow steaming matters a lot |
| Bulk carrier | Panamax / Capesize | Bunker fuel | to source (IMO) | ~70,000–180,000 t | Returns in ballast |
| Oil tanker | VLCC | Bunker fuel | to source (IMO) | ~300,000 t | Returns in ballast |
| Air freighter | 777F / 747-8F class | Jet fuel | ~9–11 L/km | ~100 t | ~60–70% |

Aviation shows CO₂ only here. Contrails and NOx at altitude add a substantial non-CO₂ effect. That is a Tab 3 topic; the comparison view should carry a footnote saying so.

#### 2.6.3 Stationary combustors

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
| Gas flare | Natural gas | flaring combustion efficiency | ~95–98% (unburnt slip feeds §2.3) | bcm flared |
| Off-road diesel | Diesel | L per operating hour | to source (tractors, irrigation pumpsets in India, mining haul trucks, construction plant) | operating hour |

Other combustors, noted but not given rows in v1:
- **Waste incineration:** plastics are fossil carbon, so burning them is fossil CO₂. Paper and food waste are biogenic.
- **Fishing vessels and inland waterway barges:** cover with the ship rows and an edited size.
- **Traditional biomass and crop-residue burning:** large in South Asia and Africa, but not fossil carbon. Mention it as out of scope, because it is a big source of the same CO and black-carbon by-products.

Cement and lime kilns report two lines, **fuel CO₂** and **rock (calcination) CO₂**, so the reader sees that most of the kiln's CO₂ comes from the limestone (§2.2.1).

#### 2.6.4 Moving people and goods: per passenger-km and per tonne-km

Resolved (Q7): included. The point is to compare **which modes move the most for the least carbon**. It is not a personal footprint.

Transport rows carry:
- **Capacity:** seats (or standing capacity), or payload tonnes.
- **Load factor:** occupancy or payload utilisation, including empty return trips. Editable, with a "typical" default (§2.6.1–2.6.2) and a one-click "full" option. This shows how much load factor matters: a crowded bus or train against a half-empty one, or a laden ship against one returning in ballast.
- **Fuel use per vehicle-km.** v1 treats this as independent of load, and the page says so. A full truck burns more than an empty one; a later version could make fuel use rise linearly from empty to full.

Derived outputs: g CO₂ per vehicle-km, per **passenger-km**, and per **tonne-km**.

**Comparison view:**
- Level 1: the short-list modes at typical load, on two simple charts, with the plain labels "CO₂ per passenger per km" and "CO₂ per tonne of goods per km". Full load, the full vehicle list, log scale and the well-to-wheel toggle are Level 2.
- Two ranked bar charts, one for passenger modes (g CO₂/pkm) and one for freight modes (g CO₂/tkm). Never put both on one axis.
- Freight spans ~2 orders of magnitude (bulk ship ~3–8, rail ~15–30, heavy truck ~60–120, light commercial ~300+, air freight ~500–1,000 g CO₂/tkm; order of magnitude, to verify), so offer a log scale.
- Each bar shows "typical" and "full" load, so the reader sees the load-factor range, not one number.
- Basis: combustion only (tank-to-wheel) by default. A toggle adds upstream (§2.8) to give well-to-wheel, which is the basis used by the GLEC Framework / ISO 14083 for freight.

**Allocation rule:** mixed passenger and freight vehicles (belly cargo on airliners, ro-pax ferries) allocate everything to the primary use in v1. The ICAO calculator and GLEC have allocation methods; note them as a refinement.

**Electric modes (Q11):** most of the world's passenger rail (India, China, Europe) and a large share of China's new cars are electric. A comparison without them would mislead. Proposal: an electric row has energy use (kWh per vehicle-km) and a grid intensity (kg CO₂/MWh), taken from a power-station row or a chosen generation mix. The carbon then comes from the power station, which keeps it inside this tab's combustion story.

### 2.7 Calculator: input modes and scale

The same engine must cover 10⁰–10¹³ kg.

Level 1 (§1.1): presets plus the Activity mode with short-list combustors, and a result in auto-scaled units with one relatability anchor. Other input modes, the emission profile, the breakdown and scientific notation are Level 2.

Input modes:

1. **Activity**: combustor × distance (km), output (MWh), product (t clinker, t steel) or operating time × count
2. **Transport task**: passengers × km or tonnes × km, plus a mode. The vehicle count comes from capacity and load factor (§2.6.4), and the vehicle count is shown.
3. **Fuel quantity**: amount of a fuel directly (L, kL, ML, GL, kg, t, Mt, m³, bcm)
4. **Presets**: "a heavy truck driving 1,000 km", "a Delhi–Mumbai flight", "one container ship crossing Shanghai–Rotterdam", "move 1 Mt of iron ore 1,000 km by ship / rail / truck", "one year of a 1 GW subcritical coal plant", "1 tonne of cement", "a year of China's cement production", "China's annual coal combustion", "global annual fossil emissions" (presets are just parameter sets, i.e. ready-made thought experiments, §1)
   - **Population-scaled presets** (from OWID data, §4): any activity × a country's population for a chosen year, e.g. "everyone in India rides a motorcycle 10 km a day for a year". Shown as per-person activity × population, so the scale-up is visible.
5. *(optional)* **Energy**: MJ/PJ/EJ of a fuel

Emission profile:
- **Single pulse (default)**: the whole amount is released at t = 0. Tab 1 deals only in quantity; time first appears in Tab 2.
- **Constant rate (option)**: the same amount repeated each year for N years, e.g. "this nation's coal burn, every year for 30 years". This is a small Tab 1 control (on/off and N). Tab 2 handles it by summing pulses, so it costs little to support.

Output display:
- kg → t → kt → Mt → Gt with SI prefixes; scientific notation toggle
- Always both **CO₂** and **C**
- Relatability anchors at scale: equivalent ppm of atmospheric CO₂ if all stayed airborne (1 ppm ≈ 2.12 GtC ≈ 7.8 Gt CO₂); fraction of current global annual emissions
- Breakdown: combustion vs upstream overhead

Numeric type: `double` is sufficient (range and precision are far beyond need); keep calculation in SI base units (kg, J, m) internally, convert only at the UI edge.

### 2.8 Upstream overheads

- Per-fuel editable factors: extraction, refining, transport, each as % of combustion CO₂ (or kg CO₂ per unit fuel).
- **Fugitive methane** (leaks from gas and coal) is a large upstream effect but is CH₄, not CO₂. Show it as a separate line, never merged into CO₂. Its mass is carried to Tab 3 twice over: as a greenhouse gas in its own right, and as an ozone precursor (§2.3). Editable leak rate per fuel (% of throughput for gas; m³ CH₄ per tonne for coal).

### 2.9 Output contract to Tab 2

```
EmissionResult
  CarbonMassKg          // total C released (kg C)
  Co2MassKg             // derived, for display
  Profile               // Pulse (default) | ConstantRate(years); CarbonMassKg is per pulse / per year
  Breakdown             // combustion, calcination (rock), upstream, incomplete-combustion products
  NonCo2                // masses for Tab 3: CH₄ (fugitive + slip), CO, NMVOC, NOx (as NO₂), SO₂
  ParameterSnapshot     // which parameter values produced this
```

Tab 2 consumes carbon mass + profile. Tab 3 additionally consumes `NonCo2` for methane, ozone and sulfate aerosol forcing. Changing Tab 1 re-runs downstream tabs in order.

Carbon accounting rule: carbon in CO, NMVOC and CH₄ is included in `CarbonMassKg`, since it all ends up as CO₂ eventually. It is also reported in `NonCo2` for its short-term effects. The UI must label this clearly so it isn't read as double counting.

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

1. **IPCC.** The 2006 Guidelines as amended by the **2019 Refinement**, which is the latest inventory methodology. Vol 2 (Energy) covers fuel NCV, carbon content and CO₂ factors, Tier 1 CH₄ / N₂O factors for stationary and mobile combustion, and fugitive emissions (Ch 4). The IPCC Emission Factor Database (EFDB) adds to it. **AR6** (WG1) is used for the physical constants and later-tab parameters (forcing, lifetimes, GWPs), and **AR6 WG3** for the sector split in §2.6.
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

## 4. Data from ClimateExplorer

### 4.1 Emissions and population: OWID CO₂ dataset

Source: [owid/co2-data](https://github.com/owid/co2-data), one row per country per year, in CSV, XLSX or JSON (split by country). Preferred over using the Global Carbon Budget directly because it bundles population with emissions.

Columns of interest (confirmed in the codebook):
- `iso_code`, `country`, `year`, `population`
- `co2` (Mt CO₂/yr, excluding land-use change), `co2_per_capita`
- by source: `coal_co2`, `oil_co2`, `gas_co2`, `flaring_co2`, `cement_co2`
- `land_use_change_co2`, `methane`, `primary_energy_consumption`

Uses:
- Population-scaled presets (§2.7).
- National presets: "China's coal CO₂ in 2024" as a ready-made quantity, for any country.
- Reference anchors in the results: "this is X% of the world's (or a chosen country's) annual fossil CO₂".
- Gas flaring: `flaring_co2` gives a national check for the gas-flare combustor (§2.6.3).

Notes:
- OWID's CO₂ figures *are* Global Carbon Budget numbers, processed and converted at 3.664. The two are the same source, so cite both.
- They're inventory estimates, not measurements, and the UI should say so.
- OWID gives CO₂ by fuel, not fuel quantities. A national preset entered as "coal CO₂" skips the combustion chemistry. To keep the chemistry in play, back-calculate fuel mass with the page's own emission factor, and show that step ("≈ N Mt of bituminous coal at the current carbon fraction"). An alternative is to drive presets from `primary_energy_consumption` through the energy input mode.
- Updated about annually. Pin a version or snapshot date in the parameter source field.

### 4.2 Other ClimateExplorer data

Likely needs (to confirm):
- Tab 5: Mauna Loa CO₂ (from 1958) and global surface temperature, plus optional observed CH₄ and ocean pH (see §7).

---

## 5. Work breakdown (Tab 1)

1. Parameter system: record type, registry, constants set, edit popover, assumptions panel, URL state (incl. expansion state, §1.1)
2. Calculation engine + unit tests
3. Source and verify all fuel and combustor defaults; record citations
4. Fuel table UI (sortable, default sort CO₂ per unit energy; units toggle; Level 1 short list and columns, "show all" for Level 2; per-column visibility controls later)
5. Combustor table UI, grouped by sector (transport passenger, transport freight, stationary)
6. Transport comparison view: g CO₂/pkm and g CO₂/tkm rankings, typical vs full load, log scale
7. Calculator UI: input modes (incl. transport task), unit scaling, presets, results breakdown
8. Chemistry content: equations (coal → methane → octane), mass-gain explainer, incomplete combustion and ozone formation sub-section, plus Level 1 plain-language text and term definitions for every section
9. Illustration set (fuels and combustors) and chemical diagrams — single style
10. Hand-off to Tab 2 (contract + placeholder Tab 2 that displays the received result)

---

## 6. Open questions

- ~~Q1~~ Resolved: the table is sortable on any column; default sort is CO₂ per unit energy, highest first (§2.5).
- ~~Q2~~ Resolved: sub-bituminous coal is a Level 2 row (§2.5).
- ~~Q3~~ Resolved: single pulse by default, constant rate over N years as an option (§2.7).
- ~~Q4~~ Resolved: fugitive methane is in v1 as a separate line, feeding Tab 3 as a GHG and an ozone precursor (§2.8).
- **Q8** Ozone and sulfate in Tab 3: a simple editable yield factor per precursor, or published per-precursor forcing coefficients (e.g. AR6 emissions-based ERF)? The second is more defensible, but it attributes forcing to global emissions rather than computing ozone or aerosol.
- **Q9** Tab 5 spin-up: start the model run at 1750 using estimated historical emissions, or at 1958 from an assumed starting state? See §7.
- **Q10** Tab 5 temperature comparison: start it at 1958 to match CO₂, or show the full instrumental record back to ~1850?
- ~~Q5~~ Resolved: transport (globally common vehicles), power stations, industry, buildings and gas flaring are all in (§2.6).
- ~~Q6~~ Resolved: latest IPCC first, then sector bodies, then literature; no country-specific defaults (§3.5).
- ~~Q7~~ Resolved: per passenger-km and per tonne-km are in, with a mode-comparison view (§2.6.4).
- **Q11** Electric modes (electric rail, metro, EVs, e-buses) in v1, using kWh per vehicle-km × grid intensity from a power-station row? Recommended: yes for rail and buses at least, or the pkm comparison under-represents the lowest-carbon modes (§2.6.4).
- ~~Q12~~ Resolved: limestone calcination is in, at Level 1 next to coal. The scope rule is carbon released from geological stores (fossil fuels and carbonate rock); land clearing and biofuels are out (§1, §2.2.1).

---

## 7. Later tabs (stubs; Tab 5 in more detail)

The §1.1 layering applies to every tab. For example:
- Tab 2 Level 1: "of each tonne of CO₂ released, roughly 40% is still in the air after a century, and about a fifth after a thousand years".
- Tab 3 Level 1: "more CO₂ traps more heat", with the logarithmic relation at Level 2.
- Tab 5 Level 1: one chart of model against measurement, with residuals and fit statistics at Level 2.

- **Tab 2 — Circulation:** reservoirs (atmosphere, land biosphere/soil, ocean surface/deep, crust as limestone & kerogen); impulse-response curve for airborne fraction over time; summary table at years / centuries / millennia.
- **Tab 3 — Radiative forcing:** CO₂ concentration → forcing (logarithmic relation). Also CH₄ (square-root relation, ~decade lifetime) and tropospheric ozone from the `NonCo2` precursors (short-lived; forcing tracks the emission rate, not a cumulative total). Sulfate aerosol from SO₂ as a negative forcing, also short-lived.
- **Tab 4 — Temperature:** forcing → transient and equilibrium response.

### Tab 5 — Modelled vs measured

**Purpose:** run the same chain (Tabs 1–4) on historical emissions and overlay the result on observed records. It shows whether the simplified model reproduces what has actually happened, and how far each assumption moves the fit. Assumption edits made on other tabs flow through, so the user can see an edit improve or worsen the match.

**Measured series (comparison window):**
- **CO₂:** Mauna Loa monthly and annual means, starting 1958, the first continuous direct measurements. This is the anchor series.
- **Temperature:** global mean surface temperature record (e.g. HadCRUT5, GISTEMP, NOAA, Berkeley Earth; pick one, optionally show the spread).
- Optional later: atmospheric CH₄ (NOAA network, from the 1980s) to check the methane path; ocean pH (e.g. Hawaii Ocean Time-series, from 1988) if ocean acidification goes into Tab 2.

**Model input:**
- Historical fossil emissions by year, by fuel where available (OWID CO₂ dataset, §4.1), plus SO₂ and the ozone precursors for the non-CO₂ terms. Uses the constant-rate / time-series profile path, not a single pulse.
- Label clearly that these are *inventory estimates* built from fuel production and trade statistics. They are the model's input, not a measurement, and the comparison only tests the model against what was actually measured.
- Land-use emissions (deforestation) also add CO₂. Either include them as a separate editable series or state clearly that they're omitted; omitting them will make the model under-predict.

**Why the run can't simply start in 1958:** in 1958 the system was already ~200 years into a disturbance. The ocean and land were taking up carbon because the atmosphere was above equilibrium. A model started cold at 315 ppm, assumed to be in balance, would have almost no ocean uptake at first and would overshoot the measured curve. Options (Q9):
1. Run from 1750 using estimated emissions; show only 1958 onward in the comparison. Most defensible; the pre-1958 emissions are estimates but the comparison stays on measured data.
2. Start in 1958 with an assumed reservoir state (e.g. a prescribed initial ocean uptake rate). Simpler, but the starting state is itself an assumption that has to be made editable.
- Pre-1958 ice-core CO₂ (e.g. Law Dome) could be shown as a separately labelled proxy line, off by default, kept visually distinct from the direct measurements.

**Display:**
- Measured vs modelled on one chart per quantity, with residual (modelled − measured) underneath.
- Simple fit statistics: mean bias, RMSE, and trend comparison (model vs measured ppm/yr and °C/decade).
- Which assumptions were edited, from the parameter snapshot, so a fit can be reproduced.
