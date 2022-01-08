using AcornSat.Core;
using AcornSat.Core.InputOutput;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AcornSat.Core.Enums;
using static AcornSat.Core.InputOutput.DataReader;

namespace AcornSat.UnitTests
{
    [TestClass]
    public class DataReaderTests
    {
        [TestMethod]
        public void ReadMonthlyData()
        {
            var max = @"19390101 19390131    30.2
19390201 19390228    29.7
19390301 19390331    30.0
19390401 19390430    28.5
19390501 19390531    27.5
19390601 19390630    26.2
19390701 19390731    25.9
19390801 19390831    25.2
19390901 19390930    26.9";

            var min = @"19390101 19390131    25.4
19390201 19390228    24.6
19390301 19390331    25.0
19390401 19390430    24.7
19390501 19390531    23.4
19390601 19390630    22.6
19390701 19390731    21.8
19390801 19390831    20.6
19390901 19390930    21.6";

            var maxMinRecords = new MaxMinRecords()
            {
                MaxRows = max.Split(Environment.NewLine),
                MinRows = min.Split(Environment.NewLine)
            };

            var raia = new DataSetDefinition
            {
                Id = Guid.NewGuid(),
                Name = "RAIA",
                Description = "This ACORN-SAT dataset includes homogenised monthly data from the Remote Australian Islands and Antarctica network of 8 locations, which provide ground-based temperature records.",
                FolderName = "RAIA",
                DataType = DataType.Temperature,
                DataResolution = DataResolution.Monthly,
                DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})\d{2}\s\d+\s+(?<temperature>\d+\.\d+)$",
                MaxTempFolderName = "maxT",
                MinTempFolderName = "minT",
                MaxTempFileName = "acorn.ria.maxT.[station].monthly.txt",
                MinTempFileName = "acorn.ria.minT.[station].monthly.txt",
            };

            var temperatureRecords = DataReader.ProcessMaxMinRecords(maxMinRecords, raia);
        }
    }
}
