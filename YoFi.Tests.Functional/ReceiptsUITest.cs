using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Tests.Functional.Helpers;

namespace YoFi.Tests.Functional
{
    /// <summary>
    /// Feature 1189: Receipt Matching: Upload multiple receipts in one place, auto match them to transactions by file name
    /// </summary>
    [TestClass]
    public class ReceiptsUITest : FunctionalUITest
    {
        #region Helpers

        protected async Task WhenUploadingSampleReceipts(IEnumerable<string> filenames)
        {
            // Conjure up some bytes (the bytes don't really matter)
            byte[] bytes = Enumerable.Range(0, 255).Select(i => (byte)i).ToArray();

            // Make file payloads out of them
            var payloads = filenames.Select(x =>
                new FilePayload()
                {
                    Name = x,
                    MimeType = "image/png",
                    Buffer = bytes
                }
            );

            await Page.ClickAsync("[aria-label=Upload]");
            await Page.SetInputFilesAsync("[aria-label=Upload]", payloads);
            await Page.SaveScreenshotToAsync(TestContext);
            await Page.ClickAsync("data-test-id=btn-create-receipt");
        }

        #endregion

        #region Init/Cleanup

        [TestCleanup]
        public async Task Cleanup()
        {
            //
            // Delete all test items
            //

            var api = new ApiKeyTest();
            api.SetUp(TestContext);
            await api.ClearTestData("all");
        }

        #endregion

        #region User Story 1190: [User Can] Upload receipts independently of transactions

        [TestMethod]
        public async Task NavigateToReceiptsPage()
        {
            /*
            Given: On Transaction Page
            And: Clicking on Actions
            When: Clicking on Match Receipts
            Then: On Receipts Page
            */

            // Given: On Transactions Page
            await WhenNavigatingToPage("Transactions");

            // And: Clicking on Actions
            await Page.ClickAsync("#dropdownMenuButtonAction");
    
            if (TestContext.TestName == "NavigateToReceiptsPage")
                await Page.SaveScreenshotToAsync(TestContext, "Slide 07");

            // When: Clicking on Match Receipts
            await Page.ClickAsync("text=Match Receipts");

            // And: Dismissing any help text
            await DismissHelpTest();

            // Then: On Receipts Page
            await Page.ThenIsOnPageAsync("Receipts");

        }

        [TestMethod]
        public async Task UploadReceipts()
        {
            /*
            Given: On receipts page
            When: Uploading many files with differing name compositions
            Then: Results displayed match number and composition of expected receipts
            */

            // Given: On receipts page
            await NavigateToReceiptsPage();
            await Page.SaveScreenshotToAsync(TestContext, "Slide 08");

            // When: Uploading many files with differing name compositions
            // Here are the filenames we want. Need __TEST__ on each so they can be cleaned up
            var filenames = new[]
            {
                // Matches exactly one
                $"Olive Garden $130.85 {testmarker}.png",
                // Matches exactly one
                $"Waste Management 12-27 {testmarker}.png",
                // Matches many
                $"Uptown Espresso ({testmarker}).png",
                // Matches none
                $"Create Me $12.34 12-21 {testmarker}.png"
            };
            await WhenUploadingSampleReceipts(filenames);
            await Page.SaveScreenshotToAsync(TestContext, "Slide 10");

            // Then: Results displayed match number and composition of expected receipts
            var table = await ResultsTable.ExtractResultsFrom(Page);
            Assert.IsNotNull(table);

            // Correct count
            Assert.AreEqual(4, table.Rows.Count);

            // Memo is "__TEST__" on all
            Assert.IsTrue(table.Rows.All(x => x["Memo"] == testmarker));

            // Spot check values (Note that these are all subject to culture variablility)
            Assert.AreEqual("Olive Garden",table.Rows[0]["Name"]);
            Assert.AreEqual("Create Me", table.Rows[3]["Name"]);

            Assert.AreEqual("12/31/2022", table.Rows[2]["Date"]);
            Assert.AreEqual("12/21/2022", table.Rows[3]["Date"]);

            Assert.AreEqual("130.85", table.Rows[0]["Amount"]);
            Assert.AreEqual("12.34", table.Rows[3]["Amount"]);

            // Check filenames
            for(int i = 0; i < filenames.Count(); i++)
            {
                Assert.AreEqual(filenames[i], table.Rows[i]["Filename"]);
            }
        }

