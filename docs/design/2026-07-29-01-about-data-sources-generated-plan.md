# Generate About.razor "Data sources" section from DataSetDefinition data

- **Date:** 2026-07-29
- **Status:** Implemented 2026-07-29 (see addendum)
- **Author:** Claude
- **Scope:** `ClimateExplorer.Core/Model/DataSetDefinition.cs`, `ClimateExplorer.Core/ViewModel/DataSetDefinitionViewModel.cs`, `ClimateExplorer.WebApi/MetadataEndpoints.cs`, `ClimateExplorer.Core/DataSetDefinitionsBuilder/DataSetDefinitionsBuilder.cs` and its `.Bom.cs`/`.Atmosphere.cs`/`.Ocean.cs` partials, `ClimateExplorer.Web.Client/Pages/About.razor` + `About.razor.cs`
- **Branch context:** `issues/co2-obs`

## Goal

`About.razor`'s "Data sources" section (previously lines 53–169) was hand-written
HTML describing each dataset. It had drifted from the actual `DataSetDefinition`
data in `DataSetDefinitionsBuilder`: some descriptions were duplicated/stale
(e.g. a dead zenodo link for the Global Carbon Project), several datasets
weren't documented at all (Niño 3.4, IOD, Arctic/Antarctic sea ice, Greenland
ice melt, ocean acidity, GHCNd/GHCNdp, Mauna Loa atmospheric transmission), and
some prose that's useful only existed in the page, never in the code
(BOM-CDO's 4 sub-datasets, the HadCET/HadCEP precipitation history, AMO's
Kaplan SST rationale).

This change makes the section self-maintaining: pull `Name`/`ShortName`/
`Description`/`MoreInformationUrl`/`Publisher` from data via the existing
`IDataService.GetDataSetDefinitions()` API (already used elsewhere in the
client), fold the prose that was only in About.razor into the `Description`
fields so nothing is lost, and keep the page organized the same way (org
heading → dataset sub-heading → description). The Global Monitoring
Laboratory group specifically stays a compact "org blurb + generated list of
linked dataset names" like it is today, since it fronts 5 different datasets
and the reader shouldn't have to wade through five full paragraphs to get the
gist.

Decisions made with the user before implementation:
- Org-level narrative (Copernicus, Global Carbon Project, GML, STAR/LSA) →
  new `PublisherDescription` field on `DataSetDefinition`, set on one
  representative dataset per org/division.
- Dataset-specific facts that only existed in About.razor (BOM-CDO's
  sub-datasets, HadCEP/EWP history, AMO's Kaplan SST rationale, ROB's URL) →
  folded into `Description`/`PublisherUrl` in the builder files.
- Coverage → **all** `DataSetDefinition`s render, not just the ones
  documented today.

## Data model changes

