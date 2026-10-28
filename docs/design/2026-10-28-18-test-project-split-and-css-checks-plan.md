# Test Project Split, CI Test Steps and CSS Class Checks

- **Date:** 2026-10-28
- **Status:** Implemented 2026-10-28 (see addendum). Build and unit tests are clean. The workflows and the UI are
  unverified.
- **Author:** Claude
- **Scope:** `ClimateExplorer.UnitTests` (split into three projects), `ClimateExplorer.sln`,
  `ClimateExplorer.WebApi/Properties/AssemblyInfo.cs`, `.github/workflows/master_api-climateexplorer.yml`,
  `.github/workflows/master_web-climateexplorer.yml`, CSS in `ClimateExplorer.Web.Client` and `ClimateExplorer.Web`,
  `AGENTS.md`, `CONTEXT.md`, `.github/copilot-instructions.md`
- **Builds on:** [17 Blazorise package removal and renames](2026-10-28-17-blazorise-package-removal-and-renames-plan.md).
  Its step 3 dropped the `climate-` prefix from ~43 CSS classes, and the build couldn't check that rename.
- **Branch context:** `issues/custom-components`

## Problem

1. **One test project for everything.** `ClimateExplorer.UnitTests` references the API, the web client, Core and four
   data projects. It has 98 test files and 980 tests. Neither deployment runs any of them: both workflows build,
   publish and deploy without a test step.
2. **CSS class names aren't checked.** A class name is a plain string in markup, C# and CSS. A half-done rename, or a
   rule left behind after its markup is deleted, compiles cleanly and quietly stops styling anything. Two prototype
   scans of the current code show this has already happened:
   - **23 of 440 class selectors** match nothing in any `.razor`, `.cs` or `.js` file. 19 are dead, left over from
     earlier refactors and the Blazor template. The other 4 are built by string interpolation.
   - **12 rules in scoped `.razor.css` files** style a class that the component passes to a child component
     (`<Button Class="today-btn">`) without `::deep`. Blazor puts the scope attribute only on the component's own
     elements, so these rules never apply. Examples: `.add-series-button` (`ChartView`), `.today-btn`/`.sort-toggle-btn`
     (`ClimateRecords`), `.record-link` (`Locations`).

## Approach

Split the tests along the deployment boundary, so each workflow runs the tests for what it deploys. Then add the
CSS checks as unit tests in the new web test project. That uses the same source-scanning approach as the existing
`UnitTextArchitectureTests`, and needs no new tooling (the repo has no Node toolchain, so PurgeCSS/Stylelint would
mean adding one). The checks are tests, not part of the build: `dotnet test` is already the verification step, and
failing the build over CSS while mid-edit would get in the way.

## Step 1 — Split `ClimateExplorer.UnitTests` into three projects

The user asked for at least two, one for the API and one for the web. Core is shared, and both apps deploy it, so its
tests get a third project that both workflows run. With only two projects, Core's tests would run in just one
workflow, or both workflows would need to run both projects.

| Project | References | Files | Lines |
|---|---|---|---|
| `ClimateExplorer.Core.UnitTests` | Core | 28 | ~4,200 |
| `ClimateExplorer.WebApi.UnitTests` | Core, WebApi, Data.Downloading, Data.Ghcnd, Data.Ghcnm, DataPipeline | 30 | ~5,400 |
| `ClimateExplorer.Web.UnitTests` | Core, Web.Client, WebApiClient | 40 | ~9,800 |

A file goes to the most specific project among those it uses: Web if it uses `ClimateExplorer.Web.*` or `WebApiClient`,
otherwise WebApi if it uses `WebApi` or any `Data.*`/`DataPipeline` project, otherwise Core.

- **Core:** `AnomalyCalculatorTests`, `BinIdentifierTests`, `Co2RegressionTests`, `CustomTransformationParserTests`,
  `DataFileSourceTests`, `DataReaderTests`, `DataRecordSerializationTests`, `DataSetBuilderTests`,
  `DataSetDefinitionsBuilderTests`, `DateHelpersTests`, `EnumsDayOfYearIfFrostLabelTests`, `GroupByTests`,
  `HeatingScoreTests`, `LocationTemperatureTrendsSerializationTests`, `MeteorologicalSeasonCalculatorTests`,
  `PolynomialRegressionCalculatorPolynomialTests`, `PolynomialRegressionCalculatorTests`, `PolynomialRootFinderTests`,
  `RecentObservationComparisonTests`, `RecentObservationRecordDetectionTests`, `RegionIsRegionIdTests`,
  `Smoothing/*` (3), `SquaringPerformanceTests`, `StandardDeviationTests`, `TemperatureTrendCalculatorTests`,
  `TrendWindowCalculatorTests`. Fixtures: `Auckland/`, `PolynomialRegressionFixtures/`, `co2_daily_mlo.csv`.
