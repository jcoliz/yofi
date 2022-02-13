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
