# Bootstrap Removal

- **Date:** 2026-10-28
- **Status:** Implemented 2026-10-28 (see addenda). Build and unit tests are clean; the UI is unverified.
- **Author:** Claude
- **Scope:** `ClimateExplorer.Web/App.razor`, `ClimateExplorer.Web/wwwroot/css/` (new `base.css`,
  `utilities.css`; `app.css`), remaining Bootstrap class references in Razor/CSS
- **Builds on:** [Blazorise Removal — Overview](2026-10-28-01-blazorise-removal-overview-plan.md)
- **Branch context:** `development`
- **Implementation order:** steps 2, 3 and 17 of 18; see [overview](2026-10-28-01-blazorise-removal-overview-plan.md#implementation-order)

## Question

How much do we use Bootstrap? Can it be removed outright, or should we use it directly rather than through Blazorise?

## Findings

Bootstrap 5.3.3 is loaded from jsdelivr in `App.razor`: CSS render-blocking in `<head>`, and the JS
bundle deferred at the top of `<body>`.

**JS bundle: unused.** The app has no `data-bs-*` attributes and no `bootstrap.*` / `new bootstrap`
calls. Blazorise's Bootstrap5 provider drives its dropdowns, modals and tooltips with its own JS
(Tippy for tooltips). The bundle costs a download, parse and compile on every page for nothing.

**CSS: four separate jobs.**

1. **Styling Blazorise-rendered markup.** `.navbar*`, `.nav-tabs`/`.nav-link`, `.dropdown-menu`/`.dropdown-item`,
   `.form-select`, `.form-check*`, `.form-control`/`.invalid-feedback`, `.modal*`/`.btn-close`, `.table*`,
   `.pagination`/`.page-*`, `.btn*`. This is most of what we use, and each family plan removes its piece.
2. **Reboot (base element styles).** Things we inherit without noticing: `box-sizing: border-box`
   everywhere, `body { margin: 0; line-height: 1.5; color: #212529 }`, heading and paragraph margins,
   list padding, `table { border-collapse: collapse }`, form controls inheriting `font`, `[hidden]`
   enforcement, `img { vertical-align: middle }`, focus behaviour, and so on.
3. **Utility classes written in our markup.** About 23 distinct classes, ~55 uses:

   | Group | Classes |
   |---|---|
   | Display/flex | `d-flex`, `d-block`, `flex-column`, `flex-md-row`, `flex-wrap`, `align-items-center`, `justify-content-between`, `justify-content-center`, `justify-content-end` |
   | Gap | `gap-1`, `gap-2`, `gap-4` |
   | Spacing | `mb-0`, `mb-2`, `mb-3`, `mb-4`, `mt-1`, `mt-2`, `mt-3`, `px-4` (already overridden at ≤767px in `app.css`) |
   | Text | `text-center`, `text-end`, `text-muted` |
   | Accessibility | `visually-hidden` |

   No grid (`row`/`col-*`/`container`) is used.
4. **Bootstrap CSS variables (`--bs-*`).** None referenced in our CSS.

`app.css` also overrides Bootstrap in places (`.btn-primary`, `.btn-link`, `.navbar`, `.px-4`,
`a`/`.btn-link` colour, heading scale) that become dead or simpler once Bootstrap is gone.

## Decision: remove outright

**Don't adopt Bootstrap directly.** Its value to us was as Blazorise's style provider. Once our
components have their own CSS on the `--climate-*` tokens, using Bootstrap directly would mean
re-coupling our markup to its class names, to keep ~230 KB minified (~30 KB gzipped) of
render-blocking third-party CSS that we mostly override. What we actually rely on is a few dozen
reboot rules and ~23 utility classes. Both are easy to own.

Benefits: one fewer render-blocking cross-origin stylesheet (and its `preconnect`) on the critical
path, no deferred JS bundle, and no specificity fights with framework rules.

## Plan (staged so the final removal is close to a no-op)

### Step 1 — Remove the JS bundle (overall step 2, independent)

Delete the `bootstrap.bundle.min.js` `<script>` from `App.razor` and update the comment above it
(which mentions bootstrap being deferred).

- *Manual check:* Blazorise dropdowns, modals and tooltips still open and close (they use
  Blazorise's JS). If anything regresses, Blazorise does depend on it after all. Restore the tag and
  remove it after plans 04–06 instead.

### Step 2 — Add `base.css` and `utilities.css` after Bootstrap (overall step 3)

Create both in `ClimateExplorer.Web/wwwroot/css/` and link them in `App.razor` **after** Bootstrap
and **before** `app.css`, so they win over Bootstrap while it's still present. Removing Bootstrap
later then changes almost nothing.

`base.css`: the reboot subset we depend on (~60 lines), no more:

```css
*, *::before, *::after { box-sizing: border-box; }
html { -webkit-text-size-adjust: 100%; }
body { margin: 0; font-size: 1rem; line-height: 1.5; color: #212529; background: #fff; }
h1, h2, h3, h4, h5, h6 { margin-top: 0; margin-bottom: 0.5rem; }
p { margin-top: 0; margin-bottom: 1rem; }
ul, ol { padding-left: 2rem; margin-top: 0; margin-bottom: 1rem; }
img, svg { vertical-align: middle; }
table { border-collapse: collapse; caption-side: bottom; }
th { text-align: inherit; }
button, input, select, textarea { margin: 0; font: inherit; line-height: inherit; }
button, select { text-transform: none; }
button, [type="button"], [type="submit"] { -webkit-appearance: button; }
button:not(:disabled) { cursor: pointer; }
label { display: inline-block; }
hr { margin: 1rem 0; color: inherit; border: 0; border-top: 1px solid; opacity: 0.25; }
[hidden] { display: none !important; }
```

(Heading sizes/weights and the font stack already live in `app.css`.) To build it, diff Bootstrap
5.3.3's `_reboot.scss` against what renders, and keep only the rules that visibly matter. Err on
including a rule if unsure. Each rule is cheap.

`utilities.css`: exactly the ~23 classes in the table above, **same names, same values**, so no
markup changes (e.g. `.mb-4 { margin-bottom: 1.5rem !important; }`, with Bootstrap's spacer scale
0.25/0.5/1/1.5rem and `!important` as Bootstrap has it). Include the `@media (min-width: 768px)`
`flex-md-row` breakpoint, which matches our tablet breakpoint, and `.visually-hidden`. Move the
`.px-4` mobile override from `app.css` into this file, next to `.px-4`.

Add to AGENTS.md: "Utility classes are limited to those in `utilities.css`; prefer component-scoped
CSS, and don't add new utilities without a reason."

### Step 3 — Clean up as families land (during overall steps 4–16; final sweep at step 17)

Each family plan deletes the Bootstrap-targeted selectors in its own CSS. After overall step 16, grep for
leftovers in Razor and CSS: `btn`, `btn-*`, `form-*` (except our `form-row`/`form-label`/`form-control-wrap`),
`navbar`, `nav-link`, `nav-item`, `dropdown-*`, `modal*`, `table` (as a class), `page-*`, `text-muted`
outside `utilities.css`. In `app.css`, remove `.btn-primary`, the `.btn-link` part of the
`a, .btn-link` rule, `.navbar { padding-left }`, `.modal-open`, and the Tippy/`--b-tooltip-*` block
(if plan 04 hasn't already).

### Step 4 — Remove Bootstrap CSS (overall step 17)

- Delete the Bootstrap `<link>` and the `cdn.jsdelivr.net` `preconnect` from `App.razor` (check that
  nothing else uses jsdelivr first; Chart.js is vendored, Leaflet comes from unpkg).
- Update the comment "app.css loaded after Bootstrap so its rules … override framework defaults".
- This step can only happen once no Blazorise component renders Bootstrap markup, i.e. after all
  family plans. It happens just before or together with [17](2026-10-28-17-blazorise-package-removal-and-renames-plan.md),
  which removes `blazorise.bootstrap5.css`.

## Risks

- **Reboot drift**, such as a slightly different margin or line-height somewhere. Mitigated by step
  2 landing early, so differences show up while Bootstrap can still be compared side by side (toggle
  the Bootstrap link in DevTools).
- **Third-party components** that assumed Bootstrap. Leaflet/DPBlazorMapLibrary bring their own CSS.
  Check the map page once in step 4.

## Manual checks (developer)

- Step 1: overlays still work (see above).
- Step 2: no visual change on any page (it only restates what Bootstrap already applies).
- Step 4: compare each page at the three breakpoints with Bootstrap disabled vs enabled in DevTools
  before deleting the link. Pay attention to body text spacing, headings, lists in the About page,
  form controls, the map page and blog posts.
- Network panel: no requests to jsdelivr for Bootstrap.

## Summary

Bootstrap is removed outright rather than used directly. Its JS bundle goes immediately (unused). A
small `base.css` (the reboot rules we depend on) and `utilities.css` (the ~23 utility classes in
use, same names) are added after Bootstrap early on, so the final removal of the Bootstrap
stylesheet, after the last Blazorise component is gone, changes almost nothing.

### Change size

- **Projects:** `ClimateExplorer.Web` (App.razor, wwwroot/css), `ClimateExplorer.Web.Client`
  (leftover class cleanup), `AGENTS.md`.

| Area | Added | Changed | Removed |
|---|---|---|---|
| `base.css` | 1 file, +60 | — | — |
| `utilities.css` | 1 file, +45 | — | — |
| `App.razor` (script, links, preconnect, comments) | — | 1 file, ~10 | — |
| `app.css` overrides | — | 1 file, ~10 | ~30 lines |
| Leftover Bootstrap class cleanup in Razor/CSS (after family plans) | — | ~5 files, ~20 | — |
| AGENTS.md | — | 1 file, ~2 | — |
| **Total** | **2 files, +105** | **~8 files, ~42** | **~30 lines** |

Risk: low per step. The main risk (reboot drift) is surfaced early by step 2.

## Addendum — implementation notes (steps 1–2, 2026-10-28)

### What shipped

- **Step 1.** The `bootstrap.bundle.min.js` `<script>` is gone from `App.razor`, and the comment above the
  deferred scripts now only mentions Leaflet. A repo-wide grep found no `data-bs-*` attributes or
  `bootstrap.*` calls. The `cdn.jsdelivr.net` preconnect stays because the Bootstrap CSS still comes from there.
- **Step 2.** `ClimateExplorer.Web/wwwroot/css/base.css` (~215 lines) and `utilities.css` (~65 lines) are
  linked in `App.razor` after Bootstrap and before `app.css`. Every value is copied from Bootstrap 5.3.3, so
  with Bootstrap still loaded the rendered result should be the same. The `.px-4` mobile override moved
  from `app.css` into `utilities.css`. AGENTS.md has the utility-class rule. `.github/copilot-instructions.md`
  now says the load-order note also covers the two new files.

### Deviations from the plan

- **`base.css` is larger than the sketch (~215 lines instead of ~60).** A check of the elements we render
  turned up reboot rules the sketch didn't have, so they were added:
  - Nested-list `margin-bottom: 0`. Without it, removing Bootstrap later would add space under nested lists.
  - `dl`/`dt`/`dd` (trend stats).
  - `blockquote` and `pre`/`code` (blog posts use quotes and fenced code; Bootstrap's monospace stack
    and `#d63384` code colour are kept).
  - `figure`.
  - `thead, tbody, tfoot, tr, td, th { border-style: solid; border-width: 0 }`. Table CSS that sets only a
    border width depends on this.
  - `button { border-radius: 0 }` and `button:focus:not(:focus-visible) { outline: 0 }`.
  - The number-input spin button, the search input, `[role="button"]` cursor, `summary`,
    `a:not([href]):not([class])`, body `font-weight`/`-webkit-tap-highlight-color`, and the
    reduced-motion-aware `scroll-behavior: smooth`.
  - Skipped as unused: `abbr`, `address`, `mark`, `sub`/`sup`, `kbd` styling, `caption`, `fieldset`/`legend`,
    `iframe`, `progress`, `output`, `::file-selector-button`, and the Bootstrap heading sizes and weights
    (which `app.css` overrides).
- **`utilities.css` has 24 classes.** That is all of them in the plan's table. A grep of Razor, C# and blog
  Markdown found no others: `flex-grow` in `Global.razor` is an inline style, and the other matches were prose.
- **`.text-muted` keeps Bootstrap's value** (`rgba(33, 37, 41, 0.75)`) rather than switching to
  `--climate-color-muted-text` (`#595959`), so this step changes nothing visually. It has two uses.

### Verification

- `dotnet build` of `ClimateExplorer.Web` is clean. The two files are registered as static web assets, so
  `@Assets` fingerprints them. The local `bin` was locked by a running instance, so the build went to a
  separate artifacts path.
- `ClimateExplorer.UnitTests`: 939 passed, 0 failed.
- Manual checks for the developer are still to do: the step 1 overlays (dropdowns, modals, tooltips) and
  "no visual change" for step 2.

### Follow-ups

- Steps 3–4 (cleanup and removing the Bootstrap CSS) stay with overall step 17.
- Optional: point `.text-muted` at `--climate-color-muted-text`, or replace its two uses with scoped CSS.

## Addendum — implementation notes (steps 3–4, 2026-10-28)

### What shipped

- **Step 4.** The Bootstrap `<link>` and the `cdn.jsdelivr.net` preconnect are gone from `App.razor`. Nothing else
  used jsdelivr. The unpkg preconnect stays for Leaflet, and its comment now says so. The load-order comment now
  describes `base.css` → `utilities.css` → `app.css`. `blazorise.css` and `blazorise.bootstrap5.css` stay for
  [17](2026-10-28-17-blazorise-package-removal-and-renames-plan.md).
- **Step 3.** The grep found no Blazorise components left, and none of the selectors the plan listed for `app.css`
  except `.btn-primary` and the `.btn-link` half of `a, .btn-link`, which are removed. `.navbar`, `.modal-open` and
  the Tippy block had already gone with plans 14, 06 and 04. Markup that still relied on Bootstrap classes:
  - **Four raw `<input>`s with `form-control form-control-sm`** (aggregation threshold, Locations search, Recent
    observations reference date and completeness threshold). They now use `climate-control` plus a scoped class for
    padding (and `width: 100%` where Bootstrap's block width mattered). They stay raw inputs rather than `TextInput`
    because they're numeric or update per keystroke, and `TextInput` is text-only and fires on change.
  - **Reference date validation.** `is-invalid` → `recent-observations-input--invalid` (danger border), and
    `invalid-feedback d-block` → `recent-observations-reference-date-error`, matching `TextInput`'s error look.
    Two accessibility fixes came with it: `aria-describedby` pointed at a `-help` id that no element had, and now
    points at the error message only while it shows; `aria-invalid` rendered as an empty attribute (which means
    false) and now renders `"true"`, as in `TextInput`.
  - **`btn-close`** on the Recent observations tile is now a plain button with `fa-xmark`, 24px with a mouse and 44px
    on touch. The old `transform: scale(0.72)` made its target about 22px, below WCAG 2.5.8.
  - **`dropdown-toggle`** on the aggregation button (dropdown plan follow-up) is replaced by the same caret,
    drawn in `AggregationOptions.razor.css`.
- `base.css`'s header and `.github/copilot-instructions.md`'s load-order note no longer refer to Bootstrap as loaded.

### Deviations from the plan

- No `a:hover` rule was added to replace Bootstrap's. `app.css`'s `a { color: … }` already overrode the property
  Bootstrap's hover changed through `--bs-link-color-rgb`, so links never changed colour on hover before either.
- `CONTEXT.md` still lists "Blazorise (Bootstrap 5)" in the stack. The `Blazorise.Bootstrap5` package is still
  referenced until 17, which updates that line.

### Verification

- `dotnet build` of `ClimateExplorer.Web` is clean.
- `ClimateExplorer.UnitTests`: 980 passed, 0 failed.
- A grep of Razor, C#, CSS and the UI tests finds no `form-control`, `is-invalid`, `invalid-feedback`, `btn-*` or
  `dropdown-toggle` outside our own `form-control-wrap`, and no jsdelivr references.
- Manual checks for the developer: the step 4 page comparison (see "Manual checks" above), plus the four inputs,
  the reference-date error, the tile remove button and the aggregation caret.
