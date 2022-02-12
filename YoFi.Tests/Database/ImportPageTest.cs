using Common.AspNet.Test;
using Common.DotNet;
using Common.DotNet.Test;
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
using YoFi.Core.SampleGen;
using YoFi.Tests.Helpers;

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
        private UniversalImporter importer;
        private Mock<IAuthorizationService> authservice;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContextInMemory(options);
            dbset = context.Transactions;

            // By default it's 2021, which is the year all our sample data is generated for
            clock = new TestClock() { Now = new System.DateTime(2021, 1, 1) };

            var storage = new TestAzureStorage();

            repository = new TransactionRepository(context, storage: storage);
            importer = new UniversalImporter(new AllRepositories(repository, new BudgetTxRepository(context), new PayeeRepository(context)));

            authservice = new Mock<IAuthorizationService>();
            AuthorizationResult result = AuthorizationResult.Success();
            authservice.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>())).Returns(Task.FromResult(result));

            var dir = Environment.CurrentDirectory + "/SampleData";
            var config = new Mock<ISampleDataConfiguration>();
            config.Setup(x => x.Directory).Returns(dir);

            var loader = new SampleDataLoader(context, clock, config.Object);

            page = new ImportModel(repository, authservice.Object, loader);
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
            var result = await page.OnPostUploadAsync(new List<IFormFile>() { file }, importer);

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

        static public IFormFile PrepareUpload<X>(IEnumerable<X> what) where X : class
        {
            // Build a spreadsheet with the chosen number of items
            // Note that we are not disposing the stream. User of the file will do so later.
            var stream = new MemoryStream();
            using (var ssr = new SpreadsheetWriter())
            {
                ssr.Open(stream);
                ssr.Serialize(what);
            }

            // Create a formfile with it
            var filename = $"{typeof(X).Name}s";
            stream.Seek(0, SeekOrigin.Begin);
            IFormFile file = new FormFile(stream, 0, stream.Length, filename, $"{filename}.xlsx");

            return file;
        }

        [TestMethod]
        public async Task UploadError()
        {
            var actionresult = await page.OnPostUploadAsync(null, null);
            Assert.That.IsOfType<PageResult>(actionresult);

            Assert.IsNotNull(page.Error);
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


        //
        // User Story 1177: [User Can] Import Payees or BudgetTx from main Import page
        //

        [TestMethod]
        public async Task BudgetImportLots()
        {
            // Given: An XLSX file with TOO MANY budget transactions in a sheet called "BudgetTx"
            var howmany = 50;
            var lotsofbtx = Enumerable.Range(1, howmany).Select(x => new BudgetTx() { Timestamp = clock.Now + TimeSpan.FromDays(x), Category = x.ToString() });
            var file = PrepareUpload(lotsofbtx);

            // When: Uploading it
            var actionresult = await page.OnPostUploadAsync(new List<IFormFile>() { file }, importer);

            // Then: All items are imported successfully
            Assert.AreEqual(ImportModel.MaxOtherItemsToShow, page.BudgetTxs.Count());

            // And: There is an indicator that more are available
            Assert.AreEqual(howmany, page.NumBudgetTxsUploaded);
        }

        [TestMethod]
        public async Task PayeeImportLots()
        {
            // Given: An XLSX file with TOO MANY payees in a sheet called "payees"
            var howmany = 50;
            var lotsofpayees = Enumerable.Range(1, howmany).Select(x => new Payee() { Name = x.ToString(), Category = x.ToString() });
            var file = PrepareUpload(lotsofpayees);

            // When: Uploading it
            var actionresult = await page.OnPostUploadAsync(new List<IFormFile>() { file }, importer);

            // Then: Only the first few are shown
            Assert.AreEqual(ImportModel.MaxOtherItemsToShow, page.Payees.Count());

            // And: There is an indicator that more are available
            Assert.AreEqual(howmany, page.NumPayeesUploaded);
        }

        //
        // User Story 1178: [User Can] Import spreadsheets with all data types in a single spreadsheet from the main Import page
        //

        [TestMethod]
        public async Task AllDataTypes()
        {
            // Given: An XLSX file with all four types of data in sheets named for their type
            var filename = "Test-Generator-GenerateUploadSampleData.xlsx";
            var stream = SampleData.Open(filename);
            var length = stream.Length;
            IFormFile file = new FormFile(stream, 0, length, filename, filename);

            // When: Uploading it
            var actionresult = await page.OnPostUploadAsync(new List<IFormFile>() { file }, importer);

            // Then: All items are imported successfully
            Assert.AreEqual(25, dbset.Count());
            Assert.AreEqual(12, dbset.Count(x=>x.Splits.Count > 0));

            Assert.AreEqual(3, context.Payees.Count());
            Assert.AreEqual(4, context.BudgetTxs.Count());
        }

        [TestMethod]
        public async Task GetSampleOfferings()
        {
            // When: Getting the page
            var actionresult = await page.OnGetAsync();

            // Then: There are the expected amount of sample offerings
            Assert.IsTrue(page.Offerings.Count() >= 27);
        }

        [TestMethod]
        public async Task DownloadAllSamples()
        {
            // Given: Already got the page, so we have the offerings populated
            await page.OnGetAsync();

            foreach(var offering in page.Offerings)
            {
                // When: Downloading each offering
                var actionresult = await page.OnGetSampleAsync(offering.ID);
                var fsresult = Assert.That.IsOfType<FileStreamResult>(actionresult);
                Assert.IsTrue(fsresult.FileStream.Length > 1000);

                // Then: The file downloads successfully
                var dir = TestContext.FullyQualifiedTestClassName + "." + TestContext.TestName;
                Directory.CreateDirectory(dir);
                var filename = dir + "/" + fsresult.FileDownloadName;
                File.Delete(filename);
                using (var outstream = File.OpenWrite(filename))
                {
                    await fsresult.FileStream.CopyToAsync(outstream);
                }
                TestContext.AddResultFile(filename);
            }
        }

        [TestMethod]
        public async Task DownloadBogusSample_BadRequest()
        {
            // When: Trying to download an offering that doesn't exist
            var actionresult = await page.OnGetSampleAsync("bogus-1234");
            Assert.That.IsOfType<BadRequestResult>(actionresult);
        }

        [TestMethod]
        public async Task PostGo_AccessDenied()
        {
            AuthorizationResult result = AuthorizationResult.Failed();
            authservice.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>())).Returns(Task.FromResult(result));

            var actionresult = await page.OnPostGoAsync("ok");
            var rdresult = Assert.That.IsOfType<RedirectToPageResult>(actionresult);
            Assert.AreEqual("/Account/AccessDenied", rdresult.PageName);
        }

        [TestMethod]
        public async Task PostUpload_AccessDenied()
        {
            AuthorizationResult result = AuthorizationResult.Failed();
            authservice.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>())).Returns(Task.FromResult(result));

            var actionresult = await page.OnPostUploadAsync(null, null);
            var rdresult = Assert.That.IsOfType<RedirectToPageResult>(actionresult);
            Assert.AreEqual("/Account/AccessDenied", rdresult.PageName);
        }
    }
}
