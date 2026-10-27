# Carbon Cycle page — Tab 3: Waste and industrial gases

- **Date:** 2026-10-21
- **Status:** Proposed (stub; split out of the overview plan)
- **Author:** Patrick Lea (with Claude)
- **Scope:** Tab 3 of the Carbon Cycle page: landfill, wastewater, halogenated gases and industrial N₂O
- **Builds on:** [Carbon Cycle page — Overview](2026-10-21-01-carbon-cycle-plan.md)
- **Branch context:** `issues/carbon-cycle`

Tab 3 is one of three optional input tabs ([overview §2.1](2026-10-21-01-carbon-cycle-plan.md)), confirmed by Q13. It is a stub that defines scope and model shape; it will be expanded to the level of [Tab 1](2026-10-21-04-carbon-cycle-tab1-fossil-fuels-plan.md) before implementation.

---

## 1. Purpose

The remaining human sources. These are the halogenated-gas bar, about as big as ozone (+0.41 W/m², [overview §1.1](2026-10-21-01-carbon-cycle-plan.md)), and the waste share of methane (~18% of human CH₄; to verify). They don't fit with combustion (Tab 1) or agriculture (Tab 2).

## 2. Activities

| Activity | Forcing agents | Chemistry / Level 1 point |
|---|---|---|
| Landfill (rotting food, paper, wood without air) | CH₄; the CO₂ is not counted ([overview §1.2](2026-10-21-01-carbon-cycle-plan.md)) | `C₆H₁₀O₅ + H₂O → 3 CH₄ + 3 CO₂`: the same "microbes without air" story as [Tab 2](2026-10-21-05-carbon-cycle-tab2-land-agriculture-plan.md) |
| Wastewater | CH₄, N₂O | As above |
| Refrigerants, foams, sprays (CFCs, HCFCs, HFCs) | Halogenated-gas forcing (+); CFCs and HCFCs also destroy stratospheric ozone (−) | Per kg, thousands of times stronger than CO₂. They leak slowly from equipment in use ("banks"), so emissions lag production by years |
| Electrical switchgear (SF₆), aluminium smelting (PFCs) | Halogenated-gas forcing | Lifetimes of thousands of years |
| Nitric and adipic acid plants | N₂O | Industrial; largely avoidable with catalytic abatement |

## 3. Level 2 chemistry: stratospheric ozone destruction

UV breaks a CFC (`CFCl₃ + UV → CFCl₂ + Cl`), then `Cl + O₃ → ClO + O₂` and `ClO + O → Cl + O₂`. The net is `O₃ + O → 2 O₂`, and one chlorine atom destroys thousands of ozone molecules. A teaching point: the Montreal Protocol is why the halogenated-gas bar has stopped growing, and HFCs are why it hasn't fallen faster.

## 4. Model shape

Kg of gas emitted (or bank × annual leak rate) by species. Radiative efficiency and lifetime per species come from AR6 Ch 7 Supplementary Material.

## 5. Output to the simulation

Fills the shared `EmissionResult` ([overview §2.2](2026-10-21-01-carbon-cycle-plan.md)): biogenic CH₄ (not added to `CarbonMassKg`, [overview §2.3](2026-10-21-01-carbon-cycle-plan.md)), N₂O and halogenated gases by species in `NonCo2`. Fossil carbon from waste incineration of plastics stays with the Tab 1 combustors ([Tab 1 combustor sub-plan §2.3](2026-10-23-01-carbon-cycle-tab1-combustors-plan.md)).
