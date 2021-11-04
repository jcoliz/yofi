using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

// Currently shaves off all of 4 seconds to run them in parallel!
// Two or three workers seems to yield the optimum results.
[assembly: Parallelize(Workers = 3, Scope = ExecutionScope.ClassLevel)]

namespace YoFi.Tests.Functional
{
    public class FunctionalUITest: PageTest
    {

        public override BrowserNewContextOptions ContextOptions => _ContextOptions;

        private static BrowserNewContextOptions _ContextOptions { get; set; } = new BrowserNewContextOptions { AcceptDownloads = true };

        protected const string testmarker = "__TEST__";
        private int nextid = 1;
        protected string NextName => $"AA{testmarker}{TestContext.TestName}_{nextid++}";
        protected string NextCategory => $"AA{testmarker}:{TestContext.TestName}:{nextid++}";

        protected FunctionalUITest() { }

        protected async Task GivenLoggedIn()
        {
            // Navigate to the root of the site
            var site = TestContext.Properties["webAppUrl"] as string;
            await Page.GotoAsync(site);

            // Are we already logged in?
            var hellouser = await Page.QuerySelectorAsync("data-test-id=hello-user");

            // If we're not already logged in, well we need to do that then
            if (null == hellouser)
            {
                Console.WriteLine("Logging in...");

                await Page.ClickAsync("data-test-id=login");

                // And: User credentials as specified in user secrets
                var config = new ConfigurationBuilder().AddUserSecrets(Assembly.GetAssembly(typeof(FunctionalUITest))).Build();
                var email = config["AdminUser:Email"];
                var password = config["AdminUser:Password"];

                // When: Filling out the login form with those credentials and pressing "sign in"
                await Page.FillAsync("id=floatingInput", email);
                await Page.FillAsync("id=floatingPassword", password);
                await Page.ClickAsync("data-test-id=signin");

                // Then: We land back at the home page
                await Page.ThenIsOnPageAsync("Home");

                // And: The navbar has our email
                var content = await Page.TextContentAsync("data-test-id=hello-user");
                Assert.IsTrue(content.Contains(email));

                // And: The login button is not visible
                var login = await Page.QuerySelectorAsync("data-test-id=login");
                Assert.IsNull(login);

                // Save storage state into a file for later use            
                var ConfigFileName = $"{TestContext.FullyQualifiedTestClassName}.loginstate.json";
                await Context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = ConfigFileName });

                // Set it as our new context options for later contexts
                _ContextOptions = new BrowserNewContextOptions { StorageStatePath = ConfigFileName, AcceptDownloads = true };

                // Once we're logged int, the timeouts can get a lot tighter
                base.Context.SetDefaultTimeout(5000);
            }
        }

        protected async Task ThenH2Is(string expected)
        {
            var content = await Page.TextContentAsync("h2");
            Assert.AreEqual(expected, content);
        }

        protected class IdOnly
        {
            public int ID { get; set; }
        }
    }

    public static class PageExtensions
    {
        public static async Task FillFormAsync(this IPage page, Dictionary<string, string> entries)
        {
            foreach (var kvp in entries)
                await page.FillAsync($"input[name=\"{kvp.Key}\"]", kvp.Value);
        }

        public static async Task<int> GetTotalItemsAsync(this IPage page)
        {
            var totalitems = await page.TextContentAsync("data-test-id=totalitems");

            if (!Int32.TryParse(totalitems, out int result))
                result = 0;

            return result;
        }

        private static int ScreenShotCount = 1;

        public static async Task SaveScreenshotToAsync(this IPage page, TestContext testContext)
        {
            var filename = $"Screenshot {testContext.FullyQualifiedTestClassName} {ScreenShotCount++:D4} {testContext.TestName}.png";
            await page.ScreenshotAsync(new PageScreenshotOptions() { Path = filename, OmitBackground = true });
            testContext.AddResultFile(filename);
        }

        public static async Task ThenIsOnPageAsync(this IPage page, string expected)
        {
            var title = await page.TitleAsync();
            Assert.AreEqual($"{expected} - Development - YoFi", title);
        }

        public static async Task ThenContainsItemsAsync(this IPage page, int from, int to)
        {
            Assert.AreEqual(from.ToString(), await page.TextContentAsync("data-test-id=firstitem"));
            Assert.AreEqual(to.ToString(), await page.TextContentAsync("data-test-id=lastitem"));
        }

        public static async Task<IEnumerable<T>> ThenIsSpreadsheetContainingAsync<T>(this IDownload source, string name, int count, TestContext savetocontext = null) where T : class, new()
        {
            using var stream = await source.CreateReadStreamAsync();
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            Assert.AreEqual(name, ssr.SheetNames.First());
            var items = ssr.Deserialize<T>(name);
            Assert.AreEqual(count, items.Count());

            if (savetocontext != null)
            {
                var filename = $"{savetocontext.FullyQualifiedTestClassName}-{savetocontext.TestName}.xlsx";
                await source.SaveAsAsync(filename);
                savetocontext.AddResultFile(filename);
            }

            return items;
        }

        public static async Task<Image> DownloadImageAsync(this IDownload source)
        {
            var stream = await source.CreateReadStreamAsync();
            var image = await Image.LoadAsync(stream);

            return image;
        }

        public static async Task SearchFor(this IPage page, string q)
        {
            await page.FillAsync("data-test-id=q", q);
            await page.ClickAsync("data-test-id=btn-search");
        }
    }
}
