# Carbon Cycle simulation — Run clock, rewind and the event record

- **Date:** 2026-11-01 (split from the [simulation overview](2026-10-24-01-carbon-cycle-simulation-plan.md), written 2026-10-24)
- **Status:** Proposed. Partly built: a clock bar (play/pause, restart, speed stops up to 100 years per second) with the loop in the page, not a `SimulationRunner` service. No rewind, record or fast-forward button.
- **Author:** Patrick Lea (with Claude)
- **Scope:** The clock (speeds and controls), `SimulationRunner` and its loop, rewind and replay over recorded samples, checkpoints and branching, the event record, and saving and opening a run as JSON.
- **Builds on:** [Carbon Cycle simulation — Overview](2026-10-24-01-carbon-cycle-simulation-plan.md); [engine](2026-11-01-01-carbon-cycle-simulation-engine-plan.md); [scenarios](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md)
- **Branch context:** `development`

Section references without a plan name are to this document. "Overview" is the [Carbon Cycle page overview](2026-10-21-01-carbon-cycle-plan.md); the other simulation plans are named by their topic, as listed in the [simulation overview §3](2026-10-24-01-carbon-cycle-simulation-plan.md).

## 1. Speed and clock controls

**Speed.** Simulated time per real second, in logical stops: real time, 1 min/s, 1 h/s, 1 day/s, 1 week/s, 1 month/s, 1 yr/s, 10, 100 and 1,000 yr/s. Each tick's step is speed × the real time since the last tick (§2). The readout shows the speed and the step in use. The engine takes any step ([engine §2](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)).

**Clock controls.** One clock for every scenario in the run, in a bar shown on every output tab ([web client §4](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md)):

| Control | What it does |
|---|---|
| **Play / pause** | Starts or stops the clock. Play from time 0 starts the run. |
| **Fast-forward** | Each press moves the speed up one stop. Past the top stop it returns to real time. A `DropdownButton` beside it lists every stop for a direct choice. |
| **Rewind** | Runs the clock backwards through what has been recorded (§3), at the speed stops. Each press moves up one stop. It pauses at time 0. |
| **Restart** | Goes back to time 0 and pauses. The event record is kept, so play runs the same run again, including the eruptions. "Clear added events" (Level 2) removes the events added during the run. |
| **Step once / run to next event** | While paused. |

Real burning can't run backwards (`CO₂` doesn't turn back into coal and O₂), but a simulation can be wound back to look again. Level 1 text says so.

**Changing the inputs.** Editing an input tab, an include toggle or a parameter changes the starting events, so the run goes back to time 0 and pauses. The events added during the run are kept, at their times.

## 2. The loop: sim rate apart from redraw rate

One `SimulationRunner` (client, `UiLogic`) owns all the scenarios, the clock, the event record and one timer. It is a scoped service registered in `ClimateExplorer.Web.Client` (and in `ClimateExplorer.Web`, per AGENTS.md), so it lives above the tabs: the run keeps playing while the reader moves between tabs, and each output tab attaches its charts to it when shown.

| Activity | Rate | What it does |
|---|---|---|
| Sim tick | ~60 Hz (`PeriodicTimer`, 16 ms) | Measures real time since the last tick (`Stopwatch`), works out `dt` from the speed and direction, applies any pending input events, and calls `Advance` on every scenario with the same `dt`, so they stay in sync. Records a sample per scenario, plus one at every event, and checkpoints (§3). When rewinding or replaying recorded time, it moves the clock through the samples instead of calling `Advance`. |
| Chart push | 10–15 Hz | For each chart, one interop call carrying only the points added since its last push ([web client §2](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md)). No `StateHasChanged`. |
| Readouts | 4–5 Hz | Copies the latest values to the readout components and calls `StateHasChanged` on them alone. Events also trigger one, so the flame-out shows at once. |

