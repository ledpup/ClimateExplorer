using System.Text.RegularExpressions;

namespace ClimateExplorer.Web.UiTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class ChartTests : TestBase
{
    [Test]
    public async Task RemoveChart()
    {
        await page.GotoAsync(_baseSiteUrl);
        await page.GotoAsync($"{_baseSiteUrl}/location/aed87aa0-1d0c-44aa-8561-cde0fc936395");
        await page.GotoAsync($"{_baseSiteUrl}/location?chartAllData=false&groupingDays=14&groupingThreshold=70&csd=ReturnSingleSeries,b13afcaf-cdbc-4267-9def-9629c8066321*TempMean*Adjusted*aed87aa0-1d0c-44aa-8561-cde0fc936395,Mean,AutoAssigned,ByYear,Line,False,None,False,MovingAverage,20,Value,,False,Identity,,True,;ReturnSingleSeries,e5eea4d6-5fd5-49ab-bf85-144a8921111e*Precipitation**aed87aa0-1d0c-44aa-8561-cde0fc936395,Sum,AutoAssigned,ByYear,Line,False,None,False,MovingAverage,20,Value,,False,Identity,,True,");
        await page.Locator("div").Filter(new() { HasTextRegex = new Regex("^Hobart \\| Precipitation$") }).ClickAsync();
        await page.GetByTitle("Remove this series").Nth(2).ClickAsync();
        await page.WaitForURLAsync($"{_baseSiteUrl}/location?chartAllData**");

        var count = await page.Locator("div").Filter(new() { HasTextRegex = new Regex("^Hobart \\| Precipitation$") }).CountAsync();

        Assert.That(count, Is.EqualTo(0));
    }
}