**`ClimateExplorer.Core/Model/DataSetDefinition.cs`** — add two optional
fields after `PublisherUrl`:
```csharp
public string? PublisherDescription { get; set; }
public string? PublisherDivision { get; set; }
```
`PublisherDivision` groups multiple datasets from the same org under one
sub-heading (e.g. "Global Monitoring Laboratory", "Physical Sciences
Laboratory", "Global Historical Climatology Network", "Satellite
Applications and Research (STAR)"); left null for datasets that stand alone.

**`ClimateExplorer.Core/ViewModel/DataSetDefinitionViewModel.cs`** — add the
same two properties. This is the DTO actually sent to the client;
`DataSetDefinition` itself never crosses the wire because it also carries
`DataFileMapping`/download config.

**`ClimateExplorer.WebApi/MetadataEndpoints.cs`** (`GetDataSetDefinitions()`)
— map the two new fields into the DTO alongside the existing
`Publisher`/`PublisherUrl` mapping.

## Data edits in `DataSetDefinitionsBuilder.*.cs`

All edits are to `Description`, `Publisher`, `PublisherUrl`,
`PublisherDescription`, `PublisherDivision` fields only — no structural/id
changes.

**`DataSetDefinitionsBuilder.Bom.cs`**
- BOM-CDO: extend `Description` to mention the 4 sub-datasets (precipitation,
  solar radiation, unadjusted min/max temperature) and that they're combined
  with ACORN-SAT's 112 locations into a unified representation.

**`DataSetDefinitionsBuilder.Atmosphere.cs`**
- CO2 (Mauna Loa) entry: `PublisherDivision = "Global Monitoring Laboratory"`,
  `PublisherDescription` = the GML org paragraph from About.razor ("conducts
  research that addresses three major challenges...").
- CH4, N2O, ODGI, Mauna Loa atmospheric transmission: `PublisherDivision =
  "Global Monitoring Laboratory"` only (description already set once on the
  CO2 entry).
- Carbon dioxide emissions (GCP): `PublisherDescription` = About's GCP org
  paragraph 1 (integrates GHG knowledge, 3 gases, urban/regional/cumulative
  efforts). Existing `Description`/`MoreInformationUrl` already supersede
  About's stale zenodo link — no change needed there.
- SH ozone hole area: `PublisherDescription` = About's Copernicus org
  paragraph ("Copernicus is the Earth observation component of the EU's
  Space programme...").
- SH ozone column: no change (shares Publisher with ozone hole area,
  description carried on that entry).

**`DataSetDefinitionsBuilder.Ocean.cs`**
- Niño 3.4: set `Publisher = "National Oceanic and Atmospheric
  Administration (NOAA)"`, `PublisherUrl = "https://www.noaa.gov/"`,
  `PublisherDivision = "Physical Sciences Laboratory"` (currently has neither
  set at all).
- IOD: set `Publisher = "Australian Bureau of Meteorology"`, `PublisherUrl =
  "https://www.bom.gov.au/"` (groups with ACORN-SAT/BOM-CDO; its
  `MoreInformationUrl` is already a BOM ENSO page).
- AMO: set `Publisher = "National Oceanic and Atmospheric Administration
  (NOAA)"` (PublisherUrl already set), `PublisherDivision = "Physical
  Sciences Laboratory"`; extend `Description` with the Kaplan SST rationale
  ("...because the original Kaplan SST dataset is no longer updated").
- Ocean acidity: set `Publisher = "National Oceanic and Atmospheric
  Administration (NOAA)"` (PublisherUrl already set). No division — stands
  alone under NOAA.

**`DataSetDefinitionsBuilder.cs`** (`BuildOtherDataSetDefinitions`)
- Arctic sea ice extent, Antarctic sea ice extent, Greenland ice melt area:
  set `Publisher = "National Snow & Ice Data Center (NSIDC)"`, `PublisherUrl
  = "https://nsidc.org/"` (none currently set — this becomes a new top-level
  org section).
- HadCET: extend `Description` with the HadUKP/EWP paragraph (precipitation
  series from 1766, ClimateExplorer's own HadCEP daily series beginning
  1931).
- Sunspot number: set `PublisherUrl = "https://www.astro.oma.be/en/"`;
  simplify `Publisher` from `"WDC-SILSO, Royal Observatory of Belgium,
  Brussels"` to `"Royal Observatory of Belgium"` so it reads cleanly as a
  heading (WDC-SILSO detail stays in `Description`).
- Total solar irradiance: set `PublisherUrl = "https://www.noaa.gov/"`;
  append the "beginning 1978" satellite-record detail to `Description`.
- Global temperature anomaly (NOAAGlobalTemp): set `PublisherUrl =
  "https://www.noaa.gov/"`; add the "(formerly known as MLOST)"
  parenthetical to `Description`.
- Mean sea level: `PublisherDivision = "Satellite Applications and Research
  (STAR)"`, `PublisherDescription` = About's LSA/STAR org paragraph
  (Laboratory for Satellite Altimetry's remit).

## Rendering design (About.razor / About.razor.cs)

**`About.razor.cs`**: add `private List<DataSetDefinitionViewModel>
dataSetDefinitions = [];` and fetch it in `OnInitializedAsync` right after
the existing `apiMetadata = await DataService.GetAbout();` line, same
pattern/error handling. The page's TOC-builder JS only scans `h1`/`h2` (see
`getMainHeadings` in the `<script>` block), so the H3/H4 dataset headings
this section introduces don't affect the TOC; fetching in
`OnInitializedAsync`, before first render, is still right so the section
isn't empty on initial paint.

Add a small grouping helper in the code-behind (private nested records,
consistent with the existing private `TocItem`/`HeadingInfo` classes in this
file):
```csharp
private record DivisionGroup(string? Name, List<DataSetDefinitionViewModel> Items);
private record PublisherGroup(string Publisher, string? PublisherUrl, string? PublisherDescription, List<DivisionGroup> Divisions);
```
Group `dataSetDefinitions` by `Publisher` (preserving encounter order —
matches `Bom → Ghcn → Atmosphere → Ocean → Other` build order, a sensible
read order), then within each group, sub-group by `PublisherDivision` (null
= standalone, one dataset per implicit "division"). Pick
`PublisherDescription` as the first non-null value found among a group's/
division's members.

**`About.razor`**: replace the hardcoded block from `<h3>Australian
datasets</h3>` through the end of the Sunspot paragraph — i.e. everything
under `<h2>Data sources</h2>` — with a `@foreach` over the publisher groups:

- `<h3>@group.Publisher</h3>`, with `group.PublisherDescription` rendered as
  an intro `<p>` if present (reuse the existing `.Replace("\r\n", "<br>")` +
  `(MarkupString)` pattern already used in `AboutDataDetails.razor` for
  `Description`).
- For each `DivisionGroup` in the publisher group:
  - **If `division.Name == "Global Monitoring Laboratory"`**: render
    `<h4>Global Monitoring Laboratory</h4>`, the division's
    `PublisherDescription` paragraph, then one generated sentence linking
    each member's `ShortName` to its `MoreInformationUrl` (mirrors today's
    "The carbon dioxide, methane, nitrous oxide, and ODGI data ... are
    sourced from GML" sentence, but built from data — small helper to
    Oxford-comma-join the linked names). This is the one explicitly
    requested special case, kept narrow and named rather than a generic
    "compact if >1 items" rule, so it doesn't unexpectedly swallow detail
    from other divisions later.
  - **Any other division with >1 dataset** (GHCN, PSL): render
    `<h4>@division.Name</h4>` once, then each member dataset in full (own
    heading via `Name` linked to `MoreInformationUrl`, then its
    `Description` paragraph) — same treatment as standalone datasets, just
    nested under the shared division heading.
  - **Division with exactly 1 dataset** (e.g. STAR/sea level) or no division
    (standalone): render the dataset's own `Name` (linked to
    `MoreInformationUrl` if present, else plain text) as `<h4>`, then its
    `Description` as a paragraph. This is exactly `AboutDataDetails.razor`'s
    existing link+description pattern — reuse that logic rather than
    reinventing it.

Everything above/below "Data sources" (site intro, Source code section, Data
pipeline, Notes, Glossary) is untouched.

## Verification
- `dotnet build` on the solution to confirm the new fields compile through
  `Core` → `WebApi` → `WebApiClient` → `Web.Client`.
- Run existing test suite (`dotnet test`) — check for any snapshot/unit
  tests over `DataSetDefinition`/`DataSetDefinitionViewModel` field lists or
  the `/datasetdefinition` endpoint shape that would need updating for the
  two new nullable fields.
- No dev server / browser check (per standing project guidance) — rely on
  build + tests, and a manual read-through diff of the generated markup
  logic against the intended structure above.

## Addendum — implementation notes

Implemented as planned. Worth restating the one rendering nuance that's easy
to miss reading `About.razor` cold: a `PublisherDivision` group only renders
as its own `<h4>` sub-section (with member datasets nested under `<h5>`)
when it has **more than one** member. A division with exactly one dataset
(this only affects Satellite Applications and Research (STAR), which fronts
just "Mean sea level") collapses to the same standalone treatment as
datasets with no division at all — its `PublisherDescription` still prints
as an intro paragraph, but the heading becomes the dataset's own `Name`
rather than the division name, avoiding a one-item division heading that
adds nesting for no grouping benefit. Divisions with no `PublisherDivision`
set (`Name == null`) are *always* rendered standalone, even when a publisher
has several such datasets (e.g. Australian Bureau of Meteorology now has
three: ACORN-SAT, BOM-CDO, IOD) — only an explicit `PublisherDivision`
triggers grouping.

`dotnet build` and the full `ClimateExplorer.UnitTests` suite (440 tests)
both pass; no test asserted on `DataSetDefinition`/`DataSetDefinitionViewModel`
field lists or the `/datasetdefinition` endpoint shape, so nothing needed
updating there.

No manual browser verification was done (per standing project guidance —
see [[feedback-no-playwright-or-dev-servers]]); the generated markup was
reviewed by re-reading `About.razor`/`About.razor.cs` against this plan
after writing it.
