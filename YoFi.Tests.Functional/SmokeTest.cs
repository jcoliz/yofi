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
        //
        // NOTE: I have removed most of the smoke tests. They are just part of the individual test areas now.
        //

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
    }
}
