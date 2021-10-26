using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    [TestClass]
    public class NotLoggedInUITest: PageTest
    {
        private readonly string Site = "http://localhost:50419/";

        [TestMethod]
        public async Task HomePage()
        {
            // Given: An empty context, where we are not logged in
            // (This is accomplished by ordering this test before the login test)

            // When: Navigating to the root of the site
            await Page.GotoAsync(Site);

            // Then: The home page loads
            await ThenIsOnPage("Home");
        }

        [TestMethod]
        public async Task LoginPage()
        {
            // Given: Starting at the home page, not logged in
            await HomePage();

            // When: Clicking on the login link
            await Page.ClickAsync("data-test-id=login");

            // Then: The login page loads
            await ThenIsOnPage("Login");
        }

        private async Task ThenIsOnPage(string expected)
        {
            var title = await Page.TitleAsync();
            Assert.AreEqual($"{expected} - Development - YoFi", title);
        }

    }
}
