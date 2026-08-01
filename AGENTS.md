# Agent Notes

- A plan or analysis document goes in docs/design. See [readme](docs/design/README.md)
- Do not run the website, Playwright, Lighthouse, or browser tests. This includes starting
  `dotnet run`/dev servers for the Web or WebApi projects for manual or automated UI verification.
  Verify with `dotnet build` and the unit test suite only.

## C#

- Unit test names use `MethodName_StateUnderTest_ExpectedBehavior`

## Blazor

- Dependency injection for Blazor components go in Blazor Server `ClimateExplorer.Web` and Blazor WebAssembly `ClimateExplorer.Web.Client` projects
- Razor components keep C# in `.razor.cs` code-behind files
- Supported screen sizes:
  - Mobile: max-width = 767px
  - Tablet: min-width = 768px and max-width = 1024px
  - Fullscreen: min-width = 1025px
- UI controls must have an accessible name

Use the following existing common components, when appriopriate:
- ClimateButton
- Collapsible
- DelayedLoadingIndicator
- DelayedTooltip
- DropdownButton
- InfoPanel
- OverviewField
- PaginationControl
- SidePanel

There are Blazorise components in use, with common styling in `app.css` such as:
- Select
- Table
- Tabs