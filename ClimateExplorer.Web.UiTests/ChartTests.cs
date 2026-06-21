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
        await LoadMainPage();

        await page.GotoAsync($"{_baseSiteUrl}/location/hobart");
        await page.ClickAsync("i.fa-map");
        await page.GetByTitle("Launceston Airport, Australia").ClickAsync();

        await Assertions.Expect(page.Locator("span.title").GetByText("Launceston Airport")).ToBeAttachedAsync();

    }

    private async Task LoadMainPage()
    {
        await page.GotoAsync(_baseSiteUrl);
        await page.ClickAsync("button.info-panel-close");
        await page.WaitForURLAsync($"{_baseSiteUrl}/location?chartAllData**");
        Thread.Sleep(1000);
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

    private static async Task<string> GetBorderLeftColourAsync(ILocator locator)
    {
        return await locator.EvaluateAsync<string>("element => getComputedStyle(element).borderLeftColor");
    }

    [GeneratedRegex("^Hobart \\| Precipitation$")]
    private static partial Regex HobartPrecipitationRegex();
}
