namespace ClimateExplorer.Web.Client.Components.Common;

using Microsoft.AspNetCore.Components;

public partial class Collapsible
{
    // CollapserSize is what an unfit person does when they try to Exercise
    public enum CollapserSizes
    {
        Normal,
        Large,
        ExtraLarge,
    }

    public enum CollapserContentLayoutTypes
    {
        Block,
        FlexboxColumns,
        FlexboxRow,
    }

    [Parameter]
    public CollapserContentLayoutTypes ContentLayoutType { get; set; }

    [Parameter]
    public CollapserSizes CollapserSize { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? FullTitle { get; set; }

    [Parameter]
    public RenderFragment? CollapsedContent { get; set; }

    [Parameter]
    public RenderFragment? Content { get; set; }

    [Parameter]
    public bool AllowCollapse { get; set; } = true;

    [Parameter]
    public bool ShadeBackground { get; set; }

    [Parameter]
    public EventCallback<bool> OnShowOrHide { get; set; }

    [Parameter]
    public bool NoBottomMargin { get; set; }

    [Parameter]
    public bool ShowTitleWhenExpanded { get; set; }

    [Parameter]
    public bool ShowContent { get; set; } = true;

    public void CollapserOnClick()
    {
        ShowContent = !ShowContent;
        OnShowOrHide.InvokeAsync(ShowContent);
    }

    protected override void OnInitialized()
    {
        ShowTitleWhenExpanded = true;

        base.OnInitialized();
    }
}
