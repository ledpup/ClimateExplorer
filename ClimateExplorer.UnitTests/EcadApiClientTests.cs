namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Data.Ecad;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class EcadApiClientTests
{
    private const string StationId = "ecad_0000162";

    [TestMethod]
    public async Task GetObservationsAsync_RateLimited_WaitsForTheWindowAndRetriesRatherThanFailing()
    {
        // Bootstrapping every station's history takes more requests than one window allows, so being
        // throttled part way through is ordinary. Before this was handled, a throttled run silently
        // published a mapping missing half its stations.
        var handler = new FakeHandler([RateLimited(600), Observations()]);
        var timeProvider = new RecordingTimeProvider();

        var observations = await CreateClient(handler, timeProvider)
            .GetObservationsAsync(StationId, ["tn3"], new DateOnly(2026, 6, 30), new DateOnly(2026, 7, 1), CancellationToken.None);

        Assert.HasCount(2, handler.Requests);
        Assert.AreEqual(TimeSpan.FromSeconds(601), timeProvider.Delays.Single());
        Assert.AreEqual(17.7, observations["tn3"][new DateOnly(2026, 6, 30)]);
    }

    [TestMethod]
    public async Task GetObservationsAsync_RateLimitedForLongerThanAWindowCouldExplain_FailsInsteadOfWaiting()
    {
        var handler = new FakeHandler([RateLimited((int)TimeSpan.FromHours(9).TotalSeconds)]);
        var timeProvider = new RecordingTimeProvider();

        await Assert.ThrowsExactlyAsync<HttpRequestException>(
            () => CreateClient(handler, timeProvider).GetObservationsAsync(
                StationId, ["tn3"], new DateOnly(2026, 6, 30), new DateOnly(2026, 7, 1), CancellationToken.None));

        Assert.IsEmpty(timeProvider.Delays);
    }

    [TestMethod]
    public async Task GetObservationsAsync_RateLimitedEveryTime_GivesUpRatherThanRetryingForever()
    {
        var handler = new FakeHandler(Enumerable.Repeat(0, 20).Select(_ => RateLimited(60)).ToList());

        await Assert.ThrowsExactlyAsync<HttpRequestException>(
            () => CreateClient(handler, new RecordingTimeProvider()).GetObservationsAsync(
                StationId, ["tn3"], new DateOnly(2026, 6, 30), new DateOnly(2026, 7, 1), CancellationToken.None));

        Assert.IsLessThanOrEqualTo(10, handler.Requests.Count);
    }

    private static EcadApiClient CreateClient(HttpMessageHandler handler, TimeProvider timeProvider)
    {
        return new EcadApiClient(
            new HttpClient(handler) { BaseAddress = new Uri(EcadConstants.BaseUrl) },
            logger: null,
            timeProvider);
    }

    private static HttpResponseMessage RateLimited(int resetSeconds)
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("<html><body><h1>You have hit the rate limit.</h1></body></html>"),
        };
        response.Headers.Add("X-RateLimit-Limit", "400");
        response.Headers.Add("X-RateLimit-Remaining", "0");
        response.Headers.Add("X-RateLimit-Reset", resetSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return response;
    }

    private static HttpResponseMessage Observations()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"type\":\"CoverageCollection\",\"coverages\":[{\"domain\":{\"axes\":{\"t\":{\"values\":" +
                "[\"2026-06-30T00:00:00Z\",\"2026-07-01T00:00:00Z\"]}}},\"ranges\":{" +
                "\"tn3\":{\"values\":[17.7,null]}," +
                "\"tn3_q\":{\"values\":[0,null]}}}]}"),
        };
    }

    private sealed class FakeHandler(IReadOnlyList<HttpResponseMessage> responses) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.ToString());
            return Task.FromResult(responses[Math.Min(Requests.Count - 1, responses.Count - 1)]);
        }
    }

    /// <summary>
    /// Records what the client asked to wait for instead of actually waiting, so the retry behaviour can
    /// be asserted without the test taking as long as a rate limit window.
    /// </summary>
    private sealed class RecordingTimeProvider : TimeProvider
    {
        public List<TimeSpan> Delays { get; } = [];

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Delays.Add(dueTime);
            return new ImmediateTimer(callback, state);
        }

        private sealed class ImmediateTimer : ITimer
        {
            public ImmediateTimer(TimerCallback callback, object? state)
            {
                callback(state);
            }

            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
