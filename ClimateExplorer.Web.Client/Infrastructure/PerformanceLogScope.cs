namespace ClimateExplorer.Web.Client.Infrastructure;

using System.Diagnostics;

public sealed class PerformanceLogScope : IDisposable
{
    private readonly ILogger logger;
    private readonly string name;
    private readonly IReadOnlyDictionary<string, object?> context;
    private readonly long startTimestamp;
    private bool disposed;

    private PerformanceLogScope(ILogger logger, string name, IReadOnlyDictionary<string, object?> context)
    {
        this.logger = logger;
        this.name = name;
        this.context = context;
        startTimestamp = Stopwatch.GetTimestamp();

        logger.LogInformation("PerfStart {Name} {@Context}", name, context);
    }

    public static PerformanceLogScope Start(ILogger logger, string name, params (string Key, object? Value)[] context)
    {
        return new PerformanceLogScope(logger, name, context.ToDictionary(x => x.Key, x => x.Value));
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        var elapsedMilliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        logger.LogInformation("PerfEnd {Name} elapsedMs={ElapsedMilliseconds:0.0} {@Context}", name, elapsedMilliseconds, context);
    }
}
