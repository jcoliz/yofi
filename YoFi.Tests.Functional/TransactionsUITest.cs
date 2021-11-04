using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
        const string MainPageName = "Transactions";

        [TestInitialize]
        public new async Task SetUp()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Transactions" on the navbar
            await Page.ClickAsync("text=Transactions");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            //
            // Delete all test items
            //

            // Given: Clicking "Transactions" on the navbar
            await Page.ClickAsync("text=Transactions");

            // If: totalitems > expected TotalItemCount
            var totalitems = await Page.GetTotalItemsAsync();
            if (totalitems > TotalItemCount)
            {
                // When: Asking server to clear this test data
                var api = new ApiKeyTest();
                api.SetUp(TestContext);
                await api.ClearTestData("trx");
            }

            // And: Releaging the page
            await Page.ReloadAsync();

            // Then: Total items are back to normal
            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());
        }


        [TestMethod]
        public async Task ClickTransactions()
        {
            // Given: We are logged in and on the transactions page

            // Then: All expected items are here
            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());
        }

        [TestMethod]
        public async Task IndexQAny12()
        {
            // Given: We are logged in and on the transactions page

            // When: Searching for "Farquat"
            await Page.SearchFor("Farquat");

            // Then: Exactly 12 transactions are found, because we know this about our source data
            Assert.AreEqual(12, await Page.GetTotalItemsAsync());
        }

        [TestMethod]
        public async Task IndexQAnyCategory()
        {
            // Given: We are logged in and on the transactions page

            // When: Searching for "Food:Away:Coffee"
            await Page.SearchFor("Food:Away:Coffee");

            // Then: Exactly 156 transactions are found, because we know this about our source data
            Assert.AreEqual(156, await Page.GetTotalItemsAsync());
        }

        [TestMethod]
        public async Task IndexClear()
        {
            // Given: We are logged in and on the transactions page, with an active search
            await IndexQAny12();

            // When: Pressing clear
            await Page.ClickAsync("data-test-id=btn-clear");

            // Then: Back to all the items
            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());
        }

        [TestMethod]
        public async Task DownloadAll()
        {
            // Given: We are logged in and on the transactions page

            // When: Downloading transactions
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Export");

            var download1 = await Page.RunAndWaitForDownloadAsync(async () =>
            {
                await Page.ClickAsync("[data-test-id=\"btn-dl-save\"]");
            });

            // Then: A spreadsheet containing 889 Transactions was downloaded
            await download1.ThenIsSpreadsheetContainingAsync<IdOnly>(name: "Transaction", count: 889);
        }

        [TestMethod]
        public async Task DownloadQ12()
        {
            // Given: We are logged in and on the transactions page

            // When: Searching for "Farquat"
            var searchword = "Farquat";
            await Page.SearchFor(searchword);

            // And: Downloading transactions
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Export");

            var download1 = await Page.RunAndWaitForDownloadAsync(async () =>
            {
                await Page.ClickAsync("[data-test-id=\"btn-dl-save\"]");
            });

            // Then: A spreadsheet containing 12 Transactions were downloaded
            var items = await download1.ThenIsSpreadsheetContainingAsync<IdAndPayee>(name: "Transaction", count: 12);

            // And: All items match the search criteria
            Assert.IsTrue(items.All(x => x.Payee.Contains(searchword)));
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

        [TestMethod]
        public async Task Create()
        {
            // Given: We are logged in and on the transactions page

            // When: Creating a new item
            var originalitems = await Page.GetTotalItemsAsync();

            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Create New");
            await Page.FillFormAsync(new Dictionary<string, string>()
            {
                { "Category", NextCategory },
                { "Payee", NextName },
                { "Timestamp", "2021-12-31" },
                { "Amount", "100" },
                { "Memo", testmarker },
            });
            await Page.SaveScreenshotToAsync(TestContext);
            await Page.ClickAsync("input:has-text(\"Create\")");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);

            // And: There is one more item
            var itemsnow = await Page.GetTotalItemsAsync();
            Assert.AreEqual(originalitems + 1,itemsnow);

            await Page.SaveScreenshotToAsync(TestContext);
        }

        [DataRow(1)]
        [DataRow(5)]
        [DataRow(10)]
        [DataTestMethod]
        public async Task Read(int count)
        {
            // Given: One item created
            for ( int i=count ; i > 0 ; i-- )
                await Create();

            // When: Searching for the new item
            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext);

            // Then: It's found
            Assert.AreEqual(count, await Page.GetTotalItemsAsync());
        }

        [TestMethod]
        public async Task UpdateModal()
        {
            // Given: One item created
            // And: It's the one item in search results
            await Read(1);

            // When: Editing it
            var newcategory = NextCategory;
            var newpayee = NextName;

            await Page.ClickAsync("[aria-label=\"Edit\"]");
            await Page.FillFormAsync(new Dictionary<string, string>()
            {
                { "Category", newcategory },
                { "Payee", newpayee },
            });
            await Page.SaveScreenshotToAsync(TestContext);
            await Page.ClickAsync("text=Save");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);

            // And: Searching for the new item...
            await Page.SearchFor(newcategory);
            await Page.SaveScreenshotToAsync(TestContext);

            // Then: It's found
            Assert.AreEqual(1, await Page.GetTotalItemsAsync());

            // TODO: Also check that the amount is correct
        }

        [TestMethod]
        public async Task UpdatePage()
        {
            // Given: One item created
            // And: It's the one item in search results
            await Read(1);

            // When: Editing it
            var newcategory = NextCategory;
            var newpayee = NextName;

            await Page.ClickAsync("[aria-label=\"Edit\"]");
            var NextPage = await Page.RunAndWaitForPopupAsync(async () =>
            {
                await Page.WaitForSelectorAsync("input[name=\"Category\"]");
                await Page.SaveScreenshotToAsync(TestContext);
                await Page.ClickAsync("text=More");
            });

            await NextPage.FillFormAsync(new Dictionary<string, string>()
            {
                { "Category", newcategory },
                { "Payee", newpayee },
                { "Amount", "200" },
            });

            await NextPage.SaveScreenshotToAsync(TestContext);
            await NextPage.ClickAsync("text=Save");

            // Then: We are on the main page for this section
            await NextPage.ThenIsOnPageAsync(MainPageName);

            // And: Searching for the new item...
            await Page.SearchFor(newcategory);
            await Page.SaveScreenshotToAsync(TestContext);

            // Then: It's found
            Assert.AreEqual(1, await Page.GetTotalItemsAsync());

            // TODO: Also check that the amount is correct
        }

        [TestMethod]
        public async Task Delete()
        {
            // Given: One item created
            // And: It's the one item in search results
            await Read(1);

            // When: Deleting first item in list

            await Page.ClickAsync("[aria-label=\"Edit\"]");
            var NextPage = await Page.RunAndWaitForPopupAsync(async () =>
            {
                await Page.ClickAsync("text=More");
                await Page.SaveScreenshotToAsync(TestContext);
            });
            await NextPage.ClickAsync("text=Delete");

            // Then: We land at the delete page
            await NextPage.ThenIsOnPageAsync("Delete Transaction");
            await NextPage.SaveScreenshotToAsync(TestContext);

            // When: Clicking the Delete button to execute the delete
            await NextPage.ClickAsync("input:has-text(\"Delete\")");

            // Then: We land at the transactions index page
            await NextPage.ThenIsOnPageAsync(MainPageName);
            await NextPage.SaveScreenshotToAsync(TestContext);

            // And: Total number of items is back to the standard amount
            Assert.AreEqual(TotalItemCount, await NextPage.GetTotalItemsAsync());
        }

        [TestMethod]
        public async Task CreateReceipt()
        {
            // Given: One item created
            // And: It's the one item in search results
            await Read(1);

            // And: Editing it
            await Page.ClickAsync("[aria-label=Edit]");
            var NextPage = await Page.RunAndWaitForPopupAsync(async () =>
            {
                await Page.WaitForSelectorAsync("input[name=Category]");
                await Page.SaveScreenshotToAsync(TestContext);
                await Page.ClickAsync("text=More");
            });

            // When: Uploading a receipt
            await NextPage.ClickAsync("[aria-label=UploadReceipt]");
            await NextPage.SetInputFilesAsync("[aria-label=UploadReceipt]", new[] { "SampleData/Test-Generator-GenerateUploadSampleData.xlsx" });
            await NextPage.SaveScreenshotToAsync(TestContext);
            await NextPage.ClickAsync("data-test-id=btn-create-receipt");

            // Then: Get Receipt button is visible
            var delete = await NextPage.QuerySelectorAsync("data-test-id=btn-delete-receipt");
            await NextPage.SaveScreenshotToAsync(TestContext);

            Assert.IsNotNull(delete);

            // TODO: Clean up the storage, else this is going to leave a lot of extra crap lying around there
        }

        private async Task recording()
        {
            var page = Page;

            // Click [aria-label="Edit"]
            await page.ClickAsync("[aria-label=\"Edit\"]");
            // Click text=More
            var page1 = await page.RunAndWaitForPopupAsync(async () =>
            {
                await page.ClickAsync("text=More");
            });
            // Click [aria-label="UploadReceipt"]
            await page1.ClickAsync("[aria-label=\"UploadReceipt\"]");
            // Upload budget-white-60x.png
            await page1.SetInputFilesAsync("[aria-label=\"UploadReceipt\"]", new[] { "budget-white-60x.png" });
            // Click :nth-match(:text("Upload"), 2)
            await page1.ClickAsync(":nth-match(:text(\"Upload\"), 2)");
            // Assert.AreEqual("http://localhost:50419/Transactions/Edit/234", page1.Url);
            // Click text=Download
            var download1 = await page1.RunAndWaitForDownloadAsync(async () =>
            {
                await page1.ClickAsync("text=Download");
            });
            // Click button:has-text("Delete")
            await page1.ClickAsync("button:has-text(\"Delete\")");
            // Assert.AreEqual("http://localhost:50419/Transactions/Edit/234", page1.Url);
        }
    }
}
