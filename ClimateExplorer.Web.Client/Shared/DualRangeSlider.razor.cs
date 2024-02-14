namespace ClimateExplorer.Web.Client.Shared;

using Microsoft.AspNetCore.Components;

public partial class DualRangeSlider
{
    [Parameter]
    public int Min { get; set; }

    [Parameter]
    public int Max { get; set; }

    [Parameter]
    public int? FromValue { get; set; }

    [Parameter]
    public int? ToValue { get; set; }

    [Parameter]
    public string? SlideColor { get; set; }

    [Parameter]
    public string? RangeColor { get; set; }

    [Parameter]
    public EventCallback<ExtentValues> OnValuesChanged { get; set; }

    [Parameter]
    public EventCallback<bool?> OnShowComponent { get; set; }

    private int InternalFromValue { get; set; }
    private int InternalToValue { get; set; }

    private string? SliderOutput { get; set; }

    private int RangeMin { get; set; }
    private int RangeMax { get; set; }
    private int RangeValue { get; set; }
    private string? RangeWidth { get; set; }

    protected override void OnInitialized()
    {
        SlideColor = "#C6C6C6";
        RangeColor = "#25DAA5";
        SliderOutput = string.Empty;

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Min > 0 && Max > Min)
        {
            // This block is a hack to force an update to the UI components
            // https://github.com/dotnet/aspnetcore/issues/38656
            InternalFromValue = Min + 1;
            InternalToValue = Max - 1;
            await Task.Yield();

            InternalFromValue = FromValue == null ? Min : FromValue.Value;
            InternalToValue = ToValue == null ? Max : ToValue.Value;
            SetRangeSlider();
        }
    }

    private async Task FromChangedAsync(ChangeEventArgs e)
    {
        var newFrom = Convert.ToInt32(e.Value);
        InternalFromValue = newFrom < InternalToValue ? newFrom : InternalToValue;
        SetRangeSlider();
        await OnValuesChanged.InvokeAsync(new ExtentValues { FromValue = InternalFromValue.ToString(), ToValue = InternalToValue.ToString() });
    }

    private async Task ToChangedAsync(ChangeEventArgs e)
    {
        var newTo = Convert.ToInt32(e.Value);
        InternalToValue = newTo > InternalFromValue ? newTo : InternalFromValue;
        SetRangeSlider();
        await OnValuesChanged.InvokeAsync(new ExtentValues { FromValue = InternalFromValue.ToString(), ToValue = InternalToValue.ToString() });
    }

    private void SetRangeSlider()
    {
        var halfExtent = (int)((float)(InternalToValue - InternalFromValue) / 2);
        RangeMin = Min + halfExtent;
        RangeMax = Max - halfExtent;

        RangeValue = (int)Math.Round((float)(InternalToValue - InternalFromValue) / 2) + InternalFromValue;
        var width = (float)(InternalToValue - InternalFromValue) / (Max - Min) * 100;
        RangeWidth = $"{width}%";
        SliderOutput = string.Empty;
    }

    private async Task RangeChangedAsync(ChangeEventArgs e)
    {
        var newRange = Convert.ToInt32(e.Value);

        RangeValue = newRange;
        await SetFromAndToValuesAsync(newRange, true);

        SliderOutput = string.Empty;
    }

    private async Task SetFromAndToValuesAsync(int newRange, bool sendOnChangedEvent = false)
    {
        var extent = InternalToValue - InternalFromValue;
        var newFrom = newRange - (extent / 2f);
        var newTo = newRange + (extent / 2f);

        if (newFrom < Min)
        {
            InternalFromValue = Min;
            InternalToValue = Min + extent;
        }
        else if (newTo > Max)
        {
            InternalFromValue = Max - extent;
            InternalToValue = Max;
        }
        else
        {
            InternalFromValue = (int)newFrom;
            InternalToValue = (int)newTo;
        }

        if (sendOnChangedEvent)
        {
            await OnValuesChanged.InvokeAsync(new ExtentValues { FromValue = InternalFromValue.ToString(), ToValue = InternalToValue.ToString() });
        }
    }

    private void FromOnInput(ChangeEventArgs e)
    {
        SliderOutput = $"{e.Value}-{InternalToValue}";
    }

    private void ToOnInput(ChangeEventArgs e)
    {
        SliderOutput = $"{InternalFromValue}-{e.Value}";
    }

    private async Task RangeOnInputAsync(ChangeEventArgs e)
    {
        var newRange = Convert.ToInt32(e.Value);

        var extent = InternalToValue - InternalFromValue;
        var newFrom = Math.Round(newRange - (extent / 2f));
        var newTo = Math.Round(newRange + (extent / 2f));

        SliderOutput = $"{newFrom}-{newTo}";

        await SetFromAndToValuesAsync(newRange);
    }

    private async Task Close()
    {
        await OnShowComponent.InvokeAsync(false);
    }
}

public class ExtentValues
{
    public string? FromValue { get; set; }
    public string? ToValue { get; set; }
}