        [TestMethod]
        public async Task UploadMatching()
        {
            /*
            Given: A set of transactions
            And: On receipts page
            When: Uploading many files with differing name compositions, some of which will exactly match the transactions
            Then: Matching receipts are found and shown, as expected, with matches shown as expected
            */

            // Given: A set of transactions
            // (Will use the transactions already in place from sample data. Note that these receipts are precisely
            // tuned to the exact contents of the expected sample data)

            // And: On receipts page
            await NavigateToReceiptsPage();

            // When: Uploading many files with differing name compositions, some of which will exactly match the transactions
            var receipts = new (string name,int matches)[]
            {
                // Matches exactly one at 200 (name and amount), but will also match 3 others at 100 (name only)
                ($"Olive Garden $130.85 {testmarker}.png",4),
                // Matches exactly one
                ($"Waste Management 12-27 {testmarker}.png",1),
                // Matches many
                ($"Uptown Espresso ({testmarker}).png",5),
                // Matches none
                ($"Create Me $12.34 12-21 {testmarker}.png",0)
            };
            await WhenUploadingSampleReceipts(receipts.Select(x=>x.name));
            await Page.SaveScreenshotToAsync(TestContext, "Uploaded");

            // Then: Matching receipts are found and shown, as expected, with matches shown as expected
            var table = await ResultsTable.ExtractResultsFrom(Page);
            Assert.IsNotNull(table);

            for(int i = 0; i < table.Rows.Count; i++)
            {
                Assert.AreEqual(receipts[i].matches, int.Parse( table.Rows[i]["Matches"]), $"For {table.Rows[i]["Filename"]}" );
            }
        }

        #endregion

        #region User Story 1191: [User Can] Accept automatically matched receipts to transactions 

        [TestMethod]
        public async Task AcceptAll()
        {
            /*
            Given: A set of transactions
            And: On receipts page
            And: Many receipts with differing name compositions, some of which will exactly match the transactions
            When: Accept All
            Then: The receipts which have a single match are all assigned to that transaction
            And: The receipts are removed from the display
            */

            // Given: A set of transactions
            await WhenNavigatingToPage("Transactions");

            var names = new string[]
            {
                NextName, NextName, NextName, NextName
            };

            var date = new DateTime(2022, 12, 31);
            var transactions = new List<(string name, decimal amount, DateTime date)>()
            {
                (names[0],100m,date), // 12-31
                (names[0],200m,date - TimeSpan.FromDays(1)), // 12-30
                (names[0],300m,date - TimeSpan.FromDays(2)), // 12-29
                (names[1],100m,date - TimeSpan.FromDays(3)), // 12-28
                (names[2],100m,date - TimeSpan.FromDays(4)),
                (names[2],200m,date - TimeSpan.FromDays(5)),
                (names[2],300m,date - TimeSpan.FromDays(6)),
                (names[2],400m,date - TimeSpan.FromDays(7)),
            };

            foreach (var tx in transactions)
            {
                await WhenCreatingTransaction(Page, new Dictionary<string, string>()
                {
                    { "Payee", tx.name },
                    { "Timestamp", tx.date.ToString("yyyy-MM-dd") },
                    { "Amount", tx.amount.ToString() },
                });
            }
            await Page.SaveScreenshotToAsync(TestContext, "CreatedTx");

            // And: On receipts page
            await NavigateToReceiptsPage();

            // And: Uploading many files with differing name compositions, some of which will exactly match the transactions
            var receipts = new (string name, int matches)[]
            {
                // Matches exactly one at 200 (name and amount), but will also match 3 others at 100 (name only)
                ($"{names[0]} $200 {testmarker}.png",3),
                // Matches exactly one
                ($"{names[1]} 12-28 {testmarker} 1.png",1),
                // Matches many
                ($"{names[2]} ({testmarker}).png",4),
                // Matches none
                ($"{names[3]} $12.34 12-28 {testmarker}.png",0)
            };

            await WhenUploadingSampleReceipts(receipts.Select(x => x.name));
            await Page.SaveScreenshotToAsync(TestContext, "Slide 12");

            // When: Accept All
            await Page.ClickAsync("data-test-id=accept-all");
            await Page.SaveScreenshotToAsync(TestContext, "Slide 13");

            // Then: The receipts are removed from the display
            // (Receipt for names[1] is gone)
            var table = await ResultsTable.ExtractResultsFrom(Page);
            Assert.AreEqual(3, table.Rows.Count);
            Assert.IsFalse(table.Rows.Any(x=>x["Name"] == names[1]));

            // And: The receipts which have a single match are all assigned to that transaction

            // Go to transactions page
            await WhenNavigatingToPage("Transactions");

            // Search for test marker
            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext,"FindMarked");

