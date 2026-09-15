namespace ClimateExplorer.Data.Downloading.Storage;

using ClimateExplorer.Data.Downloading.Downloaders;

/// <summary>
/// Persists the <c>p_c</c> token BOM's <c>availableYears</c> endpoint returns for a given station/observation
/// code, so <see cref="BomDailyDataClient"/> doesn't have to re-fetch it on every refresh. The token identifies
/// which underlying file BOM's backend holds for that station/series - it isn't date-dependent, so it's stable
/// across many refreshes in practice, even though it isn't a documented, guaranteed-stable public contract.
/// </summary>
public interface IBomAvailableYearsTokenStore
{
    Task<string?> GetAsync(string stationId, BomDailyObservationCode observationCode, CancellationToken cancellationToken);

    Task PutAsync(string stationId, BomDailyObservationCode observationCode, string token, CancellationToken cancellationToken);

    Task DeleteAsync(string stationId, BomDailyObservationCode observationCode, CancellationToken cancellationToken);
}
