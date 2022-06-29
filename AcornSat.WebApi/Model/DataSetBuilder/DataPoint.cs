using System;

namespace AcornSat.WebApi.Model.DataSetBuilder
{
    public struct DataPoint
    {
        public short Year { get; set; }
        public short? Month { get; set; }
        public short? Day { get; set; }
        public float? Value { get; set; }

        /// <summary>
        /// Returns a clone of this DataPoint, but with the specified value
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public DataPoint WithValue(float? value)
        {
            return
                new DataPoint
                {
                    Year = Year,
                    Month = Month,
                    Day = Day,
                    Value = value
                };
        }
    }
}
