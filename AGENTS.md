# Agent Notes

- A plan or analysis document goes in docs/design. See [readme](docs/design/README.md)
- Do not run the website, Playwright, Lighthouse, or browser tests. This includes starting
  `dotnet run`/dev servers for the Web or WebApi projects for manual or automated UI verification.
  Verify with `dotnet build` and the unit test projects only (`ClimateExplorer.Core.UnitTests`,
  `ClimateExplorer.WebApi.UnitTests`, `ClimateExplorer.Web.UnitTests`).
- If a build fails because IIS Express (or another dev server the user started) has locked files in a `bin`
  folder, you may stop that process and rebuild.
- When estimating effort, don't give time estimates (hours/days). Report how many files would
  change and by roughly how much (e.g. "3 files, ~40 lines; 1 new file").
- Architecture, component and data-layer reference: [CONTEXT.md](CONTEXT.md)

## C#

- Unit test names use `MethodName_StateUnderTest_ExpectedBehavior`
- No `async void`; event handlers return `Task`

## Blazor

- Dependency injection for Blazor components go in Blazor Server `ClimateExplorer.Web` and Blazor WebAssembly `ClimateExplorer.Web.Client` projects
- Razor components keep C# in `.razor.cs` code-behind files
- Supported screen sizes:
  - Mobile: max-width = 767px
  - Tablet: min-width = 768px and max-width = 1024px
  - Fullscreen: min-width = 1025px
- UI controls must have an accessible name
- Smallest allowed `font-size` for any label/text is `0.75rem` (12px). Don't use keyword sizes like
  `x-small`/`smaller`/`xx-small`, or px/rem/em values below that, even for secondary/caption text.
- Utility classes are limited to those in `utilities.css`; prefer component-scoped CSS, and don't add new
  utilities without a reason.
- `CssClassUsageTests` fails on a CSS class selector that no markup or code uses, and on a scoped `.razor.css`
  selector that styles a child component's markup (including a class passed through its `Class` parameter)
  without `::deep`. Delete the rule or fix the selector; a class built at runtime goes in its allow-list.

Use the following existing common components, when appriopriate:
- Autocomplete (text box with suggestions; needs an `AriaLabel`, or a `<label for>` pointing at its `Id`)
- Button (`<Button>` is the component and `<button>` the HTML element; Razor matches tags case-sensitively, so
  don't "fix" one into the other)
- Checkbox
- Collapsible
- DropdownButton
- InfoPanel
- LoadingIndicator
  - Overlay (default): wraps existing content that stays visible underneath while it refreshes (chart, tables,
    dashboard fields).
  - Inline (`Variant="LoadingIndicatorVariant.Inline"`): sits where content will go when there's nothing to show yet
    (first load of a panel or list).
- Modal (`Title` or `TitleContent`; open with `ShowAsync()` or `@bind-Visible`; the body only renders while open)
- OverviewField
- PaginationControl
- Select / SelectItem (needs a `<label for>` pointing at its `Id`, or an `aria-label`)
- SidePanel
- Tabs / Tab (needs an `AriaLabel`; it renders the tab strip only, and the parent renders the selected content)
- TextInput (fires on change, not per keystroke; the parent validates and passes `Invalid`/`ErrorMessage`)
- Tooltip

There are Blazorise components in use, with common styling in `app.css` such as:
- Table

Common inputs (`Select`, `TextInput`, `Checkbox`) bind with `Value`/`ValueChanged` (not `Checked`, `Text`, `SelectedValue`, etc.).
Style Blazorise child elements with `::deep`; if a component's root is a Blazorise component,
wrap it in a `<div>` with `display: contents` so the scoped CSS applies.

### Colours

Use the `--climate-*` variables in `app.css` where one exists.

| Role | Hex |
|---|---|
| Primary green (text, modal headers, accents) | `#425f59` |
| Hover (all interactive elements) | `#cfc` |
| Info icons (`fa-circle-info`) | `#79c6f4` |
| Danger (trash/delete) | `#a00000` |
| Control background | `#f8f8f8` |
| Secondary text | `#595959` |

Border radius: controls 8px, cards 12px, modals/InfoPanel 16px.

Target size: 44px minimum height for touch (`@media (pointer: coarse)`, WCAG 2.5.5 AAA). With a mouse/trackpad,
controls may be compact, but never below 24px (WCAG 2.5.8 AA). `Select` and `TextInput` already do this via
`.control` in `app.css`.

## Chart.js

Chart.js is vendored in `ClimateExplorer.Web.Client/wwwroot/lib/chartjs` (4.6.1). The `seriesNote` plugin in
`ChartJsChart.razor.js` wraps the private `Legend._fitRows`; when upgrading Chart.js, check it still exists and
behaves the same.

## Font Awesome

Font Awesome Free 6.7.2 is vendored in `ClimateExplorer.Web.Client/wwwroot/lib/fontawesome`, solid style only
(`fas`/`fa-solid`). Using a `far`/`fab` icon means vendoring that style's CSS and font too. It is the only icon font;
don't add Open Iconic or other icon sets.