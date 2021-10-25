using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YoFi.PWTests
{
    /// <summary>
    /// Test the various permutation of Reports
    /// </summary>
    [TestClass]
    public class ReportsUITest : PageTest
    {
        public override BrowserNewContextOptions ContextOptions => _ContextOptions;

        private static BrowserNewContextOptions _ContextOptions { get; set; }

        private readonly string Site = "http://localhost:50419/";

        private readonly string ConfigFileName = "reportsuitest-loginstate.json";

        [TestMethod]
        public async Task AAC_LoginAction()
        {
            // Given: An empty context, where we are not logged in
            // And: Starting at the login page
            await Page.GotoAsync(Site);
            await Page.ClickAsync("data-test-id=login");

            // And: User credentials as specified in user secrets
            var config = new ConfigurationBuilder().AddUserSecrets(Assembly.GetAssembly(typeof(ReportsUITest))).Build();
            var email = config["AdminUser:Email"];
            var password = config["AdminUser:Password"];

            // When: Filling out the login form with those credentials and pressing "sign in"
            await Page.FillAsync("id=floatingInput", email);
            await Page.FillAsync("id=floatingPassword", password);
            await Page.ClickAsync("data-test-id=signin");

            // Then: We land back at the home page
            var title = await Page.TitleAsync();
            Assert.AreEqual("Home - Development - YoFi", title);

            // And: The navbar has our email
            var content = await Page.TextContentAsync("data-test-id=hello-user");
            Assert.IsTrue(content.Contains(email));

            // And: The login button is not visible
            var login = await Page.QuerySelectorAsync("data-test-id=login");
            Assert.IsNull(login);

            // Save storage state into a file for later use            
            await Context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = ConfigFileName });

            // Set it as our new context options for later contexts
            _ContextOptions = new BrowserNewContextOptions { StorageStatePath = ConfigFileName };
        }

        private async Task GivenLoggedIn()
        {
            // Navigate to the root of the site
            await Page.GotoAsync(Site);

            // Are we already logged in?
            var login = await Page.QuerySelectorAsync("data-test-id=hello-user");

            // If we're not already logged in, well we need to do that then
            if (null == login)
            {
                Console.WriteLine("Logging in...");
                await AAC_LoginAction();
            }
        }

        [TestMethod]
        public async Task ClickReports()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync("text=Reports");

            // Then: We land at the budget index page
            var title = await Page.TitleAsync();
            Assert.AreEqual("Reports - Development - YoFi", title);

            // And: There are the expected number of items.
            // NOTE: This will change if the sample data pattern definitions change
            // This may be somewhat brittle. Consider changing this to a range
            var expected = "Summary";
            var content = await Page.TextContentAsync("h2");
            Assert.AreEqual(expected, content);
        }
    }
}
