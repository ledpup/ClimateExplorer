namespace ClimateExplorer.Web.Client.Components;

using Blazorise;
using Microsoft.AspNetCore.Components;

public partial class OverviewField
{
    private Modal? popup;
    private bool popupContentVisible = false;

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Tooltip { get; set; }

    [Parameter]
    public string? AdditionalInfo { get; set; }

    [Parameter]
    public RenderFragment? Value { get; set; }

    [Parameter]
    public string? PopupTitle { get; set; }

    [Parameter]
    public RenderFragment? PopupContent { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    private string Title
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(PopupTitle))
            {
                return PopupTitle!;
            }

            if (PopupContent != null)
            {
                return Label ?? string.Empty;
            }

            return string.Empty;
        }
    }

    public async Task ShowPopup()
    {
        if (popup is null || PopupContent is null)
        {
            return;
        }

        // Ensure the popup content is instantiated only when user opens the modal
        popupContentVisible = true;
        StateHasChanged();
        await popup.Show();
    }

    private async Task HandleClick()
    {
        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync();
            return;
        }

        await ShowPopup();
    }

    private Task OnModalClosing(ModalClosingEventArgs e)
    {
        popupContentVisible = false;

        return Task.CompletedTask;
    }
}
