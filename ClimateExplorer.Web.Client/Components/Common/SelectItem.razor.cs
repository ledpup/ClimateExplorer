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
        // Razor infers TValue from Value before the cascaded type, so <SelectItem Value="7"> inside a Select<short> is
        // a SelectItem<int> that never registers, and choosing it is silently ignored
        if (Parent is null)
        {
            throw new InvalidOperationException(
                $"{nameof(SelectItem<TValue>)}<{typeof(TValue).Name}> must be inside a {nameof(Select<TValue>)}<{typeof(TValue).Name}>. " +
                $"If the Select has a different TValue, set TValue on the item.");
        }

        // In a @foreach, Blazor may reuse this item for a different value, so re-register when it changes
        if (registered && EqualityComparer<TValue>.Default.Equals(registeredValue, Value))
        {
            return;
        }

        Unregister();

        key = Parent.Register(Value);
        registeredValue = Value;
        registered = true;
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
