# Copilot Instructions

## Don'ts

- **Never build `ClimateExplorer.DataPipeline`** — its build step creates large zip files.
- **No `async void`** — event handlers and lifecycle methods must return `Task`.
- **No `new List<T>()`** — use C# 14 collection expressions: `[]`, `[a, b, c]`, `[.. existing, item]`.
- **No `out` parameters** — prefer tuple returns for multiple values.
- **Do not add tests to `ClimateExplorer.Web.UiTests`** unless explicitly asked — that project is for UI/integration tests only.

---

## Blazorise 2.x — Always Use `Value` / `ValueChanged`

All input components bind via `Value` / `ValueChanged`. The old names no longer exist.

| Component | Correct | Wrong |
|---|---|---|
| `Check<T>` | `Value` / `ValueChanged` | `Checked` / `CheckedChanged` |
| `TextInput` | `Value` / `ValueChanged` | `Text` / `TextChanged` |
| `Select<T>` | `Value` / `ValueChanged` | `SelectedValue` / `SelectedValueChanged` |
| `NumericEdit<T>` | `Value` / `ValueChanged` | `Number` / `NumberChanged` |
| `Switch<T>` | `Value` / `ValueChanged` | `Checked` / `CheckedChanged` |
| `DateEdit<T>` | `Value` / `ValueChanged` | `Date` / `DateChanged` |

All modals: use `Class="custom-modal-header"` on `<ModalHeader>` (dark green bg, white text).

---

## C# Conventions

- Target **.NET 10 / C# 14**; nullable reference types are enabled in every project.
- Add `using static ClimateExplorer.Core.Enums;` in any file that references domain enums.
- Use `LogAugmenter` (wraps `ILogger` with method name + elapsed time) in all significant methods:
  ```csharp
  var log = new LogAugmenter(Logger!, nameof(MyMethod));
  log.LogInformation("Starting");
  ```

---

## Blazor / Razor Component Rules

- Every component gets a co-located `.razor.css` scoped stylesheet.
- Use `::deep` to style Blazorise child elements from a parent's `.razor.css`.
- **CSS isolation problem:** If a component's root element is a Blazorise component (not a plain HTML tag), Blazor cannot stamp the CSS scope attribute and `::deep` rules silently do nothing.
  **Fix:** Wrap in a plain `<div style="display: contents">` as the outer root — layout-transparent but gives Blazor a native element to stamp.
- Use `@key="series.Id"` (the `Guid` on `ChartSeriesDefinition`) on every `foreach` that renders `ChartSeriesView`.
- Call `CreateNewListWithoutDuplicates()` after any mutation to `ChartSeriesList`.

---

## CSS Rules

- Colours: primary green `#425f59`, hover `#cfc`, info blue `#79c6f4`, danger red `#a00000`, light bg `#f8f8f8`.
- Radii: `8px` buttons/controls · `12px` cards · `16px` modals/panels.
- Responsive breakpoints: mobile ≤ `1024px`, desktop ≥ `1025px`.
- `chart-control-download` must be declared **after** `.chart-control` in the stylesheet so `margin-left: auto` wins the cascade.

---

## Testing

- Use **MSTest** (`[TestClass]` / `[TestMethod]`) and **Moq**.
- Test files live flat in the `ClimateExplorer.UnitTests` project root (no subdirectories).
