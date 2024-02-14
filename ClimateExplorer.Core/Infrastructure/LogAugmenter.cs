namespace ClimateExplorer.Core.Infrastructure;

using Microsoft.Extensions.Logging;
using System.Diagnostics;

public class LogAugmenter
{
    private readonly Guid g = Guid.NewGuid();
    private readonly string name;
    private readonly ILogger logger;
    private readonly Stopwatch sw;

    public LogAugmenter(ILogger logger, string name)
    {
        this.logger = logger;
        this.name = name;
        sw = new Stopwatch();
        sw.Start();
    }

    public void LogInformation(string s)
    {
        logger.LogInformation($"{name} {g} {s} ({sw.Elapsed})", name, g, s, sw.Elapsed);
    }

    public void LogError(string s)
    {
        logger.LogError($"{name} {g} {s} ({sw.Elapsed})", name, g, s, sw.Elapsed);
    }
}
