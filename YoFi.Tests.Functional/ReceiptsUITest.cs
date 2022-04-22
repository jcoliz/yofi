using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Tests.Functional.Helpers;

namespace YoFi.AspNet.Tests.Functional
{
    /// <summary>
    /// Feature 1189: Receipt Matching: Upload multiple receipts in one place, auto match them to transactions by file name
    /// </summary>
    [TestClass]
    public class ReceiptsUITest : FunctionalUITest
    {
        #region Helpers

        protected IEnumerable<FilePayload> MakeImageFilePayloadsFromNames(IEnumerable<string> filenames)
        {
            // Conjure up some bytes (the bytes don't really matter)
            byte[] bytes = Enumerable.Range(0, 255).Select(i => (byte)i).ToArray();

            // Make file payloads out of them
            return filenames
                .Select(x =>
                    new FilePayload()
                    {
                        Name = x,
                        MimeType = "image/png",
                        Buffer = bytes
                    }
                )
                .ToList();
        }

        protected async Task WhenUploadingSampleReceipts(IEnumerable<string> filenames)
        {
            var payloads = MakeImageFilePayloadsFromNames(filenames);

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
            var today = DateTime.Now.Date;
            var oldday = today - TimeSpan.FromDays(10);
            var recentday = today - TimeSpan.FromDays(3);
            var receipts = new (string name, int matches, int order)[]
            {
                // Matches none
                ($"Create Me $12.34 {oldday:M-dd} {testmarker}.png",0,3),
                // Matches exactly one at 200 (name and amount), but will also match 3 others at 100 (name only)
                ($"Olive Garden $130.85 {testmarker}.png",4,0),
                // Matches exactly one
                // TODO: Today minus 3 days
                ($"Waste Management {recentday:M-dd} {testmarker}.png",1,2),
                // Matches many
                ($"Uptown Espresso ({testmarker}).png",5,1),
            };
            await WhenUploadingSampleReceipts(receipts.Select(x => x.name));
            await Page.SaveScreenshotToAsync(TestContext, "Slide 10");

            // Then: Results displayed match number and composition of expected receipts
            var table = await ResultsTable.ExtractResultsFrom(Page);
            Assert.IsNotNull(table);

            // Correct count
            Assert.AreEqual(4, table.Rows.Count);

            // Memo is "__TEST__" on all
            Assert.IsTrue(table.Rows.All(x => x["Memo"].Contains(testmarker)),"All memos contain testmarker");

            // Spot check values (Note that these are all subject to culture variablility)
            Assert.AreEqual("Olive Garden",table.Rows[0]["Name"]);
            Assert.AreEqual("Waste Management", table.Rows[2]["Name"]);

            // NOTE: I am optimistic that this will work if today is first week of Jan.
            // TODO: Still need to TEST that, though
            Assert.AreEqual(today, DateTime.Parse(table.Rows[1]["Date"]));
            Assert.AreEqual(oldday, DateTime.Parse(table.Rows[3]["Date"]));

            Assert.AreEqual(130.85m, decimal.Parse(table.Rows[0]["Amount"], NumberStyles.Currency));
            Assert.AreEqual(12.34m, decimal.Parse(table.Rows[3]["Amount"], NumberStyles.Currency));

            // Check filenames
            var orderedreceipts = receipts.OrderBy(x => x.order).ToList();
            for (int i = 0; i < orderedreceipts.Count; i++)
                Assert.IsTrue(table.Rows[i]["Filename"].Contains(orderedreceipts[i].name), orderedreceipts[i].name);
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
            await WhenNavigatingToPage("Transactions");

            var today = DateTime.Now.Date;
            var name1 = NextName;
            var name2 = NextName;
            var name3 = NextName;
            var specialamount = 123.45m;
            var specialday = today - TimeSpan.FromDays(20);
            (string name, DateTime date, decimal amount)[] txs = new[] 
            { 
                (name1, today, 100m),
                (name1, specialday, 200m),
                (name2, today, specialamount),
                (name2, today, 2 * specialamount),
                (name2, today, 3 * specialamount),
                (name3, today - TimeSpan.FromDays(1), 100.45m),
                (name3, today - TimeSpan.FromDays(2), 200.45m),
                (name3, today - TimeSpan.FromDays(3), 300.45m),
            };
            foreach(var tx in txs)
                await WhenCreatingTransaction(Page, new Dictionary<string, string>()
                {
                    { "Payee", tx.name },
                    { "Timestamp", tx.date.ToString("yyyy-MM-dd") },
                    { "Amount", tx.amount.ToString() },
                });

            // NOTE: Cannot use transactions already in place from sample data. BECAUSE this test could
            // be run at any time, and receipts are VERY date sensitive.

            // And: On receipts page
            await NavigateToReceiptsPage();

            // When: Uploading many files with differing name compositions, some of which will exactly match the transactions
            var olddate = today - TimeSpan.FromDays(10);
            var receipts = new (string name,int matches,int order)[]
            {
                // Matches none
                ($"Create Me $1234.56 {olddate:M-dd} {testmarker}.png",0,2),
                // Matches exactly one at 200 (name and amount), but will also match 2 others at 100 (name only)
                ($"{name2} {specialamount:C2} {testmarker}.png",3,0),
                // Matches exactly one
                ($"{name1} {specialday:M-dd} {testmarker}.png",1,3),
                // Matches many
                ($"{name3} ({testmarker}).png",3,1),
            };
            await WhenUploadingSampleReceipts(receipts.Select(x=>x.name));
            await Page.SaveScreenshotToAsync(TestContext, "Uploaded");

            // Then: Matching receipts are found and shown, as expected, with matches shown as expected
            var table = await ResultsTable.ExtractResultsFrom(Page);
            Assert.IsNotNull(table);

            var orderedreceipts = receipts.OrderBy(x=>x.order).ToList();
            for(int i = 0; i < table.Rows.Count; i++)
            {
                Assert.AreEqual(orderedreceipts[i].matches, int.Parse( table.Rows[i]["Matches"]), $"For {table.Rows[i]["Filename"]}" );
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

            var date = DateTime.Now;
            var recentdate = date - TimeSpan.FromDays(3);
            var transactions = new List<(string name, decimal amount, DateTime date)>()
            {
                (names[0],100m,date), // 12-31
                (names[0],200m,date - TimeSpan.FromDays(1)), // 12-30
                (names[0],300m,date - TimeSpan.FromDays(2)), // 12-29
                (names[1],100m,recentdate), // 12-28
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
                ($"{names[1]} {recentdate:M-dd} {testmarker} 1.png",1),
                // Matches many
                ($"{names[2]} ({testmarker}).png",4),
                // Matches none
                ($"{names[3]} $12.34 {recentdate:M-dd} {testmarker}.png",0)
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

        public async Task<decimal> GivenReceiptWithMultipleMatchingTransactions(string name, int numtx)
        {
            // Given: Several Transactions
            await WhenNavigatingToPage("Transactions");
            var date = DateTime.Now;
            var amount = 1234.56m;
            var matchamount = amount + 100m;
            for (int i = 0; i < numtx; i++)
            {
                await WhenCreatingTransaction(Page, new Dictionary<string, string>()
                {
                    { "Payee", name },
                    { "Timestamp", (date-TimeSpan.FromDays(i)).ToString("yyyy-MM-dd") },
                    { "Amount", (amount + i * 100m).ToString() },
                });
            }
            await Page.SaveScreenshotToAsync(TestContext, "Created Tx");

            // And: A receipt which will match the transactions
            await NavigateToReceiptsPage();
            var filenames = new[]
            {
                // Matches all tx
                $"{name} ${matchamount} {testmarker}.png"
            };
            await WhenUploadingSampleReceipts(filenames);
            await Page.SaveScreenshotToAsync(TestContext, "Slide 15");

            return matchamount;
        }

        [TestMethod]
        public async Task ReviewMatches()
        {
            /*
            Given: Several transactions
            And: A receipt which will match the transactions
            When: Tapping “review”
            Then: The receipts which match the selected transaction are shown
            */

            // Given: Several Transactions
            // And: A receipt which will match the transactions
            var name = NextName;
            var numtx = 3;
            var matchamount = await GivenReceiptWithMultipleMatchingTransactions(name,numtx);

            // When: Clicking "Review" from the line actions dropdown
            await Page.ClickAsync("#actions-1");
            await Page.SaveScreenshotToAsync(TestContext, "Context Menu");
            await Page.ClickAsync("button[data-test-id=review]");
            var detailsModal = Page.Locator("div#detailsModal");
            await detailsModal.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });
            await Task.Delay(200);
            await Page.SaveScreenshotToAsync(TestContext, "Slide 16");

            // Then: The receipts which match the selected transaction are shown
            var txtable = await ResultsTable.ExtractResultsFrom(detailsModal);
            Assert.AreEqual(numtx,txtable.Rows.Count);
            Assert.IsTrue(txtable.Rows.All(x=>x["Payee"] == name));

            // And: Best match is listed first
            // (That's the one with the exact matching amount)
            var actual_amount = decimal.Parse(txtable.Rows.First()["Amount"], NumberStyles.Currency);
            Assert.AreEqual(matchamount,actual_amount);
        }

        [TestMethod]
        public async Task AcceptChosenMatch()
        {
            /*
            Given: Reviewing multiple matches for a receipt
            When: Tapping “Match”
            Then: The receipt is added to the selected transaction
            And: The receipt is removed from the display
            */

            // Given: Several Transactions
            // And: A receipt which will match the transactions
            var name = NextName;
            var numtx = 3;
            var matchamount = await GivenReceiptWithMultipleMatchingTransactions(name, numtx);

            // And: Having clicked "Review"
            await Page.ClickAsync("#actions-1");
            await Page.ClickAsync("button[data-test-id=review]");
            await Page.Locator("div#detailsModal").WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible } );
            await Task.Delay(500);
            await Page.SaveScreenshotToAsync(TestContext, "Slide 16");