- **WebApi:** `AcornSat*Tests` (5), `Bom*DataSetDownloaderTests` (2), `CacheJsonOptionsTests`,
  `ClimateRecordsEndpoints*Tests` (2), `CountryCodeDataTests`, `DataPackageDefinitionTests`, `DataPackageRegistryTests`,
  `DataSetDownload*Tests` (2), `DataSetEndpointSourceUpdateTests`, `DataSetFreshnessPolicyTests`,
  `DataSetMetadataBuilderTests`, `DataSetSource*Tests` (3), `DiagnosticsEndpointsTests`,
  `DirectHttpDataSetDownloaderTests`, `FipsToIsoMapperTests`, `GhcndDataSetDownloaderTests`,
  `GreenlandDataSetDownloaderTests`, `LocationDataSetMetadataServiceTests`, `MetadataEndpointsTests`,
  `NoaaGlobalTempDataSetDownloaderTests`, `WgmsReferenceGlacierMassBalanceTests`. Fixtures: `GlacierFixtures/`.
- **Web:** everything else: the `Chart*`, `RecentObservation*`, `Series*`, `Trend*`/`LocationTrend*`, `Unit*` tests,
  `ApiMeasuresTests`, `AutocompleteFilterTests`, `ClimateStripeUnitsTests`, `DataServiceTests`, `DateLabelsTests`,
  `NotificationServiceTests`, `PaginationControlTests`, `SelectValueMapTests`, `TabNavigationTests`,
  `UnitTextArchitectureTests`, and the `RecordingUnitFormatter` helper.

Details:

1. **Move with `git mv`** so history follows each file. Change `namespace ClimateExplorer.UnitTests` to the new
   project's name (one line per file). Keep the `Smoothing/` subfolder.
2. **csproj files.** Each gets the current package references (MSTest 4.0.2, Test.Sdk, Moq, coverlet). Each also gets
   the `CopyToOutputDirectory` entries for its own fixtures, and only the project references in the table. Keep
   `ServerGarbageCollection` in all three, so test behaviour doesn't change. Its comment refers to "packaged-source
   validation tests", which no longer seem to exist, so flag it as a follow-up rather than drop it here.
3. **One shared helper crosses projects.** Three Web tests (`LocationTrendDetailsBuilderTests`,
   `LocationTrendFieldBuilderTests`, `TrendUnitsTests`) call `TemperatureTrendCalculatorTests.CreateLocation()` and
   `.CreateNoisyLine()`, which live in a Core test class. Move both into a `TrendTestData` static class in
   `ClimateExplorer.Core.UnitTests/TestData/TrendTestData.cs`, and compile it into the Web project as a linked file
   (`<Compile Include="..\ClimateExplorer.Core.UnitTests\TestData\TrendTestData.cs" Link="TestData\TrendTestData.cs" />`).
   One copy keeps the seeded noisy line identical, and the trend assertions depend on it. Every other cross-file
   helper (`AcornSat*`, `PolynomialRegression*`, `DataSetDownloadMetadataTests`, `RecordingUnitFormatter`) stays
   within one project.
4. **`InternalsVisibleTo`.** `ClimateExplorer.WebApi/Properties/AssemblyInfo.cs` names `ClimateExplorer.UnitTests`.
   Change it to `ClimateExplorer.WebApi.UnitTests`. No other production project has `InternalsVisibleTo`.
5. **Repo-root lookups keep working.** `CountryCodeDataTests`, `DataPackageDefinitionTests` and `UnitTextArchitectureTests`
   walk up from `AppContext.BaseDirectory` to the `.sln` or the `ClimateExplorer.Web.Client` folder, which works from
   any project at the same depth.
6. **`SquaringPerformanceTests` gets `[TestCategory("Performance")]`.** It asserts that multiplication beats
   `Math.Pow` by a margin. It's tuned for noisy hardware, but on a shared CI runner a timing failure would block a
   deploy. CI filters it out (step 2), and it still runs locally.
