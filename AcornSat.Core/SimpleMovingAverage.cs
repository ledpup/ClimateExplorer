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
                samples = new Queue<float?>();
                return null;
            }

            samples.Enqueue(value);

            if (samples.Count > _windowSize)
            {
                samples.Dequeue();
            }

            if (samples.Count > _windowSize / 3f)
            {
                return samples.Average();
            }

            return null;
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
