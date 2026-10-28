namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;

/// <summary>
/// A native <c>&lt;select&gt;</c> whose options are <see cref="SelectItem{TValue}"/> children. Needs a
/// <c>&lt;label for&gt;</c> pointing at <see cref="Id"/>, or an <c>aria-label</c>.
/// </summary>
public partial class Select<TValue>
{
    private readonly SelectValueMap<TValue> values = new();

    [Parameter]
    public TValue Value { get; set; } = default!;

    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// The id of the <c>&lt;select&gt;</c>, so a <c>&lt;label for&gt;</c> elsewhere can name it.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CurrentKey => SelectValueMap<TValue>.KeyFor(Value);

    private string CssClass =>
        string.IsNullOrWhiteSpace(Class)
            ? "climate-control climate-select"
            : $"climate-control climate-select {Class}";

    internal string Register(TValue value) => values.Register(value);

    internal void Unregister(TValue value) => values.Unregister(value);

    private async Task OnChangeAsync(ChangeEventArgs e)
    {
        if (values.TryGetValue(e.Value as string, out var value))
        {
            await ValueChanged.InvokeAsync(value);
        }
    }
}
