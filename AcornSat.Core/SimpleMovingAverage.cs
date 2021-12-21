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
        private Queue<double?> samples = new Queue<double?>();
        private int _windowSize = 5;

        public double? AddSample(double? value)
        {
            if (value == null)
            {
                samples = new Queue<double?>();
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

        public static List<double?> Calculate(int windowSize, List<double?> values)
        {
            var ma = new SimpleMovingAverage(windowSize);
            var movingAverageValues = new List<double?>();
            values.ForEach(x => movingAverageValues.Add(ma.AddSample(x)));
            return movingAverageValues;
        }
    }
}
