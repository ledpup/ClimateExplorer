namespace ClimateExplorer.Web.Client.Components.Common;

using CurrentDevice;
using Microsoft.AspNetCore.Components;

public partial class PaginationControl
{
    private bool? isMobileDevice;

    [Parameter]
    [EditorRequired]
    public int TotalPages { get; set; }

    [Parameter]
    [EditorRequired]
    public int CurrentPage { get; set; }

    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    [Inject]
    private ICurrentDeviceService? CurrentDeviceService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        isMobileDevice = await CurrentDeviceService!.Mobile();
    }

    private List<int> GetVisiblePages()
    {
        int maxPages = isMobileDevice == true ? 5 : 9;

        if (TotalPages <= maxPages)
        {
            return [.. Enumerable.Range(1, TotalPages)];
        }

        // Budget: 1 + ... + [window] + ... + N
        // Both ellipses present → window gets maxPages - 4 slots.
        // One ellipsis (near an edge) → window expands to maxPages - 2 slots.
        int innerWindow = maxPages - 4;

        var wStart = Math.Max(2, CurrentPage - (innerWindow / 2));
        var wEnd = Math.Min(TotalPages - 1, wStart + innerWindow - 1);
        wStart = Math.Max(2, wEnd - innerWindow + 1);

        var needLeading = wStart > 2;
        var needTrailing = wEnd < TotalPages - 1;

        if (!needLeading)
        {
            wStart = 1;
            wEnd = Math.Min(TotalPages - 1, maxPages - 2);
        }
        else if (!needTrailing)
        {
            wEnd = TotalPages;
            wStart = Math.Max(2, TotalPages - (maxPages - 3));
        }

        var pages = new List<int>();
        if (needLeading)
        {
            pages.Add(1);
            pages.Add(-1);
        }

        for (var i = wStart; i <= wEnd; i++)
        {
            pages.Add(i);
        }

        if (needTrailing)
        {
            pages.Add(-1);
            pages.Add(TotalPages);
        }

        return pages;
    }
}
