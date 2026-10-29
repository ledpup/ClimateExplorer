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

    [Parameter]
    public string AriaLabel { get; set; } = "Pagination";

    [Parameter]
    public string? Class { get; set; }

    [Inject]
    private ICurrentDeviceService? CurrentDeviceService { get; set; }

    /// <summary>
    /// Returns the page numbers to show, with -1 marking an ellipsis. The result never has more
    /// than <paramref name="maxPages"/> entries, and always includes the first and last page.
    /// </summary>
    public static List<int> GetVisiblePages(int totalPages, int currentPage, int maxPages)
    {
        if (totalPages <= 0)
        {
            return [];
        }

        if (totalPages <= maxPages)
        {
            return [.. Enumerable.Range(1, totalPages)];
        }

        // Budget: 1 + ... + [window] + ... + N
        // Both ellipses present → window gets maxPages - 4 slots.
        // One ellipsis (near an edge) → window expands to maxPages - 2 slots.
        int innerWindow = maxPages - 4;

        var wStart = Math.Max(2, currentPage - (innerWindow / 2));
        var wEnd = Math.Min(totalPages - 1, wStart + innerWindow - 1);
        wStart = Math.Max(2, wEnd - innerWindow + 1);

        var needLeading = wStart > 2;
        var needTrailing = wEnd < totalPages - 1;

        if (!needLeading)
        {
            wStart = 1;
            wEnd = Math.Min(totalPages - 1, maxPages - 2);
        }
        else if (!needTrailing)
        {
            wEnd = totalPages;
            wStart = Math.Max(2, totalPages - (maxPages - 3));
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
            pages.Add(totalPages);
        }

        return pages;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            isMobileDevice ??= await CurrentDeviceService!.Mobile();
            StateHasChanged();
        }
    }

    private List<int> GetVisiblePages() => GetVisiblePages(TotalPages, CurrentPage, isMobileDevice == true ? 9 : 15);

    private Task OnPageClicked(int pageNumber) => pageNumber == CurrentPage ? Task.CompletedTask : PageChanged.InvokeAsync(pageNumber);
}
