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
        public const int TotalItemCount = 46;
        const string MainPageName = "Budget Line Items";

        #region Init/Cleanup

        [TestInitialize]
        public new async Task SetUp()
        {
            base.SetUp();

            // Given: We are already logged in and starting at the root of the site
            // When: Navigating to the main page for this section
            await WhenNavigatingToPage("Budget/Edit Budget");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            //
            // Delete all test items
            //

            // When: Navigating to the main page for this section
            await WhenNavigatingToPage("Budget/Edit Budget");

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

        #endregion

        #region Index Tests

        /// <summary>
        /// [User Can] view their list of budget items
        /// [Scenario] First page
        /// </summary>
        [TestMethod]
        public async Task ClickBudget()
        {
            // Given: We are already logged in and on the budget page

            // And: All expected items are here
            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());

            // And: This page covers items 1-25
            await Page.ThenContainsItemsAsync(from: 1, to: 25);
        }

        /// <summary>
        /// [User Can] view their list of budget items
        /// [Scenario] Second page
        /// </summary>
        [TestMethod]
        public async Task Page2()
        {
            // Given: We are already logged in and on the budget page

            // When: Clicking on the next page on the pagination control
            await Page.ClickAsync("data-test-id=nextpage");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);

            // And: This page covers items 26-46
            await Page.ThenContainsItemsAsync(from: 26, to: 46);
        }

        /// <summary>
        /// [User Can] Search for budget items containing a chosen term
        /// [Scenario] Enter search term
        /// </summary>
        [TestMethod]
        public async Task IndexQ25()
        {
            // Given: We are already logged in and on the budget page

            // When: Searching for "Healthcare"
            await Page.SearchFor("Healthcare");

            // Then: Exactly 3 items are found, because we know this about our source data
            Assert.AreEqual(3, await Page.GetTotalItemsAsync());
        }

        /// <summary>
        /// [User Can] Search for budget items containing a chosen term
        /// [Scenario] Clear search term
        /// </summary>
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

        #endregion

        #region Download Tests

        /// <summary>
        /// [User Can] Download their budget items to a spreadsheet
        /// </summary>
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

        #endregion

        #region  CRUD Tests

        /// <summary>
        /// [User Can] Manually create a new budget item
        /// [Scenario] Create item
        /// </summary>
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
            await Page.WaitForLoadStateAsync();

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);

            // And: There is one more item
            var itemsnow = await Page.GetTotalItemsAsync();
            Assert.AreEqual(originalitems + 1,itemsnow);

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// [User Can] View the details of a budget item
        /// (Note this would normally entail a "details" screen, but budgettx are pretty simple so we don't need that)
        /// </summary>
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

        /// <summary>
        /// [User Can] Update the editable values of a budget item
        /// </summary>
        [TestMethod]
        public async Task Update()
        {
            // Given: One item created
            // And: It's the one item in search results
            await Read();

            // When: Editing the category to {newcategory}
            var newcategory = NextCategory;
            await Page.ClickAsync("#actions-1");
            await Page.SaveScreenshotToAsync(TestContext, "Context Menu");
            await Page.ClickAsync("[data-test-id=edit]");
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

        /// <summary>
        /// [User Can] Delete a budget item
        /// </summary>
        [TestMethod]
        public async Task Delete()
        {
            // Given: One item created
            // And: It's the one item in search results
            await Read();

            // When: Clicking delete on first item in list (in line context menu)
            await Page.ClickAsync("#actions-1");
            await Page.SaveScreenshotToAsync(TestContext, "Context Menu");
            await Page.ClickAsync("[data-test-id=delete]");

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

        #endregion

        #region Upload Tests

        /// <summary>
        /// [User can] Upload new budget line items from a spreadsheet which was created in Excel
        /// </summary>
        [TestMethod]
        public async Task Upload()
        {
            //
            // Step 1: Upload payees
            //

            // Given: We are logged in and on the payees page

            // Then: We are on the main page for this section
            await Page.ClickAsync("[aria-label=\"Upload\"]");
            await Page.SetInputFilesAsync("[aria-label=\"Upload\"]", new[] { "SampleData/Test-Generator-GenerateUploadSampleData.xlsx" });
            await Page.ClickAsync("text=Upload");

            // Then: We land at the uploaded OK page
            await Page.ThenIsOnPageAsync("Uploaded Budget");

            //
            // Step 2: Search for the new payees
            //

            // When: Navigating to edit budget page
            await WhenNavigatingToPage("Budget/Edit Budget");

            // Then: {numadded} more items found than before, because we just added them
            var numadded = 4;
            Assert.AreEqual(TotalItemCount + numadded, await Page.GetTotalItemsAsync());

            // When: Searching for what we just imported
            await Page.SearchFor(testmarker);

            // Then: {numadded} items are found, because we know this about our imported data
            Assert.AreEqual(numadded, await Page.GetTotalItemsAsync());
        }

        #endregion

        #region User Story 911: [User Can] Designate additional 'memo' information about a single budget line item

        /// <summary>
        /// [Scenario] User Can create an item with a memo
        /// </summary>
        [TestMethod]
        public async Task CreateWithMemo()
        {
            // Given: We are already logged in and on the budget page

            // When: Creating a new item with a Memo
            var memotext = "Memo Created";
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Create New");
            await Page.FillFormAsync(new Dictionary<string, string>()
            {
                { "Category", NextCategory },
                { "Timestamp", "2021-12-31" },
                { "Amount", "100" },
                { "Memo", memotext },
            });
            await Page.SaveScreenshotToAsync(TestContext);
            await Page.ClickAsync("input:has-text(\"Create\")");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
            await Page.SaveScreenshotToAsync(TestContext);

            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext);

            // And: The first item shown has the newly created memo
            var text = await Page.Locator("data-test-id=memo").First.TextContentAsync();
            var actual = text.Trim();

            Assert.AreEqual(memotext, actual);
        }

        /// <summary>
        /// [Scenario] User Can edit the memo and change it
        /// </summary>
        [TestMethod]
        public async Task EditMemo()
        {
            // Given: One item created with a memo
            await CreateWithMemo();

            // When: Editing the memo to {newmemo}
            var newmemo = "Edited the memo";
            await Page.ClickAsync("#actions-1");
            await Page.SaveScreenshotToAsync(TestContext, "Context Menu");
            await Page.ClickAsync("[data-test-id=edit]");
            await Page.FillFormAsync(new Dictionary<string, string>()
            {
                { "Memo", newmemo },
            });

            await Page.SaveScreenshotToAsync(TestContext);
            await Page.ClickAsync("text=Save");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
            await Page.SaveScreenshotToAsync(TestContext);

            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext);

            // And: The first item shown has the newly created memo
            var text = await Page.Locator("data-test-id=memo").First.TextContentAsync();
            var actual = text.Trim();

            Assert.AreEqual(newmemo, actual);
        }

        /// <summary>
        /// [Scenario] User Can see the memo when deleting it
        /// </summary>
        [TestMethod]
        public async Task DeleteWithMemo()
        {
            // Given: One item created with a memo
            await CreateWithMemo();

            // When: Clicking delete on first item in list
            await Page.ClickAsync("#actions-1");
            await Page.SaveScreenshotToAsync(TestContext, "Context Menu");
            await Page.ClickAsync("[data-test-id=delete]");
            await Page.ThenIsOnPageAsync("Delete Budget Line Item");
            await Page.SaveScreenshotToAsync(TestContext,"Deleted");

            // Then: The memo is shown when confirming details of what to delete
            var text = await Page.Locator("data-test-id=memo").TextContentAsync();
            var actual = text.Trim().ToLowerInvariant();

            Assert.IsTrue(actual.Contains("memo"));
        }

        #endregion

        #region User Story 1194: [User Can] Delete multiple budgettx in a single operation

        [TestMethod]
        public async Task BulkDelete()
        {
            /*
            Given: On the Edit Budget page, logged in
            And: Three new items added with a distinctive name
            And: Showing a search result with just those added items
            When: Entering bulk edit mode
            And: Clicking select on each item
            And: Clicking "Delete" on the bulk edit bar
            And: Clicking "OK" on the confirmation dialog
            Then: Still on the Edit Budget page
            And: Showing all items
            And: Bulk edit toolbar is gone (Don't know how to check for this)
            And: Total number of items is back to the standard amount
             */

            // Given: On the Edit Budget page, logged in
            // (Done by Setup())

            // And: Three new items added with a distinctive name
            await Create();
            await Create();
            await Create();

            // And: Showing a search result with just those added items
            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext);

            // When: Entering bulk edit mode
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Bulk Edit");
            await Page.SaveScreenshotToAsync(TestContext);

            // And: Clicking select on each item
            var numdelete = int.Parse(await Page.TextContentAsync("data-test-id=totalitems"));
            for (int i = 1; i <= numdelete; i++)
                await Page.ClickAsync($"data-test-id=line-{i} >> data-test-id=check-select");
            await Page.SaveScreenshotToAsync(TestContext);

            // When: Clicking "Delete" on the bulk edit bar
            await Page.ClickAsync("data-test-id=btn-bulk-delete");
            await Page.SaveScreenshotToAsync(TestContext);


            // And: Clicking "OK" on the confirmation dialog
            await Page.WaitForSelectorAsync("#deleteConfirmModal");
            await Page.SaveScreenshotToAsync(TestContext);
            await Page.ClickAsync("data-test-id=btn-modal-ok");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
            await Page.SaveScreenshotToAsync(TestContext);

            // And: Total number of items is back to the standard amount
            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());
        }

        #endregion

        #region User Story 1226: [User Can] Describe their budget with a single line item per category, which may repeat over the year

        /// <summary>
        /// [Scenario] User can create a new budget line item with an alternative frequency
        /// </summary>
        
        [DataRow("1","")]
        [DataRow("4", "Quarterly")]
        [DataRow("12", "Monthly")]
        [DataRow("52", "Weekly")]
        [DataTestMethod]
        public async Task CreateWithFrequency(string number, string text)
        {
            // Given: We are already logged in and on the budget page

            // When: Creating a new item with a Memo
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Create New");
            await Page.FillFormAsync(new Dictionary<string, string>()
            {
                { "Category", NextCategory },
                { "Timestamp", "2021-12-31" },
                { "Amount", "100" },
            });
            await Page.SelectOptionAsync("select[name=\"Frequency\"]", new[] { number });
            await Page.SaveScreenshotToAsync(TestContext);
            await Page.ClickAsync("input:has-text(\"Create\")");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
            await Page.SaveScreenshotToAsync(TestContext);

            // And: The first item shown has the newly created frequency
            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext);
            var freqtext = await Page.Locator("data-test-id=freq").First.TextContentAsync();
            var actual = freqtext.Trim();

            Assert.AreEqual(text, actual);
        }

        /// <summary>
        /// [Scenario] User can create a edit an existing budget line item to an alternative frequency
        /// </summary>
        [DataRow("1", "")]
        [DataRow("4", "Quarterly")]
        [DataRow("12", "Monthly")]
        [DataRow("52", "Weekly")]
        [DataTestMethod]
        public async Task EditFrequency(string frequency_num, string frequency_text)
        {
            // Given: One item created with no special frequency
            await Create();

            // And: Page displaying only that item
            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext);

            // When: Editing the memo to {newfrequency}
            await Page.ClickAsync("#actions-1");
            await Page.SaveScreenshotToAsync(TestContext, "Context Menu");
            await Page.ClickAsync("[data-test-id=edit]");
            await Page.SelectOptionAsync("select[name=\"Frequency\"]", new[] { frequency_num });
            await Page.SaveScreenshotToAsync(TestContext);
            await Page.ClickAsync("text=Save");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
            await Page.SaveScreenshotToAsync(TestContext);

            // And: The first item shown has the newly created frequency
            await Page.SearchFor(testmarker);
            var text = await Page.Locator("data-test-id=freq").First.TextContentAsync();
            var actual = text.Trim();

            Assert.AreEqual(frequency_text, actual);
        }

        #endregion

    }
}