7. **Solution.** Remove `ClimateExplorer.UnitTests` from `ClimateExplorer.sln` and add the three new projects under the
   existing `Tests` solution folder. Delete the old project folder (`TestResults/` is already git-ignored).
8. **Comment.** `EnumsDayOfYearIfFrostLabelTests` mentions `ChartTooltipMetadataBuilderTests`, which will now be in
   another project. Say so in the comment.
9. **Verify:** `dotnet build`, then `dotnet test` on each project. The three totals must add up to 980.

## Step 2 — Test steps in the workflows

Add the steps after `Build with dotnet` and before `dotnet publish`, so a failing test stops the publish and the deploy:

```yaml
      - name: Test Core
        run: dotnet test ClimateExplorer.Core.UnitTests/ClimateExplorer.Core.UnitTests.csproj -c Release --filter "TestCategory!=Performance"

      - name: Test WebApi        # master_web-climateexplorer.yml: "Test Web" / ClimateExplorer.Web.UnitTests
        run: dotnet test ClimateExplorer.WebApi.UnitTests/ClimateExplorer.WebApi.UnitTests.csproj -c Release
```

- **One step per project, not one multi-line `run`.** The runner is `windows-latest`, whose default shell is `pwsh`.
  GitHub Actions only checks the *last* command's exit code there, so a failing first `dotnet test` followed by a
  passing second one would pass the step.
- **API path filter.** `master_api-climateexplorer.yml` only runs when its `paths` change. Add
  `ClimateExplorer.Core.UnitTests/**` and `ClimateExplorer.WebApi.UnitTests/**`, so a test change gets validated. The
  catch: a test-only change then also redeploys an unchanged API. The tool projects the API tests reference
  (`Data.Ghcnm`, `DataPipeline`, `SourceData`) stay out of the filter, so a breaking change there surfaces on the
  next API run, not immediately.
- **Web workflow** has no path filter (it runs on every push to master), so no filter change is needed.
- `dotnet test` builds each test project and its references in Release, so no extra restore or build step is needed.
- Not in CI: `ClimateExplorer.Web.UiTests` (Playwright). It needs a running site.

## Step 3 — Global CSS class check and dead-selector cleanup

**Cleanup first**, so the test lands green. These selectors match nothing in any source file:

| File | Selectors |
|---|---|
| `Components/Chart/ChartSeriesView.razor.css` | `colour-indicator`, `series-action`, `remove-series`, `duplicate-series`, `about-this-data-button` |
| `Components/ClimateRecords/ClimateRecords.razor.css` | `subtle-link` |
| `Components/Location/LocationDashboard.razor.css` | `location-header`, `location-header-link` |
| `Components/Notifications/NotificationHost.razor.css` | `read-column` |
| `Components/RecentObservations/RecentObservationsPanel.razor.css` | `recent-observation-control-row`, `recent-observations-notes`, `recent-observations-source-link`, `recent-observations-retrieval-entry` |
| `ClimateExplorer.Web/wwwroot/css/app.css` | `large-link`; Blazor template leftovers: `validation-message`, `blazor-error-boundary` (nothing uses `EditForm`, `ValidationMessage` or `ErrorBoundary`) and the whole `#blazor-error-ui` block including `.dismiss` (`App.razor` no longer renders that element) |
| `ClimateExplorer.Web/wwwroot/css/utilities.css` | `d-block`, `mb-0` (unused utilities) |

Delete a rule when every selector in it is dead. If a selector list mixes live and dead selectors, drop only the dead
ones. Before deleting each one, confirm with `git log -S` that its markup really was removed (not, say, renamed
without the CSS).

**Test:** `ClimateExplorer.Web.UnitTests/CssClassUsageTests.cs`

- `CssClassSelectors_AllStylesheets_EachUsedInSource`: every class selector in every `.css` file under
  `ClimateExplorer.Web.Client` and `ClimateExplorer.Web` must appear as a whole token in some `.razor`, `.cs`, `.js`
  or `.html` file in those projects. Skip `obj/`, `bin/` and `wwwroot/lib/` (vendored Font Awesome and Chart.js).
- **Parsing.** Strip `/* */` comments, then remove declaration blocks innermost-first, which leaves selectors and
  at-rule preludes. Then take `\.(-?[A-Za-z_][\w-]*)` where it isn't preceded by a word character, so `767.98px` and
  `0.5rem` aren't read as classes. The prototype found 440 class selectors this way and no parsing false positives.
