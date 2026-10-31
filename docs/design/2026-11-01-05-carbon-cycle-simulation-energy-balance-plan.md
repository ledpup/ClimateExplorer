# Carbon Cycle simulation — Sunlight, forcing and the planet energy balance

- **Date:** 2026-11-01 (split from the [simulation overview](2026-10-24-01-carbon-cycle-simulation-plan.md), written 2026-10-24)
- **Status:** Proposed. Not built. The dead planet uses a stand-in, a "warm sky" with CO₂ forcing alone ([its plan §3](2026-10-31-01-dead-planet-cars-scenario-plan.md)). Next: revise this plan to add sunlight as an input and a forcing schema that starts with CO₂ and sunlight, so the dead planet can use it.
- **Author:** Patrick Lea (with Claude)
- **Scope:** The planet's zero-dimensional energy balance: sunlight in, albedo, outgoing longwave, climate feedback, heat capacity. Radiative forcing by agent (CO₂, CH₄, N₂O, halogenated gases, short-lived agents, stratospheric sulfate, land-use albedo). The daily TSI driver for planets and greenhouses.
- **Builds on:** [Carbon Cycle simulation — Overview](2026-10-24-01-carbon-cycle-simulation-plan.md); [engine](2026-11-01-01-carbon-cycle-simulation-engine-plan.md); [Carbon Cycle page overview §7](2026-10-21-01-carbon-cycle-plan.md) (forcing formulas)
- **Branch context:** `development`

Section references without a plan name are to this document. "Overview" is the [Carbon Cycle page overview](2026-10-21-01-carbon-cycle-plan.md); the other simulation plans are named by their topic, as listed in the [simulation overview §3](2026-10-24-01-carbon-cycle-simulation-plan.md).

## 1. Planet energy balance

A zero-dimensional energy balance on the ocean mixed layer:

```
C dT/dt = (TSI / 4) × (1 − α(T)) − [OLR₀ + λ (T − T_ref) − F(CO₂, CH₄, sulfate)]
```

