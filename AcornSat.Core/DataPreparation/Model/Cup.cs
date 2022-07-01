using System;

namespace ClimateExplorer.Core.DataPreparation
{
    public class Cup
    {
        public DateOnly FirstDayInCup { get; set; }
        public DateOnly LastDayInCup { get; set; }
        public TemporalDataPoint[] DataPoints { get; set; }
    }
}
