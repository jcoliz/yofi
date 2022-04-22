using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace YoFi.AspNet.Tests.Functional
{
    [TestClass]
    public class NotLoggedInUITest: FunctionalUITest
    {
        // We need to override the browser new context options here so we can NOT have the cookies file, and ergo NOT be logged in.
        public override BrowserNewContextOptions ContextOptions => new BrowserNewContextOptions() { ViewportSize = CurrentViewportSizeFrom(TestContext) };

        [TestMethod]
        public async Task HomePage()
        {
            // Given: An empty context, where we are not logged in
            // (This is accomplished by ordering this test before the login test)

            // When: Navigating to the root of the site
            await Page.GotoAsync(Properties.Url);

            // Then: The home page loads
            await Page.ThenIsOnPageAsync("Home");
            await Page.SaveScreenshotToAsync(TestContext);
        }

        [TestMethod]
        public async Task LoginPage()
        {
            // Given: Starting at the home page, not logged in
            await HomePage();

            // When: Clicking on the login link

            var login = Page.Locator("data-test-id=login");
            if (!await login.IsVisibleAsync())
            {
                var navtoggle = Page.Locator("[aria-label=\"Toggle navigation\"]");
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

            // Then: The login page loads
            await Page.ThenIsOnPageAsync("Login");
            await Page.SaveScreenshotToAsync(TestContext);
        }

    }
}
