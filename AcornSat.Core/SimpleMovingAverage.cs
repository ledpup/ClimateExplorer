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
            // The "samples" queue contains the most recent data points, up to a max of _windowSize entries.
            // Each can be null or have a value.
            //
            // Whether the new sample is null or not, we add it to the window.
            samples.Enqueue(value);

            // If that has made our "history window" larger than its max size, we remove the oldest entry.
            if (samples.Count > _windowSize)
            {
                samples.Dequeue();
            }

            // If we have at least (max size of history-window / 4) non-null samples, return the average
            // of them. (Note that, for example, this means that if the max size of the history window is
            // 20, and we have only seen 5 samples so far, and none of them are null, then we will return
            // an average).
            if (samples.Count(x => x != null) >= _windowSize * .25f)
            {
                return samples.Where(x => x != null).Average();
            }

            // Otherwise, we don't have enough data, so just return null
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
