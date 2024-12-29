namespace ClimateExplorer.Core;

using ClimateExplorer.Core.Model;

public interface IMovingAverageCalculator
{
    public double? AddSample(double? value);
}

public class SimpleMovingAverageCalculator : IMovingAverageCalculator
{
    private readonly Queue<double?> samples = new Queue<double?>();
    private readonly int windowSize = 5;

    public SimpleMovingAverageCalculator(int windowSize)
    {
        this.windowSize = windowSize;
    }

    public double? AddSample(double? value)
    {
        // The "samples" queue contains the most recent data points, up to a max of _windowSize entries.
        // Each can be null or have a value.
        //
        // Whether the new sample is null or not, we add it to the window.
        samples.Enqueue(value);

        // If that has made our "history window" larger than its max size, we remove the oldest entry.
        if (samples.Count > windowSize)
        {
            samples.Dequeue();
        }

        // If we have at least (max size of history-window / 4) non-null samples, return the average
        // of them. (Note that, for example, this means that if the max size of the history window is
        // 20, and we have only seen 5 samples so far, and none of them are null, then we will return
        // an average).
        var nonNullSamplesInWindow = samples.Where(x => x != null).ToArray();
        if (nonNullSamplesInWindow.Length >= windowSize * .25f)
        {
            return nonNullSamplesInWindow.Average();
        }

        // Otherwise, we don't have enough data, so just return null
        return null;
    }
}

public class OptimizedMovingAverageCalculator : IMovingAverageCalculator
{
    private readonly Queue<double?> samples = new Queue<double?>();
    private readonly int windowSize = 5;

    private int numberOfNonNullSamplesInQueue = 0;
    private double runningTotalOfNonNullSamplesInQueue = 0;

    public OptimizedMovingAverageCalculator(int windowSize)
    {
        this.windowSize = windowSize;
    }

    public double? AddSample(double? value)
    {
        var requiredNumberOfNonNullSamples = (int)(windowSize * 0.25f);

        // The "samples" queue contains the most recent data points, up to a max of _windowSize entries.
        // Each can be null or have a value.
        //
        // Whether the new sample is null or not, we add it to the window.
        samples.Enqueue(value);

        if (value != null)
        {
            numberOfNonNullSamplesInQueue++;
            runningTotalOfNonNullSamplesInQueue += value.Value;
        }

        // If that has made our "history window" larger than its max size, we remove the oldest entry.
        if (samples.Count > windowSize)
        {
            var dequeued = samples.Dequeue();

            if (dequeued != null)
            {
                numberOfNonNullSamplesInQueue--;
                runningTotalOfNonNullSamplesInQueue -= dequeued.Value;
            }
        }

        // If we have at least (max size of history-window / 4) non-null samples, return the average
        // of them. (Note that, for example, this means that if the max size of the history window is
        // 20, and we have only seen 5 samples so far, and none of them are null, then we will return
        // an average).
        if (numberOfNonNullSamplesInQueue >= requiredNumberOfNonNullSamples)
        {
            return runningTotalOfNonNullSamplesInQueue / numberOfNonNullSamplesInQueue;
        }

        // Otherwise, we don't have enough data, so just return null
        return null;
    }
}

public class DataRecordMovingAverageCalculator
{
    private readonly IMovingAverageCalculator calculator;

    public DataRecordMovingAverageCalculator(IMovingAverageCalculator calculator)
    {
        this.calculator = calculator;
    }

    public List<BinnedRecord> Calculate(IEnumerable<BinnedRecord> dataRecords)
    {
        var returnDataRecords = new List<BinnedRecord>();

        bool haveEmittedAnyRecords = false;

        foreach (var dr in dataRecords)
        {
            var val = calculator.AddSample(dr.Value);

            if (val != null || haveEmittedAnyRecords)
            {
                returnDataRecords.Add(new BinnedRecord(dr.BinId, val));
            }
        }

        return returnDataRecords;
    }
}
