using Common.AspNet;
using Common.AspNet.Test;
using Common.NET.Test;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Data;
using YoFi.Core.Importers;
using YoFi.Core.Models;
using YoFi.Core.Reports;
using YoFi.Core.Repositories;
using Dto = YoFi.AspNet.Controllers.TransactionsController.TransactionIndexDto;
using Transaction = YoFi.Core.Models.Transaction;

namespace YoFi.Tests
{
    [TestClass]
    public class TransactionControllerTest
    {
        private ControllerTestHelper<Transaction, TransactionsController> helper = null;

        TransactionsController controller => helper.controller;
        ApplicationDbContext context => helper.context;
        List<Transaction> Items => helper.Items;
        DbSet<Transaction> dbset => helper.dbset;

        TestAzureStorage storage;

        public TestContext TestContext { get; set; }

        private static TestContext _testContext;

        [ClassInitialize]
        public static void SetupTests(TestContext testContext)
        {
            _testContext = testContext;
        }

        public static List<Transaction> TransactionItems
        {
            get
            {
                // Need to make a new one every time we ask for it, because the old items
                // tracked IDs for a previous test
                return new List<Transaction>()
                {
                    new Transaction() { Category = "B", Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, BankReference = "C" },
                    new Transaction() { Category = "A", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m, BankReference = "D" },
                    new Transaction() { Category = "C", Payee = "5", Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Amount = 300m, BankReference = "B" },
                    new Transaction() { Category = "B", Payee = "1", Timestamp = new DateTime(DateTime.Now.Year, 01, 05), Amount = 400m, BankReference = "E" },
                    new Transaction() { Category = "B", Payee = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m, BankReference = "A" },
                    new Transaction() { Category = "B", Payee = "34", Memo = "222", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m, BankReference = "J" },
                    new Transaction() { Category = "B", Payee = "1234", Memo = "Wut", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m, BankReference = "I" },
                    new Transaction() { Category = "C", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m, BankReference = "G" },
                    new Transaction() { Category = "ABC", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m, BankReference = "H" },
                    new Transaction() { Category = "DE:CAF", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m, BankReference = "F" },
                    new Transaction() { Category = "GH:CAF", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "DE:RGB", Payee = "2", Memo = "CAFE", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:RGB", Payee = "2", Memo = "CONCACAF", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:XYZ", Payee = "2", Memo = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:RGB", Payee = "2", Memo = "Wut", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "DE:RGB", Payee = "CAFE", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:RGB", Payee = "CONCACAF", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:XYZ", Payee = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:RGB", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Amount = 123m },
                    new Transaction() { Amount = 1.23m },
                    new Transaction() { Memo = "123" },
                };
            }
        }
            
        readonly List<Split> SplitItems = new List<Split>()
        {
            new Split() { Amount = 25m, Category = "A" },
            new Split() { Amount = 75m, Category = "C" },
            new Split() { Amount = 75m, Category = "C", Memo = "CAFES" },
            new Split() { Amount = 75m, Category = "C", Memo = "WHOCAFD" }
        };

        readonly List<Payee> PayeeItems = new List<Payee>()
        {
            new Payee() { Category = "Y", Name = "3" },
            new Payee() { Category = "X", Name = "2" },
            new Payee() { Category = "Z", Name = "5" },
            new Payee() { Category = "X", Name = "1" },
            new Payee() { Category = "Y", Name = "4" }
        };

        static IEnumerable<Transaction> TransactionItemsLong;

        // This is public in case someone ELSE wants a big boatload of transactions
        public static async Task<IEnumerable<Transaction>> GetTransactionItemsLong()
        {
            if (null == TransactionItemsLong)
            {
                using var stream = SampleData.Open("FullSampleData-Month02.ofx");
                OfxDocument Document = await OfxDocumentReader.FromSgmlFileAsync(stream);
                TransactionItemsLong = Document.Statements.SelectMany(x=>x.Transactions).Select(tx=> new Transaction() { Amount = tx.Amount, Payee = tx.Memo.Trim(), BankReference = tx.ReferenceNumber?.Trim(), Timestamp = tx.Date.Value.DateTime });
            }
            return TransactionItemsLong;
        }

        public async Task<IActionResult> DoUpload(ICollection<Transaction> what)
        {
            // Make an HTML Form file containg a spreadsheet.
            var file = ControllerTestHelper<Transaction,TransactionsController>.PrepareUpload(what);

            // Upload that
            var result = await controller.Upload(new List<IFormFile>() { file }, new TransactionImporter(_repository,new PayeeRepository(helper.context)));

            return result;
        }

        ITransactionRepository _repository;

