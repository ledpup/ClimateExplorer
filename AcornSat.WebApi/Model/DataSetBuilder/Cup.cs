using System;

namespace AcornSat.WebApi.Model.DataSetBuilder
{
    public class Cup
    {
        public DateOnly FirstDayInCup { get; set; }
        public DateOnly LastDayInCup { get; set; }
        public TemporalDataPoint[] DataPoints { get; set; }
    }
}
