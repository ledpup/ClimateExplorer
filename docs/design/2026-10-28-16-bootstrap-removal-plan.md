# Bootstrap Removal

- **Date:** 2026-10-28
- **Status:** Proposed
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
