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
        public const int TotalItemCount = 40;

        [TestMethod]
        public async Task ClickPayees()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync("text=Payees");

            // Then: We land at the budget index page
            await ThenIsOnPage("Payees");

            // And: All expected items are here
            await ThenTotalItemsAreEqual(TotalItemCount);

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
            await ThenPageContainsItems(from: 26, to: TotalItemCount);
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
            await ThenTotalItemsAreEqual(TotalItemCount);
        }

        [TestMethod]
        public async Task DownloadAll()
        {
            // Given: We are logged in and on the payees page
            await ClickPayees();

            // When: Downloading items
            await Page.ClickAsync("#dropdownMenuButtonAction");

            var download1 = await Page.RunAndWaitForDownloadAsync(async () =>
            {
                await Page.ClickAsync("text=Export");
            });

            // Then: A spreadsheet containing 40 Payees was downloaded
            await ThenSpreadsheetWasDownloadedContaining<IdOnly>(source: download1, name: "Payee", count: TotalItemCount);

#if false
            // Enable if need to inspect
            var filename = $"{TestContext.FullyQualifiedTestClassName}-{TestContext.TestName}.xlsx";
            await download1.SaveAsAsync(filename);
            TestContext.AddResultFile(filename);
#endif
        }

        [TestMethod]
        public async Task UploadAndDelete()
        {
            //
            // Step 1: Upload payees
            //

            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync("text=Payees");

            // Then: We land at the payee index page
            await ThenIsOnPage("Payees");

            // NOTE: It's possible that we already have the payees we're doing to import already in the
            // database, perhaps from a failed test. So here we'll first delete them if they exist.
            await DeletePayees("big");

            // Click [aria-label="Upload"]
            await Page.ClickAsync("[aria-label=\"Upload\"]");
            // Upload Test-Generator-GenerateUploadSampleData.xlsx
            await Page.SetInputFilesAsync("[aria-label=\"Upload\"]", new[] { "SampleData/Test-Generator-GenerateUploadSampleData.xlsx" });
            // Click text=Upload
            await Page.ClickAsync("text=Upload");

            // Then: We land at the budget index page
            await ThenIsOnPage("Uploaded Payees");

            //
            // Step 2: Search for the new payees
            //

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync("text=Payees");

            // Then: We land at the budget index page
            await ThenIsOnPage("Payees");

            // Then: 43 items are found, because we just added 3
            await ThenTotalItemsAreEqual(TotalItemCount + 3);

            // When: Searching for "big" (which we just imported)
            await Page.FillAsync("data-test-id=q", "big");
            await Page.ClickAsync("data-test-id=btn-search");

            // Then: 3 items are found, because we know this about our source data
            await ThenTotalItemsAreEqual(3);

            //
            // Step 3: Delete them
            //

            await DeletePayees("big");

            // NOTE: I could use bulkdelete here, BUT this way I still get to test
            // the regular delete path
        }

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

#if false
            // Clean up any payees from previously failed tests
            await GivenLoggedIn();
            await Page.ClickAsync("text=Payees");
            await DeletePayees("XYZ");
#endif

            // Given: We are logged in and on the payees page
            await ClickPayees();

            // And: Three new items added with a distinctive name
            await AddPayee("XYZA", "X:Y:Z");
            await AddPayee("XYZB", "X:Y:Z");
            await AddPayee("XYZC", "X:Y:Z");

            // When: Bulk Deleteing Payees
            await BulkDeletePayees("XYZ");

            // Then: Completes without error
        }

        async Task AddPayee(string name, string category)
        {
            // Given: We are starting at the payee index page
            await ThenIsOnPage("Payees");
            var originalitems = Int32.Parse(await Page.TextContentAsync("data-test-id=totalitems"));

            // When: Creating a new item

            // Click #dropdownMenuButtonAction
            await Page.ClickAsync("#dropdownMenuButtonAction");
            // Click text=Create New
            await Page.ClickAsync("text=Create New");
            // Click input[name="Name"]
            await Page.FillAsync("input[name=\"Name\"]", name);
            // Fill input[name="Category"]
            await Page.FillAsync("input[name=\"Category\"]", category);
            // Click input:has-text("Create")
            await Page.ClickAsync("input:has-text(\"Create\")");
            // Assert.AreEqual("http://localhost:50419/Payees", page.Url);

            // Then: We finish at the payee index page
            await ThenIsOnPage("Payees");

            // And: There is one more item
            var itemsnow = Int32.Parse(await Page.TextContentAsync("data-test-id=totalitems"));
            Assert.IsTrue(itemsnow == originalitems + 1);

        }

        async Task DeletePayees(string q)
        {
            // Delete payees matching q until down to expected TotalItemCount

            // Given: We are at the payees page
            await ThenIsOnPage("Payees");

            // And: Clearing the search
            await Page.ClickAsync("data-test-id=btn-clear");

            // And: totalitems > expected TotalItemCount
            var totalitems = Int32.Parse( await Page.TextContentAsync("data-test-id=totalitems") );

            while (totalitems > TotalItemCount)
            {
                // When: Searching for supplied search term
                await Page.FillAsync("data-test-id=q", q);
                await Page.ClickAsync("data-test-id=btn-search");

                // When: Clicking delete on first item in list
                await Page.ClickAsync("[aria-label=\"Delete\"]");

                // Then: We land at the delete payee page
                await ThenIsOnPage("Delete Payee");

                // When: Clicking the Delete button to execute the delete
                await Page.ClickAsync("input:has-text(\"Delete\")");

                // Then: We land at the payees page
                await ThenIsOnPage("Payees");

                // And: totalitems > expected TotalItemCount
                totalitems = Int32.Parse(await Page.TextContentAsync("data-test-id=totalitems"));
            }
        }
        async Task BulkDeletePayees(string q)
        {
            // Delete payees matching q

            // Given: We are at the payees page
            await ThenIsOnPage("Payees");

            // And: Clearing the search
            await Page.ClickAsync("data-test-id=btn-clear");

            // And: Searching for {q} 
            await Page.FillAsync("data-test-id=q", q);
            await Page.ClickAsync("data-test-id=btn-search");

            // And: In bulk edit mode
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Bulk Edit");
            await ScreenShotAsync();

            // When: Clicking select on each item
            var numdelete = Int32.Parse(await Page.TextContentAsync("data-test-id=totalitems"));
            for(int i = 1; i <= numdelete; i++)
                await Page.ClickAsync($"data-test-id=line-{i} >> data-test-id=check-select");
            await ScreenShotAsync();

            // When: Clicking "Delete" on the bulk edit bar
            await Page.ClickAsync("data-test-id=btn-bulk-delete");

            // And: Clicking "OK" on the confirmation dialog
            await Page.WaitForSelectorAsync("#deleteConfirmModal");
            await ScreenShotAsync();
            await Page.ClickAsync("data-test-id=btn-modal-ok");

            // Then: Still on the Payees page
            await ThenIsOnPage("Payees");
            await ScreenShotAsync();

            // And: All expected items are here
            await ThenTotalItemsAreEqual(TotalItemCount);
        }
    }
}
