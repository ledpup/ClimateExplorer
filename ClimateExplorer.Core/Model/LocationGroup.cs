using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateExplorer.Core.Model;

public class LocationGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    public List<Guid> LocationIds { get; set; }

    public static LocationGroup GetLocationGroup(Guid id)
    {
        return new LocationGroup
        {
            Id = id,
            Name = "Australia",
            LocationIds = new List<Guid>
            {
                new Guid("aed87aa0-1d0c-44aa-8561-cde0fc936395"),
                new Guid("00147d6f-1569-422f-8322-a42b98e25071")
            }
        };
    }
}
