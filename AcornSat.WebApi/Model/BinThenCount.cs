using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcornSat.WebApi.Model
{
    public class BinThenCount : AggregationParameters
    {
        public BinThenCount(short sufficientNumberOfDaysInYearThreshold, short? numberOfBins, float? binSize)
        {
            SufficientNumberOfDaysInYearThreshold = sufficientNumberOfDaysInYearThreshold;
            NumberOfBins = numberOfBins;
            BinSize = binSize;
        }
        public short? NumberOfBins { get; set; }
        public float? BinSize { get; set; }
        public short SufficientNumberOfDaysInYearThreshold { get; set; }
    }
}
