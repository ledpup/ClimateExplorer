using Microsoft.AspNetCore.Components;

namespace ClimateExplorer.Visualiser.Shared
{
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
        public string SlideColor { get; set; }
        [Parameter]
        public string RangeColor { get; set; }

        [Parameter] public EventCallback<string> OnFromChanged { get; set; }
        [Parameter] public EventCallback<string> OnToChanged { get; set; }

        string? ToGradient { get; set; }

        protected override void OnInitialized()
        {
            SlideColor = "#C6C6C6";
            RangeColor = "#25DAA5";

            base.OnInitialized();
        }

        protected override void OnParametersSet()
        {
            InternalFromValue = FromValue == null ? Min : FromValue.Value;
            InternalToValue = ToValue == null ? Max : ToValue.Value;
            
            ToGradient = GetGradient();

            base.OnParametersSet();            
        }

        public async Task FromChanged(ChangeEventArgs e)
        {
            var newFrom = Convert.ToInt32(e.Value);

            InternalFromValue = newFrom < InternalToValue ? newFrom : InternalToValue;
            ToGradient = GetGradient();
            await OnFromChanged.InvokeAsync(InternalFromValue.ToString());
        }

        public async Task ToChanged(ChangeEventArgs e)
        {
            var newTo = Convert.ToInt32(e.Value);

            InternalToValue = newTo > InternalFromValue ? newTo : InternalFromValue;
            ToGradient = GetGradient();
            await OnToChanged.InvokeAsync(InternalToValue.ToString());
        }

        string GetGradient()
        {
            var rangeDistance = Max - Min;
            var fromPosition = (float)InternalFromValue - Min;
            var toPosition = (float)InternalToValue - Min;
            var from = fromPosition / rangeDistance * 100;
            var to = toPosition / rangeDistance * 100;

            var background = $"background:linear-gradient(to right, {SlideColor} 0%, {SlideColor} {from}%, {RangeColor} {from}%, {RangeColor} {to}%, {SlideColor} {to}%, {SlideColor} 100%)";

            return background;
        }
    }
}
