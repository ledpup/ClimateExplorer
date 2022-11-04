using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateExplorer.Core;

public interface IMovingAverageCalculator
{
    public float? AddSample(float? value);
}

public class SimpleMovingAverageCalculator : IMovingAverageCalculator
{
    public SimpleMovingAverageCalculator(int windowSize)
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
        var nonNullSamplesInWindow = samples.Where(x => x != null).ToArray();
        if (nonNullSamplesInWindow.Length >= _windowSize * .25f)
        {
            return nonNullSamplesInWindow.Average();
        }

        // Otherwise, we don't have enough data, so just return null
        return null;
    }
}

public class OptimizedMovingAverageCalculator : IMovingAverageCalculator
{
    public OptimizedMovingAverageCalculator(int windowSize)
    {
        _windowSize = windowSize;
    }
    private Queue<float?> samples = new Queue<float?>();
    private int _windowSize = 5;

    int _numberOfNonNullSamplesInQueue = 0;
    float _runningTotalOfNonNullSamplesInQueue = 0;

    public float? AddSample(float? value)
    {
        var requiredNumberOfNonNullSamples = (int)(_windowSize * 0.25f);

        // The "samples" queue contains the most recent data points, up to a max of _windowSize entries.
        // Each can be null or have a value.
        //
        // Whether the new sample is null or not, we add it to the window.
        samples.Enqueue(value);

        if (value != null)
        {
            _numberOfNonNullSamplesInQueue++;
            _runningTotalOfNonNullSamplesInQueue += value.Value;
        }

        // If that has made our "history window" larger than its max size, we remove the oldest entry.
        if (samples.Count > _windowSize)
        {
            var dequeued = samples.Dequeue();

            if (dequeued != null)
            {
                _numberOfNonNullSamplesInQueue--;
                _runningTotalOfNonNullSamplesInQueue -= dequeued.Value;
            }
        }

        // If we have at least (max size of history-window / 4) non-null samples, return the average
        // of them. (Note that, for example, this means that if the max size of the history window is
        // 20, and we have only seen 5 samples so far, and none of them are null, then we will return
        // an average).
        if (_numberOfNonNullSamplesInQueue >= requiredNumberOfNonNullSamples)
        {
            return _runningTotalOfNonNullSamplesInQueue / _numberOfNonNullSamplesInQueue;
        }

        // Otherwise, we don't have enough data, so just return null
        return null;
    }
}

public class DataRecordMovingAverageCalculator
{
    IMovingAverageCalculator _calculator;

    public DataRecordMovingAverageCalculator(IMovingAverageCalculator calculator)
    {
        _calculator = calculator;
    }

    public List<DataRecord> Calculate(IEnumerable<DataRecord> dataRecords)
    {
        var returnDataRecords = new List<DataRecord>();

        bool haveEmittedAnyRecords = false;

        foreach (var dr in dataRecords)
        {
            var val = _calculator.AddSample(dr.Value);

            if (val != null || haveEmittedAnyRecords)
            {
                returnDataRecords.Add(
                    new DataRecord
                    {
                        Label = dr.Label,
                        BinId = dr.BinId,
                        Value = val
                    }
                );
            }
        }

        return returnDataRecords;
    }
}
