# Agent Notes

- Dependency injection for Blazor components go in Blazor Server `ClimateExplorer.Web` and Blazor WebAssembly `ClimateExplorer.Web.Client` projects.
- Unit test names use `MethodName_StateUnderTest_ExpectedBehavior`.
- Razor components keep C# in `.razor.cs` code-behind files.
- Supported screen sizes:
  - Mobile: max-width = 767px
  - Tablet: min-width = 768px and max-width = 1024px
  - Full-size: min-width = 1025px