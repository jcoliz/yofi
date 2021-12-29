using Common.DotNet.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Data;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Database
{
    [TestClass]
    public class AjaxBudgetTxControllerTest
    {
        private AjaxBudgetController controller;
        private IBudgetTxRepository repository;
        private ApplicationDbContext context;

        Task AddFive() => AddFive(repository);

        public static async Task AddFive(IRepository<BudgetTx> __repository)
        {
            await __repository.AddRangeAsync
            (
                new List<BudgetTx>()
                {
                    new BudgetTx() { Category = "Y", Amount = 3 },
                    new BudgetTx() { Category = "X", Amount = 2 },
                    new BudgetTx() { Category = "Z", Amount = 5 },
                    new BudgetTx() { Category = "X", Amount = 1 },
                    new BudgetTx() { Category = "Y", Amount = 4 }
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
            repository = new BudgetTxRepository(context);
            controller = new AjaxBudgetController(repository);
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
    }
}