- **Allowlist with reasons**, in the same shape as `UnitTextArchitectureTests.AllowList`, keyed by class or by prefix:
  - `leaflet-` (rendered by Leaflet's JS from the CDN)
  - `notification-type-` (built by interpolation, `$"notification-type-{type}"` in `NotificationHost.razor.cs`)
  - `fa-`, only if a rule targets a Font Awesome class that no markup uses

  Each entry must still match at least one selector, so the list can only shrink.
- **Failure message:** `file: .class`, one per line, ending with "Delete the rule, or fix the markup that should use
  it."
- **Shared helper.** Move `UnitTextArchitectureTests.FindWebClientRoot()` into a small `SourceTree.FindProjectRoot(name)`
  helper, which both tests use.

This check catches a half-done rename whichever side is missed. If the markup was renamed, the old selector matches
nothing. If the CSS was renamed, the new selector matches nothing. It doesn't catch a class in markup that has no rule.
Many of those are deliberate JS or root hooks (`tabs`, `is-active`), so checking that direction would mostly produce
noise.

## Step 4 — Scoped CSS check and the rules that never apply

**Test:** `ScopedCssClassSelectors_BeforeDeep_RenderedByOwnComponent`. In each `X.razor.css`, split every selector at
`::deep`. Each class *before* `::deep` must be rendered by `X` itself. That means it appears in `X.razor.cs`, or in
`X.razor` outside a `Class="…"` attribute. A `Class="…"` attribute passes the class to a child component, which
renders it under the child's scope.

**Today's violations**, besides the dead selectors in step 3:

- **Class only on a child component (rule never applies):**
  - `add-series-button` (`ChartView`)
  - `chart-control` (`ChartView`, `AggregationOptions`)
  - `add-dataset` (`DataSetFolderList`)
  - `today-btn`, `sort-toggle-btn` (`ClimateRecords`)
  - `top100-btn` (`Top100`)
  - `record-link`, `data-sources-link` (`Locations`)
  - `location-dataset-metadata-retry` (`LocationDataSetMetadataSidePanel`)
  - `recent-observations-retry`, `recent-observations-reference-date-reset` (`RecentObservationsPanel`)
- **Class rendered by some other component:** `info-icon` (`ClimateRecords.razor.css`) and `description`
  (`SuggestedChartsMini.razor.css`).

For each one, decide whether to fix it or delete it:

- **Fix** by adding `::deep`, anchored on an element in the component's own markup (for example
  `.records-toolbar ::deep .today-btn`). If the component's root is the child component, wrap it in a `<div>` with
  `display: contents`, as AGENTS.md describes.
- **Delete** if the page already looks right without the rule. It has never applied, so what's on screen today is
  the look without it.

**Fixing changes how the page looks**, because those styles start applying for the first time. The developer
should check each fixed component at the three breakpoints. That's why this is a separate step from the
cleanup in step 3, which changes nothing visible.

**Parsing caveat.** Removing `Class="…"` with a regex fails when a Razor expression inside it contains quotes
(`Class="@(x ? "a" : "b")"`). Match the attribute value with a balanced `@( … )` scan, or allowlist the few cases where
that happens. The prototype's simple regex gave no false positives on the current code.

## Step 5 — Documentation

- **AGENTS.md:** under "Verify with `dotnet build` and the unit test suite", name the three projects. Add a line saying
  that unused CSS selectors, and scoped selectors that need `::deep`, fail `CssClassUsageTests`.
- **CONTEXT.md:** the project table lists `ClimateExplorer.UnitTests` as "xUnit tests…". It's MSTest. Replace the row
  with the three projects.
- **`.github/copilot-instructions.md`:** "Test files live flat in the `ClimateExplorer.UnitTests` project root" → name
  the three projects, and allow a subfolder for fixtures and test data (`Smoothing/` already exists).
- Older design docs and `docs/notes` that mention `ClimateExplorer.UnitTests` stay as they are, as dated records.

## Manual checks (developer)

- Push to master, or use `workflow_dispatch`, and confirm each workflow shows its two test steps, with the expected
  counts, before publish.
- Step 4: visual check of each component whose rule was fixed rather than deleted.

## Summary

Split the single test project along the deployment boundary into Core, WebApi and Web test projects. Each workflow
runs Core's tests plus its own, after building and before publishing, so a failing test blocks the deploy. Add two
source-scanning unit tests to the web test project. One requires every CSS class selector to be used somewhere. The
other requires a scoped selector to target its own component's markup unless it goes through `::deep`. Before those
tests land, delete 19 dead selectors and fix or delete 12 scoped rules that have never applied.

