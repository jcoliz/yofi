using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Pages
{
    [TestClass]
    public class ImportPageTest: IntegrationTest
    {
        #region Init/Cleanup

        [ClassInitialize]
        public static void InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            integrationcontext.Dispose();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean out database
            context.Set<Payee>().RemoveRange(context.Set<Payee>());
            context.Set<BudgetTx>().RemoveRange(context.Set<BudgetTx>());
            context.Set<Transaction>().RemoveRange(context.Set<Transaction>());
            context.SaveChanges();

            // Reset auth overrides
            integrationcontext.canwrite.Ok = true;
        }

        #endregion

        #region Helpers

        protected async Task<IHtmlDocument> WhenImportingAsSpreadsheet<T>(IEnumerable<T> items) where T: class
        {
            // Given: A spreadsheet of the supplied items
            var stream = GivenSpreadsheetOf(items);

            // When: Uploading it
            var document = await WhenUploadingSpreadsheet(stream, $"/Import/", $"/Import?handler=Upload");

            return document;
        }

        protected async Task<HttpResponseMessage> WhenPostingImportCommand(string command)
        {
            var formData = new Dictionary<string, string>()
            {
                { "command", command }
            };
            var response = await WhenGettingAndPostingForm($"/Import/", FormAction, formData);

            return response;
        }

        #endregion

        #region Upload Tests

        [TestMethod]
        public async Task UploadTransactionsXlsx()
        {
            // Given: Many items
            var items = GivenFakeItems<Transaction>(15).OrderBy(TestKeyOrder<Transaction>());

            // When: Uploading them as a spreadsheet
            var document = await WhenImportingAsSpreadsheet(items);

            // Then: The uploaded items are returned
            ThenResultsAreEqualByTestKey(document, items);

            // And: The database now contains the items
            items.SequenceEqual(context.Set<Transaction>().OrderBy(TestKeyOrder<Transaction>()));
        }

        [TestMethod]
        public async Task UploadDuplicate()
        {
            // Given: One item in the database
            var initial = await GivenFakeDataInDatabase<Transaction>(1);

            // And: A list of many items, one of which is a duplicate of the one item already in the database
            // (Item 1 in this list naturally overlaps item 1 in the previous list.
            var items = GivenFakeItems<Transaction>(15).OrderBy(TestKeyOrder<Transaction>());

            // When: Uploading them as a spreadsheet
            var document = await WhenImportingAsSpreadsheet(items);

            // Then: All items are in the database
            var actual = context.Set<Transaction>().AsNoTracking().OrderBy(TestKeyOrder<Transaction>());
            Assert.AreEqual(initial.Count() + items.Count(), actual.Count());

            // And: All the uploaded item are imported
            var imported = context.Set<Transaction>().Where(x=>x.Imported == true).AsNoTracking().OrderBy(TestKeyOrder<Transaction>());
            Assert.IsTrue(imported.SequenceEqual(items));

            // And: Only the non-overlapping items are selected
            var selected = context.Set<Transaction>().Where(x => x.Selected == true).AsNoTracking().OrderBy(TestKeyOrder<Transaction>());
            Assert.IsTrue(imported.SequenceEqual(items));
        }

        [TestMethod]
        public async Task UploadWithID()
        {
            // Given: One item in the database
            var initial = await GivenFakeDataInDatabase<Transaction>(1);

            // And: A list of many items, NONE of which is a duplicate of the one item already in the database
            var items = GivenFakeItems<Transaction>(15).Skip(1).OrderBy(TestKeyOrder<Transaction>()).ToList();

            // And: One of those items has the same ID as the existing item
            items[0].ID = initial.First().ID;

            // When: Uploading them as a spreadsheet
            var document = await WhenImportingAsSpreadsheet(items);

            // Then: All items are in the database
            var actual = context.Set<Transaction>().AsNoTracking().OrderBy(TestKeyOrder<Transaction>());
            Assert.IsTrue(actual.SequenceEqual(initial.Concat(items)));
        }

        [TestMethod]
        public async Task UploadError()
        {
            // When: Calling upload with no files
            var response = await WhenUploadingEmpty($"/Import/", $"/Import?handler=Upload");

            // Then: OK
            response.EnsureSuccessStatusCode();

            // And: Database is empty
            Assert.IsFalse(context.Set<Transaction>().Any());
        }

        [TestMethod]
        public async Task UploadHighlights()
        {
            // Given: A large set of transactions
            var items = GivenFakeItems<Transaction>(15).OrderBy(TestKeyOrder<Transaction>());

            // And: Having uploaded some of them initially (and approved it)
            var uploaded = items.Skip(10);
            _ = await WhenImportingAsSpreadsheet(uploaded);
            _= await WhenPostingImportCommand("ok");

            // And: Having made subtle changes to the transactions
            foreach (var t in context.Set<Transaction>())
                t.Timestamp += TimeSpan.FromDays(10);
            await context.SaveChangesAsync();

            // When: Uploading all the transactions, which includes re-uploading the already-uploaded transactions
            var document = await WhenImportingAsSpreadsheet(items);

            // Then: Only the non-overlapping items are selected
            var selected = context.Set<Transaction>().Where(x => x.Selected == true).AsNoTracking().OrderBy(TestKeyOrder<Transaction>()).ToList();
            Assert.IsTrue(selected.SequenceEqual(items.Except(uploaded)));

            // And: The overlapping new transactions are highlighted and deselected, indicating that they
            // are probably duplicates
            var highlights = document
                .QuerySelectorAll("table[data-test-id=results] tbody tr.alert td[data-test-id=memo]")
                .Select(x=>x.TextContent.Trim())
                .OrderBy(x=>x);
            var expected = uploaded
                .Select(TestKeyOrder<Transaction>());
            Assert.IsTrue(expected.SequenceEqual(highlights));
        }


        [TestMethod]
        public async Task Upload_AccessDenied()
        {
            // Given: User doesn't have CanWrite permissions
            integrationcontext.canwrite.Ok = false;

            // When: Calling upload with no files
            var response = await WhenUploadingEmpty($"/Import/", $"/Import?handler=Upload");

            // Then: Redirected to access denied page
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual("/Identity/Account/AccessDenied", redirect);
        }

        [TestMethod]
        public async Task UploadTwins_Bug883()
        {
            //
            // Bug 883: Apparantly duplicate transactions in import are (incorrectly) coalesced to single transaction for input
            //

            // Given: Two transactions, which are the same
            var item = GivenFakeItems<Transaction>(1);
            var items = item.Concat(item);

            // When: Uploading them as a spreadsheet
            _ = await WhenImportingAsSpreadsheet(items);

            // Then: There are two items in db
            var actual = context.Set<Transaction>().AsNoTracking().OrderBy(TestKeyOrder<Transaction>());
            Assert.IsTrue(items.SequenceEqual(actual));
        }

        [TestMethod]
        public async Task UploadMatchPayees()
        {
            // Given : More than five payees, one of which matches the name of the transaction we care about
            (_, var payeeschosen) = await GivenFakeDataInDatabase<Payee>(15, 1);
            var payee = payeeschosen.Single();

            // Given: Five transactions, all of which have no category, and have "payee" matching name of chosen payee
            var items = GivenFakeItems<Transaction>(5, x => { x.Category = null; x.Payee = payee.Name; return x; });

            // When: Uploading them as a spreadsheet
            _ = await WhenImportingAsSpreadsheet(items);

            // Then: All the items in the DB have category matching the payee
            Assert.IsTrue(context.Set<Transaction>().All(x => x.Category == payee.Category));
        }

        [TestMethod]
        public async Task UploadMatchPayees_RegexFirst_Bug880()
        {
            //
            // Bug 880: Import applies substring matches (incorrectly) before regex matches
            //

            // Given: Two payee matching rules, with differing payees, one with a regex one without (ergo it's a substring match)
            // And: A transaction which could match either
            var regexpayee = new Payee() { Category = "Y", Name = "/DOG.*123/" };
            var substrpayee = new Payee() { Category = "X", Name = "BIGDOG" };
            var tx = new Transaction() { Payee = "BIGDOG SAYS 1234", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m };

            context.Payees.Add(regexpayee);
            context.Payees.Add(substrpayee);
            await context.SaveChangesAsync();

            // When: Uploading them as a spreadsheet
            _ = await WhenImportingAsSpreadsheet(new[] { tx });

            // Then: The transaction will be mapped to the payee which specifies a regex
            // (Because the regex is a more precise specification of what we want.)
            var actual = context.Transactions.AsNoTracking().Single();
            Assert.AreEqual(regexpayee.Category, actual.Category);
        }

        [TestMethod]
        public async Task UploadSplitsWithTransactions()
        {
            // This is the correlary to SplitsShownDownload(). The question is, now that we've
            // DOWNLOADED transactions and splits, can we UPLOAD them and get the splits?

            // Given: A spreadsheet with 1 Transactions and many Splits, where most of the splits
            // match the transaction, but not all

            // Here's the test data set. Note that "Transaction ID" in this case is used just
            // as a matching ID for the current spreadsheet. It should be discarded.
            var transactions = new List<Transaction>();
            var splits = new List<Split>()
            {
                new Split() { Amount = 25m, Category = "A", TransactionID = 1000 },
                new Split() { Amount = 75m, Category = "C", TransactionID = 1000 },
                new Split() { Amount = 175m, Category = "X", TransactionID = 12000 } // Not going to be matched!
            };

            var item = new Transaction() { ID = 1000, Payee = "3", Category = "RemoveMe", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = splits };
            transactions.Add(item);

            // And: A spreadsheet from those transactions and splits

            // Note that we are not disposing the stream. User of the file will do so later.
            var stream = new MemoryStream();
            using (var ssr = new SpreadsheetWriter())
            {
                ssr.Open(stream);
                ssr.Serialize(transactions);
                ssr.Serialize(splits);
            }
            stream.Seek(0, SeekOrigin.Begin);

            // When: Uploading it
            var document = await WhenUploadingSpreadsheet(stream, $"/Import/", $"/Import?handler=Upload");

            // Then: Database contains one transaction with two splits, whitch matches our initial transaction.
            var actual = context.Transactions.Include(x => x.Splits).AsNoTracking().Single();
            Assert.AreEqual(2, actual.Splits.Count);
            Assert.AreEqual(item, actual);

            // And: The transaction has no category
            Assert.IsNull(actual.Category);
        }

        #endregion

        #region Import Tests

        [TestMethod]
        public async Task Import()
        {
            // Given: A mix of transactions, some flagged as imported, some as not
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(10, 4, (x => { x.Imported = true; return x; }));

            // When: Loading the import page
            var document = await WhenGetAsync($"/Import/");

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task ImportOk()
        {
            // Given: As set of items, some with imported & selected flags, some with not
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(10, 4, (x => { x.Imported = x.Selected = true; return x; }));

            // When: Approving the import
            var response = await WhenPostingImportCommand("ok");
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: All items remain, none have imported flag
            Assert.AreEqual(10, context.Set<Transaction>().Count());
            Assert.AreEqual(0, context.Set<Transaction>().Where(x => x.Imported == true).Count());
        }

        [TestMethod]
        public async Task ImportOkSelected()
        {
            // Given: As set of items, all of which have imported flags, some of which have selected flags
            (var _, var imported) = await GivenFakeDataInDatabase<Transaction>(10, 10, (x => { x.Imported = true; x.Selected = (x.Amount % 200) == 0; return x; }));
            var selected = imported.Where(x => x.Selected == true).ToList();

            // When: Approving the import
            var response = await WhenPostingImportCommand("ok");
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: Only selected items remain
            var actual = context.Set<Transaction>().AsNoTracking().OrderBy(TestKeyOrder<Transaction>());
            Assert.IsTrue(selected.SequenceEqual(actual));
        }

        [TestMethod]
        public async Task ImportCancel()
        {
            // Given: A mix of transactions, some flagged as imported, some as not
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(10, 4, (x => { x.Imported = true; return x; }));
            var expected = items.Except(chosen).ToList();

            // When: Cancelling the import
            var response = await WhenPostingImportCommand("cancel");
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: Only items without imported flag remain
            var actual = context.Set<Transaction>().AsNoTracking().OrderBy(TestKeyOrder<Transaction>());
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [DataRow(null)]
        [DataRow("Bogus")]
        [DataTestMethod]
        public async Task ImportWrong(string command)
        {
            // Given: A mix of transactions, some flagged as imported, some as not
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(10, 4, (x => { x.Imported = true; return x; }));
            var expected = items.Except(chosen).ToList();

            // When: Sending the import an incorrect command
            var response = await WhenPostingImportCommand(command);

            // Then: Bad request
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            // Then: Bad request

            // Then: No change to db
            var actual = context.Set<Transaction>().AsNoTracking().OrderBy(TestKeyOrder<Transaction>());
            Assert.AreEqual(10, actual.Count());
            Assert.AreEqual(4, actual.Where(x => x.Imported == true).Count());
        }

        [TestMethod]
        public async Task ImportOk_NotSelected_Bug839()
        {
            //
            // Bug 839: Imported items are selected automatically :(
            //

            // When: Uploading items and approving the import
            await ImportOk();

            // Then: No items remain selected
            Assert.IsFalse(context.Set<Transaction>().Any(x => x.Selected == true));
        }

        [TestMethod]
        public async Task Import_AccessDenied()
        {
            // Given: User doesn't have CanWrite permissions
            integrationcontext.canwrite.Ok = false;

            // When: Attempting to approve the import
            var response = await WhenPostingImportCommand("ok");

            // Then: Redirected to access denied page
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual("/Identity/Account/AccessDenied", redirect);
        }

        #endregion

        #region OFX

        [TestMethod]
        public async Task OfxUploadLoanSplit_Story802()
        {
            // Given: A payee with {loan details} in the category and {name} in the name
            // See TransactionRepositoryTest.CalculateLoanSplits for where we are getting this data from. This is
            // payment #53, made on 5/1/2004 for this loan.
            var principalcategory = "Principal __TEST__";
            var interestcategory = "Interest __TEST__";
            var rule = $"{principalcategory} [Loan] {{ \"interest\": \"{interestcategory}\", \"amount\": 200000, \"rate\": 6, \"term\": 180, \"origination\": \"1/1/2000\" }} ";
            var payee = "AA__TEST__ Loan Payment";
            context.Set<Payee>().Add(new Payee() { Name = payee, Category = rule });
            await context.SaveChangesAsync();

            // When: Importing an OFX file containing a transaction with payee {name}
            var filename = "User-Story-802.ofx";
            var stream = SampleData.Open(filename);
            _ = await WhenUploadingFile(stream, "files", filename, $"/Import/", $"/Import?handler=Upload");

            // Then: All transactions are imported successfully
            Assert.AreEqual(1, context.Set<Transaction>().Count());

            // And: The transaction is imported as a split
            var actual = context.Set<Transaction>().Include(x => x.Splits).AsNoTracking().Single();
            Assert.AreEqual(2, actual.Splits.Count);

            // And: The splits match the categories and amounts as expected from the {loan details} 
            var principal = -891.34m;
            var interest = -796.37m;
            Assert.AreEqual(principal, actual.Splits.Where(x => x.Category == principalcategory).Single().Amount);
            Assert.AreEqual(interest, actual.Splits.Where(x => x.Category == interestcategory).Single().Amount);
        }

        [TestMethod]
        public async Task OfxUploadNoBankRef()
        {
            // Given: An OFX file containing transactions with no bank reference
            var filename = "FullSampleData-Month02.ofx";
            var expected = 74;
            var stream = SampleData.Open(filename);

            // When: Uploading that file
            _ = await WhenUploadingFile(stream, "files", filename, $"/Import/", $"/Import?handler=Upload");

            // Then: All transactions are imported successfully
            Assert.AreEqual(expected, context.Set<Transaction>().Count());
        }

        #endregion

        #region AB#1177 Import payees & budgettx

        //
        // User Story 1177: [User Can] Import Payees or BudgetTx from main Import page
        //

        [TestMethod]
        public async Task BudgetImportLots()
        {
            // Given: Many budget transactions
            var howmany = 50;
            var items = GivenFakeItems<BudgetTx>(howmany);

            // When: Uploading them as a spreadsheet
            var document = await WhenImportingAsSpreadsheet(items);

            // Then: All items are imported successfully
            var NumBudgetTxsUploaded = document.QuerySelector("[data-test-id=NumBudgetTxsUploaded]").TextContent.Trim();
            Assert.AreEqual(howmany.ToString(), NumBudgetTxsUploaded);
        }

        [TestMethod]
        public async Task PayeeImportLots()
        {
            // Given: Many payees
            var howmany = 50;
            var items = GivenFakeItems<Payee>(howmany);

            // When: Uploading them as a spreadsheet
            var document = await WhenImportingAsSpreadsheet(items);

            // Then: All items are imported successfully
            var NumPayeesUploaded = document.QuerySelector("[data-test-id=NumPayeesUploaded]").TextContent.Trim();
            Assert.AreEqual(howmany.ToString(), NumPayeesUploaded);
        }

        #endregion

        #region AB#1178 Import all data types

        //
        // User Story 1178: [User Can] Import spreadsheets with all data types in a single spreadsheet from the main Import page
        //

        [TestMethod]
        public async Task AllDataTypes()
        {
            // Given: An XLSX file with all four types of data in sheets named for their type
            var filename = "Test-Generator-GenerateUploadSampleData.xlsx";
            var stream = SampleData.Open(filename);

            // When: Uploading that file
            _ = await WhenUploadingFile(stream, "files", filename, $"/Import/", $"/Import?handler=Upload");

            // Then: All items are imported successfully
            Assert.AreEqual(25, context.Set<Transaction>().Count());
            Assert.AreEqual(12, context.Set<Transaction>().Count(x => x.Splits.Count > 0));
            Assert.AreEqual(3, context.Set<Payee>().Count());
            Assert.AreEqual(4, context.Set<BudgetTx>().Count());
        }

        #endregion

        #region Sample Data

        //
        // Sample Data Downloads
        //

        [TestMethod]
        public async Task GetSampleOfferings()
        {
            // When: Getting the page
            var getdocument = await WhenGetAsync("/Import/");

            // Then: There are the expected amount of sample offerings
            var offerings = getdocument.QuerySelectorAll("a[data-test-id=offering]");
            Assert.IsTrue(offerings.Count() >= 27);
        }

        [TestMethod]
        public async Task DownloadAllSamples()
        {
            // Given: Already got the page, so we have the offerings populated
            var getdocument = await WhenGetAsync("/Import/");
            var offerings = getdocument.QuerySelectorAll("a[data-test-id=offering]");

            foreach (var offering in offerings)
            {
                // When: Downloading each offering
                var a = offering as IHtmlAnchorElement;
                var response = await client.GetAsync(a.Href.Replace("about://",string.Empty));

                // Then: It's a stream
                Assert.IsInstanceOfType(response.Content, typeof(StreamContent));
                var streamcontent = response.Content as StreamContent;

                // And: It's long
                Assert.IsTrue(streamcontent.Headers.ContentLength > 1000);

                // Then: The file downloads successfully
                var dir = TestContext.FullyQualifiedTestClassName + "." + TestContext.TestName;
                Directory.CreateDirectory(dir);
                var filename = dir + "/" + streamcontent.Headers.ContentDisposition.FileNameStar;
                File.Delete(filename);
                using (var outstream = File.OpenWrite(filename))
                {
                    await streamcontent.CopyToAsync(outstream);
                }
                TestContext.AddResultFile(filename);
            }
        }
#if false
        [TestMethod]
        public async Task DownloadBogusSample_BadRequest()
        {
            // When: Trying to download an offering that doesn't exist
            var actionresult = await page.OnGetSampleAsync("bogus-1234");
            Assert.That.IsOfType<BadRequestResult>(actionresult);
        }
#endif

        #endregion

    }
}
