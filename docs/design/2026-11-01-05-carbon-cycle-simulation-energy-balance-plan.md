# Carbon Cycle simulation — Sunlight, forcing and the planet energy balance

- **Date:** 2026-11-01 (split from the [simulation overview](2026-10-24-01-carbon-cycle-simulation-plan.md), written 2026-10-24; revised 2026-11-01: sunlight as an input, a physical start temperature, effective radiative forcing by agent)
- **Status:** Proposed. First part partly built (2026-11-01): sunlight, the energy budget, CO₂, solar and albedo ERF, and the dead planet moved onto them, in the engine; the charts and the slide text are still to do. See [Planet energy budget and forcing — implementation](2026-11-01-08-carbon-cycle-planet-forcing-implementation-plan.md).
- **Author:** Patrick Lea (with Claude)
- **Scope:** The planet's zero-dimensional energy balance: sunlight, albedo, the greenhouse effect, heat to space, feedbacks and heat capacity. Effective radiative forcing (ERF) by agent, and what isn't a forcing. The daily TSI driver for planets and greenhouses. What the Radiative forcing tab (Tab 6) shows. The physics only; the code is in the implementation plan.
- **Builds on:** [Carbon Cycle simulation — Overview](2026-10-24-01-carbon-cycle-simulation-plan.md); [engine](2026-11-01-01-carbon-cycle-simulation-engine-plan.md); [A billion cars on a dead planet](2026-10-31-01-dead-planet-cars-scenario-plan.md) (its "warm sky" is replaced here); [Carbon Cycle page overview §7](2026-10-21-01-carbon-cycle-plan.md) (forcing formulas)
- **Branch context:** `development`

Section references without a plan name are to this document. "Overview" is the [Carbon Cycle page overview](2026-10-21-01-carbon-cycle-plan.md); the other simulation plans are named by their topic, as listed in the [simulation overview §3](2026-10-24-01-carbon-cycle-simulation-plan.md). Numbers marked "to verify" or "to source" are provisional.

## 1. Sunlight

A planet setting has one sunlight input, the **solar constant** `S`: the sunlight arriving at the planet's distance from the sun, on a surface facing it, in W/m². Default 1361 W/m², today's (Kopp and Lean 2011). Like the albedo, it's a setting input with no control on screen yet; a later input or scenario can change it, for a fainter sun or a planet nearer the sun.

- **Why ÷ 4.** The planet catches sunlight on a disc (πr²) but has four times that area (4πr²) to spread it over and radiate from. So the average sunlight per square metre of the planet is `S/4`, ~340 W/m². This is how a single-temperature model allows for the planet being a sphere.
- **What's left out:** day and night, seasons and latitude. The planet has one temperature, the global mean. Day and night would matter for a box (the greenhouses), and the TSI driver (§6) handles them there.
- **Albedo** `α` is the share of the sunlight reflected straight back to space. It's a setting parameter; the dead planet takes Earth's, 0.30 (CERES measures ~0.29; to source). Earth's albedo is about half clouds, which a dry planet wouldn't have; the scenario text says so. The reader can't change it on screen yet, but the setting carries it, so a later input or scenario can.
- **Constant or recorded.** The sunlight is either the constant `S` (planets for now) or the daily TSI record (§6), for Earth runs on real dates and for the greenhouses.

## 2. Energy budget

Each term is shown to the reader, in W/m² of the planet's surface:

```
incoming sunlight     I   = S / 4
reflected                   α I
absorbed sunlight     ASR = (1 − α) I
heat to space         OLR = (1 − g(T)) σ T⁴ − F_GHG
imbalance             N   = ASR − OLR
temperature           C dT/dt = N + H
```

- **`σT⁴`** is what the surface would radiate at temperature `T` (Stefan–Boltzmann). Without an atmosphere, all of it would reach space.
- **`g`, the normalised greenhouse effect** (Raval and Ramanathan 1989): the share of the surface's heat radiation that the atmosphere keeps from reaching space, at the reference gases. Earth's is ~0.39 (σT⁴ ≈ 391 W/m² at 15 °C against ~238 W/m² reaching space; to verify against CERES).
- **`F_GHG`** is the forcing from the greenhouse gases' change since the reference state (§3). It lowers the heat reaching space.
- **`H`** is non-radiative heat: the dead planet's engines (§4).
- **`C`** is the heat capacity per square metre: the air alone on the dead planet, a ~100 m ocean mixed layer on Earth. A deep-ocean heat layer is left out of v1, so Earth's warming responds faster than it really does. A two-layer response is the next step, shown on the Temperature tab (Tab 7, overview §7).

**The start temperature comes from the physics.** A setting starts in balance (`N = 0`) at its reference gases, so its start temperature is the one where the heat to space equals the absorbed sunlight:

