# Carbon Cycle simulation — Web client

- **Date:** 2026-11-01 (split from the [simulation overview](2026-10-24-01-carbon-cycle-simulation-plan.md), written 2026-10-24)
- **Status:** Proposed. Partly built: units (§3) and the `/carbon-sim` page with a scenario carousel, two floating panels and a Chart.js gas chart in place of the output tabs and uPlot. No per-route render mode.
- **Author:** Patrick Lea (with Claude)
- **Scope:** `ClimateExplorer.Web.Client` (and `App.razor` in `ClimateExplorer.Web`): render-mode routing, uPlot charts through JS interop, units, the output tabs' layout and accessibility, parameters and URL state.
- **Builds on:** [Carbon Cycle simulation — Overview](2026-10-24-01-carbon-cycle-simulation-plan.md); [clock](2026-11-01-06-carbon-cycle-simulation-clock-plan.md) (the runner feeds the charts and readouts); [US customary units toggle](2026-10-21-02-us-customary-units-plan.md)
- **Branch context:** `development`

Section references without a plan name are to this document. "Overview" is the [Carbon Cycle page overview](2026-10-21-01-carbon-cycle-plan.md); the other simulation plans are named by their topic, as listed in the [simulation overview §3](2026-10-24-01-carbon-cycle-simulation-plan.md).

## 1. Render mode: WebAssembly, no prerender

The simulation loop runs in the browser. On the server over SignalR, every tick and chart update would cross the network.

- **A page-level `@rendermode` won't work here.** `App.razor` gives `<Routes>` (and `<HeadOutlet>`) a global `InteractiveAuto`. A page inside an already-interactive tree can't switch to another interactive mode.
- **Instead, `App.razor` chooses the mode per request.** A new `App.razor.cs` code-behind (per AGENTS.md) reads the cascaded `HttpContext`. It returns `new InteractiveWebAssemblyRenderMode(prerender: false)` for the Carbon Cycle page's routes and `InteractiveAuto` for everything else, and `<Routes>` and `<HeadOutlet>` both use it.
- **Arriving by in-app navigation** wouldn't re-run `App.razor`, so the reader could land on the page in a Server-mode session. Two guards stop this:
  - Links into the Carbon Cycle page from the rest of the site use a full page load (`forceLoad: true`).
  - The page checks `RendererInfo.Name` on first render, and reloads itself if it isn't running on WebAssembly.
- **Scope:** the whole Carbon Cycle page, input tabs included, because the run has to survive moving between tabs ([clock §2](2026-11-01-06-carbon-cycle-simulation-clock-plan.md)). Moving between its tabs is then ordinary in-app navigation. Q18 asks whether the input tabs should keep prerendered HTML instead.
- **First visit:** with no prerender, nothing shows until WebAssembly starts. `App.razor` renders a static "Loading the Carbon Cycle page…" placeholder for these routes. The page title isn't in the first HTML.

## 2. Charts: uPlot through a thin JS module

uPlot is small (~50 KB), MIT-licensed, and draws hundreds of thousands of points on canvas. The site's charts use Chart.js, which takes whole datasets on update and is too slow for streaming at this rate. (The first slice's gas chart is Chart.js; it moves to uPlot here.)

- **Asset:** a pinned uPlot 1.6.x release, vendored into `ClimateExplorer.Web.Client/wwwroot/lib/uplot/` with its licence. No CDN.
- **Module:** a collocated `SimulationChart.razor.js`. It holds the series arrays, and .NET never keeps a copy of a chart's history. Its API is minimal:
  - `create(element, options)` returns a handle. Options set the series, axes and colours, and the 0.75rem minimum canvas font (AGENTS.md).
  - `append(handle, points)` adds the new points.
  - Small helpers beside those two: `setVisible(handle, series, on)`, `setUnits(handle, series, scale, offset)` (§3), `annotate(handle, time, label)`, `setCursor(handle, time)` (the current time while rewound, [clock §3](2026-11-01-06-carbon-cycle-simulation-clock-plan.md)), `truncate(handle, time)` (drop points after a branch), `reset(handle)` and `destroy(handle)`.