### Change size

- **Projects:** `ClimateExplorer.UnitTests` (removed), `ClimateExplorer.Core.UnitTests`, `ClimateExplorer.WebApi.UnitTests`,
  `ClimateExplorer.Web.UnitTests` (new), `ClimateExplorer.WebApi` (one attribute), `ClimateExplorer.Web.Client` and
  `ClimateExplorer.Web` (CSS only), both workflows, `ClimateExplorer.sln`, three docs.
- **Not changed:** `ClimateExplorer.Web.UiTests`, every non-test C# project apart from the one attribute.

| Area | Added | Changed | Removed |
|---|---|---|---|
| Test files moved (`git mv`, namespace line) | — | 98 files, ~98 | — |
| Test csproj files | 3 files, +~120 | — | 1 file, ~45 |
| Shared trend helper (`TrendTestData.cs`) + callers | 1 file, +~30 | 5 files, ~20 | ~20 lines |
| `SquaringPerformanceTests` category, frost-label comment | — | 2 files, ~2 | — |
| `AssemblyInfo.cs`, `ClimateExplorer.sln` | — | 2 files, ~30 | — |
| Workflows | — | 2 files, +~16 | — |
| Dead CSS selectors (step 3) | — | ~7 files | ~60 lines |
| Scoped rules fixed or deleted (step 4) | — | ~9 `.razor.css` (+ a few `.razor` if a wrapper is needed), ~30 | — |
| Tests: `CssClassUsageTests.cs`, `SourceTree.cs` | 2 files, +~200 | `UnitTextArchitectureTests.cs`, ~20 | — |
| Docs (AGENTS.md, CONTEXT.md, copilot-instructions) | — | 3 files, ~10 | — |
| **Total** | **6 files, +~370** | **~125 files, ~250** | **1 file + ~125 lines** |

Most of the "changed" files are the 98 moved test files, each with one namespace line changed.

**Risk:**

- **Low** for the split and CI. The build catches missing references, and the test totals must add up to 980.
- **Medium** for step 4. Fixing a rule makes a style apply that never has, so check those components visually.
- **CI cost:** both deploys get slower by roughly the suite's runtime (~13 s locally for all three, plus the build of
  the test projects).

## Addendum — implementation notes (2026-10-28)

### What shipped

- **Step 1.** The tests are split into `ClimateExplorer.Core.UnitTests` (28 files, 274 tests),
  `ClimateExplorer.WebApi.UnitTests` (30 files, 215 tests) and `ClimateExplorer.Web.UnitTests` (40 files, 491 tests,
  plus 3 new), as tabled above. Together that's the original 980 tests. Files and fixtures were moved with `git mv`
  and the old csproj became the Core one, so history follows them. Namespaces now match the projects.
  - The shared trend helpers are now `TrendTestData` (namespace `ClimateExplorer.UnitTests.TestData`), in
    `ClimateExplorer.Core.UnitTests/TestData/`, and linked into the Web project. `TemperatureTrendCalculatorTests`
    uses them through `using static`, so its call sites are unchanged.
  - `InternalsVisibleTo` names `ClimateExplorer.WebApi.UnitTests`. `SquaringPerformanceTests` has
    `[TestCategory("Performance")]`. The solution has the three projects under `Tests`.
- **Step 2.** Both workflows run `Test Core` (with `--filter "TestCategory!=Performance"`) and then `Test WebApi` or
  `Test Web`, each as its own step, between `Build with dotnet` and `dotnet publish`. The API workflow's `paths`
  now include both of its test projects.
- **Step 3.** The dead selectors are deleted, and `CssClassUsageTests` adds
  `CssClassSelectors_AllStylesheets_EachUsedInSource` and `CssClassSelectors_AllowList_EveryEntryStillNeeded`.
  `SourceTree.FindProjectRoot` replaces `UnitTextArchitectureTests.FindWebClientRoot`.
- **Step 4.** `ScopedCssClassSelectors_BeforeDeep_RenderedByOwnComponent` is in, and every violation was **deleted**,
  not fixed (see below).
- **Step 5.** AGENTS.md (the test projects, and a note on what `CssClassUsageTests` enforces), CONTEXT.md (three
  project rows; the old row also wrongly said xUnit), `.github/copilot-instructions.md`, and `README.md`, which
  listed `UnitTests` too.