            // Extract those results
            var txtable = await ResultsTable.ExtractResultsFrom(Page);

            // There is only one with a receipt
            var found = txtable.Rows.Where(x => x["Receipt"] == "True");
            Assert.AreEqual(1,found.Count());

            // The one with a receipt has name=names[1]
            Assert.AreEqual(names[1],found.First()["Payee"]);
        }

        #endregion

        #region User Story 1310: [User Can] Manually choose among possible matches for uploaded receipts when no direct match is found

        public async Task ReviewMatches()
        {
            /*
            Given: Many receipts with differing name compositions, some of which will exactly match the transactions
            When: Tapping “review”
            Then: The receipts which match the selected transaction are shown
            */
        }

        public async Task AcceptChosenMatch()
        {
            /*
            Given: Reviewing multiple matches for a receipt
            When: Tapping “Match”
            Then: The receipt is added to the selected transaction
            And: The receipt is removed from the display
            */
        }

        public async Task MemoInTransaction()
        {
            /*
            Given: A receipt which matches an existing transaction and contains a memo
            When: Accept All
            And: Navigating to Transactions Page
            And: Searching for the affected Transaction
            Then: The memo of the transaction matches the memo from the receipt
            */
        }

        #endregion

        #region User Story 1192: [User Can] Delete uploaded receipts before matching them to a transaction

        [TestMethod]
        public async Task Delete()
        {
            /*
            Given: On Receipts page, with an uploaded unmatched receipt
            When: Deleting it
            Then: No receipts shown on the page
            */

            // Given: On receipts page
            await NavigateToReceiptsPage();

            // And: With an uploaded unmatched receipt
            // Here are the filenames we want. Need __TEST__ on each so they can be cleaned up
            var filenames = new[]
            {
                // Matches none
                $"Create Me $12.34 12-21 {testmarker}.png"
            };
            await WhenUploadingSampleReceipts(filenames);
            await Page.SaveScreenshotToAsync(TestContext, "Uploaded");

            // When: Deleting it
            var deletebutton = await Page.QuerySelectorAsync("table[data-test-id=results] tbody input[value=Delete]");
            Assert.IsNotNull(deletebutton);
            await deletebutton.ClickAsync();
            await Page.SaveScreenshotToAsync(TestContext, "Deleted");

            // Then: No receipts shown on the page
            var table = await ResultsTable.ExtractResultsFrom(Page);
            Assert.IsNotNull(table);
            Assert.IsFalse(table.Rows.Any());
        }

        #endregion
    }
}