- **Interop:** `[JSImport]`, available because the route is WebAssembly-only.
  - The delta is passed as a `Span<double>` marshalled as a `MemoryView`: the time column, then each series, flattened. This avoids JSON.
  - The call is synchronous, and the module copies the values before returning, because a `MemoryView` over a span is valid only during the call.
- **Deltas only:** the runner keeps a per-chart buffer of points since the last push and clears it after each push.
- **Long runs:** when a series passes a cap (default 20,000 points), the module halves its resolution. It keeps every event point and each bucket's minimum and maximum, so spikes and dips survive.
- **Annotations:** cut-offs, eruptions and other events are drawn by a uPlot draw hook as vertical lines with labels. uPlot has no built-in annotations.
- **Time axis:** linear or log (Level 2 toggle). Log suits side-by-side runs where the house is over in a minute and the Earth runs for millennia.
- **Layout per scenario panel:** a stack of small charts, one per group of shown quantities, each auto-scaling. Which groups appear depends on the tab (§4).
- **Compare view (Level 2, later):** one chart per quantity, with a line per scenario, plotted as change since the start, because the scenarios' absolute values differ by orders of magnitude.

## 3. Units

Readouts and chart axes follow the site's unit setting from the [US customary units plan](2026-10-21-02-us-customary-units-plan.md) (`UnitPreference`). The site's setting is US customary, not imperial: a US gallon, not an imperial one.

- **Converted:** temperature (°C ↔ °F), enclosure volume (m³ ↔ ft³), liquid fuel flow (L/s ↔ US gal/h), gas flow (L/s ↔ ft³/h), masses at human scale (kg ↔ lb), areas (m² ↔ ft²), pressure (kPa ↔ psi).
- **Unchanged, as in the units plan:** mole fractions (%, ppm, ppb), W/m², energy and power (J, W), and large masses (t, Mt, Gt).
- **Where conversion happens:**
  - The engine works in SI throughout, and readouts convert at display.
  - Charts receive SI, and the JS module applies each series' display transform (scale and offset).
  - When the setting changes, `setUnits` re-transforms the stored series in place, so no history is resent.
- **Readout format:** value, plus the change since the start, each auto-scaled (e.g. "O₂ 20.946% (−0.4 ppm)"). A tiny change on the Earth stays visible next to a large one in the house.

**Built (2026-10-31):** Core units `Kilograms`, `CubicMetres`, `Kilopascals`, `GasLitresPerSecond` and `LiquidLitresPerSecond`, converted by `UnitFormatter`. Gas flow is ft³/h and liquid flow gal/h, not per minute or per second, because US gas appliances and oil-burner nozzles are rated per hour ([simulation overview §6](2026-10-24-01-carbon-cycle-simulation-plan.md)).

## 4. Layout and accessibility

**Output tabs.** Each shows its aspect of every scenario in the run, on the same clock. Before the first play they show the scenarios at time 0, with a prompt to press play; with no input tab in use, the empty state (overview §2.1).

| Tab | Shows (Level 1; Level 2 adds the rest of the [tracked quantities](2026-10-24-01-carbon-cycle-simulation-plan.md)) |
|---|---|
| 4 Overview | Per scenario: the Level 1 readouts (O₂, CO₂, temperature, flame state) and one small chart from each of Tabs 5–7, each linking to its tab. The event log. |
| 5 Carbon, gases and rocks | O₂ and CO₂; where the carbon is (air, plants, rock, ocean surface and deep). Level 2: major gases (N₂, O₂, Ar), trace gases (CO₂, CH₄, CO, N₂O; log axis option), water, pressure, N₂ vented, fuel burned, stratospheric sulfate. |
| 6 Radiative forcing | Planet settings: forcing by agent over time, and the overview §8 bar chart at the current simulated time, attributable to species and activities (overview §1); see the [energy balance plan](2026-11-01-05-carbon-cycle-simulation-energy-balance-plan.md). Boxes: sunlight absorbed; ideal greenhouses: gas emissivity and infrared to the sky. |
| 7 Temperature | Gas and floor temperatures (boxes), surface temperature and ice cover (Earth), heat released, water and ice. |

