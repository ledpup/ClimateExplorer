# Carbon Cycle page — Tab 1 sub-plan: combustors and upstream

- **Date:** 2026-10-23
- **Status:** Proposed (split out of the Tab 1 plan)
- **Author:** Patrick Lea (with Claude)
- **Scope:** Tab 1 stage 2: where fossil fuel is burned (transport, power, industry, buildings), the per-passenger and per-tonne transport comparison, per-combustor by-product factors, and upstream overheads
- **Builds on:** [Tab 1: Fossil fuels and limestone](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md) (combustion and calcination as chemistry); [Carbon Cycle page — Overview](2026-10-21-01-carbon-cycle-plan.md)
- **Branch context:** `issues/carbon-cycle`

Tab 1 stage 1 treats combustion in the abstract: fuel, oxygen, products and energy ([Tab 1 §2–§4](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md)). This sub-plan adds the place where the fuel is burned and the energy spent getting the fuel there. It builds on the stage 1 chemistry engine, fuel table and calculator and changes none of the chemistry. Section references without "Tab 1" or "overview" are to this document.

---

## 1. What stage 2 adds

1. **Where it's burned** (§2): combustors in transport, power, industry and buildings, and a per-passenger / per-tonne transport comparison
2. **Per-combustor by-products** (§3): combustion completeness, CO / NOx / NMVOC emission factors and SO₂ removal, replacing the stage 1 per-fuel defaults
3. **Upstream: extraction, refinement, transport** (§4): overhead factors, kept light, plus fugitive methane
4. **Calculator additions** (§5): activity and transport-task input modes, activity presets

Each new sub-section follows [overview §1.3](2026-10-21-01-carbon-cycle-plan.md), as in stage 1.

## 2. Combustor table ("where the carbon is burned")

Scope: transport, power stations, and the other large fossil-fuel combustors: industry, buildings, and oil and gas operations. Rows are grouped by sector. The approximate global split shows where the carbon actually goes, and the page should open on it so the table isn't read as "mostly cars".

**Global energy-related CO₂ by sector (approx., ~37 Gt CO₂/yr; IEA; to verify):**

| Sector | ~Share | Main combustors |
|---|---|---|
| Electricity and heat | ~40% | Coal and gas power stations, district-heating boilers |
| Industry | ~25% | Blast furnaces, cement kilns, industrial boilers, brick kilns, chemicals |
| Transport | ~22% | Road ~75% of this; aviation and shipping ~10% each; rail ~1% |
| Buildings (direct) | ~8% | Cooking and space heating |
| Other energy industry | ~5% | Refinery own use, gas flaring, oil and gas field equipment |

**Level 1 short list ([overview §1.3](2026-10-21-01-carbon-cycle-plan.md)):** the sector split above, plus about ten plain-named combustors: bicycle (the zero-fuel baseline), motorbike, car, bus, truck, train, plane, ship, coal power station, gas power station, steelworks, cement kiln, and home cooking and heating. Each one stands for its group in the Level 2 tables below. For example, "Truck" defaults to the heavy truck and expands to the light, medium and heavy rows. Level 1 columns: name, image, fuel, and CO₂ per km (or per MWh, or per tonne of product). Efficiency, capacity, emission factors and SO₂ removal are Level 2.

Defaults in the tables below are **provisional placeholders to be sourced** ([overview §3.5](2026-10-21-01-carbon-cycle-plan.md)). Every row has: default fuel (changeable to any compatible fuel from the [Tab 1 fuel table](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md)), fuel-use or efficiency input, combustion completeness, CO / NOx / NMVOC emission factors, and SO₂ removal efficiency (§3).

### 2.1 Transport: passenger

Representative vehicles are chosen by global count, not Western fleets. Transport rows also carry capacity and load factor (§2.4).

