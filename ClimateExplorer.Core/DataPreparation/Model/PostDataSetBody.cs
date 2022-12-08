using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateExplorer.Core.DataPreparation.Model
{
    public class PostDataSetBody
    {
        public CompoundSeriesTypes CompoundSeriesType { get; set; }
        public PostDataSetsRequestBody[] Body { get; set; }
    }
}
