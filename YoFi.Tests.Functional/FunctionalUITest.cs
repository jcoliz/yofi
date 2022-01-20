﻿using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Can't parallelize anymore. The tests have too much overlap now
// [assembly: Parallelize(Workers = 3, Scope = ExecutionScope.ClassLevel)]

namespace YoFi.Tests.Functional
{
    public class FunctionalUITest: PageTest
    {

        public override BrowserNewContextOptions ContextOptions => _ContextOptions;

        private static readonly ViewportSize iPadLandscapeViewport = new ViewportSize() { Width = 1080, Height = 810 };

        private static BrowserNewContextOptions _ContextOptions { get; set; } = new BrowserNewContextOptions { AcceptDownloads = true, ViewportSize = iPadLandscapeViewport };

        protected const string testmarker = "__TEST__";
        private int nextid = 1;
        protected string NextName => $"AA{testmarker}{TestContext.TestName}_{nextid++}";
        protected string NextCategory => $"AA{testmarker}:{TestContext.TestName}:{nextid++}";
        protected TestConfigProperties Properties = null;

        protected FunctionalUITest() { }

        [TestInitialize]
        public void SetUp()
        {
            Properties = new TestConfigProperties(TestContext.Properties);
        }

        protected async Task GivenLoggedIn(IPage where = null)
        {
            var page = where ?? Page;

            // Navigate to the root of the site
            await page.GotoAsync(Properties.Url);

            // Are we already logged in?
            var hellouser = await page.QuerySelectorAsync("data-test-id=hello-user");

            // If we're not already logged in, well we need to do that then
            if (null == hellouser)
            {
                Console.WriteLine("Logging in...");

                await page.ClickAsync("data-test-id=login");

                // When: Filling out the login form with those credentials and pressing "sign in"
                await page.FillAsync("id=floatingInput", Properties.AdminUserEmail);
                await page.FillAsync("id=floatingPassword", Properties.AdminUserPassword);
                await page.ClickAsync("data-test-id=signin");

                // Then: We land back at the home page
                await page.ThenIsOnPageAsync("Home");

                // And: The navbar has our email
                var content = await page.TextContentAsync("data-test-id=hello-user");
                Assert.IsTrue(content.Contains(Properties.AdminUserEmail));

                // And: The login button is not visible
                var login = await page.QuerySelectorAsync("data-test-id=login");
                Assert.IsNull(login);

                // Save storage state into a file for later use            
                var ConfigFileName = $"{TestContext.FullyQualifiedTestClassName}.loginstate.json";
                await Context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = ConfigFileName });

                // Set it as our new context options for later contexts
                _ContextOptions = new BrowserNewContextOptions { StorageStatePath = ConfigFileName, AcceptDownloads = true, ViewportSize = iPadLandscapeViewport };

                // Once we're logged int, the timeouts can get a lot tighter
                base.Context.SetDefaultTimeout(5000);
            }
        }
        protected async Task WhenNavigatingToPage(string path)
        {
            // NOTE: page can be a click path as well, if there are "/"
            var split = path.Split('/');

            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "{page}" on the navbar
            await Page.ClickAsync($".navbar >> text={split[0]}");
            await Page.WaitForLoadStateAsync();

            if (split.Length > 1)
            {
                // Special case. The old "Budget" top level page has now been moved to
                // the "Edit Budget" page, sitting behind the new "Budget" page.
                // This code will get you there
                await Page.ClickAsync("#dropdownMenuButtonAction");
                await Page.ClickAsync($"text={split[1]}");
            }

            // And: Dismissing any help text
            await DismissHelpTest();
        }

        protected async Task GivenPayeeInDatabase(string category, string name)
        {
            // Given: We are starting at the payee index page

            await WhenNavigatingToPage("Payees");

            // And: Creating a new item

            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Create New");
            await Page.FillFormAsync(new Dictionary<string, string>()
            {
                { "Category", category ?? NextCategory },
                { "Name", name ?? NextName },
            });
            await Page.ClickAsync("input:has-text(\"Create\")");
            await Page.SaveScreenshotToAsync(TestContext, "CreatedPayee");
        }

        protected async Task DismissHelpTest()
        {
            await Page.WaitForLoadStateAsync();
            var dialogautoshow = await Page.QuerySelectorAsync(".dialog-autoshow");
            if (null != dialogautoshow)
            {
                await dialogautoshow.WaitForElementStateAsync(ElementState.Visible);
                await Page.ClickAsync("data-test-id=btn-help-close");
                await dialogautoshow.WaitForElementStateAsync(ElementState.Hidden);
                await Page.WaitForSelectorAsync(".modal-backdrop", new Microsoft.Playwright.PageWaitForSelectorOptions() { State = Microsoft.Playwright.WaitForSelectorState.Hidden });

#if VERBOSE_SCREENSHOTS
                await Page.SaveScreenshotToAsync(TestContext, $"-autoshow");
#endif
            }
        }

        protected class IdOnly
        {
            public int ID { get; set; }
        }
    }    

    public static class PageExtensions
    {
        public static async Task ThenH2Is(this IPage page, string expected)
        {
            var content = await page.TextContentAsync("h2");
            Assert.AreEqual(expected, content);
        }

        public static async Task FillFormAsync(this IPage page, Dictionary<string, string> entries)
        {
            foreach (var kvp in entries)
                await page.FillAsync($"input[name=\"{kvp.Key}\"]", kvp.Value);
        }

        public static async Task<int> GetTotalItemsAsync(this IPage page) => await page.GetNumberAsync("data-test-id=totalitems");

        public static async Task<int> GetNumberAsync(this IPage page, string selector)
        {
            var totalitems = await page.TextContentAsync(selector);

            if (!Int32.TryParse(totalitems, out int result))
                result = 0;

            return result;
        }

        private static readonly Dictionary<string, int> ScreenShotCounter = new Dictionary<string, int>();

        public static async Task SaveScreenshotToAsync(this IPage page, TestContext testContext, string moment = null)
        {
            var testname = $"{testContext.FullyQualifiedTestClassName.Split(".").Last()}/{testContext.TestName}";
            var counter = 1 + ScreenShotCounter.GetValueOrDefault(testname);
            ScreenShotCounter[testname] = counter;

            var displaymoment = string.IsNullOrEmpty(moment) ? string.Empty : $"-{moment}";

            var filename = $"Screenshot/{testname} {counter:D4}{displaymoment}.png";
            await page.ScreenshotAsync(new PageScreenshotOptions() { Path = filename, OmitBackground = true, FullPage = true });
            testContext.AddResultFile(filename);
        }

        public static async Task ThenIsOnPageAsync(this IPage page, string expected)
        {
            var title = await page.TitleAsync();
            var split = title.Split(" - ").ToList();
            if (split.First() == "DEMO")
                split.RemoveAt(0);
            Assert.AreEqual(expected,split.First());
            Assert.AreEqual("YoFi",split.Last());
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
