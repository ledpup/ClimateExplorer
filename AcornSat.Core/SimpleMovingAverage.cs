using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcornSat.Core
{
    public class SimpleMovingAverage
    {
        public SimpleMovingAverage(int windowSize)
        {
            _windowSize = windowSize;
        }
        private Queue<float?> samples = new Queue<float?>();
        private int _windowSize = 5;

        public float? AddSample(float? value)
        {
            if (value == null)
            {
                return samples.Average();
            }

            samples.Enqueue(value);

            if (samples.Count > _windowSize)
            {
                samples.Dequeue();
            }

            if (samples.Count > _windowSize * .25f)
            {
                return samples.Average();
            }

            return null;
        }

        public static List<DataRecord> Calculate(int windowSize, List<DataRecord> dataRecords)
        {
            var ma = new SimpleMovingAverage(windowSize);
            var movingAverageValues = new List<float?>();
            var values = dataRecords.Select(x => x.Value).ToList();
            var returnDataRecords = new List<DataRecord>();

            dataRecords.ForEach(x => returnDataRecords.Add(new DataRecord
            {
                Day = x.Day,
                Month = x.Month,
                Year = x.Year,
                Label = x.Label,
                Value = ma.AddSample(x.Value)
            }));
            returnDataRecords = returnDataRecords.SkipWhile(x => !x.Value.HasValue).ToList();
            return returnDataRecords;
        }

        public static List<float?> Calculate(int windowSize, List<float?> values)
        {
            var ma = new SimpleMovingAverage(windowSize);
            var movingAverageValues = new List<float?>();
            values.ForEach(x => movingAverageValues.Add(ma.AddSample(x)));
            return movingAverageValues;
        }
    }
}
