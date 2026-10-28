namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;

/// <summary>
/// An option of the enclosing <see cref="Select{TValue}"/>. It registers its value with the select so a choice can
/// be mapped back to the original <typeparamref name="TValue"/>.
/// </summary>
public partial class SelectItem<TValue> : IDisposable
{
    private string key = string.Empty;
    private bool registered;
    private TValue registeredValue = default!;

    [CascadingParameter]
    public Select<TValue>? Parent { get; set; }

    [Parameter]
    public TValue Value { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public void Dispose()
    {
        Unregister();
    }

    protected override void OnParametersSet()
    {
        // In a @foreach, Blazor may reuse this item for a different value, so re-register when it changes
        if (registered && EqualityComparer<TValue>.Default.Equals(registeredValue, Value))
        {
            return;
        }

        Unregister();

        key = Parent?.Register(Value) ?? SelectValueMap<TValue>.KeyFor(Value);
        registeredValue = Value;
        registered = Parent is not null;
    }

    private void Unregister()
    {
        if (registered)
        {
            Parent?.Unregister(registeredValue);
            registered = false;
        }
    }
}
