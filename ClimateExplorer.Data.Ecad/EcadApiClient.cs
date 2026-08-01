namespace ClimateExplorer.Data.Ecad;

using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static ClimateExplorer.Core.Enums;

/// <summary>
/// Talks to the ECA&amp;D non-blended collection of EUMETNET's MeteoGate API. The collection is flagged
/// pre-release, so its shape can still change; every response is read defensively and anything unexpected
/// is reported rather than guessed at.
/// </summary>
public sealed class EcadApiClient(HttpClient httpClient, ILogger? logger = null, TimeProvider? timeProvider = null)
{
    /// <summary>
    /// The server allows a fixed number of requests per window (400 at the time of writing) and answers a
    /// 429 with <c>X-RateLimit-Reset</c>, the seconds until the window rolls over. Bootstrapping every
    /// station's history takes enough requests to cross that, so waiting it out is part of normal
    /// operation rather than an error - but a reset far longer than a window means something other than
    /// ordinary throttling, so it is not waited on indefinitely.
    /// </summary>
    private static readonly TimeSpan MaximumRateLimitWait = TimeSpan.FromHours(2);

    private const int MaximumRateLimitRetries = 5;

    private readonly HttpClient httpClient = httpClient;
    private readonly ILogger logger = logger ?? NullLogger.Instance;
    private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;

    // The catalogue describes the collection, not a station, and is needed once per station refresh; it is
    // fetched once and kept for the process's lifetime rather than re-requested a few hundred times.
    private readonly SemaphoreSlim parameterNamesLock = new(1, 1);
    private IReadOnlyList<string>? parameterNames;

    public static HttpClient CreateHttpClient()
    {
        return new HttpClient
        {
            BaseAddress = new Uri(EcadConstants.BaseUrl),
            Timeout = TimeSpan.FromMinutes(5),
        };
    }

    /// <summary>
    /// Every station in the collection, with the parameter codes and date ranges each one reports. The
    /// listing is a single unpaginated response of roughly ten megabytes.
    /// </summary>
    public async Task<IReadOnlyList<EcadStation>> GetStationsAsync(CancellationToken cancellationToken)
    {
        var uri = $"collections/{EcadConstants.CollectionId}/locations";
        using var response = await SendAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, uri, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await EcadStationCatalogueReader.ReadAsync(stream, cancellationToken);
    }

    /// <summary>
    /// The parameter codes the collection accepts. Requesting a code outside this list fails the whole
    /// query with an HTTP 400, and the numbered variants are not contiguous (there is no <c>tg23</c>, for
    /// instance), so candidate lists have to be derived from the catalogue rather than from a range.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetParameterNamesAsync(CancellationToken cancellationToken)
    {
        if (parameterNames != null)
        {
            return parameterNames;
        }

        await parameterNamesLock.WaitAsync(cancellationToken);
        try
        {
            return parameterNames ??= await FetchParameterNamesAsync(cancellationToken);
        }
        finally
        {
            parameterNamesLock.Release();
        }
    }

    public async Task<IReadOnlyList<string>> GetParameterNamesAsync(DataType dataType, CancellationToken cancellationToken)
    {
        var prefix = EcadConstants.GetParameterPrefix(dataType);
        return [.. (await GetParameterNamesAsync(cancellationToken)).Where(x => EcadConstants.IsInFamily(x, prefix))];
    }

