# Dropdown — Native `DropdownButton` with Popover

- **Date:** 2026-10-28
- **Status:** Implemented 2026-10-28 (see addendum). Build and unit tests are clean; the UI is unverified.
- **Author:** Claude
- **Scope:** `Components/Common/DropdownButton.*` (rewritten), new `Components/Common/DropdownItem.*`,
  new `DropdownButton.razor.js`, consumers `ChartView`, `ChartAxisListView`, `Top100`,
  `LocationDashboard`, `Layout/NavMenu` (its two raw Blazorise dropdowns), `app.css`
- **Builds on:** [Blazorise Removal — Overview](2026-10-28-01-blazorise-removal-overview-plan.md),
  [04 Tooltip](2026-10-28-04-tooltip-plan.md) (for `floating.js`)
- **Branch context:** `development`
- **Implementation order:** step 13 of 18; see [overview](2026-10-28-01-blazorise-removal-overview-plan.md#implementation-order)

## Current state

`DropdownButton` (Common) wraps Blazorise `Dropdown`/`DropdownToggle`/`DropdownMenu`. Parameters:
`Text`, `Icon`, `AriaLabel`, `Class`, `ChildContent`, `MenuContent`, captured attributes.
The toggle gets class `climate-dropdown-toggle`, styled in `app.css` (including `.show` and the `::after` caret).

| Consumer | Menu content |
|---|---|
| `ChartView.razor` | "Grouping": 4 `DropdownItem`s |
| `ChartAxisListView.razor` | "Axes": `DropdownHeader` + one Blazorise `Check` per axis |
| `Top100.razor` | "Download": 2 `DropdownItem`s (PNG, SVG) |
| `LocationDashboard.razor` | icon-only cog: a `Check` (precipitation stripe) + a `DropdownItem` with `Disabled` and `aria-label` |
| `NavMenu.razor` (raw `Dropdown` ×2) | hamburger: `<a class="dropdown-item">` links, a `<button class="dropdown-item">`, `DropdownDivider`, `UnitSwitch`; options cog: `UnitSwitch` |

Scoped CSS that targets Bootstrap dropdown classes (`.dropdown-menu`, `.dropdown-item`, `.dropdown`):
`NavMenu.razor.css` (most), `LocationDashboard.razor.css`, `Top100.razor.css`, `ChartAxisListView`
(via `.axes-menu-content`), and `app.css`. Overall, `.dropdown-menu` appears ~99 times across the CSS
(scoped bundles inflate that count; the source files are the ones listed).

## Design

### Semantics: disclosure, not ARIA menu

Several menus contain checkboxes, a unit switch and links, so they aren't ARIA `menu`s. Use the
**disclosure** pattern: a `<button>` that shows and hides a panel. Tab moves through the panel's
controls in order, and Escape closes it and returns focus to the button. This matches how the
menus are used today, and it avoids `role="menu"`'s arrow-key contract.

### Mechanics: `popover="auto"` + `popovertarget`

```razor
<div class="climate-dropdown">
    <button type="button" class="climate-dropdown-toggle @Class" @ref="toggle"
            popovertarget="@(isInteractive ? menuId : null)" disabled="@(!isInteractive)"
            aria-label="@EffectiveAriaLabel" @attributes="AdditionalAttributes">
        @if (!string.IsNullOrWhiteSpace(Icon)) { <i class="@Icon" aria-hidden="true"></i> }
        @(ChildContent ?? (RenderFragment)(b => b.AddContent(0, Text)))
    </button>
    <div id="@menuId" popover="auto" class="climate-dropdown-menu" @ref="menu">
        <CascadingValue Value="this" IsFixed="true">@MenuContent</CascadingValue>
    </div>
</div>
```

What the platform gives us without code: opening and closing on click, **light dismiss** (click
outside), **Escape**, only one `auto` popover open at a time, the **top layer** (no clipping by
`overflow: hidden` ancestors such as Collapsible and chart cards), focus returning to the toggle on
dismiss, popover content following the toggle in Tab order, and an exposed `aria-expanded` state on the toggle.

What we add:

- **Positioning.** `DropdownButton.razor.js` registers a `toggle` event listener on the menu. On open
  it calls `floating.place(toggle, menu, { placement: "bottom-start" })` plus `autoUpdate`, and on
  close it runs the cleanup. It's registered once in `OnAfterRenderAsync(firstRender)` and disposed
  in `DisposeAsync`, swallowing `JSDisconnectedException`.
- **Prerender.** Until `RendererInfo.IsInteractive`, render the toggle `disabled` without
  `popovertarget`, so an SSR-rendered menu can't open unpositioned with dead items.
- **Unique id.** `menuId = $"dd-{Guid.NewGuid():N}"` per instance. The prerender and interactive
  renders differ, and Blazor patches both attributes together.
- **Close on navigation.** NavMenu's hamburger links navigate without leaving the layout, so a popover
  would stay open. `DropdownButton` subscribes to `NavigationManager.LocationChanged` and hides its
  menu via JS. A public `CloseAsync()` is exposed as well.
- **Open styling.** Replace `.climate-dropdown-toggle.show` with
  `.climate-dropdown:has(.climate-dropdown-menu:popover-open) .climate-dropdown-toggle`.

### `DropdownItem`

```razor
<button type="button" class="climate-dropdown-item @Class" disabled="@Disabled"
        popovertarget="@Parent.MenuId" popovertargetaction="hide"
        @onclick="OnClick" @attributes="AdditionalAttributes">@ChildContent</button>
```

`popovertargetaction="hide"` closes the menu natively on click, and the Blazor `@onclick` still
runs. `Parent` is the cascaded `DropdownButton`. Parameters: `OnClick` (`EventCallback<MouseEventArgs>`),
`Disabled`, `Class`, `ChildContent`, captured attributes (for `aria-label`).

`DropdownItem` collides with Blazorise's `DropdownItem`. Per the overview's coexistence rule, all 7
uses (ChartView, Top100, LocationDashboard) move in this change, and those files drop `@using Blazorise`.
Any remaining Blazorise tags in them (`Check` in LocationDashboard, if
[08 Checkbox](2026-10-28-08-checkbox-plan.md) hasn't landed yet) get fully qualified.

### Header and divider: plain markup

One use each, no behaviour, so no components:

- `ChartAxisListView`: `<DropdownHeader>Scale from 0</DropdownHeader>` → `<div class="climate-dropdown-header">Scale from 0</div>`
- `NavMenu`: `<DropdownDivider />` → `<hr class="climate-dropdown-divider" />`

### NavMenu's raw dropdowns

Both become `DropdownButton`:

- Hamburger: `<DropdownButton Icon="fas fa-bars" AriaLabel="More navigation options" Class="@HamburgerClass">`.
  The `<a>` items keep `href` and switch `dropdown-item` → `climate-dropdown-item`. Close-on-navigation
  handles closing the menu. The site-overview `<button>` becomes a `DropdownItem`.
- Options cog: `<DropdownButton Icon="fas fa-cog" AriaLabel="Options" Class="options-toggle">`.

The rest of NavMenu (the `Bar*` components) is [14 Layout and navbar](2026-10-28-14-layout-navbar-plan.md).

### Styles

Move menu styling into `DropdownButton.razor.css` (`::deep` for `MenuContent` children). Global item
classes (`climate-dropdown-item`, `-header`, `-divider`) go in `app.css`, because items can be
rendered from any component:

- Menu: reset UA popover styles (`margin: 0; inset: auto; padding: 0.25rem 0;`), background white,
  border `1px solid #c4d4d1`, radius 8px (controls), `box-shadow: var(--climate-shadow-panel)`,
  `min-width: 10rem`, `max-height: calc(100dvh - 16px); overflow: auto`.
- Item: `display: flex; gap: 0.5rem; width: 100%; min-height: 44px;` (touch target), left-aligned,
  hover `#cfc`, `:focus-visible` outline, disabled at 50% opacity.
- The caret `::after` moves from `app.css` unchanged.

Then delete the Bootstrap-targeted rules in the consumer CSS listed above and re-express the few
bespoke ones (`.axes-menu-content`, `.dropdown-menu-content`, `.dropdown-item-icon`) against the new classes.

## Manual checks (developer)

- Each menu opens below its toggle and flips up near the bottom of the viewport. It isn't clipped
  inside the chart card or Collapsible.
- Click outside, Escape, and choosing an item all close the menu. Focus returns to the toggle on Escape.
- Tab moves through menu controls (axis checkboxes, unit switch).
- Hamburger link navigates and the menu closes. The current page is highlighted.
- Disabled "Data sources" item can't be activated, and screen readers announce it as disabled.
- Opening one menu closes any other open menu.
- Toolbar and nav look unchanged at mobile, tablet and fullscreen widths.

## Summary

`DropdownButton` is rebuilt on `popovertarget`/`popover="auto"`, which provides open/close, light
dismiss, Escape, focus return and top-layer rendering natively. A small module positions the menu
with the shared `floating.js` and closes it on navigation. A new `DropdownItem` closes the menu via
`popovertargetaction="hide"`, and NavMenu's two raw Blazorise dropdowns become `DropdownButton`s.

### Change size

- **Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.Web` (app.css).

| Area | Added | Changed | Removed |
|---|---|---|---|
| `DropdownButton.razor` / `.razor.cs` (rewrite) + new `.razor.css`, `.razor.js` | 2 files, +110 | 2 files, ~70 | — |
| `DropdownItem.razor` / `.razor.cs` | 2 files, +45 | — | — |
| Consumers: `ChartView`, `ChartAxisListView`, `Top100`, `LocationDashboard` | — | 4 files, ~25 | — |
| `NavMenu.razor` dropdown markup | — | 1 file, ~30 | — |
| Consumer scoped CSS (`NavMenu`, `LocationDashboard`, `Top100`, `ChartAxisListView`) | — | 4 files, ~80 | — |
| `app.css` dropdown styles | — | 1 file, ~40 | — |
| **Total** | **4 files, +155** | **12 files, ~245** | — |

Risk: medium. NavMenu's mobile menu and the top-layer stacking need the manual checks above.

## Addendum — implementation notes

Shipped as planned, with these deviations:

- **No `LocationChanged` subscription.** The chart page rewrites its URL when chart state changes, so
  ticking an axis checkbox in the Axes menu would have closed the menu. Instead,
  `DropdownButton.razor.js` closes the menu when a link inside it (`a[href]`) is clicked, which covers
  NavMenu's hamburger. `CloseAsync()` is still public.
- **Styles are global, in `app.css`, not in `DropdownButton.razor.css`.** NavMenu restyles the menu
  (dark green) with `::deep .climate-dropdown-menu`. Against a scoped `.climate-dropdown-menu[b-…]` rule
  that's a specificity tie decided by the scoped-bundle order. A single global class keeps every consumer
  override winning. No scoped CSS file was added.
- **The caret is drawn by `.climate-dropdown-toggle::after`.** It used to come from Bootstrap's
  `.dropdown-toggle::after` (Blazorise's toggle carried that class). `app.css` only set its margin.
- **`AlignEnd` parameter** (`bottom-end` placement) for the two NavMenu toggles at the end of the bar.
  It replaces the `right: 0` and `position: fixed; top: 65px` rules on their menus.
- **No flash at the UA position.** The `toggle` event fires after the popover is shown, so
  `.climate-dropdown-menu:popover-open:not([data-placement])` is `visibility: hidden` until `place()` runs.
  `data-placement` is removed on close.
- **Item height** is `min-height: 2rem` with a mouse and 44px for `pointer: coarse`, per AGENTS.md, rather
  than 44px always. LocationDashboard keeps 44px items so they match its menu-row checkbox.
- **Top100's item hover** is now the shared `#cfc`, replacing its own `#f8f8f8`.
- **NavMenu still imports Blazorise** (for `Bar*`, until [14](2026-10-28-14-layout-navbar-plan.md)), so its one
  `DropdownItem` is written fully qualified. The site-overview icon gained `aria-hidden="true"`.
- `.options-toggle` gets `gap: 0.255em` so the cog-to-caret spacing matches Bootstrap's old caret margin.
- `header.layout-header { overflow: visible }` in `app.css` (for the old hamburger menu) is no longer needed
  by the dropdowns, since they render in the top layer. It's left in place for
  [14 Layout and navbar](2026-10-28-14-layout-navbar-plan.md) to remove.

### Files

- New: `DropdownButton.razor.js`, `DropdownItem.razor`, `DropdownItem.razor.cs`.
- Changed: `DropdownButton.razor`, `DropdownButton.razor.cs`, `ChartView.razor` (+ `.razor.css`),
  `ChartAxisListView.razor`, `Top100.razor` (+ `.razor.css`), `LocationDashboard.razor` (+ `.razor.css`),
  `NavMenu.razor` (+ `.razor.cs`, `.razor.css`), `app.css`.
- `ChartView`, `ChartAxisListView`, `Top100` and `LocationDashboard` no longer import Blazorise.

### Follow-ups

- Run the manual checks above. The JS couldn't be run here.
- `AggregationOptions`' toggle still uses Bootstrap's `dropdown-toggle` class for its caret. It isn't a
  dropdown (it opens a panel), so it's left for [16 Bootstrap removal](2026-10-28-16-bootstrap-removal-plan.md).
