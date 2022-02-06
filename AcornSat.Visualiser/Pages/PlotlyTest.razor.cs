using AcornSat.Core;
using Microsoft.AspNetCore.Components;
using Plotly.Blazor;
using Plotly.Blazor.LayoutLib;
using Plotly.Blazor.LayoutLib.XAxisLib;
using System.Globalization;
using static AcornSat.Core.Enums;
using Font = Plotly.Blazor.LayoutLib.AnnotationLib.Font;

namespace AcornSat.Visualiser.Pages
{
    public partial class PlotlyTest : ComponentBase
    {
        protected PlotlyChart plotlyChart;
        protected Plotly.Blazor.Layout layout;
        protected Plotly.Blazor.Config config;
        protected IList<ITrace> data;


        [Inject]
        public IDataService DataService { get; set; }

        List<DataSet> Datasets { get; set; }
        //IEnumerable<DataSetDefinition> DataSetDefinitions;
        //IEnumerable<Location> Locations;
       

        public override async Task SetParametersAsync(ParameterView parameters)
        {


            await base.SetParametersAsync(parameters);
            layout = new Plotly.Blazor.Layout
            {
                Title = new Plotly.Blazor.LayoutLib.Title
                {
                    Text = "Annotated Heatmap"
                },
                Annotations = new List<Annotation>(),
                XAxis = new List<XAxis>
            {
                new ()
                {
                    Ticks = TicksEnum.Empty,
                    Side = SideEnum.Top
                }
            },
                YAxis = new List<YAxis>
            {
                new ()
                {
                    Ticks = Plotly.Blazor.LayoutLib.YAxisLib.TicksEnum.Empty,
                    TickSuffix = " ",
                }
            }
            };

            config = new Plotly.Blazor.Config
            {
                ShowLink = false,
                Responsive = true,
                DisplayLogo = false
            };

            //DataSetDefinitions = (await DataService.GetDataSetDefinitions()).ToList();
            //Locations = (await DataService.GetLocations()).ToList();
            Datasets = (await DataService.GetDataSet(DataType.TempMax, DataResolution.Yearly, MeasurementType.Unadjusted, Guid.Parse("aed87aa0-1d0c-44aa-8561-cde0fc936395"), Aggregation.BinThenCount, numberOfBins: 10)).ToList();

            data = GetMapData(Datasets.First());

            AddAnnotations(layout, data.First());
        }

        IList<ITrace> GetMapData(DataSet dataSet)
        {
            IList<ITrace> mapData = new List<ITrace>();

            var xValues = dataSet.BinDefinitions.Select(x => x.Name);

            var yValues = dataSet.Years.Select(x => x.ToString()).ToArray();

            var zValues = dataSet.DataRecords
                .GroupBy(x => x.Year)
                .Select(x => x.Select(y => y.Value).ToArray())
                .ToArray();
           
            mapData.Add(new Plotly.Blazor.Traces.HeatMap()
            {
                X = xValues.Cast<object>().ToList(),
                Y = yValues.Cast<object>().ToList(),
                Z = zValues.Cast<object>().ToList(),
                ColorScale = new[]
                {
                    new [] {"0", "#3D9970"},
                    new [] {"1", "#001f3f"},
                    new [] {"2", "#001f3f"},
                    new [] {"3", "#001f3f"},
                    new [] {"4", "#001f3f"},
                    new [] {"5", "#001f3f"},
                    new [] {"6", "#001f3f"},
                    new [] {"7", "#001f3f"},
                    new [] {"8", "#001f3f"},
                    new [] {"9", "#001f3f"},
                },
                ShowScale = false
            });

            return mapData;
        }

        static void AddAnnotations(Plotly.Blazor.Layout layout, ITrace trace)
        {
            if (trace is not Plotly.Blazor.Traces.HeatMap { Z: List<object> zValues } heatMap) return;

            for (var i = 0; i < heatMap.Y.Count; i++)
            {
                for (var j = 0; j < heatMap.X.Count; j++)
                {
                    var currentValue = ((float?[])zValues[i])[j];
                    var textColor = currentValue != 0.0 ? "white" : "black";

                    var result = new Annotation
                    {
                        XRef = "x1",
                        YRef = "y1",
                        X = heatMap.X[j],
                        Y = heatMap.Y[i],
                        Text = currentValue.HasValue ? currentValue.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                        Font = new Font
                        {
                            Family = "Arial",
                            Size = 12,
                            Color = textColor
                        },
                        ShowArrow = false,
                    };
                    layout.Annotations.Add(result);
                }
            }
        }
    }
}
