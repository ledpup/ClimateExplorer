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

    [GeneratedRegex("^Hobart \\| Precipitation$")]
    private static partial Regex HobartPrecipitationRegex();
}