        [TestInitialize]
        public void SetUp()
        {
            helper = new ControllerTestHelper<Transaction, TransactionsController>();
            helper.SetUp();
            storage = new TestAzureStorage();

            // https://stackoverflow.com/questions/55497800/populate-iconfiguration-for-unit-tests
            var strings = new Dictionary<string, string>
            {
                { "Storage:BlobContainerName", "Testing" }
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(strings)
                .Build();

            _repository = new TransactionRepository(helper.context, storage: storage, config: configuration);
            helper.controller = new TransactionsController(_repository, helper.context);
            helper.Items.AddRange(TransactionItems.Take(5));
            helper.dbset = helper.context.Transactions;

            // Sample data items will use Payee name as a unique sort idenfitier
            helper.KeyFor = (x => x.Payee);
        }

        [TestCleanup]
        public void Cleanup() => helper.Cleanup();
        [TestMethod]
        public void Empty() => helper.Empty();
        [TestMethod]
        public async Task IndexEmpty() => await helper.IndexEmpty<Dto>();
        [TestMethod]
        public async Task IndexSingle()
        {
            var expected = Items[0];

            context.Add(expected);
            await context.SaveChangesAsync();

            var result = await controller.Index();
            var actual = result as ViewResult;
            var model = actual.Model as IEnumerable<Dto>;

            Assert.AreEqual(1, model.Count());
            Assert.IsTrue(model.Single().Equals(expected));
        }
        [TestMethod]
        public async Task DetailsFound() => await helper.DetailsFound();
        
        // TODO: Implement directly, not using interface
        //[TestMethod]
        //public async Task EditFound() => await helper.EditFound();
        [TestMethod]
        public async Task Create() => await helper.Create();
        [TestMethod]
        public async Task EditObjectValues() => await helper.EditObjectValues();
        [TestMethod]
        public async Task DeleteFound() => await helper.DeleteFound();
        [TestMethod]
        public async Task DeleteConfirmed() => await helper.DeleteConfirmed();
        // TODO: Fix failing tests
        [TestMethod]
        public async Task Download() => await helper.Download();
        [TestMethod]
        public async Task Upload()
        {
            // Can't use the helper's upload bevause Transaction upload does not return the uploaded items.
            var result = await DoUpload(Items);

            // Test the status
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Import", actual.ActionName);

            // Now check the state of the DB

            Assert.AreEqual(Items.Count, dbset.Count());
        }
        [TestMethod]
        public async Task UploadWithID()
        {
            // Can't use the helper's upload bevause Transaction upload does not return the uploaded items.

            // Start out with one item in the DB. We are picking the ONE item that Upload doesn't upload.
            var expected = Items[4];
            context.Add(expected);
            await context.SaveChangesAsync();

            // One of the new items has an overlapping ID, but will be different in every way. We expect that
            // The end result is that the database will == items
            Items[0].ID = expected.ID;

            // Just upload the first four items. The fifth, we already did above
            // Can't use the helper's upload bevause Transaction upload does not return the uploaded items.
            var result = await DoUpload(Items.Take(4).ToList());
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Import", actual.ActionName);

            // From here we can just use the Index test, but not add items. There should be the proper "items"
            // all there now.

            var indexmany = await dbset.OrderBy(x => x.Payee).ToListAsync();

            // Sort the original items by Key
            Items.Sort((x, y) => x.Payee.CompareTo(y.Payee));

            // Test that the resulting items are in the same order
            for (int i = 0; i < 5; ++i)
                Assert.AreEqual(Items[i], indexmany[i]);
        }

        [TestMethod]
        public async Task UploadDuplicate()
        {
            // Start out with one item in the DB. We are picking the ONE item that Upload doesn't upload.
            var initial = Items[0];
            context.Add(initial);
            await context.SaveChangesAsync();

            // Need to detach the entity we originally created, to set up the same state the controller would be
            // in with not already haveing a tracked object.
            context.Entry(initial).State = EntityState.Detached;

            Items[0].ID = 0;

            // Now upload all the items. What should happen here is that only items 1-4 (not 0) get
            // uploaded, because item 0 is already there, so it gets removed as a duplicate.
            // Can't use the helper's upload bevause Transaction upload does not return the uploaded items.
            var result = await DoUpload(Items);
            var redirectooactionresult = result as RedirectToActionResult;

            Assert.AreEqual("Import", redirectooactionresult.ActionName);

            // What's expected here? The new item IS uploaded, BUT it's deselected, where the rest are selected, right?
            var indexmany = await dbset.OrderBy(x => x.Payee).ToListAsync();

            Assert.AreEqual(1 + Items.Count(), indexmany.Count);

            // We expect that FIVE items were imported.

            var wasimported = dbset.Where(x => true == x.Imported);

            Assert.AreEqual(5, wasimported.Count());

            // Out of those, we expect that ONE is not selected, because it's a dupe of the initial.

            var actual = wasimported.Where(x => false == x.Selected).Single();

            Assert.AreEqual(initial, actual);
        }

        [TestMethod]
        public async Task UploadMatchPayees()
        {
            context.Payees.AddRange(PayeeItems.Take(5));
            await context.SaveChangesAsync();

            // Strip off the categories, so they'll match on input
            var uploadme = Items.Select(x => { x.Category = null; return x; }).ToList();

            // Then upload that
            await DoUpload(uploadme);

            // This should have matched ALL the payees

            foreach(var tx in context.Transactions)
            {
                var expectedpayee = context.Payees.Where(x => x.Name == tx.Payee).Single();
                Assert.AreEqual(expectedpayee.Category, tx.Category);
            }
        }

        [TestMethod]
        public async Task Bug883()
        {
            // Bug 883: Apparantly duplicate transactions in import are coalesced to single transaction for input

            var uploadme = new List<Transaction>() { Items[0], Items[0] };

            // Then upload that
            await DoUpload(uploadme);

            Assert.AreEqual(2, context.Transactions.Count());
        }

        [TestMethod]
        public async Task Bug880()
        {
            // Bug 880: Import applies substring matches before regex matches

            // Given: Two payee matching rules, with differing payees, one with a regex one without (ergo it's a substring match)
            // And: A transaction which could match either
            var regexpayee = new Payee() { Category = "Y", Name = "/DOG.*123/" };
            var substrpayee = new Payee() { Category = "X", Name = "BIGDOG" };
            var tx = new Transaction() { Payee = "BIGDOG SAYS 1234", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m };

            context.Payees.Add(regexpayee);
            context.Payees.Add(substrpayee);
            await context.SaveChangesAsync();

            // When: Uploading the transaction
            await DoUpload(new List<Transaction>() { tx });

            // Then: The transaction will be mapped to the payee which specifies a regex
            // (Because the regex is a more precise specification of what we want.)
            var actual = context.Transactions.Single();
            Assert.AreEqual(regexpayee.Category, actual.Category);
        }

        [TestMethod]
        public async Task Bug839()
        {
            // Bug 839: Imported items are selected automatically :(
            await DoUpload(Items);

            await controller.ProcessImported("ok");

            var numimported = await dbset.Where(x => x.Imported == true).CountAsync();
            var numselected = await dbset.Where(x => x.Selected == true).CountAsync();

            Assert.AreEqual(0, numimported);
            Assert.AreEqual(0, numselected);
        }

        [TestMethod]
        public async Task Bug846()
        {
            // Bug 846: Save edited item overwrites uploaded receipt

            // So what is happening here is we are calling Edit(id) to get the item first, which has no receipturl

            await helper.AddFiveItems();
            var expected = Items[3];
            var result = await controller.Edit(expected.ID, new PayeeRepository(helper.context));
            var viewresult = result as ViewResult;
            var editing = viewresult.Model as Transaction;

            // Detach so our edits won't show up
            context.Entry(editing).State = EntityState.Detached;

            // Then separately we are committing a change to set the receipturl

            var dbversion = dbset.Where(x => x.ID == editing.ID).Single();
            dbversion.ReceiptUrl = "SET";
            context.SaveChanges();

            // Detach so the editing operation can work on this item
            context.Entry(dbversion).State = EntityState.Detached;

            // And THEN we are posting the original item :P
            await controller.Edit(editing.ID, false, editing);

            // What SHOULD happen is that the "blank" recepturl in the updated object does not overwrite
            // the receitpurl we set above.

            var actual = dbset.Where(x => x.ID == editing.ID).Single();

            Assert.AreEqual("SET", actual.ReceiptUrl);
        }

        [TestMethod]
        public async Task CreateSplit()
        {
            await helper.AddFiveItems();
            var item = Items[3];
            var expectedcategory = new String(item.Category);

            var result = await controller.CreateSplit(item.ID);
            var viewresult = result as RedirectToActionResult;
            var routes = viewresult.RouteValues;
            var newid = Convert.ToInt32(routes["id"]);
            var actual = context.Splits.Where(x => x.ID == newid).Single();

            Assert.AreEqual(item.ID, actual.TransactionID);
            Assert.AreEqual(item.Amount, actual.Amount);
            Assert.AreEqual(expectedcategory, actual.Category);
            Assert.IsNull(actual.Transaction.Category);
        }

        [TestMethod]
        public async Task CreateSecondSplit()
        {
            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = SplitItems.Take(1).ToList() };

            context.Transactions.Add(item);
            context.SaveChanges();

            var result = await controller.CreateSplit(item.ID);
            var viewresult = result as RedirectToActionResult;
            var routes = viewresult.RouteValues;
            var newid = Convert.ToInt32(routes["id"]);
            var actual = context.Splits.Where(x => x.ID == newid).Single();

            Assert.AreEqual(item.ID, actual.TransactionID);
            Assert.AreEqual(75m, actual.Amount);

            // Second split shouldn't have a category/subcat
            Assert.IsNull(actual.Category);
        }

        [TestMethod]
        public async Task SplitsShownInEdit()
        {
            // Copied from SplitTest.Includes()
            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = SplitItems.Take(2).ToList() };

            context.Transactions.Add(item);
            context.SaveChanges();

            // Not using this line from SplitTest.Includes(), instead we'll test the controller
            // edit.
            //var actual = await context.Transactions.Include("Splits").ToListAsync();

            // Copied from ControllerTestHelper.EditFound()
            var result = await controller.Edit(item.ID, new PayeeRepository(helper.context));
            var viewresult = result as ViewResult;
            var model = viewresult.Model as Transaction;

            Assert.AreEqual(2, model.Splits.Count);
            Assert.AreEqual(75m, model.Splits.Where(x => x.Category == "C").Single().Amount);
            Assert.IsTrue(model.IsSplitsOK);
        }

        [TestMethod]
        public async Task SplitsShownInIndex()
        {
            // Copied from SplitTest.Includes()
            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = SplitItems.Take(2).ToList() };
            context.Transactions.Add(item);
            context.SaveChanges();

            var result = await controller.Index();
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            Assert.AreEqual(1, model.Count());
            var actual = model.Single();
            Assert.IsTrue(actual.HasSplits);
        }
        [TestMethod]
        public async Task SplitsShownInIndexSearchCategory()
        {
            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = SplitItems.Take(2).ToList() };
            context.Transactions.Add(item);
            context.SaveChanges();

            var result = await controller.Index(q:"c=A");
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            Assert.AreEqual(1, model.Count());
            var actual = model.Single();
            Assert.IsTrue(actual.HasSplits);
        }