- **Forcing `F`:**
  - CO₂ and CH₄ use simple published forms: logarithmic in CO₂ (5.35 ln(C/C₀) W/m² as the starting point), square-root in CH₄ (overview §7). Newer forms (AR6) can replace them; the Radiative forcing tab (Tab 6) shows whichever is in use.
  - N₂O (square-root, with its CH₄ overlap) and halogenated gases (linear) as in overview §7.
  - Short-lived agents from the input tabs (ozone, aerosols, contrails, black carbon on snow) track the current emission rate through per-species coefficients (overview Q8). Land-use albedo comes from the area converted.
  - Stratospheric sulfate ([planet §1.3](2026-11-01-04-carbon-cycle-simulation-planet-plan.md)) adds a negative forcing in proportion to its burden (coefficient to source; Pinatubo's peak was a few W/m²).
- **`λ`** is set so that doubling CO₂ gives an editable equilibrium warming (default ~3 °C, the AR6 best estimate). The ice-albedo part is taken out of `λ` and modelled through `α(T)` instead.
- **`α(T)`** is a smooth step from an ice-free albedo (~0.3) to an ice-covered one (~0.6) over a temperature range, in the style of Budyko and Sellers. This is what allows a snowball state ([planet §1.2](2026-11-01-04-carbon-cycle-simulation-planet-plan.md)).
- **`C`** is a ~100 m ocean mixed layer. A deep-ocean heat layer is left out of v1, so the warming responds faster than it really does. A two-layer response (mixed layer and deep ocean) is the next step, shown on the Temperature tab (Tab 7, overview §7).
- **`T_ref` and `OLR₀`** are calibrated so that preindustrial CO₂ is in balance at the preindustrial temperature.

**The Radiative forcing tab (Tab 6)** shows forcing by agent over time for the Earth settings, and the overview §8 bar chart at the current simulated time, attributable to species and activities (overview §1). Its layout is in the [web client plan §4](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md).

**The dead planet** uses this balance with the sun and albedo replaced by a constant that balances heat to space at the start, `λ` = 3.2 W/(m²·K) (the Planck response alone), and `C` the air alone ([dead planet plan §3](2026-10-31-01-dead-planet-cars-scenario-plan.md)).

## 2. Daily total solar irradiance

**Already in ClimateExplorer.** The "Total solar irradiance" dataset (`E45293F9-B7AC-4874-9544-25E006B6B998`) is the NOAA NCEI Total Solar Irradiance CDR observed composite: daily, in W/m², from late 1978 to 2025-06-30. No new ingestion is needed. The local file name still says `v02r01`, while the download URL and the file's header and end date are `v03r00`. Renaming it is a one-line tidy-up, out of scope here.

Use:
- **Greenhouses (scenarios 2–4):** insolation at the greenhouse's latitude (default to choose; editable) from the day's TSI, the solar declination and the hour angle ([boxes §3](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md)).
  - At steps under about an hour it follows the time of day, so day and night show. At longer steps it uses the standard daily-mean insolation formula.
  - TSI is reported at 1 AU, so the Earth–Sun distance factor (±3.4% over the year) is applied.
- **Earth (scenarios 5–6):** TSI ÷ 4, the global-mean top-of-atmosphere sunlight.
- **Averaging over a step:** at long steps, the driver is the mean TSI over the step, computed from prefix sums, so it costs the same at any step size.
- **Outside the record** (Q19): by default, loop the last whole solar cycles in the record (cycles 22–24, ~1986–2019). The record's mean is an alternative. At 1,000 yr/s, the 0.1% solar cycle is invisible anyway.
- **Loading:** the client fetches the series (~17,000 values) once, through the existing WebApi dataset endpoint, and caches it for the session.

The start date that picks the TSI value is part of a scenario's set-up event ([scenarios §1](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md)).

## 3. Tests

- Doubling CO₂ gives the configured equilibrium warming.
- Preindustrial CO₂ at the preindustrial temperature is in balance.
- With ice-albedo on, a cold enough state stays frozen.
- TSI: the mean over a long step equals the record mean over that span.
- The daily-mean insolation formula matches a published value at a test latitude and date.

## 4. Open questions

- **Q19** TSI outside 1978–2025: loop the last whole solar cycles (recommended), or hold the mean? And should runs that start before 1978 (e.g. from 1750) bring in the CDR's modelled TSI (NRLTSI2: daily from 1882, annual from 1610)? That would be a new dataset.

## 5. Risks

- **The logarithmic CO₂ forcing** is fitted for roughly 180–1,000+ ppm and fails as CO₂ nears zero (Earth without volcanoes). The engine needs a floor or a different form at very low CO₂.
- **The ice-albedo step** is a tuning choice that decides when a snowball happens.

Each gets a Level 2 note.

## Summary

The planet's temperature: sunlight in (TSI ÷ 4, less what the albedo reflects), heat out to space growing with the warming, and radiative forcing by agent holding some of it in. The daily TSI record drives the sunlight for planets and greenhouses. Tab 6 shows the forcing by agent.

### Change size

Rough, from the split; refine when this plan is next revised.

- **Projects:** `ClimateExplorer.CarbonCycle`, `ClimateExplorer.Web.Client`, the unit test projects

| Area | Added | Changed | Removed |
|---|---|---|---|
| Engine: energy balance, forcing agents, albedo | 3 files, +350 | 1 file (`DeadPlanet`), ~60 | — |
| Engine: TSI driver, insolation | 2 files, +250 | — | — |
| Web: fetch and cache the TSI series | 1 file, +60 | 2 files, ~20 | — |
| Tests | 2 files, +300 | 1 file, ~30 | — |
| **Total** | **8 files, +960** | **4 files, ~110** | — |
