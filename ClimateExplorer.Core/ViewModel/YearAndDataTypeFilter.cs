using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ClimateExplorer.Core.Enums;
using DataType = ClimateExplorer.Core.Enums.DataType;

namespace ClimateExplorer.Core.ViewModel
{
    public class YearAndDataTypeFilter
    {
        public YearAndDataTypeFilter (short year)
        {
            Year = year;
        }

        public short Year { get; set; }
        public DataType? DataType { get; set; }
        public DataAdjustment? DataAdjustment { get; set; }
    }
}
