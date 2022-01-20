using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    [TestClass]
    public class NotLoggedInUITest: FunctionalUITest
    {
        [TestMethod]
        public async Task HomePage()
        {
            // Given: An empty context, where we are not logged in
            // (This is accomplished by ordering this test before the login test)

            // When: Navigating to the root of the site
            await Page.GotoAsync(Properties.Url);

            // Then: The home page loads
            await Page.ThenIsOnPageAsync("Home");
        }

        [TestMethod]
        public async Task LoginPage()
        {
            // Given: Starting at the home page, not logged in
            await HomePage();
            await Page.SaveScreenshotToAsync(TestContext);

            // When: Clicking on the login link
            await Page.ClickAsync("data-test-id=login");
            // NOTE: This line has failed in full-suite testing then passed later in individual testing

            // Then: The login page loads
            await Page.ThenIsOnPageAsync("Login");
        }

    }
}
