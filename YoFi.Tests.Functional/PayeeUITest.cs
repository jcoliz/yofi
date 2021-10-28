using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    [TestClass]
    public class PayeeUITest : FunctionalUITest
    {
        [TestMethod]
        public async Task ClickPayees()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync("text=Payees");

            // Then: We land at the budget index page
            await ThenIsOnPage("Payees");

            // And: This page covers items 1-25
            await ThenPageContainsItems(from: 1, to: 25);
        }

        [TestMethod]
        public async Task Page2()
        {
            // Given: We are logged in and on the payees page
            await ClickPayees();

            // When: Clicking on the next page on the pagination control
            await Page.ClickAsync("data-test-id=nextpage");

            // Then: We are still on the payees index page
            await ThenIsOnPage("Payees");

            // And: This page covers items 26-40
            await ThenPageContainsItems(from: 26, to: 40);
        }

        [TestMethod]
        public async Task IndexQCategory()
        {
            // Given: We are logged in and on the payees page
            await ClickPayees();

            // When: Searching for "Utilities" (which will match category)
            await Page.FillAsync("data-test-id=q", "Utilities");
            await Page.ClickAsync("data-test-id=btn-search");

            // Then: Exactly 25 items are found, because we know this about our source data
            await ThenTotalItemsAreEqual(5);
        }

        [TestMethod]
        public async Task IndexQName()
        {
            // Given: We are logged in and on the payees page
            await ClickPayees();

            // When: Searching for "am" (which will match name)
            await Page.FillAsync("data-test-id=q", "am");
            await Page.ClickAsync("data-test-id=btn-search");

            // Then: Exactly 25 items are found, because we know this about our source data
            await ThenTotalItemsAreEqual(2);
        }

        [TestMethod]
        public async Task IndexClear()
        {
            // Given: We are logged in and on the budget page, with an active search
            await IndexQCategory();

            // When: Pressing clear
            await Page.ClickAsync("data-test-id=btn-clear");

            // Then: Back to all the items
            await ThenTotalItemsAreEqual(40);
        }
    }
}
