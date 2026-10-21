# Carbon Cycle page — Tab 7: Modelled vs measured

- **Date:** 2026-10-21
- **Status:** Proposed (stub; split out of the overview plan)
- **Author:** Patrick Lea (with Claude)
- **Scope:** Tab 7 of the Carbon Cycle page: the model run on historical emissions, compared with observed records
- **Builds on:** [Carbon Cycle page — Overview](2026-10-21-01-carbon-cycle-plan.md)
- **Branch context:** `issues/carbon-cycle`

---

## 1. Purpose

Run the same chain (Tabs 1–6) on historical emissions and overlay the result on observed records. It shows whether the simplified model reproduces what has actually happened, and how far each assumption moves the fit. Assumption edits made on other tabs flow through, so the user can see an edit improve or worsen the match.

Level 1 ([overview §1.3](2026-10-21-01-carbon-cycle-plan.md)): one chart of model against measurement, with residuals and fit statistics at Level 2.

## 2. Relation to the input tabs

Tab 7 does not use the user's thought experiments from Tabs 1–3. It runs every source path (fossil fuels and limestone, land and agriculture, waste and industrial gases) on historical emissions, whichever input tabs the user has filled in ([overview §2.1](2026-10-21-01-carbon-cycle-plan.md)). The parameters edited on those tabs still apply.

It offers the same per-source include toggles as Tabs 4–6, all on by default. Turning one off shows what leaving that source out does to the fit. For example, without land clearing the modelled CO₂ curve falls well short of Mauna Loa.

## 3. Measured series (comparison window)

- **CO₂:** Mauna Loa monthly and annual means, starting 1958, the first continuous direct measurements. This is the anchor series.
- **Temperature:** global mean surface temperature record (e.g. HadCRUT5, GISTEMP, NOAA, Berkeley Earth; pick one, optionally show the spread).
- Optional later: atmospheric CH₄ (NOAA network, from the 1980s) to check the methane path; ocean pH (e.g. Hawaii Ocean Time-series, from 1988) if ocean acidification goes into Tab 4.

## 4. Model input

- Historical fossil emissions by year, by fuel where available (OWID CO₂ dataset, [overview §4.1](2026-10-21-01-carbon-cycle-plan.md)), plus SO₂ and the ozone precursors for the non-CO₂ terms. Uses the constant-rate / time-series profile path, not a single pulse.
- Label clearly that these are *inventory estimates* built from fuel production and trade statistics. They are the model's input, not a measurement, and the comparison only tests the model against what was actually measured.
- Land-use CO₂ (OWID `land_use_change_co2`) and historical CH₄, N₂O and halogenated-gas emissions feed the Tab 2 and Tab 3 paths. Leaving out land-use CO₂ alone would drop about a third of cumulative CO₂, and the model would under-predict.
- A second comparison target: the forcing chart ([overview §1.1](2026-10-21-01-carbon-cycle-plan.md)). The model's ERF by agent for 1750 → latest year, set against Forster et al.'s values and ranges.

**Why the run can't simply start in 1958:** in 1958 the system was already ~200 years into a disturbance. The ocean and land were taking up carbon because the atmosphere was above equilibrium. A model started cold at 315 ppm, assumed to be in balance, would have almost no ocean uptake at first and would overshoot the measured curve. Options (Q9):
1. Run from 1750 using estimated emissions; show only 1958 onward in the comparison. Most defensible; the pre-1958 emissions are estimates but the comparison stays on measured data.
2. Start in 1958 with an assumed reservoir state (e.g. a prescribed initial ocean uptake rate). Simpler, but the starting state is itself an assumption that has to be made editable.

Pre-1958 ice-core CO₂ (e.g. Law Dome) could be shown as a separately labelled proxy line, off by default, kept visually distinct from the direct measurements.

## 5. Display

- Measured vs modelled on one chart per quantity, with residual (modelled − measured) underneath.
- Simple fit statistics: mean bias, RMSE, and trend comparison (model vs measured ppm/yr and °C/decade).
- Which assumptions were edited, from the parameter snapshot, so a fit can be reproduced.

## 6. Open questions

Question numbers are shared across the Carbon Cycle plans.

- **Q9** Spin-up: start the model run at 1750 using estimated historical emissions, or at 1958 from an assumed starting state? See §4.
- **Q10** Temperature comparison: start it at 1958 to match CO₂, or show the full instrumental record back to ~1850?
