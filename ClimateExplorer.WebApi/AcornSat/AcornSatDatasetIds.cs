namespace ClimateExplorer.WebApi.AcornSat;

using System;

/// <summary>
/// The well-known ACORN-SAT and CDO dataset definition IDs, in one named location so ACORN-SAT
/// on-request extension detection never relies on scattered GUID literals. See
/// <c>ClimateExplorer.Core.DataSetDefinitionsBuilder</c> for where these are defined.
/// </summary>
internal static class AcornSatDatasetIds
{
    public static readonly Guid AcornSat = Guid.Parse("b13afcaf-cdbc-4267-9def-9629c8066321");

    public static readonly Guid Cdo = Guid.Parse("E5EEA4D6-5FD5-49AB-BF85-144A8921111E");
}
