using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateExplorer.Core.Infrastructure
{
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
            _logger.LogInformation(_name + " " + _g + " " + s + " (" + _sw.Elapsed + ")");
        }
    }
}