```
T₀ = ( ASR / ((1 − g₀) σ) )^¼
```

It isn't set by hand. A brighter sun, a darker planet or more greenhouse effect each give a warmer start.

**Climate response.** As `T` rises, `σT⁴` rises, so more heat reaches space; that is what brings the planet back to balance. The rate is the climate feedback `λ = −dN/dT`:
- **Planck response alone** (no water, as on the dead planet): `g` is fixed, and `λ = 4 (1 − g₀) σ T³ = 4 · OLR / T`. At Earth's temperature this is ~3.3 W/(m²·K), close to AR6's Planck response (3.2).
- **With water vapour, lapse rate and clouds** (Earth): `g` rises as the planet warms, `g(T) = g₀ + γ (T − T₀)`, because warmer air holds more water vapour. `γ` is set so that doubling CO₂ gives an editable equilibrium warming (default 3 °C, AR6's best estimate).
- **Ice and snow** change the albedo, not `g`: `α(T)` is a smooth step from an ice-free albedo (~0.3) to an ice-covered one (~0.6) over a temperature range, in the style of Budyko and Sellers. This is what allows a snowball state ([planet §1.2](2026-11-01-04-carbon-cycle-simulation-planet-plan.md)).

The step from today's model: the planet loses `OLR₀ + λ (T − T_ref)`, calibrated so that the start is in balance at a chosen temperature. That gave the right changes but not the start temperature. The form above gives both.

## 3. Effective radiative forcing

**Forcing** is a change in the energy budget caused by something other than the planet's temperature, measured against a **reference state**. **Effective** radiative forcing (ERF) includes the fast adjustments of the atmosphere (the stratosphere cooling, clouds shifting) that happen before the surface warms, and is what AR6 uses. It predicts the eventual warming better than the instantaneous forcing does.

**Reference state.** Each setting names one. The dead planet's is its start. Earth's is 1750, so its forcing by agent lines up with AR6's chart (AR6 WG1 Figure SPM.2 / Ch. 7; to verify the figure).

**Agents.** Each agent has an id, a name, a kind, a formula, a source, and whether it's built. The schema has room for every agent the overview §7 lists; v1 builds three.

| Agent | Kind | ERF (W/m²) | v1 |
|---|---|---|---|
| CO₂ | Greenhouse gas | `(3.93 / ln 2) ln(C / C₀)` ≈ `5.67 ln(C / C₀)`: AR6's 3.93 per doubling (Forster et al. 2021; to verify). AR6's full form (Meinshausen et al. 2020) adds a small N₂O overlap; it can replace this later. | ✓ |
| Solar | Sunlight | `(S − S₀)/4 × (1 − α)` (AR6 applies a small adjustment; to verify) | ✓ |
| Albedo | Surface and clouds | `−(S₀/4)(α − α₀)`; with the solar term, it sums exactly to the change in absorbed sunlight | ✓ (zero until the albedo changes) |
| CH₄ | Greenhouse gas | square-root form, with its N₂O overlap (overview §7) | later |
| N₂O | Greenhouse gas | square-root form, with its CH₄ overlap (overview §7) | later |
| Halogenated gases | Greenhouse gas | linear in each gas's amount (overview §7) | later |
| Short-lived agents (ozone, aerosols, contrails, black carbon on snow) | Various | per-species coefficients on the current emission rate (overview Q8) | later |
| Stratospheric sulfate | Aerosol | negative, in proportion to the burden ([planet §1.3](2026-11-01-04-carbon-cycle-simulation-planet-plan.md); coefficient to source) | later |
| Land-use albedo | Surface | from the area converted | later |

- **Greenhouse gases** act through the heat to space (`F_GHG` in §2). **Solar and albedo** act through the absorbed sunlight, so they're computed from `ASR` itself and reported as ERF for the chart; they're not added twice.
- **The total ERF** is the sum. At constant sunlight and albedo, the solar and albedo forcings are 0, and they're shown as 0, so the reader sees that the sun isn't the cause.
- **Energy in and out.** When CO₂ rises, heat to space falls and `N` goes positive. The planet warms until heat to space has climbed back to match the sunlight, at a higher temperature. With the air alone to heat, this takes weeks, so on the dead planet energy in and out stay almost equal throughout while the temperature follows the forcing. On Earth the ocean slows it to decades, and today's imbalance is ~1 W/m².

## 4. What isn't a forcing

- **Water vapour is a feedback, not a forcing.** The air's water content is set by its temperature, and extra water rains out within days. So the water from burning fuel has no lasting effect, and on Earth water vapour acts through `γ` (§2). (AR6 counts one small exception, water vapour made in the stratosphere by methane oxidation; it comes with CH₄.)
- **Ice, snow and clouds** are feedbacks too, through `α(T)` and `γ`.
- **The engines' heat** is non-radiative heat (`H` in §2), not a radiative forcing. It's shown beside the forcings, so the reader can compare them, but not in their total. AR6 leaves waste heat out of its forcings for the same reason.
- **The dead planet has no water** to feed back, so it has only the Planck response and warms ~1 °C per doubling, about a third of Earth's.

## 5. The dead planet under this model

The dead planet's "warm sky" ([its plan §3](2026-10-31-01-dead-planet-cars-scenario-plan.md)) is replaced by sunlight. It starts in balance at its own temperature. Provisional numbers, to verify in the engine's tests:

| Quantity | Value | Basis |
|---|---|---|
| Absorbed sunlight | 238 W/m² | 1361 / 4 × (1 − 0.30) |
| Temperature with no greenhouse effect | 255 K (−18.6 °C) | `(ASR / σ)^¼` |
| Greenhouse effect at the start, `g₀` | 0.10 (~26 W/m²) | CO₂ alone in dry air, no water vapour or clouds. To source, e.g. from line-by-line studies of CO₂'s share of the greenhouse effect (Schmidt et al. 2010) |
| Start temperature | ~261 K (−12 °C) | §2 |
| Planck response | ~3.6 W/(m²·K) | `4 × 238 / 261` |
| Warming per doubling of CO₂ | ~1.1 °C | 3.93 / 3.6 |
| After 100 years (~1,440 ppm CO₂) | ERF ~7.0 W/m², ~−10 °C | |
| When the engines stop (~4.2% CO₂, ~3,900 years) | ERF ~26 W/m² (extrapolated, Q24), ~−5 °C | |
| Engines' heat | 0.077 W/m² | unchanged |

What this changes for the scenario:
- **`g₀` is the one number that sets the start temperature**, and it comes from radiative-transfer studies, not from this model. A zero-dimensional model can't derive a greenhouse effect; it can only take one. The scenario text names it and its source.
- **The planet stays below freezing** throughout. The cars' water falls as frost and snow, not rain, and its latent heat includes freezing. There's very little of it: ~0.3 mm spread over the planet by the time the engines stop, too little to change the albedo.
- **The warming is still clear**: ~2 °C a century at first, ~7 °C in all. The engines' own heat is worth ~0.02 °C, as before.
- **"No heat to space"** (`ClimateFeedback = 0` today) goes. It was kept only as a setting, and it has no place in a model where heat to space follows from the temperature.

## 6. Daily total solar irradiance

**Already in ClimateExplorer.** The "Total solar irradiance" dataset (`E45293F9-B7AC-4874-9544-25E006B6B998`) is the NOAA NCEI Total Solar Irradiance CDR observed composite: daily, in W/m², from late 1978 to 2025-06-30. No new ingestion is needed. The local file name still says `v02r01`, while the download URL and the file's header and end date are `v03r00`. Renaming it is a one-line tidy-up, out of scope here.

Use:
- **Greenhouses (scenarios 2–4):** insolation at the greenhouse's latitude (default to choose; editable) from the day's TSI, the solar declination and the hour angle ([boxes §3](2026-11-01-03-carbon-cycle-simulation-boxes-plan.md)).
  - At steps under about an hour it follows the time of day, so day and night show. At longer steps it uses the standard daily-mean insolation formula.
  - TSI is reported at 1 AU, so the Earth–Sun distance factor (±3.4% over the year) is applied.
- **Earth (scenarios 5–6):** `S` in §1 is the day's TSI, so its changes show as solar ERF against 1750.
- **Averaging over a step:** at long steps, the driver is the mean TSI over the step, computed from prefix sums, so it costs the same at any step size.
- **Outside the record** (Q19): by default, loop the last whole solar cycles in the record (cycles 22–24, ~1986–2019). The record's mean is an alternative. At 1,000 yr/s, the 0.1% solar cycle is invisible anyway.
- **Loading:** the client fetches the series (~17,000 values) once, through the existing WebApi dataset endpoint, and caches it for the session.

The start date that picks the TSI value is part of a scenario's set-up event ([scenarios §1](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md)).

## 7. The Radiative forcing tab (Tab 6)

For planet settings, three views. How they're drawn is in the implementation plan and the [web client plan §2](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md).

1. **ERF by agent now:** one horizontal bar per agent at the current simulated time, warming to the right of zero and cooling to the left, with the total at the bottom, as in AR6's chart. Agents that aren't built aren't shown. The engines' heat is a separate bar below a divider, labelled as not a forcing.
2. **ERF over time:** a line per agent and the total.
3. **Energy budget over time:** absorbed sunlight, heat to space and the imbalance. The first two stay close together while the temperature climbs, which is the point to see.

Readouts beside them: the temperature, the absorbed sunlight, the heat to space, the imbalance, the total ERF and the warming per doubling of CO₂. Earth settings later add forcing attributed to the input tabs' activities (overview §1). Box settings show sunlight absorbed and, for ideal greenhouses, gas emissivity and infrared to the sky.

## 8. Tests

- The start is in balance (`N = 0`), and its temperature is `(ASR / ((1 − g₀) σ))^¼`.
- More sunlight, a lower albedo or a larger `g₀` each give a warmer start.
- CO₂ ERF is 3.93 W/m² per doubling and 0 at the reference.
- At constant sunlight and albedo, the solar and albedo ERF are 0. Raising `S` by `ΔS` gives a solar ERF of `ΔS/4 × (1 − α)`.
- With a constant forcing `F` and the Planck response alone, the planet settles where `N = 0`, warmer by ~`F / λ`.
- Earth: doubling CO₂ gives the configured equilibrium warming; preindustrial CO₂ in 1750 is in balance at the start temperature; with ice-albedo on, a cold enough state stays frozen.
- TSI: the mean over a long step equals the record mean over that span. The daily-mean insolation formula matches a published value at a test latitude and date.

## 9. Open questions

- **Q19** TSI outside 1978–2025: loop the last whole solar cycles (recommended), or hold the mean? And should runs that start before 1978 (e.g. from 1750) bring in the CDR's modelled TSI (NRLTSI2: daily from 1882, annual from 1610)? That would be a new dataset.
- **Q24** CO₂ above the forcing formula's range. The logarithmic forms are fitted to roughly 180–2,000 ppm (to verify), and the dead planet passes 2,000 ppm after ~160 years. At percent levels, CO₂'s weaker bands make the forcing grow faster than logarithmically (to verify, e.g. Byrne and Goldblatt 2014). Recommended: v1 extrapolates the log form and marks readouts "beyond the formula's range" past the limit; a later step adopts a published high-CO₂ fit.

## 10. Risks

- **CO₂ below preindustrial.** The logarithmic CO₂ forcing fails as CO₂ nears zero. The simulator is about the warming people cause, not paleoclimate, so it doesn't go below preindustrial CO₂ (280 ppm): `RadiativeForcing` takes lower amounts as 280 ppm. Within that range, the model uses straightforward physics, chemistry and biology wherever they apply. This bounds Earth without volcanoes ([planet §1.2](2026-11-01-04-carbon-cycle-simulation-planet-plan.md), Q25).
- **`g₀` for a dry planet** is an estimate from Earth studies, which include water vapour's overlap with CO₂. A dry atmosphere's CO₂ greenhouse effect also depends on its temperature profile, which one temperature can't represent. The start temperature is only as good as this number, and the text says so.
- **The ice-albedo step** is a tuning choice that decides when a snowball happens. It's for the Earth settings only; the dead planet has no ice-albedo.
- **Readers comparing the dead planet with Earth.** Its warming per doubling is about a third of Earth's, because it has no water vapour feedback. The scenario text says why.

Each gets a Level 2 note.

## Summary

The planet's temperature follows from its energy budget: absorbed sunlight in, heat to space out, with the greenhouse effect holding some of it back. Sunlight (the solar constant) is an input and the albedo a setting. The start temperature isn't set by hand: each setting starts in balance at its own temperature. Effective radiative forcing is reported by agent against a reference state, CO₂, solar and albedo first, in a schema that takes the other agents later; water vapour, ice and clouds are feedbacks, and the engines' heat is a non-radiative term beside them. The dead planet moves onto this model: it starts at about −12 °C in balance with the sun, stays below freezing, and warms ~2 °C a century as the cars add CO₂.

### Change size

The first part (sunlight, the energy budget, CO₂, solar and albedo ERF, the dead planet and its forcing chart) is sized in the [implementation plan](2026-11-01-08-carbon-cycle-planet-forcing-implementation-plan.md). The rest, rough, from the split:

- **Projects:** `ClimateExplorer.CarbonCycle`, `ClimateExplorer.Web.Client`, the unit test projects

| Area | Added | Changed | Removed |
|---|---|---|---|
| Engine: Earth feedbacks (`γ`, `α(T)`), the other forcing agents | 2 files, +250 | 2 files, ~60 | — |
| Engine: TSI driver, insolation | 2 files, +250 | — | — |
| Web: fetch and cache the TSI series | 1 file, +60 | 2 files, ~20 | — |
| Tests | 2 files, +250 | — | — |
| **Total** | **7 files, +810** | **4 files, ~80** | — |
