# Pagination — Native `PaginationControl` Internals

- **Date:** 2026-10-28
- **Status:** Implemented 2026-10-28 (see addendum). Build and unit tests are clean; the UI is unverified.
- **Author:** Claude
- **Scope:** `Components/Common/PaginationControl.razor`, `.razor.cs`, `.razor.css`; `ClimateExplorer.UnitTests`
- **Builds on:** [Blazorise Removal — Overview](2026-10-28-01-blazorise-removal-overview-plan.md)
- **Branch context:** `development`
- **Implementation order:** step 5 of 18; see [overview](2026-10-28-01-blazorise-removal-overview-plan.md#implementation-order)

## Current state

`PaginationControl` is our component already. Its public API (`TotalPages`, `CurrentPage`,
`PageChanged`) and its windowing logic (`GetVisiblePages`: 9 slots on mobile, 15 otherwise, `-1`
for ellipsis) are ours. Only the rendering uses Blazorise `Pagination` / `PaginationItem` /
`PaginationLink`, which produce Bootstrap `.pagination` / `.page-item` / `.page-link` markup.
Consumers (`ChangeLocation`, `ClimateRecords`, `Locations`) don't change.

Problems in the rendered output: the links are `<a>` without `href` acting as buttons; there's no
`nav` landmark or label; and the current page isn't marked `aria-current`.

## Changes

### Markup

```razor
<nav class="climate-pagination @Class" aria-label="@AriaLabel">
    <ul>
        @foreach (var pageNum in GetVisiblePages())
        {
            @if (pageNum == -1)
            {
                <li class="climate-pagination-gap" aria-hidden="true">…</li>
            }
            else
            {
                var pageNumber = pageNum;
                <li>
                    <button type="button" class="climate-pagination-page"
                            aria-current="@(CurrentPage == pageNumber ? "page" : null)"
                            aria-label="@($"Page {pageNumber}")"
                            @onclick="() => PageChanged.InvokeAsync(pageNumber)">@pageNumber</button>
                </li>
            }
        }
    </ul>
</nav>
```

- New parameters: `AriaLabel` (default `"Pagination"`; callers can pass e.g. "Nearby locations pages")
  and `Class`.
- Clicking the current page is a no-op: skip `PageChanged` when `pageNumber == CurrentPage`.

### Logic for testing

`GetVisiblePages` is already pure apart from reading `isMobileDevice`. Make it
`public static List<int> GetVisiblePages(int totalPages, int currentPage, int maxPages)`. The
instance method passes `isMobileDevice == true ? 9 : 15`. Web.Client has no `InternalsVisibleTo`, so
`public` is the simplest way for `ClimateExplorer.UnitTests` to reach it.

### Styles (`PaginationControl.razor.css`)

Replace the Bootstrap-dependent rules with:

- `ul`: `display: flex; flex-wrap: wrap; gap: 0.25rem; list-style: none; margin: 0; padding: 0;`
- Page button: `min-width: 44px; min-height: 44px;` (touch target), radius 8px, background `#f8f8f8`,
  colour `#425f59`, hover `#cfc`, `:focus-visible` outline.
- `[aria-current="page"]`: background `#425f59`, colour white, `cursor: default`.
- Gap: same box size, muted colour. On mobile, 9 × 44px plus gaps is ~420px, more than a 375px
  screen, so allow wrapping (`flex-wrap`) or reduce `min-width` to 36px below 768px while keeping a
  44px height.

## Unit tests

`PaginationControlTests.cs`:

- `GetVisiblePages_TotalWithinMax_ReturnsAllPages`
- `GetVisiblePages_CurrentNearStart_HasTrailingEllipsisOnly`
- `GetVisiblePages_CurrentNearEnd_HasLeadingEllipsisOnly`
- `GetVisiblePages_CurrentInMiddle_HasBothEllipses`
- `GetVisiblePages_AnyInput_NeverExceedsMaxPages`

## Manual checks (developer)

- Change-location, ClimateRecords and Locations pagination look and work as before at all three breakpoints.
- Keyboard: Tab through pages, Enter/Space activate. The screen reader announces "Page 3, current page".

## Summary

`PaginationControl` keeps its API and windowing logic but renders a labelled `<nav>` with real
buttons and `aria-current="page"` instead of Blazorise/Bootstrap pagination markup. The windowing
logic becomes static and gets unit tests.

### Change size

- **Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.UnitTests`.

| Area | Added | Changed | Removed |
|---|---|---|---|
| `PaginationControl.razor` / `.razor.cs` / `.razor.css` | — | 3 files, ~70 | — |
| Unit tests | 1 file, +80 | — | — |
| **Total** | **1 file, +80** | **3 files, ~70** | — |

Risk: low.

## Addendum — implementation notes

### What shipped

- `PaginationControl.razor` renders the planned `<nav class="climate-pagination">` / `<ul>` / `<button>`
  markup, with `aria-current="page"`, an `aria-label` of "Page N" on each button, and `&hellip;` gaps
  marked `aria-hidden`. The `@using Blazorise` is gone.
- New parameters: `AriaLabel` (default "Pagination") and `Class`. Clicking the current page doesn't
  raise `PageChanged`.
- `GetVisiblePages(int totalPages, int currentPage, int maxPages)` is `public static`. The windowing
  logic is unchanged, and the instance overload passes 9 or 15.
- Scoped CSS as planned: 44px boxes, a 0.25rem gap, `flex-wrap`, the `--climate-*` control tokens, a
  `#cdd8d6` border (kept from the old `.page-link` override), and the current page in solid `#425f59`.
  Below 768px, `min-width` drops to 36px and the height stays 44px.
- `PaginationControlTests.cs` has the five planned tests, plus `GetVisiblePages_NoPages_ReturnsEmpty`.
  The `NeverExceedsMaxPages` test checks every total from 1 to 60 and every current page, for both 9
  and 15 slots. For each case it also checks that the first, last and current pages are present and in
  ascending order.

### Deviations from the plan

- **Consumers do change a little.** Each of the three passes its own `AriaLabel` ("Nearby locations
  pages", "Climate records pages", "Locations pages"), so the landmarks have different names.
- **`totalPages <= 0` returns an empty list.** Before, a negative count would have thrown in
  `Enumerable.Range`.
- **The `.page-link` / `.page-item` rules were removed from `table.css`.** Nothing renders those classes
  any more. Plan 16 expects each family to remove its own overrides.

### Visible changes to expect

- Page buttons are 44px high instead of 31px, which makes the table footers taller. They are now separate
  rounded buttons with a gap, not Bootstrap's joined button group. Hover is `#cfc`.

### Verification

- `dotnet build`: 0 errors, 0 warnings (separate artifacts path, as in plan 15).
- `ClimateExplorer.UnitTests`: 945 passed, 0 failed (939 before, plus 6 new).
- The manual checks above are still to do, mainly wrapping on a 375px screen in the change-location modal.
