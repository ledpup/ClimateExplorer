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
        protected override async Task OnInitializedAsync()
        {
            Datasets = (await DataService.GetDataSet(DataType.TempMax, DataResolution.Yearly, MeasurementType.Unadjusted, Guid.Parse("aed87aa0-1d0c-44aa-8561-cde0fc936395"), Aggregation.BinThenCount, numberOfBins: null, binSize: 1.25f, sufficientNumberOfDaysInYearThreshold: 350)).ToList();
            //data = GetMapData(Datasets.First());
            //data = GetMapData(null);
            dataSet = Datasets.First();
            // dataset = Datasets.First();

            //var xValues = dataset.BinDefinitions.Select(x => x.Name).ToArray();

            //var yValues = dataset.Years.Select(x => x.ToString()).Take(4).ToArray();

            binGroups = dataSet.DataRecords
                .GroupBy(x => x.Label)
                .ToList();
            //    .Select(x => x.Select(y => y.Value).ToArray())
            //    .ToArray();


            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                //await ExtendData();
            }
        }
          
    }
}
