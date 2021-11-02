using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

        [TestInitialize]
        public new async Task SetUp()
        {
            base.SetUp();

            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync("text=Budget");

            // Then: We are on the Payees page
            await ThenIsOnPage("Budget Line Items");
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
                api.SetUp();
                await api.ClearTestData("budgettx");
            }

            await Page.ReloadAsync();

            await ThenTotalItemsAreEqual(TotalItemCount);
        }

        [TestMethod]
        public async Task ClickBudget()
        {
            // Given: We are already logged in and on the budget page

            // And: All expected items are here
            await ThenTotalItemsAreEqual(TotalItemCount);

            // And: This page covers items 1-25
            await ThenPageContainsItems(from: 1, to: 25);
        }

        [TestMethod]
        public async Task Page2()
        {
            // Given: We are already logged in and on the budget page

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
            // Given: We are already logged in and on the budget page

            // When: Searching for "Farquat"
            await Page.FillAsync("data-test-id=q", "Healthcare");
            await Page.ClickAsync("data-test-id=btn-search");

            // Then: Exactly 25 items are found, because we know this about our source data
            await ThenTotalItemsAreEqual(25);
        }

        [TestMethod]
        public async Task IndexClear()
        {
            // Given: We are logged in and on the budget page, with an active search
            await IndexQ25();

            // When: Pressing clear
            await Page.ClickAsync("data-test-id=btn-clear");

            // Then: Back to all the items
            await ThenTotalItemsAreEqual(TotalItemCount);
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
            await ThenSpreadsheetWasDownloadedContaining<IdOnly>(source: download1, name: "BudgetTx", count: TotalItemCount);

#if false
            // Enable if need to inspect
            var filename = $"{TestContext.FullyQualifiedTestClassName}-{TestContext.TestName}.xlsx";
            await download1.SaveAsAsync(filename);
            TestContext.AddResultFile(filename);
#endif
        }

        [TestMethod]
        public async Task Create()
        {
            // Given: We are already logged in and on the budget page

            // When: Creating a new item
            var originalitems = Int32.Parse(await Page.TextContentAsync("data-test-id=totalitems"));

            // Click #dropdownMenuButtonAction
            await Page.ClickAsync("#dropdownMenuButtonAction");
            // Click text=Create New
            await Page.ClickAsync("text=Create New");
            // Fill input[name="Category"]
            await Page.FillAsync("input[name=\"Category\"]", NextCategory);
            // Fill input[name="Timestamp"]
            await Page.FillAsync("input[name=\"Timestamp\"]", "2021-12-31");
            // Fill input[name="Amount"]
            await Page.FillAsync("input[name=\"Amount\"]", "100");
            await ScreenShotAsync();

            // Click input:has-text("Create")
            await Page.ClickAsync("input:has-text(\"Create\")");

            // Then: We finish at the budget index page
            await ThenIsOnPage("Budget Line Items");

            // And: There is one more item
            var itemsnow = Int32.Parse(await Page.TextContentAsync("data-test-id=totalitems"));
            Assert.IsTrue(itemsnow == originalitems + 1);

            await ScreenShotAsync();
        }

        [TestMethod]
        public async Task Read()
        {
            // Given: One item created
            await Create();

            // When: Searching for the new item
            await Page.FillAsync("data-test-id=q", testmarker);
            await Page.ClickAsync("data-test-id=btn-search");
            await ScreenShotAsync();

            // Then: It's found
            await ThenTotalItemsAreEqual(1);
        }

        [TestMethod]
        public async Task Update()
        {
            // Given: One item created
            // And: It's the one item in search results
            await Read();

            // When: Editing it
            var newcategory = NextCategory;

            // Click [aria-label="Edit"]
            await Page.ClickAsync("[aria-label=\"Edit\"]");
            // Assert.AreEqual("http://localhost:50419/BudgetTxs/Edit/161", page.Url);
            // Fill input[name="Category"]
            await Page.FillAsync("input[name=\"Category\"]", newcategory);
            // Fill input[name="Amount"]
            await Page.FillAsync("input[name=\"Amount\"]", "200");
            await ScreenShotAsync();
            // Click text=Save
            await Page.ClickAsync("text=Save");
            // Assert.AreEqual("http://localhost:50419/BudgetTxs", page.Url);

            // Then: We're back on the main page
            await ThenIsOnPage("Budget Line Items");

            // And: Searching for the new item...
            await Page.FillAsync("data-test-id=q", newcategory);
            await Page.ClickAsync("data-test-id=btn-search");
            await ScreenShotAsync();

            // Then: It's found
            await ThenTotalItemsAreEqual(1);

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
            await ThenIsOnPage("Delete Budget Line Item");
            await ScreenShotAsync();

            // When: Clicking the Delete button to execute the delete
            await Page.ClickAsync("input:has-text(\"Delete\")");

            // Then: We finish at the budget index page
            await ThenIsOnPage("Budget Line Items");
            await ScreenShotAsync();

            // And: Total number of items is back to the standard amount
            await ThenTotalItemsAreEqual(TotalItemCount);
        }
    }
}
