using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClimateExplorer.Visualiser.UiTests
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class BrowserLocationTests
    {
        IPlaywright _playwright;
        IBrowser _browser;
        IPage _page;

        [SetUp]
        public async Task Setup()
        {
            _playwright = await Playwright.CreateAsync();
            var chromium = _playwright.Chromium;
            _browser = await chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false, });
            var context = await _browser.NewContextAsync( new BrowserNewContextOptions { ViewportSize = new ViewportSize { Width = 1440, Height = 845} });
            _page = await context.NewPageAsync();           
        }

        [TearDown]
        public async Task TearDown()
        {
            await _browser.CloseAsync();
            _playwright.Dispose();
        }

        [Test]
        public async Task GpToAllLocationsAndGetAScreenshot()
        {
            var request = await _playwright.APIRequest.NewContextAsync(new()
            {
                BaseURL = "http://localhost:54836",
            });

            var response = await request.GetAsync("location");


            Assert.True(response.Ok);
            var jsonResponse = await response.JsonAsync();

            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
            };

            var locations = new List<Location>();
            foreach (var locationObj in jsonResponse?.EnumerateArray())
            {
                var location = locationObj.Deserialize<Location>(options);
                locations.Add(location);
            }

            var di = new DirectoryInfo("Locations");
            if (!di.Exists)
            {
                di.Create();
            }

            foreach (var location in locations)
            {
                await _page.GotoAsync($"http://localhost:5298/location/{location.Id}");
                Thread.Sleep(5000);
                await _page.ScreenshotAsync(new()
                {
                    Path = $"Locations\\{location.Name}.png",
                });
            }
        }

    }
}