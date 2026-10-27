# Layout and Navbar — Native `MainLayout` and `NavMenu`

- **Date:** 2026-10-28
- **Status:** Proposed
- **Author:** Claude
- **Scope:** `Layout/MainLayout.razor` / `.razor.css`, `Layout/NavMenu.razor` / `.razor.cs` / `.razor.css`, `app.css`
- **Builds on:** [Blazorise Removal — Overview](2026-10-28-01-blazorise-removal-overview-plan.md),
  [05 Dropdown](2026-10-28-05-dropdown-plan.md) (NavMenu's two dropdowns are converted there; do this after)
- **Branch context:** `development`
- **Implementation order:** step 16 of 18; see [overview](2026-10-28-01-blazorise-removal-overview-plan.md#implementation-order)

## Current state

**MainLayout** uses Blazorise `Layout` / `LayoutHeader` / `LayoutContent`, which renders
`div.b-layout > header.b-layout-header.layout-header + main.b-layout-content`. `app.css` has
`header.layout-header { overflow: visible; }` so the hamburger menu can overflow. `MainLayout.razor.css`
is mostly leftover Blazor-template CSS (`.page`, `.sidebar`, `.top-row`) that matches nothing in the
current markup.

**NavMenu** uses Blazorise `Bar` (`Breakpoint.Desktop`, `Background.Dark`, `ThemeContrast.Dark`),
`BarBrand`, `BarLink`, `BarMenu`, `BarStart`, `BarEnd`, `BarItem`. That renders Bootstrap's
`nav.navbar.navbar-expand-lg.navbar-dark.bg-dark` > `.navbar-brand` + `.navbar-collapse` >
`ul.navbar-nav` (start) + `ul.navbar-nav.ms-auto` (end) > `li.nav-item` > `a.nav-link`. There's no
`BarToggler`; the responsive behaviour is all ours:

- Primary items are always visible.
- Secondary items show inline on desktop (`desktop-secondary-nav-item`) and move into the hamburger
  dropdown below 1025px (`mobile-menu-nav-item`).
- Unit switch is inline on desktop and in the options cog / hamburger on smaller screens. The CO₂
  tile and notification bell are separate items.

`NavMenu.razor.css` is 379 lines, nearly all `::deep` selectors against Bootstrap navbar classes,
including `.navbar-collapse { display: flex !important; }` to defeat Bootstrap's collapse below
`lg`. A wrapper `<div>` exists only so CSS isolation applies (Blazorise issue 1599).

## Design

### MainLayout

```razor
<div class="app-layout">
    <header class="layout-header"><NavMenu /></header>
    <main class="layout-content">
        <article class="content px-4">@Body</article>
        <NotificationHost />
    </main>
</div>
```

- Keep the `layout-header` class name, so `app.css` keeps working. Move that rule into `MainLayout.razor.css`.
- Replace `MainLayout.razor.css` with what's actually needed: `.app-layout { display: flex;
  flex-direction: column; min-height: 100dvh; }` and `main { flex: 1; }`. The Blazorise layout CSS
  being removed did roughly this. Delete the dead `.sidebar`/`.top-row` rules.

### NavMenu markup

```razor
<nav class="site-nav" aria-label="Main">
    <a class="site-nav-brand" href=""><img src="/logo-50.png" width="42" height="42" alt="ClimateExplorer home" /></a>
    <ul class="site-nav-list site-nav-start">
        @foreach (var item in primaryItems)
        {
            <li class="site-nav-item"><a class="site-nav-link" href="@item.Route" aria-current="@AriaCurrent(item.Route)">@item.Text</a></li>
        }
        @* desktop secondary items, notification bell, hamburger DropdownButton (from plan 05) *@
    </ul>
    <ul class="site-nav-list site-nav-end">
        @* unit switch, options DropdownButton, CO₂ tile *@
    </ul>
</nav>
```

- Same items, same visibility classes (`desktop-secondary-nav-item`, `mobile-menu-nav-item`,
  `unit-switch-nav-item`, `options-nav-item`, `co2-nav-item`, `notification-nav-item`), so the
  responsive rules map one to one.
- **Current page:** add `aria-current="page"` (via `AriaCurrent(route)` in `NavMenu.razor.cs`,
  reusing `IsCurrentPage`), and style `[aria-current="page"]` instead of `.nav-current`. This is an
  accessibility gain. Keep the `nav-current` class on the hamburger toggle, which isn't a link.
- Drop the wrapper `<div>` and its comment. The `nav` is now our own element, so scoped CSS applies directly.
- The site-overview button keeps its `aria-label="Site overview"`, with the icon marked `aria-hidden="true"`
  (currently missing).
- The logo `alt` changes to "ClimateExplorer home", since it's the home link's only content.

### NavMenu CSS

Rewrite `NavMenu.razor.css` against the new classes. It's a translation, not a redesign:

| Old selector | New selector |
|---|---|
| `::deep .navbar`, `.navbar-dark`, `.bg-dark` | `.site-nav` (background `#212529`, Bootstrap's `bg-dark`, as a new `--climate-color-nav-surface` token in `app.css`) |
| `::deep .navbar-brand` | `.site-nav-brand` |
| `::deep .navbar-collapse` (+ the `!important` override) | gone; `.site-nav` is `display: flex` at every width |
| `::deep .navbar-nav`, `.navbar-nav.ms-auto` | `.site-nav-list`, `.site-nav-end { margin-left: auto; }` |
| `::deep .nav-item` | `.site-nav-item` |
| `::deep .nav-link`, `.nav-current` | `.site-nav-link`, `.site-nav-link[aria-current="page"]` |
| `::deep .nav-item .dropdown*` | the `DropdownButton` classes from plan 05 (`::deep`, since `DropdownButton` is a child component) |

Keep the existing breakpoints (`max-width: 767px`, `max-width: 1024px`, `min-width: 768px`). Expect
the file to shrink by around a third, because the Bootstrap overrides and `!important`s go. Lists
get `list-style: none; margin: 0; padding: 0;`, which Bootstrap's `.navbar-nav` supplied.

Text colours: links white at 0.85 opacity, current/hover full white, as Bootstrap's `navbar-dark`
did. Check the colours against WCAG AA on `#212529` (white at 85% is fine).

`Bar*` and `Layout*` don't collide with anything in Common. Both files drop `@using Blazorise` in
this change. NavMenu's dropdowns are already `DropdownButton` from plan 05.

## Manual checks (developer)

- Nav looks the same at mobile (≤767px), tablet (768–1024px) and fullscreen (≥1025px): items, order,
  which items move into the hamburger, unit switch placement, CO₂ tile, bell.
- Current page highlighted. The screen reader announces "current page" on that link.
- Hamburger and options menus overflow the header and aren't clipped.
- Page height: footer-less pages still fill the viewport. There's no double scrollbar.

## Summary

`MainLayout` becomes a plain `header`/`main` layout. `NavMenu` swaps Blazorise's Bootstrap navbar
for its own `nav`/`ul` markup with the same items and responsive classes, and its 379-line
Bootstrap-override stylesheet is rewritten against our classes without the `!important` collapse
workaround. The current link gets `aria-current="page"`.

### Change size

- **Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.Web` (app.css).

| Area | Added | Changed | Removed |
|---|---|---|---|
| `MainLayout.razor` / `.razor.css` | — | 2 files, ~20 | ~70 lines (dead template CSS) |
| `NavMenu.razor` / `.razor.cs` | — | 2 files, ~70 | — |
| `NavMenu.razor.css` | — | 1 file, ~250 (rewrite) | ~120 lines net |
| `app.css` (nav token, layout-header rule moved) | — | 1 file, ~5 | — |
| **Total** | — | **6 files, ~345** | **~190 lines** |

Risk: medium. The nav is on every page, and the three breakpoints each need a visual check.
