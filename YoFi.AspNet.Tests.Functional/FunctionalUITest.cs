﻿#undef VERBOSE_SCREENSHOTS

using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// Can't parallelize anymore. The tests have too much overlap now
// [assembly: Parallelize(Workers = 3, Scope = ExecutionScope.ClassLevel)]

namespace YoFi.AspNet.Tests.Functional
{
    public class FunctionalUITest: PageTest
    {

        public override BrowserNewContextOptions ContextOptions
        {
            get
            {
                _ContextOptions.ViewportSize = CurrentViewportSizeFrom(TestContext);
                return _ContextOptions;
            }
        }

        private static readonly ViewportSize WideViewport = new ViewportSize() { Width = 1080, Height = 810 };
        private static readonly ViewportSize TabletViewport = new ViewportSize() { Width = 810, Height = 1080 };
        private static readonly ViewportSize PhoneViewport = new ViewportSize() { Width = 390, Height = 844 };

        private static BrowserNewContextOptions _ContextOptions { get; set; } = new BrowserNewContextOptions
        {
            AcceptDownloads = true
        };

        public static ViewportSize CurrentViewportSizeFrom(TestContext testContext)
        {
            return testContext.Properties["viewportSize"]?.ToString()?.ToLowerInvariant()
                switch
                {
                    "tablet" => TabletViewport,
                    "phone" => PhoneViewport,
                    _ => WideViewport
                };
        }

        protected const string testmarker = "__TEST__";
        private int nextid = 1;
        protected string NextName => $"AA{testmarker}{TestContext.TestName}_{nextid++}";
        protected string NextCategory => $"AA{testmarker}:{TestContext.TestName}:{nextid++}";
        protected static TestConfigProperties Properties = null;

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
            var hellouser = page.Locator("data-test-id=hello-user");
            var hellouser_visible = await hellouser.IsVisibleAsync();

            // If it's not visible on the main page, it may be hidden behind nav toggle
            var navtoggle = page.Locator("[aria-label=\"Toggle navigation\"]");
            if (!hellouser_visible && await navtoggle.IsVisibleAsync())
            {
                await navtoggle.ClickAsync();
                hellouser_visible = await hellouser.IsVisibleAsync();
            }

            // If we're not already logged in, well we need to do that then
            if (!hellouser_visible)
            {
                Console.WriteLine("Logging in...");

                var login = page.Locator("data-test-id=login");
                if (!await login.IsVisibleAsync())
                {
                    if (await navtoggle.IsVisibleAsync())
                    {
                        await navtoggle.ClickAsync();
                    }
                    if (!await login.IsVisibleAsync())
                    {
                        throw new ApplicationException("Can't find login button");
                    }
                }

                await login.ClickAsync();

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
                var login_visible = await page.Locator("data-test-id=login").IsVisibleAsync();
                Assert.IsFalse(login_visible);

                // Save storage state into a file for later use            
                var ConfigFileName = $"{TestContext.FullyQualifiedTestClassName}.loginstate.json";
                await Context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = ConfigFileName });

                // Set it as our new context options for later contexts
                _ContextOptions = new BrowserNewContextOptions { StorageStatePath = ConfigFileName, AcceptDownloads = true };

                // Once we're logged int, the timeouts can get a lot tighter
                base.Context.SetDefaultTimeout(5000);
            }

            // Now that we're logged in, we need to ensure database is seeded
            await DismissHelpTest();

            // If the Get Started admin button is visible, AND it links to the "Admin" page,
            // that means there is NO data
            // NOTE: I'd prefer to use btn-admin. However, I want this test to work against the current
            // deployment, without having to deploy again.
            // TODO: Change to use data-test-id in the future after 1.0.0-rc is finalized
            // var btn_admin = Page.Locator("data-test-id=btn-admin");
            var link_getstarted = Page.Locator("a", new PageLocatorOptions() { HasTextString = "Get Started" });
            if (await link_getstarted.IsVisibleAsync())
            {
                // We only need to seed if this link is going to the Admin page
                var href = await link_getstarted.GetAttributeAsync("href");
                if (href == "/Admin")
                {
                    // So let's click through to the admin page and add some data!
                    await link_getstarted.ClickAsync();
                    await Page.SaveScreenshotToAsync(TestContext,"Admin page");

                    await Page.ClickAsync($"div[data-id=all]");
                    await Task.Delay(500);
                    await Page.SaveScreenshotToAsync(TestContext,"Seeding database");
                    await Page.ClickAsync("text=Close");
                    await Page.SaveScreenshotToAsync(TestContext,"Seeding complete");
                }
            }
        }
        protected async Task WhenNavigatingToPage(string path)
        {
            // NOTE: page can be a click path as well, if there are "/"
            var split = path.Split('/');

            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "{page}" on the navbar
            await Page.ClickInMenuAsync("[aria-label=\"Toggle navigation\"]", $".navbar >> text={split[0]}");
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

        protected async Task WhenCreatingTransaction(IPage page, Dictionary<string, string> values)
        {
            await page.ClickAsync("#dropdownMenuButtonAction");
            await page.ClickAsync("text=Create New");
            await page.FillFormAsync(values);
            await page.SaveScreenshotToAsync(TestContext,"Creating");
            await page.ClickAsync("input:has-text(\"Create\")");
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
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var dialogautoshow = Page.Locator(".dialog-autoshow");
            if (await dialogautoshow.IsVisibleAsync())
            {
                await dialogautoshow.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });

#if VERBOSE_SCREENSHOTS
                await Page.SaveScreenshotToAsync(TestContext, $"Autoshow");
#endif

                await Page.ClickAsync("data-test-id=btn-help-close");
                await dialogautoshow.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Hidden });
                var backdrop = Page.Locator(".modal-backdrop");
                await backdrop.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Hidden });

#if VERBOSE_SCREENSHOTS
                await Page.SaveScreenshotToAsync(TestContext, $"Autoshow Closed");
#endif
            }
            else
            {
#if VERBOSE_SCREENSHOTS
                await Page.SaveScreenshotToAsync(TestContext, $"Autoshow Not Visible");
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

            var viewportwidth = FunctionalUITest.CurrentViewportSizeFrom(testContext).Width;
            var hostname = testContext.Properties["host"];
            var displaymoment = string.IsNullOrEmpty(moment) ? string.Empty : $"-{moment.Replace('/','-')}";

            var filename = $"Screenshot/{hostname}/{viewportwidth}/{testname} {counter:D4}{displaymoment}.png";
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

            // Ugh, playwright 1.21 decided to get rid of Read(), in favor of ReadAsync().
            // Read is called DEEP in the OpenXml.PackageLoader, and I don't want to sort ALL of that out
            // SO I am going to read it into memory first.

            using var memorystream = new MemoryStream();
            await stream.CopyToAsync(memorystream);
            memorystream.Seek(0,SeekOrigin.Begin);

            using var ssr = new SpreadsheetReader();
            ssr.Open(memorystream);
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

        public static async Task ClickInMenuAsync(this IPage page, string menuselector, string itemselector)
        {
            var item = page.Locator(itemselector);
            if (!await item.IsVisibleAsync())
            {
                await page.ClickAsync(menuselector);
                await Task.Delay(500);
                if (!await item.IsVisibleAsync())
                {
                    throw new ApplicationException($"Unable to find {itemselector}");
                }
            }
            await item.ClickAsync();
        }
    }
}
