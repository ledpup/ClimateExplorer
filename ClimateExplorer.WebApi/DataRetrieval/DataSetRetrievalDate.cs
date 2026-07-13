namespace ClimateExplorer.WebApi.DataRetrieval;

using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core.Interface;

internal static class DataSetRetrievalDate
{
    public static DateTimeOffset? OldestFor(IEnumerable<ICachedData> contributingSources)
    {
        var retrievedDates = contributingSources.Select(x => x.RetrievedDate).ToArray();
        if (retrievedDates.Length == 0 || retrievedDates.Any(x => x == null))
        {
            return null;
        }

        return retrievedDates.Min();
    }
}
