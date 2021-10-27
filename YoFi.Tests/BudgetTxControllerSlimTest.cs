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
            throw new System.NotImplementedException();
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
