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
        const int TotalItemCount = 40;

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

            //
            // Step 4: Wish for bulk delete!!
            //
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
    }
}
