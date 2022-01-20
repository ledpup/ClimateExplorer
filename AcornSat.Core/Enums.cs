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
            TempMaxMin,
            Rainfall
        }
        public enum DataResolution
        {
            Yearly,
            Monthly,
            Weekly,
            Daily,
        }

        public enum MeasurementType
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
    }
}