- **Verify.** `dotnet build -c Release` has 0 errors and 13 warnings. `MSTEST0001`, about unset parallelization,
  now fires once per test assembly, so three times where it used to fire once; the 10 `MSTEST0037` warnings are
  unchanged. Tests: 274 + 215 + 494 = 983 passed (980 moved, plus the 3 CSS tests). With the CI filter, Core runs 273.
  A mutation check also fails both CSS tests as expected: a dead class inside `@media`, and a scoped rule on a
  `Class=` value.

### Deviations from the plan

- **The prototype parser missed selectors inside `@media`.** It collapsed whole blocks, at-rules included, before
  reading the selectors. It also skipped element-qualified classes (`dialog.modal`). The test instead scans
  preludes: each `{` closes a selector list or an at-rule prelude, and each `;`/`}` drops a declaration. That found
  three more cases. All three are classes built at runtime, so they're allow-listed with reasons:
  - `status-*`: `status-@StatusClass(...)` on the recent-observation tiles.
  - `large` and `extralarge`: `Collapsible` writes `CollapserSize` lower-cased.
- **More template leftovers removed from `app.css`:** `.valid.modified` and `.invalid`, Blazor's form-validation
  classes. The check didn't flag them only because those words appear elsewhere in the source, but nothing uses
  `EditForm` or `Input*`.
- **Step 4: deleted, not fixed.** `git log` shows every one of these rules stopped applying on 2026-07-01, when
  *Standardize UI controls with reusable button components* and *Refine layout reservations and unify shared
  controls* moved those `<button>`s onto the shared component. A few were written for the component afterwards
  and never worked. So the live site has looked like this without them for four months, and deleting them changes
  nothing visible. Fixing them with `::deep` would restyle buttons the standardisation deliberately made uniform,
  and nobody can review that here without running the UI. Here is what each did, if you want any of them back
  (re-add it as `<anchor> ::deep .<class>`):

  | Component | Class | What it did |
  |---|---|---|
  | `ChartView` | `add-series-button` | `display: inline-flex`; `.button` already sets it |
  | `ChartView` | `chart-control` | `vertical-align: baseline`, where `.button` uses `text-bottom` |
  | `AggregationOptions` | `chart-control` | `margin: 0`; `.button` has no margin |
  | `ClimateRecords` | `today-btn`, `sort-toggle-btn` | 0.875rem / weight 500 text, a press scale, 0.875rem icon |
  | `ClimateRecords` | `info-icon` | info-icon colour and size; nothing in `ClimateRecords` renders it |
  | `Top100` | `top100-btn` | `gap: 0.25rem` |
  | `Locations` | `record-link`, `data-sources-link` | primary-green link colour instead of the link blue |
  | `DataSetFolderList` | `add-dataset` | `#09C2FC` bold text, an off-palette colour |
  | `LocationDataSetMetadataSidePanel` | `location-dataset-metadata-retry` | `margin-left: 0.5rem` |
  | `RecentObservationsPanel` | `recent-observations-retry` | outlined button pushed right (`margin-left: auto`) |
  | `RecentObservationsPanel` | `recent-observations-reference-date-reset` | 1.9375rem square icon button |
  | `SuggestedChartsMini` | `description` | secondary text colour; the component renders no `.description` |

- **`DateHelpersTests.DecimalDateTest` depended on line endings.** It split a verbatim string on `"\r\n"`, so it
  only passed when the file was checked out with CRLF. It now normalises the line endings first, which matters
  for a CI checkout.
- **Line endings in the working copy.** Shell edits with `sed -i`/`perl -i` in Git Bash wrote LF, and that's how the
  test above surfaced. Every file touched in this change was converted back to CRLF. Git normalises to LF on commit
  either way (`* text=auto`), so commits aren't affected.

### Follow-ups

- The `ServerGarbageCollection` comment in all three csproj files describes "packaged-source validation tests",
  which no longer seem to exist. Measure whether the setting still helps, then drop it or rewrite the comment.
- CONTEXT.md says to avoid building `ClimateExplorer.DataPipeline` because "its build step creates zip files". Its
  csproj has no build-time step (the packaging happens when it runs), and `WebApi.UnitTests` builds it, as the old
  test project did. Check the note and correct it.
- Consider `[assembly: Parallelize]` or `[assembly: DoNotParallelize]` in each test project, to clear `MSTEST0001`.