#if false
        // TODO: Rewrite for V3 reports
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task SplitsShownInReport(bool usesplits)
        {
            int year = DateTime.Now.Year;
            var ex1 = SplitItems[0];
            var ex2 = SplitItems[1];

            if (usesplits)
            {
                var item = new Transaction() { Payee = "3", Timestamp = new DateTime(year, 01, 03), Amount = 100m, Splits = SplitItems.Take(2).ToList() };

                context.Transactions.Add(item);
            }
            else
            {
                var items = new List<Transaction>();
                items.Add(new Transaction() { Category = ex1.Category, SubCategory = ex1.SubCategory, Payee = "3", Timestamp = new DateTime(year, 01, 03), Amount = ex1.Amount });
                items.Add(new Transaction() { Category = ex2.Category, SubCategory = ex2.SubCategory, Payee = "2", Timestamp = new DateTime(year, 01, 04), Amount = ex2.Amount });
                context.Transactions.AddRange(items);
            }

            context.SaveChanges();

            var result = await controller.Pivot("all", null, null, year, null);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as Table<Label, Label, decimal>;

            var row_AB = model.RowLabels.Where(x => x.Key1 == "A" && x.Key2 == "B").Single();
            var col = model.ColumnLabels.First();
            var actual_AB = model[col, row_AB];

            Assert.AreEqual(ex1.Amount, actual_AB);

            var row_CD = model.RowLabels.Where(x => x.Key1 == "C" && x.Key2 == "D").Single();
            var actual_CD = model[col, row_CD];

            Assert.AreEqual(ex2.Amount, actual_CD);

            // Make sure the total is correct as well, no extra stuff in there.
            var row_total = model.RowLabels.Where(x => x.Value == "TOTAL").Single();
            var actual_total = model[col, row_total];

            Assert.AreEqual(ex1.Amount + ex2.Amount, actual_total);
        }
