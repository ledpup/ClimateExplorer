using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcornSat.Core
{
    public static class Enums
    {
        public enum DataType
        {
            TempMax,
            TempMin,
            Rainfall,
            ENSO,
            CO2,
            CH4,
            N2O
        }
        public enum DataResolution
        {
            Yearly,
            Monthly,
            Weekly,
            Daily,
        }

        public enum DataAdjustment
        {
            Unadjusted,
            Adjusted,
            Difference
        }

        public enum EnsoIndex
        {
            Mei,
            Nino34,
            Oni,
            Soi,
        }

        public enum AggregationMethod
        {
            GroupByDayThenAverage,
            GroupByDayThenAverage_Relative,
            BinThenCount,
            Sum,
        }
    }
}
