using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    /// <summary>
    /// Run through top-level golden path to make sure primary pages load
    /// </summary>
    [TestClass]
    public class SmokeTest: FunctionalUITest
    {
#if TRACE_PLAYWRIGHT
        // This doesn't really seem to work. Doesn't export actions, just screenshots.
        [TestInitialize]
        public async Task SetUp()
        {
            await Context.Tracing.StartAsync(new Microsoft.Playwright.TracingStartOptions() { Screenshots = true, Snapshots = true });
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await Context.Tracing.StopAsync( new Microsoft.Playwright.TracingStopOptions() { Path = $"Trace {TestContext.FullyQualifiedTestClassName} {TestContext.TestName}.zip" } );
        }
#endif

        [TestMethod]
        public async Task ClickImport()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Import" on the navbar
            await Page.ClickAsync("text=Import");

            // Then: We land at import page
            await Page.ThenIsOnPageAsync("Import Transactions");
        }

        [TestMethod]
        public async Task ClickProfile()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking my email on the navbar
            await Page.ClickAsync("data-test-id=hello-user");

            // Then: We land at profile page
            await Page.ThenIsOnPageAsync("Profile");
        }

        [TestMethod]
        public async Task LogOut()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "logout" on the navbar
            await Page.ClickAsync("data-test-id=logout");

            // Then: We land at home page
            await Page.ThenIsOnPageAsync("Home");

            // And: The login button is again visible
            var login = await Page.QuerySelectorAsync("data-test-id=login");
            Assert.IsNotNull(login);
            var text = await login.TextContentAsync();
            Assert.AreEqual("Log in", text);
        }

        /// <summary>
        /// User Story 1174: [User can] View page-specific help when desired
        /// </summary>
        [DataRow("Budget")]
        [DataRow("Payees")]
        [DataRow("Import")]
        [DataTestMethod]
        public async Task HelpTopic(string page)
        {
            /*
                Given: On each page in {Transactions, Import, Budget, Payees}
                When: Clicking Actions > Help
                Then: The help topic specific to this page is displayed
                When: Clicking OK
                Then: The help topic is dismissed
             */

            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn(Page);

            // And: Navigating to {page} via the nav bar
            await Page.ClickAsync($"text={page}");

            // When: Clicking Actions > Help
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Help Topic");

            // Then: The help topic specific to this page is displayed
            var element = await Page.WaitForSelectorAsync("[data-test-id=\"help-topic-title\"]");
            await Page.SaveScreenshotToAsync(TestContext);
            var text = await element.TextContentAsync();
            Assert.IsTrue(text.Contains(page[..3]));

            // When: Clicking OK
            await Page.ClickAsync("[data-test-id=\"btn-help-close\"]");
            await Page.SaveScreenshotToAsync(TestContext);

            // Then: The help topic is dismissed
            element = await Page.WaitForSelectorAsync("[data-test-id=\"help-topic-title\"]", new Microsoft.Playwright.PageWaitForSelectorOptions() { State = Microsoft.Playwright.WaitForSelectorState.Hidden });
            Assert.IsNull(element);
        }
    }
}