#endif

        [TestMethod]
        public async Task SplitsDontAddUpInEdit()
        {
            // Copied from SplitTest.Includes()

            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = SplitItems.Take(1).ToList() };

            context.Transactions.Add(item);
            context.SaveChanges();

            // Copied from ControllerTestHelper.EditFound()
            var result = await controller.Edit(item.ID, new PayeeRepository(helper.context));
            var viewresult = result as ViewResult;
            var model = viewresult.Model as Transaction;

            Assert.IsFalse(model.IsSplitsOK);
        }

        [TestMethod]
        public async Task UploadSplitsForTransaction()
        {
            // Don't add the splits here, we'll upload them
            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m };

            context.Transactions.Add(item);
            context.SaveChanges();

            // Make an HTML Form file containg a spreadsheet containing those splits
            var file = ControllerTestHelper<Split, SplitsController>.PrepareUpload(SplitItems.Take(2).ToList());

            // Upload that
            var result = await controller.UpSplits(new List<IFormFile>() { file }, item.ID, new SplitImporter(_repository));
            var redir = result as RedirectToActionResult;

            Assert.IsNotNull(redir);
            Assert.IsTrue(item.HasSplits);
            Assert.IsTrue(item.IsSplitsOK);
        }

        [TestMethod]
        public async Task SplitsShownDownload()
        {
            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = SplitItems.Take(2).ToList() };

            context.Transactions.Add(item);
            context.SaveChanges();

            var result = await controller.Download(false);
            var fcresult = result as FileStreamResult;
            var stream = fcresult.FileStream;
            var incoming = helper.ExtractFromSpreadsheet<Split>(stream);

            Assert.AreEqual(2, incoming.Count);
            Assert.AreEqual(item.ID, incoming.First().TransactionID);
            Assert.AreEqual(item.ID, incoming.Last().TransactionID);
        }

        [TestMethod]
        public async Task UploadSplitsWithTransactions()
        {
            // This is the correlary to SplitsShownDownload(). The question is, now that we've
            // DOWNLOADED transactions and splits, can we UPLOAD them and get the splits?

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

            // Build a spreadsheet with the chosen number of items
            // Note that we are not disposing the stream. User of the file will do so later.
            var stream = new MemoryStream();
            using (var ssr = new SpreadsheetWriter())
            {
                ssr.Open(stream);
                ssr.Serialize(transactions);
                ssr.Serialize(splits);
            }

            // Create a formfile with it
            var filename = "Transactions";
            stream.Seek(0, SeekOrigin.Begin);
            IFormFile file = new FormFile(stream, 0, stream.Length, filename, $"{filename}.xlsx");

            // Upload it!
            var result = await controller.Upload(new List<IFormFile>() { file }, new TransactionImporter(_repository,new PayeeRepository(helper.context)));
            Assert.IsTrue(result is RedirectToActionResult);

            // Did the transaction and splits find each other?
            var actual = context.Transactions.Include(x=>x.Splits).Single();
            Assert.AreEqual(2,actual.Splits.Count);

            // The transaction should NOT have a category anymore, even if it had one to begin with
            Assert.IsNull(actual.Category);
        }

        [TestMethod]
        public async Task DownloadAllYears() => await DownloadAllYears_Internal();

        public async Task<FileStreamResult> DownloadAllYears_Internal()
        {
            var item_new = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m };
            var item_old = new Transaction() { Payee = "4", Timestamp = new DateTime(DateTime.Now.Year - 5, 01, 03), Amount = 200m };

            context.Transactions.Add(item_old);
            context.Transactions.Add(item_new);
            context.SaveChanges();

            var result = await controller.Download(true);
            var fcresult = result as FileStreamResult;
            var stream = fcresult.FileStream;
            var incoming = helper.ExtractFromSpreadsheet<Transaction>(stream);

            Assert.AreEqual(2, incoming.Count);

            return fcresult;
        }

        [TestMethod]
        public async Task Bug895()
        {
            // Bug 895: Transaction download appears corrupt if no splits

            // So if there are no SPLITS there should be do splits tab.
            // For convenience we'll use a different test and just grab those results.

            var fcresult = await DownloadAllYears_Internal();
            var stream = fcresult.FileStream;

            IEnumerable<Transaction> txitems;
            IEnumerable<string> sheetnames;
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            txitems = ssr.Deserialize<Transaction>();
            sheetnames = ssr.SheetNames.ToList();

            Assert.AreEqual(1, sheetnames.Count());
            Assert.AreEqual("Transaction", sheetnames.Single());
            Assert.IsTrue(txitems.Any());
        }

        [TestMethod]
        public async Task EditObjectValuesDuplicate()
        {
            var initial = Items[3];
            context.Add(initial);
            await context.SaveChangesAsync();
            var id = initial.ID;

            // Need to detach the entity we originally created, to set up the same state the controller would be
            // in with not already haveing a tracked object.
            context.Entry(initial).State = EntityState.Detached;

            var updated = Items[1];
            updated.ID = id;
            var result = await controller.Edit(id, duplicate:true, transaction:updated);
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Index", actual.ActionName);

            // The net effect of the duplicate flag is that we now have TWO of these objects,
            // so 2 total.
            Assert.AreEqual(2, context.Transactions.Count());
        }

        [TestMethod]
        public async Task OfxUploadNoBankRef()
        {
            // Given: An OFX file containing transactions with no bank reference
            var filename = "FullSampleData-Month02.ofx";
            var expected = 74;

            // When: Uploading that file
            var stream = SampleData.Open(filename);
            var length = stream.Length;
            IFormFile file = new FormFile(stream, 0, length, filename, filename);
            var result = await controller.Upload(new List<IFormFile>() { file }, new TransactionImporter(_repository, new PayeeRepository(helper.context)));

            // Then: All transactions are imported successfully
            var rdresult = result as RedirectToActionResult;

            Assert.AreEqual("Import", rdresult.ActionName);
            Assert.AreEqual(expected, dbset.Count());
        }

        [TestMethod]
        public async Task ImportCancel()
        {
            // Given: As set of items, some with imported flag, some with not
            var expected = 1; // How many should remain?
            foreach(var item in Items.Skip(expected))
                item.Imported = true;
            await helper.AddFiveItems();

            // When: Cancelling the import
            await controller.ProcessImported("cancel");

            // Then: Only items without imported flag remain
            Assert.AreEqual(expected, dbset.Count());
        }

        [TestMethod]
        public async Task ImportOk()
        {
            // Given: As set of items, some with imported & selected flags, some with not
            var notimported = 1; // How many should remain?
            foreach (var item in Items.Skip(notimported))
                item.Imported = item.Selected = true;
            await helper.AddFiveItems();

            // When: Approving the import
            var result = await controller.ProcessImported("ok");

            // Then: All items remain, none have imported flag
            Assert.AreEqual(5, dbset.Count());
            Assert.AreEqual(0, dbset.Where(x => x.Imported == true).Count());
        }

        [TestMethod]
        public async Task ImportOkSelected()
        {
            // Given: As set of items, all with imported some selected, some not
            foreach (var item in Items)
                item.Imported = true;

            var imported = 2; // How many should remain?
            foreach (var item in Items.Take(imported))
                item.Selected = true;

            await helper.AddFiveItems();

            // When: Approving the import
            var result = await controller.ProcessImported("ok");

            // Then: Only selected items remain
            Assert.AreEqual(imported, dbset.Count());
        }

        public static IEnumerable<object[]> IndexSortOrderTestData
        {
            get
            {
                return new[]
                {
                    new object[] { new { Key = "pa" , Ascending = true, Predicate = (Func<Dto, string>)(x=>x.Payee) } },
                    new object[] { new { Key = "ca" , Ascending = true, Predicate = (Func<Dto, string>)(x=>x.Category) } },
                    new object[] { new { Key = "da" , Ascending = true, Predicate = (Func<Dto, string>)(x=>x.Timestamp.ToOADate().ToString()) } },
                    new object[] { new { Key = "aa" , Ascending = true, Predicate = (Func<Dto, string>)(x=>x.Amount.ToString()) } },
                    new object[] { new { Key = "pd" , Ascending = false, Predicate = (Func<Dto, string>)(x=>x.Payee) } },
                    new object[] { new { Key = "cd" , Ascending = false, Predicate = (Func<Dto, string>)(x=>x.Category) } },
                    new object[] { new { Key = "dd" , Ascending = false, Predicate = (Func<Dto, string>)(x=>x.Timestamp.ToOADate().ToString()) } },
                    new object[] { new { Key = "ad" , Ascending = false, Predicate = (Func<Dto, string>)(x=>x.Amount.ToString()) } },
                    new object[] { new { Key = "ra" , Ascending = true, Predicate = (Func<Dto, string>)(x=>x.BankReference) } },
                    new object[] { new { Key = "rd" , Ascending = false, Predicate = (Func<Dto, string>)(x=>x.BankReference) } },
                };
            }
        }

        [DynamicData(nameof(IndexSortOrderTestData))]
        [DataTestMethod]
        public async Task IndexSortOrder(dynamic item)
        {
            // Given: A set of items
            context.Transactions.AddRange(TransactionItems.Take(10));
            context.SaveChanges();

            // When: Calling Index with a defined sort order
            var result = await controller.Index(o:item.Key);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            // Then: The items are returned sorted in that order
            var predicate = item.Predicate as Func<Dto, string>;
            List<Dto> expected = null;
            if (item.Ascending)
                expected = model.OrderBy(predicate).ToList();
            else
                expected = model.OrderByDescending(predicate).ToList();

            Assert.IsTrue(expected.SequenceEqual(model));
        }

        [TestMethod]
        public async Task IndexPayeeSearch()
        {
            // Given: A set of items with various payees
            context.Transactions.AddRange(TransactionItems.Take(7));
            context.SaveChanges();

            // When: Calling Index with payee search term
            IActionResult result;
            result = await controller.Index(q:"p=4");
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            // Then: Only the items with a matching payee are returned
            Assert.AreEqual(3, model.Count());
        }

        [TestMethod]
        public async Task IndexCategorySearch()
        {
            // Given: A set of items with various categories
            context.Transactions.AddRange(TransactionItems.Take(10));
            context.SaveChanges();

            // When: Calling Index with category search term
            IActionResult result;
            result = await controller.Index(q:"C=C");
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<TransactionsController.TransactionIndexDto>;

            // Then: Only the items with a matching category are returned
            Assert.AreEqual(4, model.Count());
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task IndexShowHidden(bool ishidden)
        {
            // Given: A set of items, some hidden some not
            GivenItemsHiddenAndNot(out var items, out var hiddenitems);

            // When: Calling Index with indirect search term for hidden items
            var searchterm = ishidden ? "H" : null;
            var result = await controller.Index(v:searchterm);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            // Then: Only the items with a matching hidden state are returned
            if (ishidden)
                Assert.AreEqual(items.Count(), model.Count());
            else
                Assert.AreEqual(items.Count() - hiddenitems.Count(), model.Count());
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task IndexShowSelected(bool isselected)
        {
            // When: Calling index with view set to 'selected'
            var searchterm = isselected ? "S" : null;
            await controller.Index(v:searchterm);

            // Then: The "show selected" state is transmitted through to the view in the view data
            Assert.AreEqual(isselected, controller.ViewData["ShowSelected"]);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataRow(null)]
        [DataTestMethod]
        public async Task IndexShowHasReceipt(bool? hasreceipt)
        {
            // Given: A set of items, some with receipts some not
            GivenItemsWithAndWithoutReceipt(context, out var items, out var receiptitems);

            // When: Calling Index with indirect search term for items with/without a receipt
            string searchterm = null;
            if (hasreceipt.HasValue)
                searchterm = hasreceipt.Value ? "R=1" : "R=0";
            var result = await controller.Index(q:searchterm);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            // Then: Only the items with a matching receipt state are returned
            if (hasreceipt.HasValue)
            {
                if (hasreceipt.Value)
                    Assert.AreEqual(receiptitems.Count(), model.Count());
                else
                    Assert.AreEqual(items.Count() - receiptitems.Count(), model.Count());
            }
            else
                Assert.AreEqual(items.Count(), model.Count());
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataRow(null)]
        [DataTestMethod]
        public async Task IndexPayeesWithReceipt(bool? hasreceipt)
        {
            // Given: A set of items, some with receipts some not
            var items = TransactionItems.Take(10);
            foreach (var i in items.Take(3))
                i.ReceiptUrl = "I have a receipt!";

            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling Index with combined search term for payee AND with/without a receipt
            string payee = "2";
            string search = $"P={payee}";
            if (hasreceipt.HasValue)
                search = hasreceipt.Value ? $"P={payee},R=1" : $"P={payee},R=0";
            var result = await controller.Index(q:search);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            // Then: Only the items with a matching payee AND receipt state are returned
            if (hasreceipt.HasValue)
            {
                if (hasreceipt.Value)
                    Assert.AreEqual(items.Where(x => x.Payee.Contains(payee) && x.ReceiptUrl != null).Count(), model.Count());
                else
                    Assert.AreEqual(items.Where(x => x.Payee.Contains(payee) && x.ReceiptUrl == null).Count(), model.Count());
            }
            else
                Assert.AreEqual(items.Where(x=>x.Payee.Contains(payee)).Count(), model.Count());
        }

        [TestMethod]
        public async Task IndexPage1()
        {
            // Given: 3 pages of items 
            await Helpers.SampleDataStore.LoadSingleAsync();
            var items = Helpers.SampleDataStore.Single.Transactions.Take(PageDivider.DefaultPageSize * 3);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling Index page 1
            var result = await controller.Index(p:1);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            // Then: Only one page's worth of items are returned
            Assert.AreEqual(TransactionsController.PageSize, model.Count());

            // And: Page Item values are as expected
            var pages = viewresult.ViewData[nameof(PageDivider)] as PageDivider;
            Assert.AreEqual(1,pages.PageFirstItem);
            Assert.AreEqual(TransactionsController.PageSize, pages.PageLastItem);
            Assert.AreEqual(items.Count(), pages.PageTotalItems);
        }

        [TestMethod]
        public async Task IndexPage2()
        {
            // Given: A long set of items, which is longer than one page, but not as long as two pages 
            await Helpers.SampleDataStore.LoadSingleAsync();
            var items = Helpers.SampleDataStore.Single.Transactions.Take(PageDivider.DefaultPageSize * 3 / 2);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling Index page 2
            var result = await controller.Index(p:2);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            // Then: Only items after one page's worth of items are returned
            Assert.AreEqual(TransactionsController.PageSize / 2, model.Count());

            // And: Page Item values are as expected
            var pages = viewresult.ViewData[nameof(PageDivider)] as PageDivider;
            Assert.AreEqual(1 + TransactionsController.PageSize, pages.PageFirstItem);
            Assert.AreEqual(items.Count(), pages.PageLastItem);
            Assert.AreEqual(items.Count(), pages.PageTotalItems);
        }

        [TestMethod]
        public async Task IndexPage5()
        {
            // Given: 4 1/2 pages of items
            await Helpers.SampleDataStore.LoadSingleAsync();
            var items = Helpers.SampleDataStore.Single.Transactions.Take(PageDivider.DefaultPageSize * 9 / 2);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling Index page 5
            var result = await controller.Index(p: 5);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            // Then: Only the remaining items are returned
            Assert.AreEqual(items.Count() % TransactionsController.PageSize, model.Count());

            // And: Page values are as expected
            var pages = viewresult.ViewData[nameof(PageDivider)] as PageDivider;
            Assert.AreEqual(3, pages.PreviousPreviousPage);
            Assert.AreEqual(1, pages.FirstPage);
        }

        [TestMethod]
        public async Task IndexPage1of2()
        {
            // Given: 1 1/2 pages of items
            await Helpers.SampleDataStore.LoadSingleAsync();
            var items = Helpers.SampleDataStore.Single.Transactions.Take(PageDivider.DefaultPageSize * 3/2);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling Index page 1
            var result = await controller.Index(p: 1);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            // Then: Only one page's worth of items are returned
            Assert.AreEqual(TransactionsController.PageSize, model.Count());

            // And: Page values are as expected
            var pages = viewresult.ViewData[nameof(PageDivider)] as PageDivider;
            Assert.AreEqual(items.Count(), pages.PageTotalItems);
        }

        [TestMethod]
        public async Task BulkEdit()
        {
            // Given: A list of items with varying categories, and varying selection states
            var targetset = TransactionItems.Take(10).Where(x => x.Category == "B");
            var expected = targetset.Count();
            foreach (var t in targetset)
                t.Selected = true;
            context.Transactions.AddRange(targetset);
            context.SaveChanges();

            // When: Calling Bulk Edit with a new category
            var newcategory = "X:Y";
            var result = await controller.BulkEdit(newcategory);
            var rdresult = result as RedirectToActionResult;

            Assert.AreEqual("Index", rdresult.ActionName);

            // Then: All previously-selected items are now that new category
            Assert.AreEqual(expected, dbset.Where(x => x.Category == newcategory).Count());

            // And: No items remain selected
            Assert.AreEqual(0, dbset.Where(x => x.Selected == true).Count());
        }

        [TestMethod]
        public async Task BulkEditParts()
        {
            // Given: A list of items with varying categories, some of which match the pattern *:B:*

            var categories = new string[] { "AB:Second:E", "AB:Second:E:F", "AB:Second:A:B:C", "G H:Second:KLM NOP" };
            context.Transactions.AddRange(categories.Select(x=>new Transaction() { Category = x, Amount = 100m, Timestamp = new DateTime(2001,1,1), Selected = true }));
            context.SaveChanges();

            // When: Calling Bulk Edit with a new category which includes positional wildcards
            var newcategory = "(1):New Category:(3+)";
            var result = await controller.BulkEdit(newcategory);
            var rdresult = result as RedirectToActionResult;

            Assert.AreEqual("Index", rdresult.ActionName);

            // Then: All previously-selected items are now correctly matching the expected category
            CollectionAssert.AreEqual(categories.Select(x => x.Replace("Second", "New Category")).ToList(), dbset.Select(x => x.Category).ToList());
        }

        [TestMethod]
        public async Task BulkEditCancel()
        {
            // Given: A list of items with varying categories, and varying selection states
            var targetset = TransactionItems.Take(10).Where(x => x.Category == "B");
            var expected = targetset.Count();
            foreach (var t in targetset)
                t.Selected = true;
            context.Transactions.AddRange(targetset);
            context.SaveChanges();

            // When: Calling Bulk Edit with blank category
            var result = await controller.BulkEdit(null);
            var rdresult = result as RedirectToActionResult;

            Assert.AreEqual("Index", rdresult.ActionName);

            // Then: No items remain selected
            Assert.AreEqual(0, dbset.Where(x => x.Selected == true).Count());
        }

        public static FormFile FormFileFromSampleData(string filename, string contenttype)
        {
            var stream = SampleData.Open(filename);
            var length = stream.Length;

            return new FormFile(stream, 0, length, filename, filename) { Headers = new HeaderDictionary(), ContentType = contenttype };
        }

        [TestMethod]
        public async Task UpReceipt()
        {
            // Given: A transaction with no receipt
            var tx = TransactionItems.First();
            context.Transactions.Add(tx);
            context.SaveChanges();

            // When: Uploading a receipt
            var contenttype = "application/json";
            var file = FormFileFromSampleData("BudgetTxs.json", contenttype);
            var result = await controller.UpReceipt(new List<IFormFile>() { file },tx.ID);
            Assert.IsTrue(result is RedirectToActionResult);

            // Then: The transaction displays as having a receipt
            Assert.IsFalse(string.IsNullOrEmpty(tx.ReceiptUrl));

            // And: The receipt is contained in storage
            Assert.AreEqual(contenttype, storage.BlobItems.Single().ContentType);
        }

        [TestMethod]
        public async Task DeleteReceipt()
        {
            // Given: A transaction with a receipt
            var tx = TransactionItems.First();
            tx.ReceiptUrl = "application/ofx";
            context.Transactions.Add(tx);
            context.SaveChanges();

            // When: Deleting the receipt
            var result = await controller.ReceiptAction(tx.ID,"delete");
            var rdresult = result as RedirectToActionResult;

            Assert.AreEqual("Edit", rdresult.ActionName);

            // Then: The transaction displays as not having a receipt
            Assert.IsTrue(string.IsNullOrEmpty(tx.ReceiptUrl));
        }

        [TestMethod]
        public async Task GetReceipt()
        {
            // Given: A transaction with a receipt
            var tx = TransactionItems.First();
            var contenttype = "application/json";
            tx.ReceiptUrl = contenttype;
            context.Transactions.Add(tx);
            context.SaveChanges();

            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = tx.ID.ToString(), InternalFile = "BudgetTxs.json", ContentType = contenttype });

            // When: Getting the receipt
            var result = await controller.ReceiptAction(tx.ID,"get");
            var fsresult = result as FileStreamResult;

            // Then: The receipt is returned
            Assert.AreEqual(tx.ID.ToString(), fsresult.FileDownloadName);
            Assert.AreEqual(contenttype, fsresult.ContentType);
        }

        // This is public & static so I can share it with other tests.
        // TODO: There is probably a more elegant way.
        public static void GivenItemsWithAndWithoutReceipt(ApplicationDbContext _context,out IEnumerable<Transaction> items, out IEnumerable<Transaction> moditems)
        {
            items = TransactionItems.Take(10);
            moditems = items.Take(3);
            foreach (var i in moditems)
                i.ReceiptUrl = "I have a receipt!";

            _context.Transactions.AddRange(items);
            _context.SaveChanges();
        }

        void GivenItemsHiddenAndNot(out IEnumerable<Transaction> items, out IEnumerable<Transaction> moditems)
        {
            items = TransactionItems.Take(10);
            moditems = items.Take(3);
            foreach (var i in moditems)
                i.Hidden = true;

            context.Transactions.AddRange(items);
            context.SaveChanges();
        }

        void GivenItemsInYearAndNot(out IEnumerable<Transaction> items, out IEnumerable<Transaction> moditems, int year)
        {
            items = TransactionItems.Take(10);
            moditems = items.Take(3);
            foreach (var i in moditems)
                i.Timestamp = new DateTime(year, i.Timestamp.Month, i.Timestamp.Day);

            context.Transactions.AddRange(items);
            context.SaveChanges();
        }

        async Task<IEnumerable<Dto>> WhenCallingIndexEmpty()
        {
            var result = await controller.Index();
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            return model;
        }

        async Task<IEnumerable<Dto>> WhenCallingIndexWithQ(string q)
        {
            var result = await controller.Index(q: q);
            var viewresult = result as ViewResult;
            var model = viewresult?.Model as IEnumerable<Dto>;

            return model;
        }

        async Task WhenCallingIndexWithQThenBadRequest(string q)
        {
            var result = await controller.Index(q: q);

            Assert.IsTrue(result is BadRequestResult);
        }

        async Task<IEnumerable<Dto>> WhenCallingIndexWithV(string v)
        {
            var result = await controller.Index(v: v);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Dto>;

            return model;
        }

        void ThenOnlyReturnedTxWith(IEnumerable<Transaction> items, IEnumerable<Dto> model, Func<Transaction, string> predicate, string word)
        {
            Assert.AreNotEqual(0, model.Count());
            Assert.AreEqual(items.Where(x => predicate(x) != null && predicate(x).Contains(word)).Count(), model.Count());
            Assert.AreEqual(model.Where(x => predicate((Transaction)x).Contains(word)).Count(), model.Count());
        }

        void ThenTxWithWereReturned(IEnumerable<Transaction> items, IEnumerable<Dto> model, Func<Transaction, string> predicate, string word)
        {
            Assert.AreNotEqual(0, model.Count());
            Assert.AreEqual(items.Where(x => predicate(x) != null && predicate(x).Contains(word)).Count(), model.Count());
            Assert.AreEqual(model.Where(x => predicate((Transaction)x).Contains(word)).Count(), model.Count());
        }

        [TestMethod]
        public async Task IndexQCategoryAny()
        {
            // Given: A mix of transactions, some with '{word}' in their category and some without
            var items = TransactionItems.Take(11);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling index q={word}
            var word = "CAF";
            var model = await WhenCallingIndexWithQ(word);

            // Then: Only the transactions with '{word}' in their category are returned
            ThenOnlyReturnedTxWith(items, model, x => x.Category, word);
        }

        [TestMethod]
        public async Task IndexQCategorySplitsAny()
        {
            // Given: A mix of transactions, some with splits, some without; some with '{word}' in their category, memo, or payee, or splits category and some without
            var items = TransactionItems.Take(20);
            items.First().Splits = SplitItems.Take(2).ToList();
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling index q='{word}'
            var word = "A";
            var model = await WhenCallingIndexWithQ(word);

            // Then: All the transactions with '{word}' directly in their category OR in their splits category are returned
            var expected = items.Where(tx => tx.Category?.Contains(word) == true || (tx.Splits?.Any(s => s.Category?.Contains(word) == true) == true));
            Assert.IsTrue(expected.All(x => model.Where(m=>m.Equals(x)).Any()));
        }
        [TestMethod]
        public async Task IndexQMemoAny()
        {
            // Given: A mix of transactions, some with '{word}' in their memo and some without
            var items = TransactionItems.Skip(11).Take(4);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling index q={word}
            var word = "CAF";
            var model = await WhenCallingIndexWithQ(word);

            // Then: Only the transactions with '{word}' in their memo are returned
            ThenOnlyReturnedTxWith(items, model, x => x.Memo, word);
        }
        [TestMethod]
        public async Task IndexQMemoSplitsAny()
        {
            // Given: A mix of transactions, some with '{word}' in their memo and some without
            // And: Some with '{word}' in their splits' memo and some without
            var items = TransactionItems.Take(20);
            items.First().Splits = SplitItems.Take(4).ToList();
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling index q={word}
            var word = "CAF";
            var model = await WhenCallingIndexWithQ(word);

            // Then: All the transactions with '{word}' directly in their memo OR in their splits memo are returned
            var expected = items.Where(tx => tx.Memo?.Contains(word) == true || (tx.Splits?.Any(s => s.Memo?.Contains(word) == true) == true));
            Assert.IsTrue(expected.All(x => model.Where(m => m.Equals(x)).Any()));
        }

        [TestMethod]
        public async Task IndexQPayeeAny()
        {
            // Given: A mix of transactions, some with '{word}' in their payee and some without
            var items = TransactionItems.Skip(15).Take(4);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling index q={word}
            var word = "CAF";
            var model = await WhenCallingIndexWithQ(word);

            // Then: Only the transactions with '{word}' in their payee are returned
            ThenOnlyReturnedTxWith(items, model, x => x.Payee, word);
        }

        [TestMethod]
        public async Task IndexQAny()
        {
            // Given: A mix of transactions, some with '{word}' in their category, memo, or payee and some without
            var items = TransactionItems.Take(19);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling index q={word}
            var word = "CAF";
            var model = await WhenCallingIndexWithQ(word);

            // Then: Only the transactions with '{word}' in their category, memo, or payee are returned
            Assert.AreEqual(6, model.Count());
            Assert.IsTrue(model.All(tx => tx.Category?.Contains(word) == true || tx.Memo?.Contains(word) == true || tx.Payee?.Contains(word) == true));
        }

        [TestMethod]
        public async Task IndexQPayee()
        {
            // Given: A mix of transactions, some with '{word}' in their category, memo, or payee and some without
            var items = TransactionItems.Take(19);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling index q='p={word}'
            var word = "CAF";
            var model = await WhenCallingIndexWithQ($"P={word}");

            // Then: Only the transactions with '{word}' in their payee are returned
            ThenOnlyReturnedTxWith(items, model, x => x.Payee, word);
        }

        [TestMethod]
        public async Task IndexQCategory()
        {
            // Given: A mix of transactions, some with '{word}' in their category, memo, or payee and some without
            var items = TransactionItems.Take(19);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling index q='c={word}'
            var word = "CAF";
            var model = await WhenCallingIndexWithQ($"C={word}");

            // Then: Only the transactions with '{word}' in their category are returned
            ThenOnlyReturnedTxWith(items, model, x => x.Category, word);
        }

        [TestMethod]
        public async Task IndexQCategoryBlank()
        {
            // Given: A mix of transactions, some with null category
            var items = TransactionItems.Take(20);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling index q='c=[blank]'
            var model = await WhenCallingIndexWithQ($"C=[blank]");

            // Then: Only the transactions blank category are returned
            Assert.AreNotEqual(0, model.Count());
            Assert.AreEqual(items.Where(x => x.Category == null).Count(), model.Count());
            Assert.AreEqual(model.Where(x => x.Category == null).Count(), model.Count());
        }

        [TestMethod]
        public async Task IndexQCategorySplits()
        {
            // Given: A mix of transactions, some with splits, some without; some with '{word}' in their category, memo, or payee, or splits category and some without
            var items = TransactionItems.Take(20);
            items.First().Splits = SplitItems.Take(2).ToList();
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling index q='c={word}'
            var word = "A";
            var model = await WhenCallingIndexWithQ($"C={word}");

            // *** This is failing on VSO but passing locally.
            // Assert.AreEqual below finds SIX items in model?!

            // https://developercommunity.visualstudio.com/t/printing-the-console-output-in-the-azure-devops-te/631082
            var resultfile = "IndexQCategorySplits.txt";
            File.Delete(resultfile);

            using (var writer = File.CreateText(resultfile))
            {
                writer.WriteLine("---");
                writer.WriteLine($"{model.Count()} items:");
                writer.WriteLine("---");
                foreach (var item in model)
                    if (null != item)
                    {
                        writer.WriteLine(item.ID.ToString());
                        writer.WriteLine("  " + (item.Category ?? "(n)") + " / " + (item.Payee ?? "(n)"));
                    }
                    else
                        writer.WriteLine("null");
                writer.WriteLine("---");
            }

            TestContext.AddResultFile(resultfile);

            // I wonder if this inspection has trued up the model result?

            // Then: Only the transactions with '{word}' in their category are returned
            Assert.AreEqual(5, model.Count());

            // We can't actually test this anymore, because Index doesn't return the actual
            // splits anymore. That was overfetching.
            //Assert.IsTrue(model.All(tx => tx.Category?.Contains(word) == true || (tx.Splits?.Any(s=>s.Category?.Contains(word) == true) == true )));
        }


        [TestMethod]
        public async Task IndexQMemo()
        {
            // Given: A mix of transactions, some with '{word}' in their category, memo, or payee and some without
            var items = TransactionItems.Take(19);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling index q='m={word}'
            var word = "CAF";
            var model = await WhenCallingIndexWithQ($"M={word}");

            // Then: Only the transactions with '{word}' in their memo are returned
            ThenOnlyReturnedTxWith(items, model, x => x.Memo, word);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task IndexQReceipt(bool with)
        {
            // Given: A mix of transactions, some with receipts, some without
            GivenItemsWithAndWithoutReceipt(context, out var items, out var moditems);

            // When: Calling index q='r=1' (or r=0)
            var model = await WhenCallingIndexWithQ($"R={(with?'1':'0')}");

            // Then: Only the transactions with (or without) receipts are returned
            if (with)
                Assert.AreEqual(moditems.Count(),model.Count());
            else
                Assert.AreEqual(items.Count() - moditems.Count(), model.Count());
        }

        [TestMethod]
        public async Task IndexQReceiptWrong()
        {
            // Given: A mix of transactions, some with receipts, some without
            GivenItemsWithAndWithoutReceipt(context, out var _, out var _);

            // When: Calling index with q='r=text'
            // Then: Bad request returned
            await WhenCallingIndexWithQThenBadRequest($"R=text");
        }


        [DataTestMethod]
        public async Task IndexVHidden()
        {
            // Given: A mix of transactions, some hidden, some not
            GivenItemsHiddenAndNot(out var items, out _);

            // When: Calling index v='h'
            var model = await WhenCallingIndexWithV("h");

            // Then: All transactions are returned
            Assert.AreEqual(items.Count(), model.Count());
        }

        [TestMethod]
        public async Task IndexNoHidden()
        {
            // Given: A mix of transactions, some hidden, some not
            GivenItemsHiddenAndNot(out var items, out var moditems);

            // When: Calling index without qualifiers
            var model = await WhenCallingIndexEmpty();

            // Then: Only non-hidden transactions are returned
            Assert.AreEqual(items.Count() - moditems.Count(), model.Count());
        }

        [TestMethod]
        public async Task IndexQYear()
        {
            // Given: A mix of transactions, in differing years
            int year = 2000;
            GivenItemsInYearAndNot(out _, out var moditems, year);

            // When: Calling index q='y={year}'
            var model = await WhenCallingIndexWithQ($"Y={year}");

            // Then: Only the transactions in {year} are returned
            Assert.AreEqual(moditems.Count(), model.Count());
        }

        [TestMethod]
        public async Task IndexQAmountText()
        {
            // Given: A mix of transactions, with differing amounts
            var items = TransactionItems.Take(22);
            context.AddRange(items);
            context.SaveChanges();

            // When: Calling index with q='a=text'
            // Then: Bad request returned
            await WhenCallingIndexWithQThenBadRequest($"A=text");
        }

        [TestMethod]
        public async Task IndexQAmountInteger()
        {
            // Given: A mix of transactions, with differing amounts
            var items = TransactionItems.Take(22);
            context.AddRange(items);
            context.SaveChanges();

            // When: Calling index with q='a=###'
            var model = await WhenCallingIndexWithQ($"A=123");

            // Then: Only transactions with amounts #.## and ###.00 are returned
            var expected = items.Count(x => x.Amount == 1.23m || x.Amount == 123m);
            Assert.AreEqual(expected, model.Count());
            Assert.IsTrue(model.All(x => x.Amount == 1.23m || x.Amount == 123m));
        }

        [TestMethod]
        public async Task IndexQAmountDouble()
        {
            // Given: A mix of transactions, with differing amounts
            var items = TransactionItems.Take(22);
            context.AddRange(items);
            context.SaveChanges();

            // When: Calling index with q='a=#.##'
            var model = await WhenCallingIndexWithQ($"A=1.23");

            // Then: Only transactions with amounts #.## are returned
            var expected = items.Count(x => x.Amount == 1.23m);
            Assert.AreEqual(expected, model.Count());
            Assert.IsTrue(model.All(x => x.Amount == 1.23m));
        }

        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataTestMethod]
        public async Task IndexQDate(int day)
        {
            // Given: A mix of transactions, with differing dates
            var items = TransactionItems.Take(22);
            context.AddRange(items);
            context.SaveChanges();

            // When: Calling index with q='d=#/##'
            var target = new DateTime(DateTime.Now.Year, 01, day);
            var model = await WhenCallingIndexWithQ($"D={target.Month}/{target.Day}");

            // Then: Only transactions on that date or the following 7 days in the current year are returned
            var expected = items.Count(x => x.Timestamp >= target && x.Timestamp < target.AddDays(7));
            Assert.AreEqual(expected, model.Count());
            Assert.IsTrue(model.All(x => x.Timestamp >= target && x.Timestamp < target.AddDays(7)));
        }

        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataTestMethod]
        public async Task IndexQDateInteger(int day)
        {
            // Given: A mix of transactions, with differing dates
            var items = TransactionItems.Take(22);
            context.AddRange(items);
            context.SaveChanges();

            // When: Calling index with q='d=###'
            var target = new DateTime(DateTime.Now.Year, 01, day);
            var model = await WhenCallingIndexWithQ($"D={target.Month}{(target.Day<10?"0":"")}{target.Day}");

            // Then: Only transactions on that date or the following 7 days in the current year are returned
            var expected = items.Count(x => x.Timestamp >= target && x.Timestamp < target.AddDays(7));
            Assert.AreEqual(expected, model.Count());
            Assert.IsTrue(model.All(x => x.Timestamp >= target && x.Timestamp < target.AddDays(7)));
        }

        [TestMethod]
        public async Task IndexQDateText()
        {
            // Given: A mix of transactions, with differing dates
            var items = TransactionItems.Take(22);
            context.AddRange(items);
            context.SaveChanges();

            // When: Calling index with q='d=text'
            // Then: Bad request returned
            await WhenCallingIndexWithQThenBadRequest($"D=text");
        }

        [TestMethod]
        public async Task IndexQIntAny()
        {
            // Given: A mix of transactions, with differing amounts, dates, and payees
            var items = TransactionItems.Take(23);
            context.AddRange(items);
            context.SaveChanges();

            // When: Calling index with q='###'
            var model = await WhenCallingIndexWithQ($"123");

            // Then: Transactions with {###} in the memo or payee are returned AS WELL AS
            // transactions on or within a week of #/## AS WELL AS transactions with amounts
            // of ###.00 and #.##.
            var expected = 4; // I'll just tell you there's 4 of these
            Assert.AreEqual(expected, model.Count());
        }

        [TestMethod]
        public async Task IndexQIntAnyMultiple()
        {
            // Given: A mix of transactions, with differing amounts, dates, and payees
            var items = TransactionItems.Take(23);
            context.AddRange(items);
            context.SaveChanges();

            // When: Calling index with q='###,###'
            var model = await WhenCallingIndexWithQ($"123,500");

            // Then: Only matching transactions are returned
            var expected = 1; // I'll just tell you there's 1 of these
            Assert.AreEqual(expected, model.Count());
        }

        [DataRow("c=B,p=4",3)]
        [DataRow("p=2,y=2000", 2)]
        [DataRow("c=C,p=2,y=2000", 1)]
        [DataRow("m=Wut,y=2000", 1)]
        [DataRow("2,y=2000", 3)]
        [DataTestMethod]
        public async Task IndexQMany(string q, int expected)
        {
            // Given: A mix of transactions, in differing years
            // And: some with '{word}' in their category, memo, or payee and some without
            // And: some with receipts, some without
            var items = TransactionItems.Take(19);
            var yearitems = items.Skip(3).Take(5);
            int year = 2000;
            foreach(var i in yearitems)
                i.Timestamp = new DateTime(year, i.Timestamp.Month, i.Timestamp.Day);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling index q='{word},{key}={value}' in various combinations
            var model = await WhenCallingIndexWithQ(q);

            // Then: Only the transactions with '{word}' in their category, memo, or payee AND matching the supplied {key}={value} are returned
            Assert.AreEqual(expected, model.Count());
        }

        [DataRow("c=B,p=4", 3)]
        [DataRow("p=2,y=2000", 2)]
        [DataRow("c=C,p=2,y=2000", 1)]
        [DataRow("m=Wut,y=2000", 1)]
        [DataRow("2,y=2000", 3)]
        [DataTestMethod]
        public async Task DownloadQMany(string q, int expected)
        {
            // Given: A mix of transactions, in differing years
            // And: some with '{word}' in their category, memo, or payee and some without
            // And: some with receipts, some without
            var items = TransactionItems.Take(19);
            var yearitems = items.Skip(3).Take(5);
            int year = 2000;
            foreach (var i in yearitems)
                i.Timestamp = new DateTime(year, i.Timestamp.Month, i.Timestamp.Day);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Downloading transactions with q='{word},{key}={value}' in various combinations
            var result = await controller.Download(allyears:true, q: q);
            var fcresult = result as FileStreamResult;
            var stream = fcresult.FileStream;
            var model = helper.ExtractFromSpreadsheet<Transaction>(stream);

            // Then: Only the transactions with '{word}' in their category, memo, or payee AND matching the supplied {key}={value} are downloaded
            Assert.AreEqual(expected, model.Count());
        }

        [TestMethod]
        public async Task IndexQUnknown()
        {
            // When: Calling index with an unknown query key q='!=1'
            // Then: Bad Request returned
            await WhenCallingIndexWithQThenBadRequest($"!=1");
        }


        [TestMethod]
        public async Task Import()
        {
            // Given: A mix of transactions, some flagged as imported, some as not
            var noimportitems = TransactionItems.Take(10);
            var importitems = TransactionItems.Skip(10).Take(5).ToList();
            foreach(var item in importitems)
                item.Imported = true;
                
            context.Transactions.AddRange(noimportitems.Concat(importitems));
            context.SaveChanges();

            // When: Calling Import
            var result = await controller.Import();
            var viewresult = result as ViewResult;
            var model = viewresult.Model as IEnumerable<Transaction>;

            // Then: Only the imported transactions are flagged
            CollectionAssert.AreEqual(importitems,model.ToList());
        }

        [TestMethod]
        public async Task ImportHighlight()
        {
            // Given: A set of numbers
            var numbers = new List<int>() { 3, 5, 7, 27, 107 };

            // When: Calling Import with highlight set to those numbers separated by colons
            var highlight = string.Join(':', numbers.Select(x => x.ToString()));
            var result = await controller.Import(highlight:highlight);
            var viewresult = result as ViewResult;

            // Then: The "Highlight" viewdata is set to a hashset of those numbers
            CollectionAssert.AreEqual(numbers, (viewresult.ViewData["Highlight"] as HashSet<int>).ToList());
        }

        [TestMethod]
        public async Task ImportHighlightBogusIgnore()
        {
            // When: Calling Import with highlight set to letters instead of numbers (which is invalid)
            var result = await controller.Import(highlight: "A:B:C:D");
            var viewresult = result as ViewResult;

            // Then: The "Highlight" viewdata is null, and everything else works
            Assert.IsNull(viewresult.ViewData["Highlight"]);
        }
