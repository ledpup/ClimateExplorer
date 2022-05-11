using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcornSat.WebApi.Model;

public class BinThenCount : StatsParameters
{
    public BinThenCount(short? numberOfBins, short? binSize)
    {
        NumberOfBins = numberOfBins;
        BinSize = binSize;
    }
    public short? NumberOfBins { get; set; }
    public short? BinSize { get; set; }
}
