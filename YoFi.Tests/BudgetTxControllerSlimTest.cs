using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Core.Repositories;
using YoFi.AspNet.Models;

namespace YoFi.Tests.Controllers.Slim
{
    /// <summary>
    /// Test the budget tx controller in the slimmest way possible
    /// </summary>
    /// <remarks>
    /// This variation of controller test mocks out the underlying repository
    /// </remarks>
    [TestClass]
    public class BudgetTxControllerSlimTest
    {
        BudgetTxsController controller;
        MockBudgetTxRepository repository;

        [TestInitialize]
        public void SetUp()
        {
            repository = new MockBudgetTxRepository();
            controller = new BudgetTxsController(repository);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public async Task IndexEmpty()
        {
            // Given: Empty repository

            // When: Fetching the index
            var actionresult = await controller.Index();

            // Then: View is returned
            var viewresult = Assert.That.IsType<ViewResult>(actionresult);

            // And: Correct kind of model is returned 
            var model = Assert.That.IsType<IEnumerable<BudgetTx>>(viewresult.Model);

            // And: Model is empty
            Assert.AreEqual(0, model.Count());
        }
    }

    internal static class MyAssert
    {
        public static T IsType<T>(this Assert assert, object actual) where T: class
        {
            if (actual is T)
                return actual as T;

            throw new AssertFailedException();
        }
    }

    class MockBudgetTxRepository : IRepository<BudgetTx>
    {
        public IQueryable<BudgetTx> All => throw new System.NotImplementedException();

        public IQueryable<BudgetTx> OrderedQuery => throw new System.NotImplementedException();

        public Task AddAsync(BudgetTx item)
        {
            throw new System.NotImplementedException();
        }

        public Task AddRangeAsync(IEnumerable<BudgetTx> items)
        {
            throw new System.NotImplementedException();
        }

        public Stream AsSpreadsheet()
        {
            throw new System.NotImplementedException();
        }

        public IQueryable<BudgetTx> ForQuery(string q)
        {
            return Enumerable.Empty<BudgetTx>().AsQueryable<BudgetTx>();
        }

        public Task<BudgetTx> GetByIdAsync(int? id)
        {
            throw new System.NotImplementedException();
        }

        public Task RemoveAsync(BudgetTx item)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> TestExistsByIdAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateAsync(BudgetTx item)
        {
            throw new System.NotImplementedException();
        }
    }
}
