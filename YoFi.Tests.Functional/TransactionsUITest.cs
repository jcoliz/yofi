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
    /// Test the Transactions page
    /// </summary>
    [TestClass]
    public class TransactionsUITest : FunctionalUITest
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
        }

        [TestMethod]
        public async Task IndexQAny12()
        {
            // Given: We are logged in and on the transactions page
            await ClickTransactions();

            // When: Searching for "Farquat"
            await Page.FillAsync("data-test-id=q", "Farquat");
            await Page.ClickAsync("data-test-id=btn-search");

            // Then: Exactly 12 transactions are found, because we know this about our source data
            await ThenTotalItemsAreEqual(12);
        }

        [TestMethod]
        public async Task IndexQAnyDate()
        {
            // Given: We are logged in and on the transactions page
            await ClickTransactions();

            // When: Searching for "Farquat"
            await Page.FillAsync("data-test-id=q", "1230");
            await Page.ClickAsync("data-test-id=btn-search");

            // Then: Exactly 3 transactions are found, because we know this about our source data
            await ThenTotalItemsAreEqual(3);
        }
    }
}
