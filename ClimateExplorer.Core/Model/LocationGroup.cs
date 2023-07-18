using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateExplorer.Core.Model;

public class LocationGroup : LocationBase
{
    public List<Guid> LocationIds { get; set; }

    public static LocationGroup GetLocationGroup(Guid id)
    {
        return GetLocationGroups().Single(x => x.Id == id);
    }

    public static List<LocationGroup> GetLocationGroups()
    {
        return new List<LocationGroup>
        { 
            new LocationGroup
            {
                Id = new Guid("143983a0-240e-447f-8578-8daf2c0a246a"),
                Name = "Australia",
                LocationIds = new List<Guid>
                {
                    new Guid("aed87aa0-1d0c-44aa-8561-cde0fc936395"),
                    new Guid("00147d6f-1569-422f-8322-a42b98e25071")
                }
            }
        };
    }
}
