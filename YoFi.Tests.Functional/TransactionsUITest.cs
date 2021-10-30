using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    /// <summary>
    /// Test the Transactions page
    /// </summary>
    [TestClass]
    public class TransactionsUITest : FunctionalUITest
    {
        const int TotalItemCount = 889;

        [TestMethod]
        public async Task ClickTransactions()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Transactions" on the navbar
            await Page.ClickAsync("text=Transactions");

            // Then: We land at the transactions index page
            await ThenIsOnPage("Transactions");

            // And: All expected items are here
            await ThenTotalItemsAreEqual(TotalItemCount);
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
        public async Task IndexQAnyCategory()
        {
            // Given: We are logged in and on the transactions page
            await ClickTransactions();

            // When: Searching for "Food:Away:Coffee"
            await Page.FillAsync("data-test-id=q", "Food:Away:Coffee");
            await Page.ClickAsync("data-test-id=btn-search");

            // Then: Exactly 156 transactions are found, because we know this about our source data
            await ThenTotalItemsAreEqual(156);
        }

        [TestMethod]
        public async Task IndexClear()
        {
            // Given: We are logged in and on the transactions page, with an active search
            await IndexQAny12();

            // When: Pressing clear
            await Page.ClickAsync("data-test-id=btn-clear");

            // Then: Back to all the items
            await ThenTotalItemsAreEqual(TotalItemCount);
        }

        [TestMethod]
        public async Task DownloadAll()
        {
            // Given: We are logged in and on the transactions page
            await ClickTransactions();

            // When: Downloading transactions
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Export");

            var download1 = await Page.RunAndWaitForDownloadAsync(async () =>
            {
                await Page.ClickAsync("[data-test-id=\"btn-dl-save\"]");
            });

            // Then: A spreadsheet containing 889 Transactions was downloaded
            await ThenSpreadsheetWasDownloadedContaining<IdOnly>(source: download1, name: "Transaction", count: 889);

#if false
            // Enable if need to inspect
            var filename = $"{TestContext.FullyQualifiedTestClassName}-{TestContext.TestName}.xlsx";
            await download1.SaveAsAsync(filename);
            TestContext.AddResultFile(filename);
#endif
        }

        [TestMethod]
        public async Task DownloadQ12()
        {
            // Given: We are logged in and on the transactions page
            await ClickTransactions();

            // When: Searching for "Farquat"
            var searchword = "Farquat";
            await Page.FillAsync("data-test-id=q", searchword);
            await Page.ClickAsync("data-test-id=btn-search");

            // And: Downloading transactions
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Export");

            var download1 = await Page.RunAndWaitForDownloadAsync(async () =>
            {
                await Page.ClickAsync("[data-test-id=\"btn-dl-save\"]");
            });

            // Then: A spreadsheet containing 12 Transactions were downloaded
            var items = await ThenSpreadsheetWasDownloadedContaining<IdAndPayee>(source: download1, name: "Transaction", count: 12);

            // And: All items match the search criteria
            Assert.IsTrue(items.All(x => x.Payee.Contains(searchword)));

#if false
            // Enable if need to inspect
            var filename = $"{TestContext.FullyQualifiedTestClassName}-{TestContext.TestName}.xlsx";
            await download1.SaveAsAsync(filename);
            TestContext.AddResultFile(filename);
#endif
        }

        private class IdAndPayee
        {
            public int ID { get; set; }
            public string Payee { get; set; }
        }

        /* Not sure how to assert this
        [TestMethod]
        public async Task HelpPopup()
        {
            // Given: We are logged in and on the transactions page
            await ClickTransactions();

            // When: Hitting the help button
            await Page.ClickAsync("data-test-id=btn-help");

            // Then: The help text is findable
            var title = await Page.QuerySelectorAsync("id=searchHelpModal");
            Assert.IsNotNull(title);
            Assert.IsTrue(await title.IsEnabledAsync());
        }

        [TestMethod]
        public async Task HelpPopupClose()
        {
            // Given: We have the help popup active
            await HelpPopup();

            // When: Hitting the close button
            await Page.ClickAsync("text=Close");

            // Then: The help text is not findable
            var title = await Page.QuerySelectorAsync("data-test-id=help-title");
            Assert.IsFalse(await title.IsEnabledAsync());
        }
        */
    }
}
