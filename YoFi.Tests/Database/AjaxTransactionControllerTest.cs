using Common.DotNet.Test;
using Common.NET.Test;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Data;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Database
{
    [TestClass]
    public class AjaxTransactionControllerTest
    {

        private AjaxTransactionController controller;
        private ITransactionRepository repository;
        private ApplicationDbContext context;

        async Task AddFive()
        {
            await repository.AddRangeAsync
            (
                new List<Transaction>()
                {
                    new Transaction() { Category = "BB:AA", Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m },
                    new Transaction() { Category = "AA:AA", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "CC:AA", Payee = "5", Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Amount = 300m },
                    new Transaction() { Category = "BB:AA", Payee = "1", Timestamp = new DateTime(DateTime.Now.Year, 01, 05), Amount = 400m },
                    new Transaction() { Category = "BB:AA", Payee = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m },
                }
            );
        }

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                //.UseLoggerFactory(logfact)
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);
            var storage = new TestAzureStorage();
            repository = new TransactionRepository(context, storage);
            controller = new AjaxTransactionController(repository);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Didn't actually solve anything. Keep it around for possible future problem
            //DetachAllEntities();

            // https://stackoverflow.com/questions/33490696/how-can-i-reset-an-ef7-inmemory-provider-between-unit-tests
            context?.Database.EnsureDeleted();
            context = default;
            controller = default;
            repository = default;
        }

        [TestMethod]
        public async Task UpReceipt()
        {
            await AddFive();
            var original = repository.All.Last();

            // Create a formfile with it
            var contenttype = "text/html";
            var count = 10;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, count).ToArray());
            var file = new FormFile(stream, 0, count, "Index", $"Index.html") { Headers = new HeaderDictionary(), ContentType = contenttype };

            var actionresult = await controller.UpReceipt(original.ID, file);

            Assert.That.IsOfType<OkResult>(actionresult);
            Assert.AreEqual(original.ID.ToString(), original.ReceiptUrl);
        }

        [TestMethod]
        public async Task UpReceiptAgainFails()
        {
            await AddFive();
            var original = repository.All.Last();

            // Create a formfile with it
            var contenttype = "text/html";
            var count = 10;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, count).ToArray());
            var file = new FormFile(stream, 0, count, "Index", $"Index.html") { Headers = new HeaderDictionary(), ContentType = contenttype };

            await controller.UpReceipt(original.ID, file);
            var actionresult = await controller.UpReceipt(original.ID, file);
            var notfoundresult = Assert.That.IsOfType<BadRequestObjectResult>(actionresult);
            Assert.That.IsOfType<string>(notfoundresult.Value);
        }
    }
}
