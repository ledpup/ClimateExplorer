using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcornSat.Core.Model;

public class DataFileLocationMapping
{
    public DataFileLocationMapping()
    {
        LocationIdToDataFileMappings = new Dictionary<Guid, List<DataFileFilterAndAdjustment>>();
    }

    public Guid? DataSetDefinitionId { get; set; }
    public Guid? MeasurementDefinitionId { get; set; }
    public Dictionary<Guid, List<DataFileFilterAndAdjustment>> LocationIdToDataFileMappings { get; set; }
}
