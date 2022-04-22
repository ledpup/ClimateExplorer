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
            Datasets = (await DataService.GetDataSet(DataType.TempMax, DataResolution.Yearly, MeasurementType.Unadjusted, Guid.Parse("aed87aa0-1d0c-44aa-8561-cde0fc936395"), Aggregation.BinThenCount, numberOfBins: null, binSize: 3f, sufficientNumberOfDaysInYearThreshold: 350)).ToList();
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
            var hue = (((float)binIndex / numberOfBins * 200) + 200) % 360;
            hue = MathF.Round(hue, 0);

            float luminosity = 100;
            if (value.HasValue)
            {
                luminosity = value.Value != 0 
                    ? MathF.Round((float)(value.Value / maxValue * 60) + 20, 0)
                    : 20;
            }

            var saturation = (value.HasValue && value.Value != 0) ? 50 : 0;

            var colour = $"hsl({hue}, {saturation}%, {luminosity}%)";

            return colour;

        }
    }
}
