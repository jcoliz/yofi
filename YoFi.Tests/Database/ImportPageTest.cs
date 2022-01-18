using Common.AspNet.Test;
using Common.DotNet;
using Common.DotNet.Test;
using Common.EFCore;
using Common.NET.Test;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Data;
using YoFi.AspNet.Pages;
using YoFi.Core.Importers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Database
{
    [TestClass]
    public class ImportPageTest
    {
        private ApplicationDbContext context;
        private TestClock clock;
        private ImportModel page;
        private TransactionRepository repository;
        private DbSet<Transaction> dbset;

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);
            dbset = context.Transactions;

            // By default it's 2021, which is the year all our sample data is generated for
            clock = new TestClock() { Now = new System.DateTime(2021, 1, 1) };

            var qex = new EFCoreAsyncQueryExecution();
            var storage = new TestAzureStorage();

            repository = new TransactionRepository(context, qex, storage: storage);

            var authservice = new Mock<IAuthorizationService>();
            AuthorizationResult result = AuthorizationResult.Success();
            authservice.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>())).Returns(Task.FromResult(result));

            page = new ImportModel(repository, qex, authservice.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Didn't actually solve anything. Keep it around for possible future problem
            //DetachAllEntities();

            // https://stackoverflow.com/questions/33490696/how-can-i-reset-an-ef7-inmemory-provider-between-unit-tests
            context?.Database.EnsureDeleted();
            context = null;
        }

        public async Task<IActionResult> DoUpload(ICollection<Transaction> what)
        {
            // Make an HTML Form file containg a spreadsheet.
            var file = ControllerTestHelper<Transaction, AspNet.Controllers.TransactionsController>.PrepareUpload(what);

            // Upload that
            var result = await page.OnPostUploadAsync(new List<IFormFile>() { file }, new TransactionImporter(new AllRepositories( repository, new BudgetTxRepository(context), new PayeeRepository(context))));

            return result;
        }

        public async Task AddFiveItems()
        {
            // context.AddRange(Items) doesn't work :(
            foreach (var item in Items)
                context.Add(item);

            await context.SaveChangesAsync();
        }

        public async Task Add(IEnumerable<Transaction> items)
        {
            await context.AddRangeAsync(items);
            await context.SaveChangesAsync();
        }

        private List<Transaction> Items => TransactionControllerTest.TransactionItems.Take(5).ToList();
        private IEnumerable<Transaction> TransactionItems => TransactionControllerTest.TransactionItems;
        private List<Payee> PayeeItems => TransactionControllerTest.PayeeItems;

        [TestMethod]
        public async Task Upload()
        {
            // Can't use the helper's upload bevause Transaction upload does not return the uploaded items.
            var result = await DoUpload(Items);

            // Test the status
            var actual = Assert.That.IsOfType<PageResult>(result);

            // Check the items on the page
            Assert.AreEqual(Items.Count, page.Transactions.Count());

            // Now check the state of the DB
            Assert.AreEqual(Items.Count, dbset.Count());
        }

        [TestMethod]
        public async Task Import()
        {
            // Given: A mix of transactions, some flagged as imported, some as not
            var noimportitems = TransactionItems.Take(10);
            var importitems = TransactionItems.Skip(10).Take(5).ToList();
            foreach (var item in importitems)
                item.Imported = true;

            context.Transactions.AddRange(noimportitems.Concat(importitems));
            context.SaveChanges();

            // When: Calling Import
            var result = await page.OnGetAsync();

            // Then: Page result
            var viewresult = Assert.That.IsOfType<PageResult>(result);

            // Then: Only the imported transactions are shown
            Assert.IsTrue(importitems.SequenceEqual(page.Transactions));
        }

        [TestMethod]
        public async Task Highlights()
        {
            // Given: Uploading a set of transactions once, successfully
            var originalitems = TransactionItems.Skip(10).Take(5);
            await DoUpload(originalitems.ToList());
            await page.OnPostGoAsync("ok");

            // And: Having made subtle changes to the transactions
            foreach (var t in dbset)
                t.Timestamp += TimeSpan.FromDays(10);
            await context.SaveChangesAsync();

            var excpectedduplicates = dbset.Select(x => x.ID).ToHashSet();

            // When: Uploading more transactions, which includes the original transactions
            await DoUpload(TransactionItems.Take(15).ToList());

            // Then: The overlapping new transactions are highlighted and deselected, indicating that they
            // are probably duplicates
            var selected = dbset.Where(x => x.Selected == true).ToList();
            Assert.AreEqual(10, selected.Count);
            Assert.AreEqual(5, page.Highlights.Count);
        }

        [TestMethod]
        public async Task ImportOk()
        {
            // Given: As set of items, some with imported & selected flags, some with not
            var notimported = 1; // How many should remain?
            var items = Items.Take(5);
            foreach (var item in items.Skip(notimported))
                item.Imported = item.Selected = true;
            await Add(items);

            // When: Approving the import
            var result = await page.OnPostGoAsync("ok");
            Assert.That.IsOfType<RedirectToActionResult>(result);

            // Then: All items remain, none have imported flag
            Assert.AreEqual(5, dbset.Count());
            Assert.AreEqual(0, dbset.Where(x => x.Imported == true).Count());
        }

        [TestMethod]
        public async Task ImportOkSelected()
        {
            // Given: As set of items, all with imported some selected, some not
            var items = Items.Take(5);
            foreach (var item in items)
                item.Imported = true;

            var imported = 2; // How many should remain?
            foreach (var item in items.Take(imported))
                item.Selected = true;

            await Add(items);

            // When: Approving the import
            var result = await page.OnPostGoAsync("ok");

            // Then: Only selected items remain
            Assert.AreEqual(imported, dbset.Count());
        }

        [TestMethod]
        public async Task ImportCancel()
        {
            // Given: As set of items, some with imported flag, some with not
            var expected = 1; // How many should remain?
            var items = Items.Take(5);
            foreach (var item in items.Skip(expected))
                item.Imported = true;
            await Add(items);

            // When: Cancelling the import
            var result = await page.OnPostGoAsync("cancel");
            Assert.That.IsOfType<RedirectToPageResult>(result);

            // Then: Only items without imported flag remain
            Assert.AreEqual(expected, dbset.Count());
        }

        [DataRow(null)]
        [DataRow("Bogus")]
        [DataTestMethod]
        public async Task ImportWrong(string command)
        {
            // Given: As set of items, some with imported & selected flags, some with not
            var notimported = 1; // How many should remain?
            var items = Items.Take(5);
            foreach (var item in items.Skip(notimported))
                item.Imported = item.Selected = true;
            await Add(items);

            // When: Sending the import an empty command
            var result = await page.OnPostGoAsync(command);

            // Then: Bad request
            Assert.That.IsOfType<BadRequestResult>(result);

            // Then: No change to db
            Assert.AreEqual(5, dbset.Count());
            Assert.AreEqual(4, dbset.Where(x => x.Imported == true).Count());
        }

        [TestMethod]
        public async Task Bug839()
        {
            // Bug 839: Imported items are selected automatically :(
            await DoUpload(Items);

            await page.OnPostGoAsync("ok");

            var numimported = await dbset.Where(x => x.Imported == true).CountAsync();
            var numselected = await dbset.Where(x => x.Selected == true).CountAsync();

            Assert.AreEqual(0, numimported);
            Assert.AreEqual(0, numselected);
        }

        [TestMethod]
        public async Task Bug883()
        {
            // Bug 883: Apparantly duplicate transactions in import are (incorrectly) coalesced to single transaction for input

            var uploadme = new List<Transaction>() { Items[0], Items[0] };

            // Then upload that
            await DoUpload(uploadme);

            Assert.AreEqual(2, dbset.Count());
        }

        [TestMethod]
        public async Task UploadDuplicate()
        {
            // Start out with one item in the DB. We are picking the ONE item that Upload doesn't upload.
            var items = Items.Take(5).ToList();
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
            var actual = Assert.That.IsOfType<PageResult>(result);

            // What's expected here? The new item IS uploaded, BUT it's deselected, where the rest are selected, right?
            var indexmany = await dbset.OrderBy(x => x.Payee).ToListAsync();

            Assert.AreEqual(1 + Items.Count(), indexmany.Count);

            // We expect that FIVE items were imported.

            var wasimported = dbset.Where(x => true == x.Imported);

            Assert.AreEqual(5, wasimported.Count());

            // Out of those, we expect that ONE is not selected, because it's a dupe of the initial.

            var notimported = wasimported.Where(x => false == x.Selected).Single();

            Assert.AreEqual(initial, notimported);
        }

        [TestMethod]
        public async Task UploadWithID()
        {
            // Can't use the helper's upload bevause Transaction upload does not return the uploaded items.

            // Start out with one item in the DB. We are picking the ONE item that Upload doesn't upload.
            var items = Items.Take(5).ToList();
            var expected = items[4];
            context.Add(expected);
            await context.SaveChangesAsync();

            // One of the new items has an overlapping ID, but will be different in every way. We expect that
            // The end result is that the database will == items
            items[0].ID = expected.ID;

            // Just upload the first four items. The fifth, we already did above
            // Can't use the helper's upload bevause Transaction upload does not return the uploaded items.
            var result = await DoUpload(items.Take(4).ToList());
            var actual = Assert.That.IsOfType<PageResult>(result);

            // From here we can just use the Index test, but not add items. There should be the proper "items"
            // all there now.

            var indexmany = await dbset.OrderBy(x => x.Payee).ToListAsync();

            // Sort the original items by Key
            items.Sort((x, y) => x.Payee.CompareTo(y.Payee));

            // Test that the resulting items are in the same order
            Assert.IsTrue(indexmany.SequenceEqual(items));
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

            foreach (var tx in context.Transactions)
            {
                var expectedpayee = context.Payees.Where(x => x.Name == tx.Payee).Single();
                Assert.AreEqual(expectedpayee.Category, tx.Category);
            }
        }

        [TestMethod]
        public async Task Bug880()
        {
            // Bug 880: Import applies substring matches (incorrectly) before regex matches

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
            var result = await page.OnPostUploadAsync(new List<IFormFile>() { file }, new TransactionImporter( new AllRepositories( repository, new BudgetTxRepository(context), new PayeeRepository(context))));
            Assert.That.IsOfType<PageResult>(result);

            // Did the transaction and splits find each other?
            var actual = context.Transactions.Include(x => x.Splits).Single();
            Assert.AreEqual(2, actual.Splits.Count);

            // The transaction should NOT have a category anymore, even if it had one to begin with
            Assert.IsNull(actual.Category);
        }

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
            var payeeRepository = new PayeeRepository(context);
            await payeeRepository.AddAsync(new Payee() { Name = payee, Category = rule });

            // When: Importing an OFX file containing a transaction with payee {name}
            var filename = "User-Story-802.ofx";
            var stream = SampleData.Open(filename);
            var length = stream.Length;
            IFormFile file = new FormFile(stream, 0, length, filename, filename);
            var actionresult = await page.OnPostUploadAsync(new List<IFormFile>() { file }, new TransactionImporter( new AllRepositories(repository, new BudgetTxRepository(context), payeeRepository)));

            Assert.That.IsOfType<PageResult>(actionresult);

            // Then: All transactions are imported successfully
            Assert.AreEqual(1, dbset.Count());

            // And: The transaction is imported as a split
            var txs = dbset.Include(x => x.Splits).ToList();
            Assert.AreEqual(2, txs.Single().Splits.Count);

            // And: The splits match the categories and amounts as expected from the {loan details} 
            var principal = -891.34m;
            var interest = -796.37m;
            Assert.AreEqual(principal, txs.Single().Splits.Where(x => x.Category == principalcategory).Single().Amount);
            Assert.AreEqual(interest, txs.Single().Splits.Where(x => x.Category == interestcategory).Single().Amount);
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
            var actionresult = await page.OnPostUploadAsync(new List<IFormFile>() { file }, new TransactionImporter( new AllRepositories( repository, new BudgetTxRepository(context), new PayeeRepository(context))));

            // Then: All transactions are imported successfully
            Assert.That.IsOfType<PageResult>(actionresult);

            Assert.AreEqual(expected, dbset.Count());
        }
    }
}
