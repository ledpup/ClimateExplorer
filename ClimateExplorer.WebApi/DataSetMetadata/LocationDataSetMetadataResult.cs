#nullable enable

namespace ClimateExplorer.WebApi.Metadata;

using System.Collections.Generic;
using ClimateExplorer.Core.Model;

internal sealed record LocationDataSetMetadataResult(
    bool LocationFound,
    List<DataSetMetadata> SourceMetadata)
{
    public static LocationDataSetMetadataResult NotFound()
    {
        return new LocationDataSetMetadataResult(false, []);
    }

    public static LocationDataSetMetadataResult Found(List<DataSetMetadata> sourceMetadata)
    {
        return new LocationDataSetMetadataResult(true, sourceMetadata);
    }
}
