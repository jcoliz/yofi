using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    /// <summary>
    /// Feature 1189: Receipt Matching: Upload multiple receipts in one place, auto match them to transactions by file name
    /// </summary>
    [TestClass]
    public class ReceiptsUITest : FunctionalUITest
    {
        #region User Story 1190: [User Can] Upload receipts independently of transactions

        [TestCleanup]
        public async Task Cleanup()
        {
            //
            // Delete all test items
            //

            var api = new ApiKeyTest();
            api.SetUp(TestContext);
            await api.ClearTestData("receipt");
        }

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

            // And: Dismissing any help text
            await DismissHelpTest();
            await Page.SaveScreenshotToAsync(TestContext, "Slide 08");

            //
            // When: Uploading many files with differing name compositions
            //
            // TODO: Make this stuff a helper class

            // Conjure up some bytes (the bytes don't really matter)
            byte[] bytes = Enumerable.Range(0,255).Select(i => (byte)i).ToArray();

            // Here are the filenames we want. Need __TEST__ on each so they can be cleaned up
            var filenames = new[]
            {
                // Matches exactly one
                "Olive Garden $130.85 __TEST__.png",

                // Matches exactly one
                "Waste Management 12-27 __TEST__.png",

                // Matches many
                "Uptown Espresso (__TEST__).png",

                // Matches none
                "Create Me $12.34 12-21 __TEST__.png"
            };

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
            await Page.SaveScreenshotToAsync(TestContext, "Slide 10");

            // Then: Results displayed match number and composition of expected receipts
            var table = await ResultTable.ExtractResultsFrom(Page);
            Assert.IsNotNull(table);

            // Correct count
            Assert.AreEqual(4, table.Rows.Count);

            // Memo is "__TEST__" on all
            Assert.IsTrue(table.Rows.All(x => x["Memo"] == "__TEST__"));

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

        protected class ResultTable
        {
            public List<string> Columns { get; } = new List<string>();
            public List<Dictionary<string, string>> Rows { get; } = new List<Dictionary<string, string>>();

            public static async Task<ResultTable> ExtractResultsFrom(IPage page)
            {
                var table = new ResultTable();

                var table_el = await page.QuerySelectorAsync("table[data-test-id=results]");
                if (table_el is null)
                    return null;

                var headers_el = await table_el.QuerySelectorAllAsync("thead th");
                foreach (var el in headers_el)
                {
                    var text = await el.TextContentAsync();
                    var header = text.Trim();
                    table.Columns.Add(header);
                }

                var rows_el = await table_el.QuerySelectorAllAsync("tbody tr");
                foreach (var row_el in rows_el)
                {
                    var row = new Dictionary<string, string>();
                    var col_enum = table.Columns.GetEnumerator();
                    col_enum.MoveNext();

                    var cells_el = await row_el.QuerySelectorAllAsync("td");
                    foreach (var cell_el in cells_el)
                    {
                        var text = await cell_el.TextContentAsync();
                        var cell = text.Trim();

                        var col = col_enum.Current;
                        if (col != null)
                        {
                            row[col] = cell;
                            col_enum.MoveNext();
                        }
                        else
                        {
                            var testid = await cell_el.GetAttributeAsync("data-test-id");
                            var testvalue = await cell_el.GetAttributeAsync("data-test-value");
                            if (testid != null && testvalue != null)
                                row[testid] = testvalue;
                        }
                    }

                    table.Rows.Add(row);
                }

                return table;
            }
        }

        public async Task UploadMatching()
        {
            /*
            Given: A set of transactions
            And: On receipts page
            When: Uploading many files with differing name compositions, some of which will exactly match the transactions
            Then: Matching receipts are found and shown, as expected, with matches shown as expected
            */
        }

        #endregion

        #region User Story 1191: [User Can] Accept automatically matched receipts to transactions 

        public async Task AcceptAll()
        {
            /*
            Given: Many receipts with differing name compositions, some of which will exactly match the transactions
            When: Accept All
            Then: The receipts which have a single match are all assigned to that transaction
            And: The receipts are removed from the display
            */
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

        public async Task Delete()
        {
            /*
            Given: On Receipts page, with an uploaded unmatched receipt
            When: Deleting it
            Then: No receipts shown on the page
            */
        }

        #endregion
    }
}
