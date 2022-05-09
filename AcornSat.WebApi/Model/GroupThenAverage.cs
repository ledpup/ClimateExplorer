using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcornSat.WebApi.Model;

public class GroupThenAverage : StatsParameters
{
    public GroupThenAverage(short dayGrouping = 14, float dayGroupingThreshold = .7f)
    {
        DayGrouping = dayGrouping;
        DayGroupingThreshold = dayGroupingThreshold;
    }
    public short DayGrouping { get; set; }
    public float DayGroupingThreshold { get; set; }
}