#if false
        // TODO: Move these to report page tests
        [TestMethod]
        public void Report()
        {
            // Given: Current time is 1/1/2002
            var now = new DateTime(2002, 1, 1);
            controller.Now = now;

            // When: Calling Report with no information
            var result = controller.Report(new ReportBuilder.Parameters());
            var viewresult = result as ViewResult;

            // Then: Everthing is filled in while proper defaults
            Assert.AreEqual("all", viewresult.ViewData["report"]);
            Assert.AreEqual(now.Month, viewresult.ViewData["month"]);
        }

        [TestMethod]
        public void ReportSetsYear()
        {
            // Given: Current time is 1/1/2002
            var now = new DateTime(2002, 1, 1);
            controller.Now = now;
            var year = 2000;

            // And: First calling report with a defined year
            controller.Report(new ReportBuilder.Parameters() { year = year });

            // When: Later calling report with no year
            var result = controller.Report(new ReportBuilder.Parameters());
            var viewresult = result as ViewResult;
            var model = viewresult.Model as Report;

            // Then: The year from the first call is used
            Assert.AreEqual(12, viewresult.ViewData["month"]);
            Assert.IsTrue(model.Description.Contains(year.ToString()));
        }
        [TestMethod]
        public void ReportNotFound() =>
            Assert.IsTrue(controller.Report(new ReportBuilder.Parameters() { id = "notfound" }) is Microsoft.AspNetCore.Mvc.NotFoundObjectResult);
