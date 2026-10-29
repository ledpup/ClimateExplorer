namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;

public partial class OverviewField
{
    private Modal? popup;

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Tooltip { get; set; }

    /// <summary>
    /// Multiline tooltips are capped at 15rem wide. Turn off for tooltips with fixed-width content,
    /// such as a table, so the tooltip grows to fit it.
    /// </summary>
    [Parameter]
    public bool TooltipMultiline { get; set; } = true;

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

        // The modal only instantiates the popup content while it's open
        await popup.ShowAsync();
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
}
