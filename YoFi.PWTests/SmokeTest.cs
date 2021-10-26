using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace YoFi.PWTests
{
    /// <summary>
    /// Run through top-level golden path to make sure primary pages load
    /// </summary>
    [TestClass]
    public class SmokeTest: FunctionalUITest
    {
        [TestMethod]
        public async Task ClickTransactions()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Transactions" on the navbar
            await Page.ClickAsync("text=Transactions");

            // Then: We land at the transactions index page
            await ThenIsOnPage("Transactions");

            // And: There are the expected number of transactions.
            // NOTE: This will change if the sample data pattern definitions change
            // This may be somewhat brittle. Consider changing this to a range
            await ThenTotalItemsAreEqual(889);
        }

        [TestMethod]
        public async Task ClickPayees()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Payees" on the navbar
            await Page.ClickAsync("text=Payees");

            // Then: We land at the payees index page
            await ThenIsOnPage("Payees");

            // And: There are the expected number of items.
            // NOTE: This will change if the sample data pattern definitions change
            // This may be somewhat brittle. Consider changing this to a range
            await ThenTotalItemsAreEqual(40);
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

            // And: There are the expected number of items.
            // NOTE: This will change if the sample data pattern definitions change
            // This may be somewhat brittle. Consider changing this to a range
            await ThenTotalItemsAreEqual(156);
        }

        [TestMethod]
        public async Task ClickImport()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Import" on the navbar
            await Page.ClickAsync("text=Import");

            // Then: We land at import page
            await ThenIsOnPage("Import Transactions");
        }

        [TestMethod]
        public async Task ClickProfile()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking my email on the navbar
            await Page.ClickAsync("data-test-id=hello-user");

            // Then: We land at profile page
            await ThenIsOnPage("Profile");
        }

        [TestMethod]
        public async Task LogOut()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "logout" on the navbar
            await Page.ClickAsync("data-test-id=logout");

            // Then: We land at home page
            await ThenIsOnPage("Home");

            // And: The login button is again visible
            var login = await Page.QuerySelectorAsync("data-test-id=login");
            Assert.IsNotNull(login);
            var text = await login.TextContentAsync();
            Assert.AreEqual("Log in", text);
        }
    }
}