#endif
        [TestMethod]
        public async Task CreateInitial()
        {
            var result = await controller.Create(); 
            var viewresult = result as ViewResult;
            Assert.IsNull(viewresult.Model);
        }

        [TestMethod]
        public void DownloadPartial()
        {
            // When: Calling Download Partial
            var result = controller.DownloadPartial();

            // Then: It returns an empty model
            var viewresult = result as PartialViewResult;
            Assert.IsNull(viewresult.Model);
        }

        [TestMethod]
        public async Task EditPartial()
        {
            // Given: A transaction with no category
            var transaction = Items.First();
            transaction.Category = null;
            context.Transactions.Add(transaction);

            // And: A payee which matches the category payee
            var payee = PayeeItems.First();
            context.Payees.Add(payee);
            context.SaveChanges();

            // When: Calling Edit Partial
            var result = await controller.EditModal(transaction.ID, new PayeeRepository(helper.context));
            var partial = result as PartialViewResult;
            var actual = partial.Model as Transaction;

            // Then: The expectedtransasction is returned
            Assert.AreEqual(transaction.Payee, actual.Payee);

            // And: The transaction gets the matching payee category
            Assert.AreEqual(payee.Category, actual.Category);
        }

        [TestMethod]
        public async Task EditPartialNoPayeeMatch()
        {
            // Given: A transaction with an existing category
            var transaction = Items.First();
            context.Transactions.Add(transaction);

            // And: A payee which matches the transaction payee, but has a different category
            var payee = PayeeItems.First();
            context.Payees.Add(payee);
            context.SaveChanges();

            // When: Calling Edit Partial
            var result = await controller.EditModal(transaction.ID, new PayeeRepository(helper.context));
            var partial = result as PartialViewResult;
            var actual = partial.Model as Transaction;

            // Then: The transaction DID NOT get the matching payee category
            Assert.AreNotEqual(payee.Category, actual.Category);

        }

        [TestMethod]
        public async Task EditPayeeMatch()
        {
            // Given: A transaction with no category
            var transaction = Items.First();
            transaction.Category = null;
            context.Transactions.Add(transaction);

            // And: A payee which matches the category payee
            var payee = PayeeItems.First();
            context.Payees.Add(payee);
            context.SaveChanges();

            // When: Calling Edit 
            var result = await controller.Edit(transaction.ID, new PayeeRepository(helper.context));
            var viewresult = result as ViewResult;
            var actual = viewresult.Model as Transaction;

            // Then: The expectedtransasction is returned
            Assert.AreEqual(transaction.Payee, actual.Payee);

            // And: The transaction gets the matching payee category
            Assert.AreEqual(payee.Category, actual.Category);
        }

        [TestMethod]
        public async Task ReceiptActionOther() =>
            Assert.IsTrue(await controller.ReceiptAction(1,string.Empty) is RedirectToActionResult);
    }
}
