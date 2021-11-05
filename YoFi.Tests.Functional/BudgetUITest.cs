using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    /// <summary>
    /// Test the Budget page
    /// </summary>
    [TestClass]
    public class BudgetUITest : FunctionalUITest
    {
        const int TotalItemCount = 156;
        const string MainPageName = "Budget Line Items";

        [TestInitialize]
        public async Task SetUp()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync("text=Budget");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            //
            // Delete all test items
            //

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync("text=Budget");

            // And: totalitems > expected TotalItemCount
            var totalitems = Int32.Parse(await Page.TextContentAsync("data-test-id=totalitems"));

            if (totalitems > TotalItemCount)
            {
                var api = new ApiKeyTest();
                api.SetUp(TestContext);
                await api.ClearTestData("budgettx");
            }

            await Page.ReloadAsync();

            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());
        }

        [TestMethod]
        public async Task ClickBudget()
        {
            // Given: We are already logged in and on the budget page

            // And: All expected items are here
            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());

            // And: This page covers items 1-25
            await Page.ThenContainsItemsAsync(from: 1, to: 25);
        }

        [TestMethod]
        public async Task Page2()
        {
            // Given: We are already logged in and on the budget page

            // When: Clicking on the next page on the pagination control
            await Page.ClickAsync("data-test-id=nextpage");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);

            // And: This page covers items 26-50
            await Page.ThenContainsItemsAsync(from: 26, to: 50);
        }

        [TestMethod]
        public async Task IndexQ25()
        {
            // Given: We are already logged in and on the budget page

            // When: Searching for "Healthcare"
            await Page.SearchFor("Healthcare");

            // Then: Exactly 25 items are found, because we know this about our source data
            Assert.AreEqual(25, await Page.GetTotalItemsAsync());
        }

        [TestMethod]
        public async Task IndexClear()
        {
            // Given: We are logged in and on the budget page, with an active search
            await IndexQ25();

            // When: Pressing clear
            await Page.ClickAsync("data-test-id=btn-clear");

            // Then: Total expected items are showing
            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());
        }

        [TestMethod]
        public async Task DownloadAll()
        {
            // Given: We are already logged in and on the budget page

            // When: Downloading items
            await Page.ClickAsync("#dropdownMenuButtonAction");

            var download1 = await Page.RunAndWaitForDownloadAsync(async () =>
            {
                await Page.ClickAsync("text=Export");
            });

            // Then: A spreadsheet containing 156 BudgetTxs was downloaded
            await download1.ThenIsSpreadsheetContainingAsync<IdOnly>(name: "BudgetTx", count: TotalItemCount);
        }

        [TestMethod]
        public async Task Create()
        {
            // Given: We are already logged in and on the budget page
            var originalitems = await Page.GetTotalItemsAsync();

            // When: Creating a new item
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Create New");
            await Page.FillFormAsync(new Dictionary<string, string>()
            {
                { "Category", NextCategory },
                { "Timestamp", "2021-12-31" },
                { "Amount", "100" },
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

        [TestMethod]
        public async Task Read()
        {
            // Given: One item created
            await Create();

            // When: Searching for the new item
            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext);

            // Then: It's found
            Assert.AreEqual(1, await Page.GetTotalItemsAsync());
        }

        [TestMethod]
        public async Task Update()
        {
            // Given: One item created
            // And: It's the one item in search results
            await Read();

            // When: Editing the category to {newcategory}
            var newcategory = NextCategory;
            await Page.ClickAsync("[aria-label=\"Edit\"]");
            await Page.FillFormAsync(new Dictionary<string, string>()
            {
                { "Category", newcategory },
                { "Amount", "100" },
            });

            await Page.SaveScreenshotToAsync(TestContext);
            await Page.ClickAsync("text=Save");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);

            // And: Searching for {newcategory}
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
            await Read();

            // When: Clicking delete on first item in list
            var deletebutton = await Page.QuerySelectorAsync("[aria-label=\"Delete\"]");
            Assert.IsNotNull(deletebutton);
            await deletebutton.ClickAsync();

            // Then: We land at the delete page
            await Page.ThenIsOnPageAsync("Delete Budget Line Item");
            await Page.SaveScreenshotToAsync(TestContext);

            // When: Clicking the Delete button to execute the delete
            await Page.ClickAsync("input:has-text(\"Delete\")");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
            await Page.SaveScreenshotToAsync(TestContext);

            // And: Total number of items is back to the standard amount
            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());
        }
    }
}
