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
        /// User Story 1249: [User Can] Receive information about HTTP failures in a manner visually consistent with the rest of the site
        /// </summary>
        [TestMethod]
        public async Task NotFound()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Typing in a Url which does not exist
            await Page.GotoAsync(Properties.Url + "/NotFound");

            // Then: We land at HTTP Error
            await Page.ThenIsOnPageAsync("HTTP Error 404");

            // And: The page is displaying the error
            var h1 = await Page.QuerySelectorAsync("h1");
            Assert.IsNotNull(h1);
            var h1_text = await h1.TextContentAsync();
            Assert.AreEqual("404", h1_text);
        }
    }
}