            // When: Tapping “Match”
            await Page.ClickAsync("div#detailsModal >> tr[data-test-id=line-1] >> input[type=\"submit\"]");
            await Task.Delay(500);
            await Page.SaveScreenshotToAsync(TestContext, "Slide 17");

            // Then: No receipts shown on the page
            var table = await ResultsTable.ExtractResultsFrom(Page);
            Assert.IsNull(table);

            // And: Navigating to Transactions Page
            await WhenNavigatingToPage("Transactions");

            // And: Searching for the affected Transaction
            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext, "Find Marked");

            // Then: The only transaction with a receipt is the one with the correct amount
            var txtable = await ResultsTable.ExtractResultsFrom(Page);
            var found = txtable.Rows.Where(x => x["Receipt"] == "True");
            Assert.AreEqual(1, found.Count());
            var actual_amount = decimal.Parse(found.First()["Amount"], NumberStyles.Currency);
            Assert.AreEqual(matchamount, actual_amount);
        }

        [TestMethod]
        public async Task MemoInTransaction()
        {
            /*
            Given: An existing transaction 
            And: A receipt which matches that existing transaction and contains a memo
            When: Accept All
            And: Navigating to Transactions Page
            And: Searching for the affected Transaction
            Then: The memo of the transaction matches the memo from the receipt
            */

            // Given: An existing transaction 

            await WhenNavigatingToPage("Transactions");
            var name = "AA__TEST__ MemoInTransaction 1";
            var date = DateTime.Now.Date;
            var amount = 1234.56m;
            await WhenCreatingTransaction(Page, new Dictionary<string, string>()
            {
                { "Payee", name },
                { "Timestamp", date.ToString("yyyy-MM-dd") },
                { "Amount", amount.ToString() },
            });
            await Page.SaveScreenshotToAsync(TestContext, "Created Tx");

            // And: A receipt which matches that existing transaction and contains a unique memo
            await NavigateToReceiptsPage();
            var filenames = new[]
            {
                // Matches tx
                $"{name} ${amount} {date:M-dd} {name}.png"
            };
            await WhenUploadingSampleReceipts(filenames);
            await Page.SaveScreenshotToAsync(TestContext, "Uploaded");

            // When: Accept All
            await Page.ClickAsync("data-test-id=accept-all");
            await Page.SaveScreenshotToAsync(TestContext, "Accept All");

            // And: Navigating to Transactions Page
            await WhenNavigatingToPage("Transactions");

            // And: Searching for the affected Transaction
            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext, "Find Marked");

            // Then: The memo of the transaction matches the unique memo from the receipt
            var txtable = await ResultsTable.ExtractResultsFrom(Page);
            Assert.AreEqual(1, txtable.Rows.Count);
            Assert.AreEqual(name, txtable.Rows[0]["Memo"]);
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
            await Page.ClickAsync("#actions-1");
            await Page.SaveScreenshotToAsync(TestContext, "Context Menu");
            await Page.ClickAsync("button[value=Delete]");
            await Page.SaveScreenshotToAsync(TestContext, "Deleted");

            // Then: No receipts shown on the page
            var table = await ResultsTable.ExtractResultsFrom(Page);
            Assert.IsNull(table);
        }

        #endregion

        #region User Story 1311: [User Can] Create a new transaction from an uploaded receipt

        [TestMethod]
        public async Task CreateDetails()
        {
            /*
            Given: A receipt which does not match any transaction
            When: Clicking "Create"
            Then: On the transaction create page with correct details
             */

            // Given: On receipts page
            await NavigateToReceiptsPage();

            // And: With an uploaded unmatched receipt
            // Here are the filenames we want. Need __TEST__ on each so they can be cleaned up
            var payee = "A Whole New Thing";
            var amount = 12.34m;
            var filenames = new[]
            {
                // Matches none
                $"{payee} ${amount} 12-21 {testmarker}.png"
            };
            await WhenUploadingSampleReceipts(filenames);
            await Page.SaveScreenshotToAsync(TestContext, "Uploaded");

            // When: Clicking "Create" in the line actions menu
            await Page.ClickAsync("#actions-1");
            await Page.ClickAsync("text=\"Create\"");
            await Page.SaveScreenshotToAsync(TestContext, "Create Page");

            // Then: On create page
            await Page.ThenIsOnPageAsync("Create Transaction");

            // And: Details filled in correctly
            var actual_payee = await Page.Locator("input[name=\"Payee\"]").GetAttributeAsync("value");
            Assert.AreEqual(payee, actual_payee);

            var actual_amount_str = await Page.Locator("input[name=\"Amount\"]").GetAttributeAsync("value");
            var actual_amount = decimal.Parse(actual_amount_str);
            Assert.AreEqual(amount, actual_amount);

            var actual_memo = await Page.Locator("input[name=\"Memo\"]").GetAttributeAsync("value");
            Assert.AreEqual(testmarker, actual_memo);

            var actual_filename = await Page.Locator("data-test-id=receipt-url").TextContentAsync();
            Assert.IsTrue(actual_filename.Contains(filenames.Single()));
        }

        [TestMethod]
        public async Task CreateSave()
        {
            /*
            Given: A receipt which does not match any transaction
            And: On the transaction create page with correct details
            When: Clicking "Save"
            Then: Transaction is created with attached receipt
             */

            // Given: On receipts page
            await NavigateToReceiptsPage();

            // And: With an uploaded unmatched receipt
            // Here are the filenames we want. Need __TEST__ on each so they can be cleaned up
            var payee = "A Whole New Thing";
            var amount = 12.34m;
            var date = DateTime.Now.Date - TimeSpan.FromDays(10);
            var filenames = new[]
            {
                // Matches none
                $"{payee} ${amount} {date:M-dd} {testmarker}.png"
            };
            await WhenUploadingSampleReceipts(filenames);
            await Page.SaveScreenshotToAsync(TestContext, "Uploaded");

            // And: On the transaction create page with correct details
            await Page.ClickAsync("#actions-1");
            await Page.ClickAsync("text=\"Create\"");

            // When: Clicking "Create"
            await Page.SaveScreenshotToAsync(TestContext, "Creating");
            await Page.ClickAsync("input:has-text(\"Create\")");
            await Page.SaveScreenshotToAsync(TestContext, "Clicked");

            // Then: Matching transaction is created 
            await Page.ThenIsOnPageAsync("Transactions");
            await Page.SearchFor(testmarker);
            await Page.SaveScreenshotToAsync(TestContext, "Created");

            var table = await ResultsTable.ExtractResultsFrom(Page);
            Assert.IsNotNull(table);

            // Correct count
            Assert.AreEqual(1, table.Rows.Count);

            // And: Transaction matches
            Assert.IsTrue(table.Rows.All(x => x["Memo"] == testmarker));
            Assert.AreEqual(payee, table.Rows[0]["Payee"]);
            Assert.AreEqual(date, DateTime.Parse(table.Rows[0]["Date"]));
            Assert.AreEqual(amount, decimal.Parse(table.Rows[0]["Amount"], NumberStyles.Currency));

            // And: It has a receipt
            Assert.AreEqual("True", table.Rows[0]["Receipt"]);
        }

        #endregion

        #region User Story 1313: [User Can] Attach receipt to existing transaction from repository, manually choosing among possible matches for uploaded receipts when no direct match is found

        class Transaction
        {
            public string Payee { get; set; }
            public DateTime Timestamp { get; set; }
            public decimal Amount { get; set; }       

            public Dictionary<string, string> AsDictionary() => new Dictionary<string, string>()
            {
                { "Payee", Payee },
                { "Timestamp", Timestamp.ToString("yyyy-MM-dd") },
                { "Amount", Amount.ToString() },
            };
        };

        private async Task<Transaction> GivenSingleTransaction()
        {
            // Given: A single transaction in the system
            await WhenNavigatingToPage("Transactions");
            var tx = new Transaction() { Payee = NextName, Timestamp = DateTime.Now.Date, Amount = 100m };
            await WhenCreatingTransaction(Page, tx.AsDictionary());
            await Page.SaveScreenshotToAsync(TestContext, "Tx Created-Slide 20");

            return tx;
        }

        private async Task<string[]> GivenMatchingReceipts(Transaction tx, int count)
        {
            // Given: Two receipts matching the transaction
            await NavigateToReceiptsPage();            
            var filenames = Enumerable.Range(0,count).Select(x => $"{tx.Payee} ${tx.Amount} {tx.Timestamp - TimeSpan.FromDays(x):M-dd} {testmarker} 01.png").ToArray();
            await WhenUploadingSampleReceipts(filenames);
            await Page.SaveScreenshotToAsync(TestContext, "Receipts Created");

            return filenames;
        }

        private async Task<IPage> NavigatingToEditPage()
        {
            // When: Navigating to the edit page for the first transaction with testmarker
            await WhenNavigatingToPage("Transactions");
            await Page.SearchFor(testmarker);
            await Page.ClickAsync("#actions-1");
            await Page.ClickAsync("[data-test-id=edit]");
            var NextPage = await Page.RunAndWaitForPopupAsync(async () =>
            {
                await Page.WaitForSelectorAsync("input[name=\"Category\"]");
                await Page.SaveScreenshotToAsync(TestContext);
                await Page.ClickAsync("text=More");
            });
            await NextPage.SaveScreenshotToAsync(TestContext, "Edit Transaction-Slide 22");

            return NextPage;
        }

        [TestMethod]
        public async Task ReceiptsOffered()
        {
            /*
            [Scenario] Receipts offered on edit tx page
            Given: A single transaction and two matching receipts in the system
            When: Navigating to the edit page for that transaction
            Then: Is displayed that 2 possible receipts exist
            And: Option to “Review” is available
            */

            // Given: A single transaction in the system
            var tx = await GivenSingleTransaction();

            // And: Two receipts matching the transaction
            var filenames = await GivenMatchingReceipts(tx,2);

            // When: Navigating to the edit page for that transaction
            var NextPage = await NavigatingToEditPage();

            // Then: Is displayed that 2 possible receipts exist
            var nmatches_loc = NextPage.Locator("data-test-id=nmatches");
            var nmatches_str = await nmatches_loc.TextContentAsync();
            Assert.AreEqual("2",nmatches_str);

            // And: Best match is suggested
            var suggestedfilename_loc = NextPage.Locator("data-test-id=suggestedfilename");
            var suggestedfilename = await suggestedfilename_loc.TextContentAsync();
            Assert.AreEqual(filenames.First(),suggestedfilename);

            // And: Option to “Accept” is available
            var accept_loc = NextPage.Locator("data-test-id=accept");
            Assert.IsTrue(await accept_loc.IsVisibleAsync());

            // And: Option to “Review” is available
            var review_loc = NextPage.Locator("text=Review");
            Assert.IsTrue(await review_loc.IsVisibleAsync());
        }

        [TestMethod]
        public async Task ReceiptsShown()
        {
            /*
            [Scenario] Receipts shown on pick page
            Given: A single transaction and two matching receipts in the system
            And: On the transaction edit page for that transaction
            When: Tapping “Review”
            Then: Receipt matches are shown, with better-matching receipts ordered first
            */
            // Given: A single transaction in the system
            var tx = await GivenSingleTransaction();

            // And: Two receipts matching the transaction
            var filenames = await GivenMatchingReceipts(tx,2);

            // And: Navigating to the edit page for that transaction
            var NextPage = await NavigatingToEditPage();

            // When: Tapping “Review”
            var review_loc = NextPage.Locator("text=Review");
            await review_loc.ClickAsync();
            await NextPage.SaveScreenshotToAsync(TestContext, "Pick Receipt");

            // Then: All receipt matches are shown
            var table = await ResultsTable.ExtractResultsFrom(NextPage);
            Assert.IsNotNull(table);
            Assert.AreEqual(filenames.Count(), table.Rows.Count);

            // And: Better-matching receipt is ordered first
            Assert.AreEqual($"{testmarker} 01",table.Rows[0]["Memo"]);
        }

        [TestMethod]
        public async Task PickedReceipt()
        {
            /*
            [Scenario] Picked receipt attached to transaction
            Given: A single transaction and two matching receipts in the system
            And: On the transaction edit page for that transaction
            And: Having tapped “Review”
            When: Tapping “Match” on one of the receipts
            Then: The selected receipt is added to the given transaction
            */
            // Given: A single transaction in the system
            var tx = await GivenSingleTransaction();

            // And: Two receipts matching the transaction
            var filenames = await GivenMatchingReceipts(tx,2);

            // And: Navigating to the edit page for that transaction
            var NextPage = await NavigatingToEditPage();

            // And: Having tapped “Review”
            var review_loc = NextPage.Locator("text=Review");
            await review_loc.ClickAsync();
            await NextPage.SaveScreenshotToAsync(TestContext, "Pick Receipt-Slide 23");

            // When: Tapping “Match” on one of the receipts
            var match_loc = NextPage.Locator("text=Accept");
            await match_loc.First.ClickAsync();
            await NextPage.SaveScreenshotToAsync(TestContext, "Matched Receipt-Slide 24");
            
            // Then: The transaction has a receipt now
            Assert.IsTrue(await NextPage.IsVisibleAsync("data-test-id=btn-get-receipt"),"btn-get-receipt is visible");
        }

        #endregion

        #region User Story 1314: [User Can] Attach receipt to existing transaction from repository, accepting automatically matched receipt

        [TestMethod]
        public async Task AcceptOffered()
        {
            /*
            [Scenario] Accept receipt offered on edit tx page
            Given: A single transaction and one matching receipts in the system
            When: Navigating to the edit page for that transaction
            Then: Is displayed that 1 matching receipt exist
            And: Option to “Accept” is available
            */

            // Given: A single transaction in the system
            var tx = await GivenSingleTransaction();

            // And: One receipts matching the transaction
            var filenames = await GivenMatchingReceipts(tx,1);

            // And: Navigating to the edit page for that transaction
            var NextPage = await NavigatingToEditPage();
            await NextPage.SaveScreenshotToAsync(TestContext, "Single Receipt-Slide 26");

            // Then: Is displayed that 1 matching receipt exist
            Assert.IsTrue(await NextPage.IsVisibleAsync("data-test-id=hasreceipts"),"hasreceipts is visible");
            Assert.IsFalse(await NextPage.IsVisibleAsync("data-test-id=nmatches"),"nmatches is visible");

            // And: Option to “Accept” is available
            var accept_loc = NextPage.Locator("data-test-id=accept");
            Assert.IsTrue(await accept_loc.IsVisibleAsync(),"accept is visible");
        }

        [TestMethod]
        public async Task AcceptSuggested()
        {
            /*
            [Scenario] Accepted receipt attached to transaction
            Given: A single transaction and one matching receipts in the system
            And: On the transaction edit page for that transaction
            When: Tapping “Accept”
            Then: The selected receipt is added to the given transaction
            */            

            // Given: A single transaction in the system
            var tx = await GivenSingleTransaction();

            // And: One receipts matching the transaction
            var filenames = await GivenMatchingReceipts(tx,1);

            // And: Navigating to the edit page for that transaction
            var NextPage = await NavigatingToEditPage();

            // When: Tapping “Accept”
            await NextPage.ClickAsync("input[value=Accept]");
            await NextPage.SaveScreenshotToAsync(TestContext, "Accepted Receipt-Slide 27");

            // Then: The transaction has a receipt now
            Assert.IsTrue(await NextPage.IsVisibleAsync("data-test-id=btn-get-receipt"));
        }

        #endregion

        #region User Story 1312: [User Can] Upload receipts on the Import page

        [TestMethod]
        public async Task UploadOnImportPage()
        {
            /*
            [Scenario] Upload on import page
            Given: On import page
            And: Having a set of receipts
            When: Uploading those receipts
            Then: Receipts are shown
            */
            
            // Given: On import page
            await WhenNavigatingToPage("Import");

            // And: Having a set of receipts
            // Here are the filenames we want. Need __TEST__ on each so they can be cleaned up
            var receipts = new[] 
            {
                $"Create Me $12.34 12-21 {testmarker}.png",
                $"Olive Garden $130.85 {testmarker}.png",
                $"Waste Management 12-27 {testmarker}.png",
                $"Uptown Espresso ({testmarker}).png",
            };
            var payloads = MakeImageFilePayloadsFromNames(receipts);

            // When: Uploading many files with differing name compositions
            await Page.ClickAsync("[aria-label=\"Upload\"]");
            await Page.SetInputFilesAsync("[aria-label=\"Upload\"]", payloads);
            await Page.SaveScreenshotToAsync(TestContext, "SetInputFiles");
            await Page.ClickAsync("text=Upload");

            // Then: Still on the import page
            await Page.ThenIsOnPageAsync("Importer");
            await Page.SaveScreenshotToAsync(TestContext, "Uploaded");

            // And: The receipts are shown on the import page
            Assert.AreEqual(receipts.Length, await Page.GetNumberAsync("data-test-id=NumReceiptsUploaded"));
        }

        [TestMethod]
        public async Task ShownOnReceiptsPageAfterImport()
        {
            /*
            [Scenario] Shown on receipts page
            Given: Having run the "Uploaded on import page" scenario
            When: Clicking on "See Receipts"
            Then: On receipts page
            And: Receipts are shown as if user uploaded them on the receipts page
            */

            // Given: Having run the "Uploaded on import page" scenario
            await UploadOnImportPage();

            // When: Clicking on "See Receipts"
            await Page.ClickAsync("data-test-id=btn-receipts");
            
            // Then: On receipts page
            await Page.ThenIsOnPageAsync("Receipts");
            await Page.SaveScreenshotToAsync(TestContext, "Receipts Page");
            
            // And: Receipts are shown as if user uploaded them on the receipts page
            var table = await ResultsTable.ExtractResultsFrom(Page);
            Assert.IsNotNull(table);

            // Correct count
            Assert.AreEqual(4, table.Rows.Count);

            // Memo is "__TEST__" on all
            Assert.IsTrue(table.Rows.All(x => x["Memo"].Contains(testmarker)),"All memos contain testmarker");
        }

        #endregion
    }
}
