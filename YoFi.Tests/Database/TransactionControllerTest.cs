using Common.AspNet;
using Common.AspNet.Test;
using Common.DotNet.Test;
using Common.NET.Test;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Data;
using YoFi.Core.Importers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using Dto = YoFi.Core.Models.Transaction; // YoFi.AspNet.Controllers.TransactionsIndexPresenter.TransactionIndexDto;
using Transaction = YoFi.Core.Models.Transaction;
using Common.DotNet;
using YoFi.Core.Repositories.Wire;

namespace YoFi.Tests.Database
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

        TestClock clock;

        public TestContext TestContext { get; set; }

        private int PageSize => BaseRepository<Payee>.DefaultPageSize;


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

        public static List<Payee> PayeeItems => new List<Payee>()
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



        ITransactionRepository _repository;

        [TestInitialize]
        public void SetUp()
        {
            helper = new ControllerTestHelper<Transaction, TransactionsController>();
            helper.SetUp();
            storage = new TestAzureStorage();
            _repository = new TransactionRepository(helper.context, storage: storage);
            clock = new TestClock(); // Use DateTime now for now
            helper.controller = new TransactionsController(_repository, clock);
            helper.Items.AddRange(TransactionItems.Take(5));
            helper.dbset = helper.context.Transactions;

            // Sample data items will use Payee name as a unique sort idenfitier
            helper.KeyFor = (x => x.Payee);
        }

        [TestCleanup]
        public void Cleanup() => helper.Cleanup();
        [TestMethod]
        public void Empty() => helper.Empty();

        // TODO: Rebuild this test not using helper method
        //[TestMethod]
        //public async Task IndexEmpty() => await helper.IndexEmpty<Dto>();
        [TestMethod]
        public async Task IndexSingle()
        {
            var expected = Items[0];

            context.Add(expected);
            await context.SaveChangesAsync();

            var result = await controller.Index();
            var viewresult = result as ViewResult;
            var model = viewresult.Model as TransactionsIndexPresenter;

            Assert.AreEqual(1, model.Items.Count());
            Assert.IsTrue(model.Items.Single().Equals(expected));
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
            var model = viewresult.Model as TransactionsIndexPresenter;

            Assert.AreEqual(1, model.Items.Count());
            var actual = model.Items.Single();
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
            var model = viewresult.Model as TransactionsIndexPresenter;

            Assert.AreEqual(1, model.Items.Count());
            var actual = model.Items.Single();
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

#if false
        [TestMethod]
        public async Task UploadSplitsForTransaction()
        {
            // Don't add the splits here, we'll upload them
            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m };

            context.Transactions.Add(item);
            context.SaveChanges();

            // TODO: Need a new way to prepare an upload of splits
            // Make an HTML Form file containg a spreadsheet containing those splits
            var file = ControllerTestHelper<Split, SplitsController>.PrepareUpload(SplitItems.Take(2).ToList());

            // Upload that
            var result = await controller.UpSplits(new List<IFormFile>() { file }, item.ID, new SplitImporter(_repository));
            var redir = result as RedirectToActionResult;

            Assert.IsNotNull(redir);
            Assert.IsTrue(item.HasSplits);
            Assert.IsTrue(item.IsSplitsOK);
        }
#endif

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
        public async Task Bug1172()
        {
            // Bug 1172: [Production Bug] Download transactions with splits overfetches

            // Transactions Index
            // Search for "p=mort"
            // Actions > Export > Current Year
            // Load downloaded file in Excel
            // Notice that the transactions page correctly includes only the transactions from the search results
            // Notice that the splits tab additionally includes paycheck splits too, which are not related to the transactions

            // Originally I thought that this bug can be triggered at the repository level, however, it seems to be tied with
            // relational DB behaviour of splits in transactions. So moving it here for noe.

            // Given: Two transactions, each with two different splits
            var transactions = new List<Transaction>()
            {
                new Transaction()
                {
                    Payee = "One", Amount = 100m, Timestamp = new DateTime(DateTime.Now.Year,1,1), 
                    Splits = new List<Split>()
                    {
                        new Split() { Category = "A:1", Amount = 25m },
                        new Split() { Category = "A:2", Amount = 75m },
                    }
                },
                new Transaction()
                {
                    Payee = "Two", Amount = -500m , Timestamp = new DateTime(DateTime.Now.Year,1,1), 
                    Splits = new List<Split>()
                    {
                        new Split() { Category = "B:1", Amount = -100m, Memo = string.Empty },
                        new Split() { Category = "B:2", Amount = -400m, Memo = string.Empty },
                    }
                },
            };

            context.Transactions.AddRange(transactions);
            context.SaveChanges();

            // When: Downloading a spreadsheet for just one of the transactions
            var selected = transactions.First();
            var result = await controller.Download(allyears: true, q: selected.Payee);
            var fcresult = result as FileStreamResult;
            var stream = fcresult.FileStream;
            var model = helper.ExtractFromSpreadsheet<Split>(stream);

            // Then: The spreadsheet includes splits for only the selected transaction
            Assert.AreEqual(2, model.Count());
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

        public static (IEnumerable<Transaction> items, IEnumerable<Transaction> moditems) GivenItems(int numitems, int nummoditems, Action<Transaction> action)
        {
            var items = TransactionItems.Take(numitems);
            var moditems = items.Take(nummoditems);
            foreach (var i in moditems)
                action(i);

            return (items, moditems);
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
            var model = viewresult.Model as TransactionsIndexPresenter;

            return model.Items;
        }

        async Task<IEnumerable<Dto>> WhenCallingIndexWithQ(string q)
        {
            var result = await controller.Index(q: q);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as TransactionsIndexPresenter;

            return model.Items;
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
            var model = viewresult.Model as TransactionsIndexPresenter;

            return model.Items;
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
            // Somehow this test starts with an extra item in the DB. Not sure how!!
            if (context.Transactions.Any())
            {
                context.RemoveRange(context.Transactions);
                await context.SaveChangesAsync();
            }

            Assert.AreEqual(0, context.Transactions.Count());

            // Given: A mix of transactions, some with splits, some without; some with '{word}' in their category, memo, or payee, or splits category and some without
            var items = TransactionItems.Take(20);
            items.First().Splits = SplitItems.Take(2).ToList();
            context.Transactions.AddRange(items);
            await context.SaveChangesAsync();

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
            Assert.IsInstanceOfType(viewresult.Model,typeof(int));
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

        [TestMethod]
        public void Error()
        {         
            var expected = "Bah, humbug!";
            var httpcontext = new DefaultHttpContext() { TraceIdentifier = expected };
            controller.ControllerContext.HttpContext = httpcontext;
            var actionresult = controller.Error();
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<ErrorViewModel>(viewresult.Model);
            Assert.AreEqual(expected, model.RequestId);
        }
    }
}
