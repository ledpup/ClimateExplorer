# Text Input and Validation — Native `TextInput`

- **Date:** 2026-10-28
- **Status:** Implemented 2026-10-28, together with [07](2026-10-28-07-select-plan.md) (see addendum). Build and unit tests are clean; the UI is unverified.
- **Author:** Claude
- **Scope:** new `Components/Common/TextInput.*`; `Components/Chart/ChartSeriesView.*`,
  `Components/Chart/Trend/ChartSeriesTrendControls.*`; their scoped CSS; `ClimateExplorer.UnitTests`
- **Builds on:** [Blazorise Removal — Overview](2026-10-28-01-blazorise-removal-overview-plan.md)
- **Branch context:** `development`
- **Implementation order:** step 10 of 18; see [overview](2026-10-28-01-blazorise-removal-overview-plan.md#implementation-order)

## Current state

Two Blazorise `Validation` + `TextInput` + `Feedback`/`ValidationError` blocks:

| File | Field | Validator | On change |
|---|---|---|---|
| `ChartSeriesView` | Custom expression (`Immediate="false"`, placeholder) | `ValidateCustomTransformation(ValidatorEventArgs)`: `null` → `None`; `CustomTransformationParser.Parse` succeeds → `Success`; throws → `Error` | `validation.Validate()`; if `Success`, raise `OnSeriesChanged` |
| `ChartSeriesTrendControls` | Predict until (`Immediate="false"`, `Disabled` when no periods, `aria-label`) | `ValidatePredictionYears`: parses an int, then `TrendPredictionRange.IsValid(year − anchor)` | `predictionYearsValidation.Validate()`; if not `Success`, return; otherwise update the request |

Blazorise behaviour to preserve: `Immediate="false"` means `ValueChanged` fires on `change` (blur or
Enter), not on each keystroke. The error message shows under the input with `is-invalid` styling
(`.invalid-feedback`, `.form-control.is-invalid`).

Code-behind uses `ValidationStatus`, `ValidatorEventArgs` and `Validation` refs, all Blazorise types.

## Design

Validation stays in the parent, as plain C#. `TextInput` only displays state. There's no
validation framework: two fields don't justify one.

### `TextInput` API

| Parameter | Notes |
|---|---|
| `Value` / `ValueChanged` (`string?`) | Fires on `change` (same as `Immediate="false"`) |
| `Immediate` (`bool`, default `false`) | If true, binds `oninput` instead. No current use, but it's cheap and avoids surprise. |
| `Placeholder`, `Disabled`, `Compact`, `Id`, `Class`, captured attributes | `Compact` replaces `Size="Size.Small"` |
| `Invalid` (`bool`) | Sets `aria-invalid="true"` and the invalid style |
| `ErrorMessage` (`string?`) | Rendered under the input when `Invalid`, in an element whose id goes in the input's `aria-describedby` |

```razor
<span class="climate-text-input-wrap @Class">
    <input id="@Id" type="text" class="climate-text-input @(Compact ? "climate-text-input--compact" : null)"
           value="@Value" placeholder="@Placeholder" disabled="@Disabled"
           aria-invalid="@(Invalid ? "true" : null)" aria-describedby="@(Invalid ? errorId : null)"
           @onchange="OnChangeAsync" @attributes="AdditionalAttributes" />
    @if (Invalid && !string.IsNullOrEmpty(ErrorMessage)) { <span id="@errorId" class="climate-text-input-error">@ErrorMessage</span> }
</span>
```

Styling uses the same tokens as `Select` (control background, 8px radius, primary-green text). The
invalid state is a `#a00000` border and the error text in `#a00000` at `0.75rem`. Pull the shared
control rules (border, radius, focus ring) into `app.css` as a `climate-control` base class used by
`Select` and `TextInput`, so the two can't drift.

`TextInput` collides with Blazorise's `TextInput`. Both consumers are also `Select` consumers, so do
this in the same change as [07 Select](2026-10-28-07-select-plan.md), or right after it.

### Validation logic → pure helpers

- `ChartSeriesView`: add `CustomTransformationParser.IsValid(string? expression)` (or a `TryParse`)
  next to `Parse` in `ClimateExplorer.Core/DataPreparation/CustomTransformationParser.cs`. It returns false on parse failure instead of throwing at the
  call site. The component keeps a `bool customTransformationInvalid`:
  - empty/`null` → not invalid, **don't** raise `OnSeriesChanged` (matches `ValidationStatus.None`);
  - valid → not invalid, raise `OnSeriesChanged`;
  - invalid → invalid, don't raise.
- `ChartSeriesTrendControls`: add `TryParsePredictUntilYear(string? text, int anchorYear, out int targetYear)`
  to `UiModel/Trends/TrendPredictionRange.cs`. `OnPredictionYearsChanged` uses it, sets
  `predictionYearsInvalid`, and returns early when invalid, exactly as now. The existing
  `PredictUntilValidationMessage` becomes the `ErrorMessage`.
- Remove the `Validation` refs, `ValidatorEventArgs` methods and the `using Blazorise;` lines from both
  code-behind files (if nothing else in them still needs Blazorise by then).

### Labels

Give each input an `Id` and turn its row's `<span class="form-label">` into `<label for>`, the same
as plans 07/08. That gives "Custom expression" an accessible name. "Predict until" can drop its
`aria-label` once the visible label is associated.

## Unit tests

Find the existing test class for the parser, if there is one, and add to it. Otherwise create
`CustomTransformationParserTests.cs`:

- `IsValid_NullExpression_ReturnsFalse` (the caller treats null separately; document the choice in the test name)
- `IsValid_SimpleComparison_ReturnsTrue` (`x > 25`)
- `IsValid_CompoundExpression_ReturnsTrue` (`x >= 1 AND x < 10`)
- `IsValid_Garbage_ReturnsFalse`

`TrendPredictionRangeTests` (new or existing):

- `TryParsePredictUntilYear_YearInRange_ReturnsTrueAndYear`
- `TryParsePredictUntilYear_NonNumeric_ReturnsFalse`
- `TryParsePredictUntilYear_YearOutOfRange_ReturnsFalse`

## Manual checks (developer)

- Typing an invalid expression and tabbing away shows the message under the field with a red
  border, and the chart doesn't update. A valid one updates the chart and clears the message.
- Clearing the field doesn't update the chart and shows no error (same as today).
- Predict until: an out-of-range year shows the range message. A valid year extends the projection.
  It's disabled when no periods are selectable.
- The screen reader announces "invalid" and reads the error message as the description.

## Summary

A small `TextInput` (change-bound, with `Invalid`/`ErrorMessage` display) replaces Blazorise's
`Validation`/`TextInput`/`Feedback` stack in two chart forms. The validation rules move into pure,
unit-tested helpers, and the inputs get associated labels and `aria-invalid`/`aria-describedby`.

### Change size

- **Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.Web` (app.css shared control class),
  `ClimateExplorer.UnitTests`, `ClimateExplorer.Core` (`CustomTransformationParser`).

| Area | Added | Changed | Removed |
|---|---|---|---|
| `TextInput.razor` / `.razor.cs` / `.razor.css` | 3 files, +110 | — | — |
| Parser/range helpers | — | 2 files, ~25 | — |
| `ChartSeriesView`, `ChartSeriesTrendControls` (Razor + code-behind) | — | 4 files, ~50 | ~30 lines (validator methods) |
| Scoped CSS + `app.css` (`climate-control`) | — | 3 files, ~25 | ~10 lines (`.form-control`, `.invalid-feedback`) |
| Unit tests | 1–2 files, +80 | — | — |
| **Total** | **4–5 files, +190** | **9 files, ~100** | **~40 lines** |

Risk: low.

## Addendum — implementation (2026-10-28)

- `Immediate` was left out. Nothing uses it, and switching between `@onchange` and `@oninput` means either two copies
  of the `<input>` markup or a nullable delegate, which `TreatWarningsAsErrors` rejects. Add it when something needs it.
- `.climate-control` in `app.css` holds the shared box (control background, 8px radius, primary-green text, border,
  `#cfc` hover, `:focus-visible` outline, 60% opacity when disabled). `TextInput.razor.css` adds padding and height,
  the `#a00000` invalid border and the `0.75rem` error text. The wrapper is `display: block` so the message sits under
  the input. The error id is generated per instance (`text-input-error-{guid}`).
- `CustomTransformationParser.IsValid` catches `FormatException` as well as `ArgumentException`. `x > 1.2.3` matches
  the regex but fails `double.Parse`.
- Behaviour change: clearing the custom expression shows no error. Blazorise sent `""` rather than `null`, so before this
  change an empty field failed validation. As the plan says, an empty or whitespace-only field now means "nothing entered
  yet": no error and no chart rebuild.
- `ChartSeriesTrendControls.OnParametersSet` already resets the text when it's out of step with the request. It now
  clears `predictionYearsInvalid` at the same time, so an error can't stay next to a value that has been reset.
- Extra tests beyond the plan: `IsValid_MalformedNumber_ReturnsFalse`, `IsValid_UnsupportedOperator_ReturnsFalse`,
  `TryParsePredictUntilYear_AnchorYear_ReturnsFalse`.
