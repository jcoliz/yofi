using Common.DotNet.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
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
            repository = new TransactionRepository(context);
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
        public async Task Select()
        {
            await AddFive();
            var expected = repository.All.First();

            var actionresult = await controller.Select(expected.ID, true);

            Assert.That.IsOfType<OkResult>(actionresult);

            Assert.IsTrue(true == expected.Selected);
        }

        [TestMethod]
        public async Task Deselect()
        {
            await AddFive();
            var expected = repository.All.First();
            expected.Selected = true;
            await repository.UpdateAsync(expected);

            var actionresult = await controller.Select(expected.ID, false);

            Assert.That.IsOfType<OkResult>(actionresult);
            Assert.IsTrue(false == expected.Selected);
        }
        [TestMethod]
        public async Task Hide()
        {
            await AddFive();
            var expected = repository.All.First();
            await repository.UpdateAsync(expected);

            var actionresult = await controller.Hide(expected.ID, true);

            Assert.That.IsOfType<OkResult>(actionresult);
            Assert.IsTrue(true == expected.Hidden);
        }
        [TestMethod]
        public async Task Show()
        {
            await AddFive();
            var expected = repository.All.First();
            expected.Hidden = true;
            await repository.UpdateAsync(expected);

            var actionresult = await controller.Hide(expected.ID, false);

            Assert.That.IsOfType<OkResult>(actionresult);
            Assert.IsTrue(false == expected.Hidden);
        }

        [TestMethod]
        public async Task Edit()
        {
            await AddFive();
            var original = repository.All.First();

            // detach the original so we have an unmodified copy around
            context.Entry(original).State = EntityState.Detached;

            var newitem = new Transaction() { ID = original.ID, Payee = "I have edited you!", Category = original.Category, Memo = original.Memo };

            var actionresult = await controller.Edit(original.ID, newitem);

            var objresult = Assert.That.IsOfType<ObjectResult>(actionresult);
            var itemresult = Assert.That.IsOfType<Transaction>(objresult.Value);
            Assert.AreEqual(newitem.Payee, itemresult.Payee);
            Assert.AreNotEqual(original, itemresult);

            var actual = await repository.All.Where(x => x.ID == original.ID).SingleAsync();
            Assert.AreEqual(newitem.Payee, actual.Payee);
            Assert.AreNotEqual(original.Payee, actual.Payee);
        }
    }
}
