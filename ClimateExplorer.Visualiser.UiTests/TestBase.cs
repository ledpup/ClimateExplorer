using Microsoft.Playwright;

namespace ClimateExplorer.Visualiser.UiTests
{
    public abstract class TestBase
    {
        public const string _baseApiUrl = "http://localhost:54836";
        public const string _baseSiteUrl = "http://localhost:5298";
        public const string _publicSiteUrl = "http://climateexplorer.net";

        public IPlaywright _playwright;
        public IBrowser _browser;
        public IPage page;

        [SetUp]
        public async Task Setup()
        {
            _playwright = await Playwright.CreateAsync();
            var chromium = _playwright.Chromium;
            _browser = await chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false, });
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions { ViewportSize = new ViewportSize { Width = 1440, Height = 1000 } });
            page = await context.NewPageAsync();
        }

        [TearDown]
        public async Task TearDown()
        {
            await _browser.CloseAsync();
            _playwright.Dispose();
        }
    }
}