    private async Task<IReadOnlyList<string>> FetchParameterNamesAsync(CancellationToken cancellationToken)
    {
        var uri = $"collections/{EcadConstants.CollectionId}";
        using var response = await SendAsync(uri, HttpCompletionOption.ResponseContentRead, cancellationToken);
        await EnsureSuccessAsync(response, uri, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("parameter_names", out var catalogue) ||
            catalogue.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("The ECA&D collection description did not list its parameter names.");
        }

        return [.. catalogue.EnumerateObject().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal)];
    }

    /// <summary>
    /// The valid observations for one station over an inclusive date range, keyed by parameter code. The
    /// range is split into API-sized windows automatically.
    /// </summary>
    /// <exception cref="InvalidDataException">The station id is not known to the collection.</exception>
    public async Task<IReadOnlyDictionary<string, IReadOnlyDictionary<DateOnly, double>>> GetObservationsAsync(
        string ecadStationId,
        IReadOnlyCollection<string> parameterCodes,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ecadStationId);
        ArgumentNullException.ThrowIfNull(parameterCodes);
        if (parameterCodes.Count == 0)
        {
            throw new ArgumentException("At least one parameter code must be requested.", nameof(parameterCodes));
        }

        var merged = new Dictionary<string, Dictionary<DateOnly, double>>(StringComparer.Ordinal);
        foreach (var (windowStart, windowEnd) in EcadQueryWindowCalculator.GetWindows(from, to, parameterCodes.Count))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var window = await GetObservationWindowAsync(ecadStationId, parameterCodes, windowStart, windowEnd, cancellationToken);
            foreach (var (parameterCode, observations) in window)
            {
                if (!merged.TryGetValue(parameterCode, out var accumulated))
                {
                    accumulated = [];
                    merged.Add(parameterCode, accumulated);
                }

                foreach (var (date, value) in observations)
                {
                    accumulated[date] = value;
                }
            }
        }

        return merged.ToDictionary(
            x => x.Key,
            x => (IReadOnlyDictionary<DateOnly, double>)x.Value,
            StringComparer.Ordinal);
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyDictionary<DateOnly, double>>> GetObservationWindowAsync(
        string ecadStationId,
        IReadOnlyCollection<string> parameterCodes,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var uri = $"collections/{EcadConstants.CollectionId}/locations/{Uri.EscapeDataString(ecadStationId)}" +
            $"?parameter-name={Uri.EscapeDataString(string.Join(',', parameterCodes))}" +
            $"&datetime={FormatInstant(from)}/{FormatInstant(to)}";

        using var response = await SendAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        // A window the station has no observations in is answered with a 404 and is entirely normal - it is
        // what an up-to-date incremental refresh gets. An unknown station id is answered with a 400, which
        // is not: the station was confirmed to exist when the crosswalk was built.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new Dictionary<string, IReadOnlyDictionary<DateOnly, double>>(StringComparer.Ordinal);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidDataException(
                $"ECA&D rejected a query for station '{ecadStationId}': {await ReadDetailAsync(response, cancellationToken)}");
        }

        await EnsureSuccessAsync(response, uri, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await EcadObservationReader.ReadAsync(stream, cancellationToken);
    }

    private static string FormatInstant(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T00:00:00Z";
    }

    /// <summary>
    /// Issues a request, waiting out the rate limit window and retrying if the server says the quota is
    /// spent. Every call goes through here so no caller has to know the limit exists.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(string uri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var response = await httpClient.GetAsync(uri, completionOption, cancellationToken);
            if (response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                return response;
            }

            var wait = GetRateLimitWait(response);
            response.Dispose();

            if (attempt > MaximumRateLimitRetries || wait > MaximumRateLimitWait)
            {
                throw new HttpRequestException(
                    $"ECA&D rate limited request '{uri}' and it was still limited after {attempt} attempt(s); the next window opens in {wait}.",
                    null,
                    HttpStatusCode.TooManyRequests);
            }

            logger.LogWarning(
                "ECA&D rate limit reached. Waiting {Wait} for the next window before retrying (attempt {Attempt} of {Maximum}).",
                wait,
                attempt,
                MaximumRateLimitRetries);
            await Task.Delay(wait, timeProvider, cancellationToken);
        }
    }

    private static TimeSpan GetRateLimitWait(HttpResponseMessage response)
    {
        // A second of slack, so a reset that lands exactly on the boundary is not retried a moment early.
        var slack = TimeSpan.FromSeconds(1);

        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues) &&
            int.TryParse(resetValues.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var resetSeconds) &&
            resetSeconds >= 0)
        {
            return TimeSpan.FromSeconds(resetSeconds) + slack;
        }

        return response.Headers.RetryAfter?.Delta is { } retryAfter
            ? retryAfter + slack
            : TimeSpan.FromMinutes(5);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string uri, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new HttpRequestException(
            $"ECA&D request '{uri}' failed with {(int)response.StatusCode} {response.ReasonPhrase}: {await ReadDetailAsync(response, cancellationToken)}",
            null,
            response.StatusCode);
    }

    private static async Task<string> ReadDetailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return body.Length > 500 ? body[..500] : body;
        }
        catch (HttpRequestException)
        {
            return "(no response body)";
        }
    }
}
