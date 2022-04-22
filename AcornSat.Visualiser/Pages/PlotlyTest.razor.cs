using Microsoft.AspNetCore.Components;
using static AcornSat.Core.Enums;

namespace AcornSat.Visualiser.Pages
{
    public partial class PlotlyTest : ComponentBase
    {


        [Inject]
        public IDataService DataService { get; set; }

        List<DataSet> Datasets { get; set; }
        //IEnumerable<DataSetDefinition> DataSetDefinitions;
        //IEnumerable<Location> Locations;
        DataSet dataSet { get; set; }
        List<IGrouping<string, DataRecord>> binGroups { get; set; }
        float? maxValue;
        int numberOfBins;
        protected override async Task OnInitializedAsync()
        {
            Datasets = (await DataService.GetDataSet(DataType.TempMax, DataResolution.Yearly, MeasurementType.Adjusted, Guid.Parse("1e743b5c-f9bf-477c-8b16-7d45c67909a7"), Aggregation.BinThenCount, numberOfBins: 40, binSize: null, sufficientNumberOfDaysInYearThreshold: 350)).ToList();
            dataSet = Datasets.First();
            maxValue = dataSet.DataRecords.Max(x => x.Value);

            binGroups = dataSet.DataRecords
                .GroupBy(x => x.Label)
                .ToList();

            numberOfBins = dataSet.BinDefinitions.Count;

            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                //await ExtendData();
            }
        }

        protected string GenerateColour(int binIndex, float? value)
        {
            var hue = (MathF.Pow((float)binIndex / numberOfBins * 20, 1.75F) + 200) % 360;
            hue = MathF.Round(hue, 0);

            float luminosity = 100;
            if (value.HasValue)
            {
                luminosity = MathF.Round(100 - ((float)(value.Value / maxValue) * 95), 0);
            }

            var saturation = (value.HasValue && value.Value != 0) ? 50 : 30;

            var colour = $"hsl({hue}, {saturation}%, {luminosity}%)";

            return colour;

        }
    }
}
