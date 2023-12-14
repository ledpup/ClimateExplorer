using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ClimateExplorer.Core.Infrastructure;

public class LogAugmenter
{
    Guid _g = Guid.NewGuid();
    string _name;
    ILogger _logger;
    Stopwatch _sw;

    public LogAugmenter(ILogger logger, string name)
    {
        _logger = logger;
        _name = name;
        _sw = new Stopwatch();
        _sw.Start();
    }

    public void LogInformation(string s)
    {
        _logger.LogInformation($"{_name} {_g} {s} ({_sw.Elapsed})", _name, _g, s, _sw.Elapsed);
    }

    public void LogError(string s)
    {
        _logger.LogError($"{_name} {_g} {s} ({_sw.Elapsed})", _name, _g, s, _sw.Elapsed);
    }
}
