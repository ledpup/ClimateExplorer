using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace ClimateExplorer.Web.UiTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public partial class ChartTests : TestBase
{
    [Test]
    public async Task RemoveChart()
    {
        await LoadMainPage();

        await page.GotoAsync($"{_baseSiteUrl}/location/hobart");
        await page.Locator("div").Filter(new() { HasTextRegex = HobartPrecipitationRegex() }).ClickAsync();
        await page.GetByTitle("Remove this series").Nth(2).ClickAsync();
        await page.WaitForURLAsync($"{_baseSiteUrl}/location?chartAllData**");

        var count = await page.Locator("div").Filter(new() { HasTextRegex = HobartPrecipitationRegex() }).CountAsync();

        Assert.That(count, Is.Zero);
    }

    [Test]
    public async Task InitialDefaultChartRendersEachSeriesOnceInChartJs()
    {
        await LoadMainPage();

        await Assertions.Expect(page.Locator(".chart-series-config")).ToHaveCountAsync(2);
        await WaitForChartDatasetCountAsync(2);

        var datasetCount = await GetChartDatasetCountAsync();

        Assert.That(datasetCount, Is.EqualTo(2));
    }

    [Test]
    public async Task InitialDefaultChartSeriesTitleColoursMatchRenderedSeriesColours()
    {
        await LoadMainPage();
        await WaitForChartDatasetCountAsync(2);

        var titleBars = page.Locator(".chart-series-config .title-bar");
        await Assertions.Expect(titleBars).ToHaveCountAsync(2);

        var firstBorderColour = await GetBorderLeftColourAsync(titleBars.Nth(0));
        var secondBorderColour = await GetBorderLeftColourAsync(titleBars.Nth(1));

        Assert.Multiple(() =>
        {
            Assert.That(firstBorderColour, Is.EqualTo("rgb(255, 45, 45)"));
            Assert.That(secondBorderColour, Is.EqualTo("rgb(54, 162, 235)"));
        });
    }

    [Test]
    public async Task ChangeLocationThenSelectLocationFromList()
    {
        await LoadHobartCanonicalChartAsync();

        await OpenChangeLocationModalAsync();
        await page.GetByTitle("Launceston Airport, Australia").ClickAsync();

        await Assertions.Expect(page.Locator("span.title").GetByText("Launceston Airport")).ToBeAttachedAsync();
        await AssertCanonicalChartUrlAsync();
        await AssertChartSeriesShowsLocationAsync("Launceston Airport");
    }

    [Test]
    public async Task MapLocationChangeUpdatesCanonicalUrlAndChart()
    {
        await LoadHobartCanonicalChartAsync();

        var markers = page.Locator(".leaflet-marker-icon");
        await page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.leaflet-marker-icon').length > 1",
            new PageWaitForFunctionOptions { Timeout = 10_000 });

        await markers.Nth(1).ClickAsync();

        await Assertions.Expect(page.Locator("span.title")).Not.ToContainTextAsync("Hobart");
        var locationName = await GetDashboardLocationNameAsync();

        await AssertCanonicalChartUrlAsync();
        await AssertChartSeriesShowsLocationAsync(locationName);
    }

    [Test]
    public async Task NearMeLocationChangeUpdatesCanonicalUrlAndChart()
    {
        await page.Context.SetGeolocationAsync(new Geolocation { Latitude = -41.545F, Longitude = 147.214F });
        await page.Context.GrantPermissionsAsync(["geolocation"], new() { Origin = _baseSiteUrl });

        await LoadHobartCanonicalChartAsync();

        await OpenChangeLocationModalAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Near me" }).ClickAsync();

        await Assertions.Expect(page.Locator("span.title").GetByText("Launceston Airport")).ToBeAttachedAsync();
        await AssertCanonicalChartUrlAsync();
        await AssertChartSeriesShowsLocationAsync("Launceston Airport");
    }

    [Test]
    public async Task RandomLocationChangeUpdatesCanonicalUrlAndChart()
    {
        await LoadHobartCanonicalChartAsync();

        var locationName = await ChangeToRandomNonHobartLocationAsync();
        await Assertions.Expect(page.Locator("span.title")).Not.ToContainTextAsync("Hobart");

        await AssertCanonicalChartUrlAsync();
        await AssertChartSeriesShowsLocationAsync(locationName);
    }

    [Test]
    public async Task NearbyLocationChangeUpdatesCanonicalUrlAndChart()
    {
        await LoadHobartCanonicalChartAsync();

        await OpenChangeLocationModalAsync();
        await page.Locator(".modal-body .table-card a").First.ClickAsync();

        await Assertions.Expect(page.Locator("span.title")).Not.ToContainTextAsync("Hobart");
        var locationName = await GetDashboardLocationNameAsync();

        await AssertCanonicalChartUrlAsync();
        await AssertChartSeriesShowsLocationAsync(locationName);
    }

    [Test]
    public async Task DirectLoadByLocationNameResolvesCorrectly()
    {
        await page.GotoAsync($"{_baseSiteUrl}/location/hobart");

        await Assertions.Expect(page.Locator("span.title").GetByText("Hobart")).ToBeAttachedAsync();
    }

    [Test]
    public async Task LocationTableClickResolvesToCanonicalChartStateUrlAndRendersChart()
    {
        await page.GotoAsync($"{_baseSiteUrl}/locations");

        await page.GetByPlaceholder("Search locations...").FillAsync("Hobart");
        await page.GetByRole(AriaRole.Link, new() { Name = "Hobart" }).ClickAsync();

        await Assertions.Expect(page.Locator("span.title").GetByText("Hobart")).ToBeAttachedAsync();
        await page.WaitForURLAsync($"{_baseSiteUrl}/location?chartAllData**csd=**");
        await WaitForChartDatasetCountAsync(2);
        await AssertChartSeriesShowsLocationAsync("Hobart");

        var currentUrl = page.Url;

        Assert.Multiple(() =>
        {
            Assert.That(currentUrl, Does.StartWith($"{_baseSiteUrl}/location?"));
            Assert.That(currentUrl, Does.Contain("csd="));
        });
    }

    [Test]
    public async Task RefreshOnLocationNameUrlStillResolves()
    {
        await page.GotoAsync($"{_baseSiteUrl}/location/hobart");
        await Assertions.Expect(page.Locator("span.title").GetByText("Hobart")).ToBeAttachedAsync();

        await page.ReloadAsync();

        await Assertions.Expect(page.Locator("span.title").GetByText("Hobart")).ToBeAttachedAsync();
    }

    [Test]
    public async Task NavigatingBetweenLocationNameUrlsShowsTheNewLocation()
    {
        await page.GotoAsync($"{_baseSiteUrl}/location/hobart");
        await Assertions.Expect(page.Locator("span.title").GetByText("Hobart")).ToBeAttachedAsync();

        await page.GotoAsync($"{_baseSiteUrl}/location/launceston-airport");
        await Assertions.Expect(page.Locator("span.title").GetByText("Launceston Airport")).ToBeAttachedAsync();
    }

    [Test]
    public async Task InvalidLocationNameShowsNotFound()
    {
        await page.GotoAsync($"{_baseSiteUrl}/location/this-location-does-not-exist");

        await page.WaitForURLAsync($"{_baseSiteUrl}/error");
    }

    private async Task LoadMainPage()
    {
        await page.GotoAsync(_baseSiteUrl);
        await page.ClickAsync("button.info-panel-close");
        await page.WaitForURLAsync($"{_baseSiteUrl}/location?chartAllData**");
        Thread.Sleep(1000);
    }

    private async Task LoadHobartCanonicalChartAsync()
    {
        await page.GotoAsync($"{_baseSiteUrl}/location/hobart");

        await Assertions.Expect(page.Locator("span.title").GetByText("Hobart")).ToBeAttachedAsync();
        await AssertCanonicalChartUrlAsync();
        await WaitForChartDatasetCountAsync(2);
        await AssertChartSeriesShowsLocationAsync("Hobart");
    }

    private async Task OpenChangeLocationModalAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { NameRegex = ChangeLocationButtonRegex() }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Dialog).GetByText("Change location")).ToBeVisibleAsync();
    }

    private async Task<string> ChangeToRandomNonHobartLocationAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await OpenChangeLocationModalAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Random location" }).ClickAsync();

            try
            {
                await page.WaitForFunctionAsync(
                    "() => document.querySelector('span.title')?.textContent?.trim() !== 'Hobart'",
                    new PageWaitForFunctionOptions { Timeout = 3_000 });

                return await GetDashboardLocationNameAsync();
            }
            catch (PlaywrightException)
            {
            }
        }

        return await GetDashboardLocationNameAsync();
    }

    private async Task AssertCanonicalChartUrlAsync()
    {
        await page.WaitForURLAsync($"{_baseSiteUrl}/location?chartAllData**csd=**");

        var currentUrl = page.Url;

        Assert.Multiple(() =>
        {
            Assert.That(currentUrl, Does.StartWith($"{_baseSiteUrl}/location?"));
            Assert.That(currentUrl, Does.Contain("csd="));
        });
    }

    private async Task AssertChartSeriesShowsLocationAsync(string locationName)
    {
        await Assertions.Expect(page.Locator(".chart-series-config .title").First).ToContainTextAsync(locationName);
        await WaitForChartDatasetCountAtLeastAsync(1);
    }

    private async Task<string> GetDashboardLocationNameAsync()
    {
        return (await page.Locator("span.title").First.InnerTextAsync()).Trim();
    }

    private async Task WaitForChartDatasetCountAsync(int expectedCount)
    {
        await page.WaitForFunctionAsync(
            @"expectedCount => {
                const canvas = document.querySelector('.chart-wrapper canvas');
                const chart = window.Chart?.getChart?.(canvas)
                    ?? Object.values(window.Chart?.instances ?? {}).find(candidate => candidate.canvas === canvas);

                return chart?.data?.datasets?.length === expectedCount;
            }",
            expectedCount);
    }

    private async Task<int> GetChartDatasetCountAsync()
    {
        return await page.EvaluateAsync<int>(
            @"() => {
                const canvas = document.querySelector('.chart-wrapper canvas');
                const chart = window.Chart?.getChart?.(canvas)
                    ?? Object.values(window.Chart?.instances ?? {}).find(candidate => candidate.canvas === canvas);

                return chart?.data?.datasets?.length ?? -1;
            }");
    }

    private async Task WaitForChartDatasetCountAtLeastAsync(int expectedCount)
    {
        await page.WaitForFunctionAsync(
            @"expectedCount => {
                const canvas = document.querySelector('.chart-wrapper canvas');
                const chart = window.Chart?.getChart?.(canvas)
                    ?? Object.values(window.Chart?.instances ?? {}).find(candidate => candidate.canvas === canvas);

                return (chart?.data?.datasets?.length ?? 0) >= expectedCount;
            }",
            expectedCount);
    }

    private static async Task<string> GetBorderLeftColourAsync(ILocator locator)
    {
        return await locator.EvaluateAsync<string>("element => getComputedStyle(element).borderLeftColor");
    }

    [GeneratedRegex("^Hobart \\| Precipitation$")]
    private static partial Regex HobartPrecipitationRegex();

    [GeneratedRegex("^(Change location|Loading locations\\.\\.\\.)$")]
    private static partial Regex ChangeLocationButtonRegex();
}
