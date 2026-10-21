# Carbon Cycle page — Tab 2: Land and agriculture

- **Date:** 2026-10-21
- **Status:** Proposed (stub; split out of the overview plan)
- **Author:** Patrick Lea (with Claude)
- **Scope:** Tab 2 of the Carbon Cycle page: land clearing, livestock, rice and fertiliser
- **Builds on:** [Carbon Cycle page — Overview](2026-10-21-01-carbon-cycle-plan.md)
- **Branch context:** `issues/carbon-cycle`

Tab 2 is one of three optional input tabs ([overview §2.1](2026-10-21-01-carbon-cycle-plan.md)). It is a stub that defines scope and model shape; it will be expanded to the level of [Tab 1](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md) before implementation.

---

## 1. Purpose

The activity sources of forcing that aren't fossil fuel or limestone. "Cutting down trees" and "cows" are the two biggest, but rice paddies and fertiliser are also major, and they're central to the global (Asian) picture. Pigs and chickens matter mostly through their manure. They have no rumen, so their enteric methane is small: a pig emits ~1–1.5 kg CH₄/yr, against ~50–120 kg for a cow (IPCC Tier 1; to verify).

Level 1 opening line ([overview §1.3](2026-10-21-01-carbon-cycle-plan.md)): "microbes that live without air make methane: in a cow's stomach, a flooded rice field and a manure pond."

## 2. Level 1 short list

| Activity | Main output | Forcing agents | Unifying chemistry |
|---|---|---|---|
| Clearing forest | CO₂ from burnt or rotting wood and soil; land turned lighter | CO₂ (+), albedo (−), BC / OC from burning | Wood is ~47–50% carbon by dry mass: `(CH₂O)n + n O₂ → n CO₂ + n H₂O` |
| Cattle (and other ruminants) | Methane belched from the rumen | CH₄ → also ozone, stratospheric water vapour | Microbes without air: `CO₂ + 4 H₂ → CH₄ + 2 H₂O` |
| Rice paddies | Methane from flooded soil | CH₄ | Microbes without air: `CH₃COOH → CH₄ + CO₂` |
| Fertiliser | N₂O leaks from soil microbes; ammonia | N₂O (+); NH₃ → nitrate and sulfate aerosol (−) | Nitrification / denitrification: `NH₄⁺ → NO₃⁻ → … N₂O … → N₂` |

## 3. Level 2 rows

Buffalo, sheep and goats (enteric); pigs and chickens (manure CH₄ and N₂O); manure management systems (lagoon against dry lot, which changes CH₄ a lot); peatland drainage and peat fires (e.g. Indonesia: a large CO₂ source); crop-residue and savanna burning (CO, BC, OC, NOx; the CO₂ is not counted, [overview §1.2](2026-10-21-01-carbon-cycle-plan.md)); irrigation (part of the land-use albedo term).

## 4. Model shape

Thought experiments, not an inventory ([overview §1.2](2026-10-21-01-carbon-cycle-plan.md)):

- **Clearing forest:** hectares × forest type → biomass (t dry matter/ha) × carbon fraction → CO₂. Optional soil-carbon loss and regrowth, and an albedo change per hectare by latitude. Default biomass and carbon fraction from IPCC 2006 / 2019 Refinement Vol 4 (AFOLU).
- **Livestock:** head count × emission factor (kg CH₄/head/yr), from IPCC Tier 1 by animal type and region, plus manure CH₄ and N₂O by management system.
- **Rice:** hectares × days flooded × daily emission factor (IPCC baseline, scaled by water regime).
- **Fertiliser:** kg N applied × N₂O emission factor. IPCC default ~1% of applied N leaves as N₂O-N; the 2019 Refinement splits this into wet and dry climates (to verify). Convert N₂O-N to N₂O with ×44/28.
- Presets, e.g. "every cow on Earth for a year" (~1.5 billion head; to verify), "clear 1 million ha of tropical forest", "a year of India's rice".
- Fertiliser *production* (Haber–Bosch hydrogen from methane) belongs in [Tab 1 §2.1](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md); its N₂O after it reaches the field is here.

## 5. Output to Tabs 4 and 5

Fills the shared `EmissionResult` ([overview §2.2](2026-10-21-01-carbon-cycle-plan.md)): CO₂ from land clearing in `CarbonMassKg`; biogenic CH₄ (not added to `CarbonMassKg`, [overview §2.3](2026-10-21-01-carbon-cycle-plan.md)), N₂O, NH₃, BC / OC and NOx in `NonCo2`; land area converted in `Other`, for albedo.
