# Autocomplete — Native Combobox for Location Search

- **Date:** 2026-10-28
- **Status:** Proposed
- **Author:** Claude
- **Scope:** new `Components/Common/Autocomplete.*`, `AutocompleteFilter.cs`;
  `Components/Location/ChangeLocation.*`; `ClimateExplorer.UnitTests`
- **Builds on:** [Blazorise Removal — Overview](2026-10-28-01-blazorise-removal-overview-plan.md),
  [06 Modal](2026-10-28-06-modal-plan.md) (same file; do after it)
- **Branch context:** `development`
- **Implementation order:** step 15 of 18; see [overview](2026-10-28-01-blazorise-removal-overview-plan.md#implementation-order)

## Current state

The only use of `Blazorise.Components` is the location search in `ChangeLocation.razor`:

```razor
<Autocomplete TItem="Location" TValue="Guid" @ref="locationAutocomplete"
    Data="@LocationDictionary.Values" TextField="@(item => item.FullTitle)" ValueField="@(item => item.Id)"
    Placeholder="Location search..." Filter="AutocompleteFilter.Contains" FreeTyping MinSearchLength="2"
    SelectedValueChanged="@FireOnLocationChange" MaxMenuHeight="350px" @bind-SelectedText="@selectedText" />
```

- Data: every location, about 2,000 today (`Locations_ghcnm_adjusted.json` ~1,900 + Australia 112 + UK).
- Behaviour used: case-insensitive *contains* on `FullTitle`, from 2 characters; free typing (the
  text doesn't have to match an item); selecting an item raises `SelectedValueChanged(Guid)` and the
  modal closes; `SelectedText` is reset to null each time the modal opens; `Focus(scrollToElement: false)`
  is called after the modal opens.
- `SuggestedChartsMini.razor` has `@using Blazorise.Components`, but uses nothing from it. Remove
  that line in this change.

## Design

A generic `Autocomplete<TItem, TValue>` in Common, implementing the **ARIA 1.2 combobox** pattern
(editable combobox, list autocomplete, manual selection). Parameter names mirror Blazorise's, to
keep the call-site diff small.

### API

| Parameter | Notes |
|---|---|
| `Data` (`IEnumerable<TItem>`) | |
| `TextField` (`Func<TItem, string>`), `ValueField` (`Func<TItem, TValue>`) | |
| `MinSearchLength` (`int`, default 1) | |
| `MaxResults` (`int`, default 50) | New. Caps the rendered options. A footer reads "Keep typing to narrow results" when capped. |
| `MaxMenuHeight` (`string`, default `"350px"`) | |
| `Placeholder`, `AriaLabel` (`[EditorRequired]` unless a `<label for>` is used), `Id`, `Class` | |
| `SelectedText` / `SelectedTextChanged` | `@bind-SelectedText` works |
| `SelectedValueChanged` (`EventCallback<TValue>`) | Raised on choosing an option |
| `AutoFocus` (`bool`) | Renders `autofocus`. `showModal()` honours it (plan 06), replacing the post-open `Focus` call. |
| `FocusAsync()` | Kept for callers outside dialogs |

Filtering is always case-insensitive *contains*, the only mode in use. There's no `Filter` enum.

### Markup

```razor
<div class="climate-autocomplete @Class">
    <input id="@Id" type="text" role="combobox" class="climate-control climate-autocomplete-input"
           aria-autocomplete="list" aria-expanded="@(IsOpen ? "true" : "false")" aria-controls="@listId"
           aria-activedescendant="@ActiveOptionId" aria-label="@AriaLabel" placeholder="@Placeholder"
           autocomplete="off" autofocus="@AutoFocus" value="@SelectedText"
           @oninput="OnInputAsync" @onkeydown="OnKeyDownAsync" @onblur="OnBlur" @ref="input" />
    <ul id="@listId" role="listbox" class="climate-autocomplete-list" hidden="@(!IsOpen)" style="max-height: @MaxMenuHeight">
        @for (var i = 0; i < results.Count; i++)
        {
            var index = i;
            <li id="@OptionId(index)" role="option" aria-selected="@(index == activeIndex ? "true" : "false")"
                class="climate-autocomplete-option @(index == activeIndex ? "is-active" : null)"
                @onpointerdown="() => ChooseAsync(index)" @onpointerdown:preventDefault="true">@TextField(results[index])</li>
        }
        @if (capped) { <li class="climate-autocomplete-more" aria-hidden="true">Keep typing to narrow results</li> }
    </ul>
    <span class="visually-hidden" role="status">@StatusText</span> @* "23 results" / "No results" *@
</div>
```

- **Choosing with the pointer** uses `pointerdown` + `preventDefault`, so the input doesn't blur
  (and close the list) before the choice registers. That's the classic autocomplete bug.
- **Keyboard:** ArrowDown/ArrowUp move `activeIndex` (opening the list if closed); Enter chooses the
  active option; Escape closes the list, or clears the text if already closed; Tab closes. Only
  Enter, Escape and the arrows need handling. `@onkeydown:preventDefault` can't be per-key, so
  ArrowUp/Down caret movement in a single-line input is harmless and isn't prevented.
- The active option is scrolled into view (`element.scrollIntoView({ block: "nearest" })`) via a tiny
  collocated `Autocomplete.razor.js`, run after the render that changes `activeIndex`.
- **Positioning:** the list is `position: absolute` under the input inside a `position: relative`
  wrapper. Inside the change-location `<dialog>` it overlays the table below, as today. If it turns
  out to be clipped by the dialog's scrolling body, switch the list to `popover="manual"` placed with
  `floating.js` (plan 04). That's a contained change.
- **Debounce:** filtering ~2,000 strings per keystroke is cheap in WASM. Debounce the filter by
  ~120 ms anyway, to avoid re-rendering the list on every keystroke while typing fast (and to keep
  Server-mode round trips down before the WASM handoff).

### Filtering (pure, unit-tested)

`AutocompleteFilter.Filter<TItem>(IEnumerable<TItem> data, Func<TItem,string> text, string? query, int minLength, int max, out bool capped)`:

- `query` trimmed. If shorter than `minLength`, return empty.
- `text(item).Contains(query, StringComparison.OrdinalIgnoreCase)`.
- **Order:** prefix matches first, then other matches, each group in data order. This is an
  improvement over Blazorise's plain data order: "Syd" puts "Sydney…" first.
- Take `max`, and set `capped` if more matched.

### `ChangeLocation` changes

- Swap the tag. Keep `TextField`, `ValueField`, `MinSearchLength`, `MaxMenuHeight`,
  `SelectedValueChanged`, `@bind-SelectedText`. Drop `Filter`/`FreeTyping` (always free typing).
  Add `AutoFocus`, `AriaLabel="Search locations"`.
- Remove the `Autocomplete<Location, Guid>?` ref type's Blazorise import. If plan 06 has landed,
  also remove the `isModalOpen`/`shouldFocusLocationSearch`/`FocusLocationSearch` machinery
  (`autofocus` covers it). Otherwise keep it, calling our `FocusAsync()`.
- Remove `@using Blazorise.Components` from `ChangeLocation.razor` and `.razor.cs`, and from `SuggestedChartsMini.razor`.

## Unit tests

`AutocompleteFilterTests.cs`:

- `Filter_QueryShorterThanMinLength_ReturnsEmpty`
- `Filter_MixedCaseQuery_MatchesCaseInsensitively`
- `Filter_PrefixAndInnerMatches_ReturnsPrefixMatchesFirst`
- `Filter_MoreMatchesThanMax_ReturnsMaxAndSetsCapped`
- `Filter_QueryWithSurroundingWhitespace_IsTrimmed`
- `Filter_NoMatches_ReturnsEmptyAndNotCapped`

## Manual checks (developer)

- Opening Change location focuses the search box. Typing "syd" shows Sydney first. Arrow keys move
  the highlight, Enter picks it, and the modal closes and the location changes.
- Mouse/touch selection works first time (no blur race).
- Escape closes the list without closing the modal on the first press. A second Escape closes the
  modal. (The input handles the first Escape and should stop the dialog `cancel`; verify, and if the
  dialog closes on the first press, call `preventDefault` on the dialog's `cancel` while the list is open.)
- The screen reader announces the result count and the active option.
- Reopening the modal shows an empty search box.

## Summary

A generic `Autocomplete<TItem, TValue>` implementing the ARIA combobox pattern replaces Blazorise's
Autocomplete in the change-location modal. It keeps the same parameter names, caps the rendered
results, ranks prefix matches first, and its filtering is a pure, unit-tested helper. This removes
the `Blazorise.Components` package dependency.

### Change size

- **Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.UnitTests`.

| Area | Added | Changed | Removed |
|---|---|---|---|
| `Autocomplete.razor` / `.razor.cs` / `.razor.css` / `.razor.js`, `AutocompleteFilter.cs` | 5 files, +300 | — | — |
| `ChangeLocation.razor` / `.razor.cs`, `SuggestedChartsMini.razor` | — | 3 files, ~25 | ~25 lines (focus machinery, if 06 landed) |
| Unit tests | 1 file, +90 | — | — |
| **Total** | **6 files, +390** | **3 files, ~25** | **~25 lines** |

Risk: medium. Combobox keyboard and blur timing are easy to get subtly wrong, so the manual checks matter.