- **Budget:** a tick spends at most ~8 ms on stepping. Past that, the runner reduces the speed actually achieved, for all scenarios together, and the speed readout shows "requested / achieved".
- **Background tab:** browsers slow timers in hidden tabs. A tick's `dt` is capped at the step for ~250 ms of real time, so returning to the tab doesn't cause one giant step. The sim keeps running at the reduced rate.
- **Only the readouts re-render.** Chart components render once and return `ShouldRender() => false`; data reaches them only through JS. The page itself doesn't re-render on ticks.
- **Only the shown tab's charts get pushes.** A tab's charts are created when it is shown and filled from the runner's history; hidden tabs cost nothing to draw.
- **Single thread:** WebAssembly .NET runs on the browser's main thread, so stepping, interop and rendering share it. Multithreaded WebAssembly is not relied on. If the budget proves too tight, the options are ahead-of-time compilation, then a Web Worker ([engine §8](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)).
- **Disposal:** switching tabs destroys that tab's charts but not the run. Leaving the Carbon Cycle page stops the timer and drops the run; its record is still in the URL.

## 3. Rewind and the event record

**Rewind is a replay, not reverse chemistry.** Forward Euler can't be run backwards, and a cut-off loses information. So the runner keeps what it has computed:
- **Samples:** the chart and readout values at every tick (the charts' own history, [web client §2](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md)).
- **Checkpoints:** the full `SimulationState` of every scenario, every N ticks and at every input event. For long runs they are thinned like the chart points, keeping those at events.

Rewinding moves the clock back through the samples; readouts and charts follow it, and a cursor on each chart marks the current time. Playing forward over time already computed replays the samples exactly. Computing resumes from the furthest time reached.

**Adding an event in the rewound past** starts a new branch. The record and samples after that time are dropped. The state is restored from the latest checkpoint at or before it, advanced to the exact time, and the event applied (Q23).

**The event record** is every input event of the run: each scenario's starting events plus every event added during it, with its simulated time and scenario ([scenarios §6](2026-11-01-02-carbon-cycle-simulation-scenarios-plan.md)). With the parameter set, it reproduces the run ([engine §6](2026-11-01-01-carbon-cycle-simulation-engine-plan.md)). Outcome events are not stored; replaying finds them again.

**Save and send.**
- The record and edited parameters go in the shared URL (overview §3.3, [web client §5](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md)). A link opens paused at time 0 with the record loaded; play replays it.
- "Save run" downloads the same thing as a JSON file, and "Open run" loads one. This is the fallback when a long record doesn't fit in a URL.
- The saved form has a version number, so records made now still open after settings and events are added.

## 4. Tests

- Rewinding then playing over recorded time gives exactly the recorded samples.
- An event added at a rewound time drops the later record and samples, and the run continues from that time.
- A record round-trips through the URL form and the JSON file unchanged; replaying it finds the same outcome events, within tolerance.
- Restart keeps the added events; "clear added events" leaves only the starting events.

Client pieces (runner rates, delta buffering, decimation) are tested where they are plain C#, in `ClimateExplorer.Web.CarbonSim.UnitTests`. The JS module and charts aren't covered by browser tests (AGENTS.md).

## 5. Risks

- **Memory** for very long runs is bounded by the decimation cap ([web client §2](2026-11-01-07-carbon-cycle-simulation-web-client-plan.md)) and the checkpoint thinning (§3). Rewinding a thinned stretch and branching there re-advances from an older checkpoint, which takes longer.
- **Long records in a URL.** Many added events can make a link too long; "Save run" is the fallback (§3).

## 6. Open questions

- **Q23** Adding an event after rewinding: drop everything after it and branch (recommended, §3), or keep the later added events and run them again after the new one?

## Summary

One clock for every scenario in a run, from real time to 1,000 years per second, run by a `SimulationRunner` that steps at ~60 Hz and redraws less often. The runner keeps samples and checkpoints, so the reader can rewind, replay and branch; the input events form a record that can be saved, sent and replayed.

### Change size

Rough, from the split; refine when this plan is next revised.

- **Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.Web`, `ClimateExplorer.Web.CarbonSim.UnitTests`

| Area | Added | Changed | Removed |
|---|---|---|---|
| Web: `SimulationRunner`, samples, checkpoints, record, JSON save/open | 5 files, +600 | — | — |
| Web: clock bar (rewind, fast-forward, step), page loop moved into the runner | 2 files, +150 | 4 files, ~150 | — |
| DI registration (both `Program.cs`) | — | 2 files, ~4 | — |
| Tests | 3 files, +400 | — | — |
| **Total** | **10 files, +1,150** | **6 files, ~154** | — |
