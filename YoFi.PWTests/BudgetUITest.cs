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
    /// Test the Budget page
    /// </summary>
    [TestClass]
    public class BudgetUITest : PageTest
    {
        public override BrowserNewContextOptions ContextOptions => _ContextOptions;

        private static BrowserNewContextOptions _ContextOptions { get; set; }

        private readonly string Site = "http://localhost:50419/";

        private readonly string ConfigFileName = "budgetuitest-loginstate.json";

        private async Task DoLogin()
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
                await DoLogin();
            }
        }

        [TestMethod]
        public async Task ClickBudget()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync("text=Budget");

            // Then: We land at the budget index page
            await ThenIsOnPage("Budget Line Items");

            // And: This page covers items 1-25
            await ThenPageContainsItems(from: 1, to: 25);
        }

        [TestMethod]
        public async Task Page2()
        {
            // Given: We are logged in and on the budget page
            await ClickBudget();

            // When: Clicking on the next page on the pagination control
            await Page.ClickAsync("data-test-id=nextpage");

            // Then: We are still on the budget index page
            await ThenIsOnPage("Budget Line Items");

            // And: This page covers items 26-50
            await ThenPageContainsItems(from: 26, to: 50);
        }

        [TestMethod]
        public async Task IndexQ25()
        {
            // Given: We are logged in and on the budget page
            await ClickBudget();

            // When: Searching for "Farquat"
            await Page.FillAsync("data-test-id=q", "Healthcare");
            await Page.ClickAsync("data-test-id=btn-search");

            // Then: Exactly 25 transactions are found, because we know this about our source data
            await ThenTotalItemsAreEqual(25);
        }

        private async Task ThenIsOnPage(string expected)
        {
            var title = await Page.TitleAsync();
            Assert.AreEqual($"{expected} - Development - YoFi", title);
        }

        private async Task ThenPageContainsItems(int from, int to)
        {
            Assert.AreEqual(from.ToString(), await Page.TextContentAsync("data-test-id=firstitem"));
            Assert.AreEqual(to.ToString(), await Page.TextContentAsync("data-test-id=lastitem"));
        }

        private async Task ThenTotalItemsAreEqual(int howmany)
        {
            Assert.AreEqual(howmany.ToString(), await Page.TextContentAsync("data-test-id=totalitems"));
        }
    }
}
