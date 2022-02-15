using Common.AspNet.Test;
using Common.DotNet;
using Common.DotNet.Test;
using Common.NET.Test;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Data;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using Transaction = YoFi.Core.Models.Transaction;

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
            clock = new TestClock(); // Use DateTime now for now
            _repository = new TransactionRepository(helper.context, clock, storage: storage);
            helper.controller = new TransactionsController(_repository, clock);
            helper.Items.AddRange(TransactionItems.Take(5));
            helper.dbset = helper.context.Transactions;

            // Sample data items will use Payee name as a unique sort idenfitier
            helper.KeyFor = (x => x.Payee);
        }

        [TestCleanup]
        public void Cleanup() => helper.Cleanup();

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
