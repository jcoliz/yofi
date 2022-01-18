using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    [TestClass]
    public class ImporterUITest : FunctionalUITest
    {
        private const string MainPageLink = "Import";
        private const string MainPageName = "Importer";

        [TestInitialize]
        public new async Task SetUp()
        {
            base.SetUp();

            // When: Navigating to the main page for this section
            await WhenNavigatingToPage(MainPageLink);

            // Then: We are on the main page for this section
            await Page.ThenIsOnPageAsync(MainPageName);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            //
            // Delete all test items
            //

            // Given: Asked server to clear this test data
            var api = new ApiKeyTest();
            api.SetUp(TestContext);
            await api.ClearTestData("all");

            // When: Navigating to Admin Page
            await WhenNavigatingToPage("Admin");

            // Then: Total items are back to normal
            Assert.AreEqual(TransactionsUITest.TotalItemCount, await Page.GetNumberAsync("data-test-id=num-tx"));
            Assert.AreEqual(BudgetUITest.TotalItemCount, await Page.GetNumberAsync("data-test-id=num-budget"));
            Assert.AreEqual(PayeeUITest.TotalItemCount, await Page.GetNumberAsync("data-test-id=num-payee"));
        }

        /// <summary>
        /// [User Can] Import transactions from an OFX file 
        ///     - [User Can] Preview transactions before commiting to import them
        /// </summary>
        [TestMethod]
        public async Task ImportOfxPreview()
        {
            // Given: Test Payees in the database
            await GivenPayeeInDatabase(name: "AA__TEST__1", category: "AA__TEST__:A");
            await GivenPayeeInDatabase(name: "AA__TEST__2", category: "AA__TEST__:B");
            await GivenPayeeInDatabase(name: "AA__TEST__3", category: "AA__TEST__:C");

            // And: Starting On import page
            await WhenNavigatingToPage(MainPageLink);
            await Page.ThenIsOnPageAsync(MainPageName);
            await Page.SaveScreenshotToAsync(TestContext, "OnPageImporter");

            // When: Importing an OFX file, where the transactions match the existing payees
            await Page.ClickAsync("[aria-label=\"Upload\"]");
            await Page.SetInputFilesAsync("[aria-label=\"Upload\"]", new[] { "SampleData\\FullSampleDAta-Month01.ofx" });
            await Page.SaveScreenshotToAsync(TestContext, "SetInputFiles");
            await Page.ClickAsync("text=Upload");

            await Page.ThenIsOnPageAsync("Importer");
            await Page.SaveScreenshotToAsync(TestContext, "Uploaded");

            // Then: Expected number of items present in importer
            Assert.AreEqual(6, await Page.GetTotalItemsAsync());

            // And: All categories are set correctly
        }

        /// <summary>
        /// [User Can] Import transactions from an OFX file 
        /// </summary>
        [TestMethod]
        public async Task ImportOfx()
        {
            // Given: Already visited the transactions page once
            // (This will clear the help text)
            await WhenNavigatingToPage("Transactions");

            // And: Transactions already imported on the import page
            await ImportOfxPreview();

            // When: Accepting the import
            await Page.ClickAsync("button:has-text(\"Import\")");
            await Page.ThenIsOnPageAsync("Transactions");
            await Page.SaveScreenshotToAsync(TestContext, "BackOnTx");

            // And: Searching for items by categories
            // Then: The number of found items matches the input data
            await Page.SearchFor($"c=AA__TEST__:A");
            Assert.AreEqual(3, await Page.GetTotalItemsAsync());

            await Page.SearchFor($"c=AA__TEST__:B");
            Assert.AreEqual(2, await Page.GetTotalItemsAsync());

            await Page.SearchFor($"c=AA__TEST__:C");
            Assert.AreEqual(1, await Page.GetTotalItemsAsync());
        }

        /// <summary>
        /// [User Can] Preview transactions before commiting to import them,
        /// and choose to discard them all in one operation.
        /// </summary>
        public void ImportOfxDelete()
        {
            // Given: Imported an OFX file

            // When: Deleting them

            // Then: No items present in importer
        }

        /// <summary>
        /// User Story 1178: [User Can] Import spreadsheets with all data types in a single spreadsheet from the main Import page
        /// </summary>
        [TestMethod]
        public async Task ImportAll()
        {
            // Given: All items start with the normal amounts
            await WhenNavigatingToPage("Admin");
            Assert.AreEqual(TransactionsUITest.TotalItemCount, await Page.GetNumberAsync("data-test-id=num-tx"));
            Assert.AreEqual(BudgetUITest.TotalItemCount, await Page.GetNumberAsync("data-test-id=num-budget"));
            Assert.AreEqual(PayeeUITest.TotalItemCount, await Page.GetNumberAsync("data-test-id=num-payee"));

            // When: Returning to the upload page
            await WhenNavigatingToPage(MainPageLink);
            await Page.ThenIsOnPageAsync(MainPageName);

            // And: Uploading a sample file with all kinds of content in it
            await Page.ClickAsync("[aria-label=\"Upload\"]");
            await Page.SetInputFilesAsync("[aria-label=\"Upload\"]", new[] { "SampleData/Test-Generator-GenerateUploadSampleData.xlsx" });
            await Page.ClickAsync("text=Upload");
            await Page.SaveScreenshotToAsync(TestContext, "Uploaded");

            // And: Accepting the transactions upload
            await Page.ClickAsync("button:has-text(\"Import\")");

            // Then: The counts of items have increased by the expected amount
            var expectedtx = TransactionsUITest.TotalItemCount + 25;
            var expectedbudget = BudgetUITest.TotalItemCount + 4;
            var expectedpayee = PayeeUITest.TotalItemCount + 3;
            await WhenNavigatingToPage("Admin");
            Assert.AreEqual(expectedtx, await Page.GetNumberAsync("data-test-id=num-tx"));
            Assert.AreEqual(expectedbudget, await Page.GetNumberAsync("data-test-id=num-budget"));
            Assert.AreEqual(expectedpayee, await Page.GetNumberAsync("data-test-id=num-payee"));
        }

    }
}
