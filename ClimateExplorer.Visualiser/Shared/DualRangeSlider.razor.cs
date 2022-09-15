using Microsoft.AspNetCore.Components;

namespace ClimateExplorer.Visualiser.Shared;

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

    int InternalFromValue { get; set; }
    int InternalToValue { get; set; }

    [Parameter]
    public string? SlideColor { get; set; }
    [Parameter]
    public string? RangeColor { get; set; }

    [Parameter] public EventCallback<ExtentValues> OnValuesChanged { get; set; }

    private string? SliderOutput { get;set; }

    private int RangeMin { get; set; }
    private int RangeMax { get; set; }
    private int RangeValue { get; set; }
    private string? RangeWidth { get; set; }



    protected override void OnInitialized()
    {
        SlideColor = "#C6C6C6";
        RangeColor = "#25DAA5";
        SliderOutput = "";

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        if (Min > 0 && Max > Min)
        {
            InternalFromValue = FromValue == null ? Min : FromValue.Value;
            InternalToValue = ToValue == null ? Max : ToValue.Value;
            SetRangeSlider();
            StateHasChanged();
        }
        
        base.OnParametersSet();
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

        RangeValue = (int)Math.Round(((float)(InternalToValue - InternalFromValue) / 2)) + InternalFromValue;
        var width = (float)(InternalToValue - InternalFromValue) / (Max - Min) * 100;
        RangeWidth = $"{width}%";
        SliderOutput = "";
    }

    private async Task RangeOnInputAsync(ChangeEventArgs e)
    {
        var newRange = Convert.ToInt32(e.Value);
        SliderOutput = newRange.ToString();

        await SetFromAndToValuesAsync(newRange);
    }

    private async Task RangeChangedAsync(ChangeEventArgs e)
    {
        var newRange = Convert.ToInt32(e.Value);
        
        RangeValue = newRange;
        await SetFromAndToValuesAsync(newRange, true);

        SliderOutput = "";
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

    private void SliderOnInput(ChangeEventArgs e)
    {
        SliderOutput = e.Value.ToString();
    }
}

public class ExtentValues
{
    public string FromValue { get; set; }
    public string ToValue { get; set; }
}