| Combustor | Representative example | Default fuel | ~Fuel use per vehicle-km | ~Capacity | ~Typical load |
|---|---|---|---|---|---|
| Bicycle | Steel utility bicycle (the world's most numerous vehicle; China, India, the Netherlands) | None | 0 (carbon is all fixed cost, §2.4) | 1 (2 with a carrier) | ~1 |
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

**Outliers.** The private jet and the superyacht are not common vehicles. They are in as extremes, labelled as such, and are Level 2 only: on the Level 1 linear chart they would flatten every other bar. Their default load is 1, the owner. Crew are not passengers, as with the airliner's pilots and cabin crew. The load is editable. For the jet, occupied flights often carry 2–4 passengers, but a large share of flights (~40% reported, to source) are empty positioning legs, so ~1–2 averaged over all km. Most of a superyacht's fuel is burned by generators while it sits at anchor, not per km, so it also gets a per-year preset (§5).

**Electric cars and electric buses are excluded.** Electric modes are in only where they give a step change per passenger-km. On today's world-average grid (~470 g CO₂/kWh), a mid-size electric SUV at ~0.2 kWh/km emits ~95 g CO₂/km at the power station. A small petrol car emits ~130 g/km at the tailpipe. A small EV emits about half the SUV's figure, and an e-bus about a third less than a diesel bus. These are real gains, but they are not the order-of-magnitude gap between a car and a bus, train, bicycle or e-bike. An electric car also still needs a city built around cars: roads and parking take a large share of urban land (~25–35% in many cities, over half in some US downtowns; to source). That is an environmental and social cost outside this page's scope. The comparison view carries a footnote saying all this.

### 2.2 Transport: freight

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

### 2.3 Stationary combustors

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
| Gas flare | Natural gas | flaring combustion efficiency | ~95–98% (unburnt slip feeds [Tab 1 §3](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md)) | bcm flared |
| Off-road diesel | Diesel | L per operating hour | to source (tractors, irrigation pumpsets in India, mining haul trucks, construction plant) | operating hour |

Other combustors, noted but not given rows in v1:
- **Waste incineration:** plastics are fossil carbon, so burning them is fossil CO₂. Paper and food waste are biogenic.
- **Fishing vessels and inland waterway barges:** cover with the ship rows and an edited size.
- **Traditional biomass and crop-residue burning:** large in South Asia and Africa, but not fossil carbon. Mention it as out of scope, because it is a big source of the same CO and black-carbon by-products.

**Kilns: two CO₂ streams.** Cement and lime kilns report two lines, **fuel CO₂** and **rock (calcination) CO₂**, so the reader sees that most of the kiln's CO₂ comes from the limestone ([Tab 1 §2.1](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md)). In a real cement kiln the split is roughly 60% rock and 40% fuel (to verify). Stage 1 shows the theoretical floor for the fuel stream ([Tab 1 §2.2](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md)). The kiln's GJ per t clinker shows how far above that floor a real kiln runs, because it also heats the material to ~1,450 °C and loses heat.

### 2.4 Moving people and goods: per passenger-km and per tonne-km

The point is to compare **which modes move the most for the least carbon**. It is not a personal footprint.

Transport rows carry:
- **Capacity:** seats (or standing capacity), or payload tonnes.
- **Load factor:** occupancy or payload utilisation, including empty return trips. Editable, with a "typical" default (§2.1–2.2) and a one-click "full" option. This shows how much load factor matters: a crowded bus or train against a half-empty one, or a laden ship against one returning in ballast.
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
- Basis: combustion only (tank-to-wheel) by default. A toggle adds upstream (§4) to give well-to-wheel, which is the basis used by the GLEC Framework / ISO 14083 for freight. For electric rows, "combustion" is at the power station.
- Fixed cost is shown by default as a lighter stacked segment on each bar, with a toggle to hide it. Without it the bicycle would be a zero bar, which a log axis can't draw.

**Allocation rule:** mixed passenger and freight vehicles (belly cargo on airliners, ro-pax ferries) allocate everything to the primary use in v1. The ICAO calculator and GLEC have allocation methods; note them as a refinement.

**Electric modes:** in where they are a step change per passenger-km or are the common form of the mode: e-bikes and e-scooters, and electric passenger and freight trains (metro, suburban, intercity and high-speed share one row; their energy per passenger-km is similar). Electric cars and buses are out (§2.1). An electric row has energy use (kWh per vehicle-km) and a grid intensity (g CO₂/kWh). The grid intensity comes from a power-station row or a chosen mix, so the carbon comes from the power station and stays inside this tab's combustion story.

Grid-intensity choices (to source): world average (~470 g/kWh; ~60% fossil, ~32% renewable, ~9% nuclear, Ember 2024), China (~550), India (~700), coal-only (~1,000), gas combined-cycle (~350–400), low-carbon (~20–50). The coal-only choice teaches the main point: grid mix decides the result. An electric motor is ~90% efficient, but a coal station is ~33–40%, and transmission and charging lose more on top. Coal also emits ~30% more CO₂ per unit of energy than diesel. On a coal-only grid, electric rail is therefore roughly level with diesel rail. Its advantages there are regenerative braking and not carrying an engine and fuel. On today's average grid it is roughly a third to a half lower, and on a low-carbon grid it is an order of magnitude lower.

## 3. Per-combustor by-products

Stage 1 gives each fuel a carbon split (default all CO₂) and one generic NOx default, and sends all of a fuel's sulfur out as SO₂ ([Tab 1 §3, §4](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md)). Combustors replace those with their own values, because these by-products vary far more by combustor than by fuel.

**Incomplete combustion and ozone precursors (editable, per combustor):**
- Combustion completeness: fraction of carbon leaving as CO₂ versus CO, unburnt HC and soot. Default ~99%+ for modern engines, lower for e.g. open burning, brick kilns and gas flares.
- Emission factors for CO, NOx and NMVOC, in g per kg of fuel. Defaults to be sourced per [overview §3.5](2026-10-21-01-carbon-cycle-plan.md) (IPCC first; the EMEP/EEA Guidebook where IPCC has no Tier 1 factor).

**Sulfur removal (editable, per combustor):**
- SO₂ removal efficiency: scrubbers on power stations, exhaust scrubbers on ships. Default 0% for vehicles.
- **Teaching example, coal power scrubbers:** flue-gas desulfurisation removes most of the SO₂ before it leaves the stack. The common wet-limestone process is itself calcium chemistry, `CaCO₃ + SO₂ + ½ O₂ → CaSO₄ + CO₂`, so it releases a little rock CO₂ for each tonne of SO₂ captured. Level 2 text; the CO₂ line is small and v1 may leave it unmodelled, saying so.

## 4. Upstream overheads

- Per-fuel editable factors: extraction, refining, transport, each as % of combustion CO₂ (or kg CO₂ per unit fuel). This adds an upstream-overhead column to the Tab 1 fuel table.
- **Fugitive methane** (leaks from gas and coal) is a large upstream effect but is CH₄, not CO₂. It is in v1 as a separate line, never merged into CO₂. Its mass is carried to Tab 5 twice over: as a greenhouse gas in its own right, and as an ozone precursor ([Tab 1 §3](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md)). Editable leak rate per fuel (% of throughput for gas; m³ CH₄ per tonne for coal).

## 5. Calculator additions

Added to the stage 1 calculator ([Tab 1 §6](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md)):

1. **Activity**: combustor × distance (km), output (MWh), product (t clinker, t steel) or operating time × count. At Level 1, with short-list combustors.
2. **Transport task**: passengers × km or tonnes × km, plus a mode. The vehicle count comes from capacity and load factor (§2.4), and the vehicle count is shown.
3. **Activity presets**: "a heavy truck driving 1,000 km", "a Delhi–Mumbai flight", "one container ship crossing Shanghai–Rotterdam", "move 1 Mt of iron ore 1,000 km by ship / rail / truck", "one year of a 1 GW subcritical coal plant", "one year of a superyacht" (cruising plus generators at anchor; published estimates are thousands of tonnes of CO₂ a year, to source), "1 tonne of cement from a real kiln"
   - **Population-scaled activity presets**: any activity × a country's population for a chosen year, e.g. "everyone in India rides a motorcycle 10 km a day for a year". Shown as per-person activity × population, as for the stage 1 population presets.

The results breakdown adds combustion vs upstream overhead.

## 6. Output additions

Stage 2 adds to Tab 1's `EmissionResult` ([Tab 1 §7](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md); [overview §2.2](2026-10-21-01-carbon-cycle-plan.md)):

- `CarbonMassKg`: upstream carbon, including the carbon in fugitive CH₄ ([overview §2.3](2026-10-21-01-carbon-cycle-plan.md)).
- `Breakdown`: upstream, vehicle fixed cost (§2.4).
- `NonCo2`: fugitive CH₄ (§4).
- `Other`: aircraft-km, for contrails in Tab 5.
- `Source`: Tab 1 plus the combustor or activity preset.

---

## 7. Work breakdown

Builds on the stage 1 engine, fuel table and calculator ([Tab 1 §8](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md)).

1. Combustor model + unit tests: fuel use, efficiency, load factor, fixed cost, per-combustor by-product factors, SO₂ removal, upstream and fugitive CH₄
2. Source and verify all combustor and upstream defaults; record citations
3. Combustor table UI, grouped by sector (transport passenger, transport freight, stationary)
4. Transport comparison view: g CO₂/pkm and g CO₂/tkm rankings, typical vs full load, log scale
5. Calculator additions: activity and transport-task modes, activity presets, upstream in the results breakdown
6. Content: sector split, kiln streams, scrubbers, upstream and fugitive methane, plus Level 1 plain-language text
7. Combustor illustration set, in the stage 1 style
