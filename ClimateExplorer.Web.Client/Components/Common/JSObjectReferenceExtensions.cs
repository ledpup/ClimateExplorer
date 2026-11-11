namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.JSInterop;

public static class JSObjectReferenceExtensions
{
    /// <summary>
    /// Invokes a JS function for its side effect, doing nothing if the Blazor Server circuit has gone (the user
    /// navigated away or reloaded, or InteractiveAuto handed off to WebAssembly). For cleanup calls such as
    /// <c>dispose</c> or <c>close</c>, where the browser has already torn the page down. Disposing the reference
    /// itself needs no guard: <see cref="IAsyncDisposable.DisposeAsync"/> ignores a lost circuit since .NET 11.
    /// </summary>
    public static async ValueTask InvokeVoidIfConnectedAsync(
        this IJSObjectReference jsObjectReference,
        string identifier,
        params object?[]? args)
    {
        try
        {
            await jsObjectReference.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