**Clock bar.** At the top of every output tab, and in compact form on the input tabs once a run exists: rewind, play/pause, fast-forward, restart, the speed, the simulated time, and the volcano button when a planet setting is in the run ([clock §1](2026-11-01-06-carbon-cycle-simulation-clock-plan.md)). It stays in place as the reader scrolls (sticky), so the controls are always in reach.

- **Fullscreen:** the scenario panels in a grid, three across (two rows for six). **Tablet:** two across. **Mobile:** one panel at a time, chosen with Tabs, plus a compact table of every scenario's key readouts and flame state. The reader can hide scenarios, and the rest fill the space.
- **Controls** use the common components (`Button`, `DropdownButton`, `Collapsible`, `InfoPanel`, `Tooltip`, `Select`, `Tabs`). Each clock button is at least 44 px high on touch.
  - Each clock button has an accessible name that says what it will do ("Fast-forward: 1 day per second"). The speed and simulated time are text ("1 year per second", "Year 12, day 40").
  - The volcano button's accessible name says what it does and where ("Erupt 10 volcanoes on Basic Earth").
- **Charts:** each has an accessible name and a text alternative (its latest values and the event log).
- **Announcements:** readouts are not live regions, since five updates a second would flood a screen reader. Events, including eruptions, are announced once, through a polite live region ("Sealed house: flame out at 1 min 9 s, O₂ below 15%").
- **Text size:** nothing below 0.75rem, including uPlot's canvas axis labels and legends.

## 5. Parameters and URL state

- Every scenario parameter, cut-off and rate constant is an Assumption with an id (e.g. `sim.house.volume`, `sim.cutoff.o2`, `sim.weathering.beta`, `sim.eruption.pinatubo.so2`) in the overview's parameter system.
- The URL holds the event record ([clock §3](2026-11-01-06-carbon-cycle-simulation-clock-plan.md)), which scenarios and quantities are shown, and edited parameters. A shared link reproduces the run: it opens at time 0, paused, and play replays it. Speed and the current time are not stored.

## 6. Risks

- **No prerender** on the Carbon Cycle page means a blank first paint on a cold visit, covered by the placeholder (§1). Search engines see less of the input tabs' chemistry text (Q18).

## 7. Open questions

- **Q18** The whole Carbon Cycle page now runs on WebAssembly without prerendering, so the run survives moving between tabs (§1). Accept that the input tabs lose prerendered HTML (recommended, simplest), or keep the input tabs prerendered and hand the run over through URL state with a full page load to the output tabs?

## Summary

The page runs on WebAssembly without prerender, so the simulation loop is local. Charts are uPlot behind a small JS module that holds each series and takes deltas over `[JSImport]`. Readouts and axes follow the unit setting. Output Tabs 4–7 show every scenario on one clock, and the URL carries the event record and edited parameters.

### Change size

Rough, from the split; refine when this plan is next revised.

- **Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.Web`, `ClimateExplorer.Web.UnitTests`, `ClimateExplorer.Web.CarbonSim.UnitTests`

| Area | Added | Changed | Removed |
|---|---|---|---|
| Render mode: `App.razor.cs`, placeholder, renderer check | 1 file, +60 | 2 files, ~30 | — |
| uPlot vendored, `SimulationChart` and its JS module | 4 files, +450 (excl. vendored uPlot) | — | `GasChart` (4 files, −250) |
| Output tabs 4–7, scenario panels, event log, live region | 12 files, +900 | 3 files, ~100 | `FloatingPanel` use, ~60 |
| URL state and parameters | 2 files, +200 | 1 file, ~30 | — |
| Tests | 2 files, +250 | 1 file, ~20 | — |
| **Total** | **21 files, +1,860** | **7 files, ~180** | **4 files, −310** |
