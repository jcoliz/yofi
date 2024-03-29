﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YoFi.AspNet.Tests.Functional
{
    [TestClass]
    public class PayeeUITest : FunctionalUITest
    {
        public const int TotalItemCount = 40;
        const string MainPageName = "Payees";

        #region Init/Cleanup

        [TestInitialize]
        public new async Task SetUp()
        {
            base.SetUp();

            // When: Navigating to the main page for this section
            await WhenNavigatingToPage(MainPageName);
            // NOTE: This line has failed in full-suite testing then passed later in individual testing

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            //
            // Delete all test items
            //

            // When: Clicking "{page}" on the navbar
            await WhenNavigatingToPage(MainPageName);

            // And: totalitems > expected TotalItemCount
            var totalitems = await Page.GetTotalItemsAsync();

            if (totalitems > TotalItemCount)
            {
                var api = new ApiKeyTest();
                api.SetUp(TestContext);
                await api.ClearTestData("payee");

                await Page.ReloadAsync();
                // (Above has in the past given timeouts)
                //await Page.ClickAsync("text=Payees");

                Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());
            }
        }

        #endregion

        #region Index Tests

        /// <summary>
        /// [User Can] View the current list of active payee matching rules
        /// </summary>
        [TestMethod]
        public async Task Initial()
        {
            // When: We are on the main page for this section
            // (Set up in Setup)
            await Page.SaveScreenshotToAsync(TestContext, "Index");

            // And: All expected items are here
            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());

            // And: This page covers items 1-25
            await Page.ThenContainsItemsAsync(from: 1, to: 25);
        }

        /// <summary>
        /// [User Can] Page through the current list of active payee matching rules, without
        /// having to wait the entire list to load.
        /// </summary>
        [TestMethod]
        public async Task Page2()
        {
            // Given: We are logged in and on the payees page

            // When: Clicking on the next page on the pagination control
            await Page.ClickAsync("data-test-id=nextpage");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);

            // And: This page covers items 26-40
            await Page.ThenContainsItemsAsync(from: 26, to: TotalItemCount);
        }

        /// <summary>
        /// [User Can] Search for payees using some part of the desired category
        /// </summary>
        [TestMethod]
        public async Task IndexQCategory()
        {
            // Given: We are logged in and on the payees page

            // When: Searching for "Utilities" (which will match category)
            await Page.SearchFor("Utilities");

            // Then: Exactly 5 items are found, because we know this about our source data
            Assert.AreEqual(5, await Page.GetTotalItemsAsync());
        }

        /// <summary>
        /// [User Can] Search for payees using some part of the desired payee name
        /// </summary>
        [TestMethod]
        public async Task IndexQName()
        {
            // Given: We are logged in and on the payees page

            // When: Searching for "am" (which will match name)
            await Page.SearchFor("am");
            await Page.ClickAsync("data-test-id=btn-search");

            // Then: Exactly 3 items are found, because we know this about our source data
            Assert.AreEqual(3, await Page.GetTotalItemsAsync());
        }

        /// <summary>
        /// [User Can] Abort a search, and instead return to the full list of items
        /// </summary>
        [TestMethod]
        public async Task IndexClear()
        {
            // Given: We are logged in and on the budget page, with an active search
            await IndexQCategory();

            // When: Pressing clear
            await Page.ClickAsync("data-test-id=btn-clear");

            // Then: Back to all the items
            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());
        }

        #endregion

        #region Upload/Download Tests

        /// <summary>
        /// [User can] Export all payees to a spreadsheet for later viewing/editing in Excel
        /// </summary>
        [TestMethod]
        public async Task DownloadAll()
        {
            // Given: We are logged in and on the payees page

            // When: Downloading items
            await Page.ClickAsync("#dropdownMenuButtonAction");

            var download1 = await Page.RunAndWaitForDownloadAsync(async () =>
            {
                await Page.ClickAsync("text=Export");
            });

            // Then: A spreadsheet containing 40 Payees was downloaded
            await download1.ThenIsSpreadsheetContainingAsync<IdOnly>(name: "Payee", count: TotalItemCount);
        }

        /// <summary>
        /// [User can] Upload new payees to a spreadsheet which was created in Excel
        /// </summary>
        [TestMethod]
        public async Task Upload()
        {
            //
            // Step 1: Upload payees
            //

            // Given: We are logged in and on the payees page

            // And: Uploading payees
            await Page.ClickAsync("[aria-label=\"Upload\"]");
            await Page.SetInputFilesAsync("[aria-label=\"Upload\"]", new[] { "SampleData/SampleData-2022-Upload.xlsx" });
            await Page.ClickAsync("text=Upload");

            // Then: We land at the budget index page
            await Page.ThenIsOnPageAsync("Uploaded Payees");

            //
            // Step 2: Search for the new payees
            //

            // When: Navgiating to Payees page
            await WhenNavigatingToPage(MainPageName);

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);

            // Then: 43 items are found, because we just added 3
            Assert.AreEqual(TotalItemCount + 3, await Page.GetTotalItemsAsync());

            // When: Searching for what we just imported
            await Page.SearchFor(testmarker);

            // Then: 3 items are found, because we know this about our imported data
            Assert.AreEqual(3, await Page.GetTotalItemsAsync());
        }

        #endregion

        #region Bulk Operations Tests

        /// <summary>
        /// [User can] Delete multiple payees in a single operation
        /// </summary>
        [TestMethod]
        public async Task BulkDelete()
        {
            /*
            Given: On the Payees page, logged in
            And: Three new items added with a distinctive name
            And: Showing a search result with just those added items
            And: In bulk edit mode
            When: Clicking select on each item
            Ahd: Clicking "Delete" on the bulk edit bar
            And: Clicking "OK" on the confirmation dialog
            Then: Still on the Payees page
            And: Showing all items
            And: Bulk edit toolbar is gone (Don't know how to check for this)
            And: Total number of items is back to the standard amount
             */

            // Given: We are logged in and on the payees page

            // And: Three new items added with a distinctive name
            await GivenPayeeInDatabase();
            await GivenPayeeInDatabase();
            await GivenPayeeInDatabase();

            // And: Searching for the newly added items
            await Page.SearchFor(testmarker);

            // And: In bulk edit mode
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Bulk Edit");
            await Page.SaveScreenshotToAsync(TestContext,"Bulk Edit Mode");

            // And: Clicking select on each item
            var numdelete = Int32.Parse(await Page.TextContentAsync("data-test-id=totalitems"));
            for (int i = 1; i <= numdelete; i++)
                await Page.ClickAsync($"data-test-id=line-{i} >> data-test-id=check-select");
            await Page.SaveScreenshotToAsync(TestContext,"Selected Items");

            // When: Clicking "Delete" on the bulk edit bar
            await Page.ClickAsync("data-test-id=btn-bulk-delete");

            // And: Clicking "OK" on the confirmation dialog
            await Page.WaitForSelectorAsync("#deleteConfirmModal");
            await Task.Delay(500);
            await Page.SaveScreenshotToAsync(TestContext,"Delete Confirm Dialog");
            await Page.ClickAsync("data-test-id=btn-modal-ok");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
            await Page.SaveScreenshotToAsync(TestContext,"Completed");

            // And: Total number of items is back to the standard amount
            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());
        }

        #endregion

        #region CRUD

        /// <summary>
        /// [User can] Add a new payee matching rule based on a selected transaction from the list of transactions
        /// </summary>
        [TestMethod]
        public async Task TransactionsAddPayee()
        {
            // This is a transactions page test but it needs to be here because it
            // inserts into the payees, which screws up our tests here unless we wait
            // for it. Also we will be equiped to clean it up here better.

            // Given: We are logged in and on the transactions page
            await WhenNavigatingToPage("Transactions");

            // When: Clicking 'Add Payee' on context menu of the the first line
            await Page.ClickAsync("#actions-1");
            await Page.Locator($"[data-bs-target=\"#addPayeeModal\"]").First.ClickAsync();

            // And: Adding a new Payee from the ensuing dialog
            var name = NextName;
            await Page.WaitForSelectorAsync("#addPayeeModal",new Microsoft.Playwright.PageWaitForSelectorOptions() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
            await Task.Delay(500);
            await Page.SaveScreenshotToAsync(TestContext,"Dialog Before Edit");
            await Page.FillFormAsync(new Dictionary<string, string>()
            {
                { "Category", NextCategory },
                { "Name", name },
            });
            await Page.SaveScreenshotToAsync(TestContext,"Dialog After Edit");

            await Page.ClickAsync("#addPayeeModal >> text=Save");

            // Then: The payees page has one more item than expected
            await WhenNavigatingToPage(MainPageName);
            Assert.AreEqual(TotalItemCount + 1, await Page.GetTotalItemsAsync());

            // And: Searching for the payee finds it
            await Page.SearchFor(name);
            Assert.AreEqual(1, await Page.GetTotalItemsAsync());
            await Page.SaveScreenshotToAsync(TestContext,"Found Items");
        }

        async Task GivenPayeeInDatabase()
        {
            // Given: We are starting at the payee index page

            // When: Creating a new item
            var originalitems = await Page.GetTotalItemsAsync();

            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Create New");
            await Page.FillFormAsync(new Dictionary<string, string>()
            {
                { "Category", NextCategory },
                { "Name", NextName },
            });
            await Page.ClickAsync("input:has-text(\"Create\")");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);

            // And: There is one more item
            var itemsnow = await Page.GetTotalItemsAsync();
            Assert.AreEqual(originalitems + 1, itemsnow);
        }

        /// <summary>
        /// [User can] Delete a single payee
        /// </summary>
        [TestMethod]
        public async Task DeletePayee()
        {
            // Given: We are logged in and on the payees page

            // And: There is one extra payee in the database
            await GivenPayeeInDatabase();

            // And: Searched for the new payee
            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext);

            // When: Opening the actions menu on the first item in the list
            await Page.ClickAsync("#actions-1");
            await Page.SaveScreenshotToAsync(TestContext, "Context Menu");

            // When: Clicking delete in that menu
            await Page.ClickAsync("[data-test-id=delete]");

            // Then: We land at the delete payee page
            await Page.ThenIsOnPageAsync("Delete Payee");
            await Page.SaveScreenshotToAsync(TestContext);

            // When: Clicking the Delete button to execute the delete
            await Page.ClickAsync("input:has-text(\"Delete\")");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
            await Page.SaveScreenshotToAsync(TestContext);

            // And: Total number of items is back to the standard amount
            Assert.AreEqual(TotalItemCount, await Page.GetTotalItemsAsync());
        }

        /// <summary>
        /// [User can] Quickly edit the most relevant details for a payee
        /// </summary>
        [TestMethod]
        public async Task UpdateModal()
        {
            // Given: One item created
            await GivenPayeeInDatabase();

            // And: It's the one item in search results
            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext);

            // When: Editing it
            var newcategory = NextCategory;
            var newpayee = NextName;

            // When: Opening the actions menu on the first item in the list
            await Page.ClickAsync("#actions-1");
            await Page.SaveScreenshotToAsync(TestContext, "Context Menu");

            // When: Clicking edit in that menu
            await Page.ClickAsync("[data-test-id=edit]");
            await Page.FillFormAsync(new Dictionary<string, string>()
            {
                { "Category", newcategory },
                { "Name", newpayee },
            });
            await Task.Delay(200);
            await Page.SaveScreenshotToAsync(TestContext);
            await Page.ClickAsync("text=Save");

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);

            // And: Searching for the new item...
            await Page.SearchFor(newcategory);
            await Page.SaveScreenshotToAsync(TestContext);

            // Then: It's found
            Assert.AreEqual(1, await Page.GetTotalItemsAsync());
        }

        #endregion
    }
}
