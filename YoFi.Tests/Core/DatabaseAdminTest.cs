using Common.DotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class DatabaseAdminTest
    {
        private DatabaseAdministration dbadmin;
        private Mock<IDataContext> context;
        private Mock<IClock> clock;

        [TestInitialize]
        public void SetUp()
        {
            context = new Mock<IDataContext>();
            context.Setup(x=>x.ClearAsync<BudgetTx>());
            context.Setup(x=>x.ClearAsync<Transaction>());
            context.Setup(x=>x.ClearAsync<Payee>());
            clock = new Mock<IClock>();
            dbadmin  = new DatabaseAdministration(context.Object,clock.Object);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(dbadmin);
        }

        [TestMethod]
        public async Task ClearBudgetTx()
        {
            // When: Clearing budgettx
            await dbadmin.ClearDatabaseAsync("budget");

            // Then: The context received a call to ClearAsync
            context.Verify(x=>x.ClearAsync<BudgetTx>(), Times.Once());
            context.Verify(x=>x.ClearAsync<Transaction>(), Times.Never());
            context.Verify(x=>x.ClearAsync<Payee>(), Times.Never());
        } 

        [TestMethod]
        public async Task ClearPayees()
        {
            // When: Clearing payees
            await dbadmin.ClearDatabaseAsync("payee");

            // Then: The context received a call to ClearAsync
            context.Verify(x=>x.ClearAsync<Payee>(), Times.Once());
            context.Verify(x=>x.ClearAsync<Transaction>(), Times.Never());
            context.Verify(x=>x.ClearAsync<BudgetTx>(), Times.Never());
        } 

        [TestMethod]
        public async Task ClearTransactions()
        {
            // When: Clearing transactions
            await dbadmin.ClearDatabaseAsync("tx");

            // Then: The context received a call to ClearAsync
            context.Verify(x=>x.ClearAsync<Transaction>(), Times.Once());
            context.Verify(x=>x.ClearAsync<Payee>(), Times.Never());
            context.Verify(x=>x.ClearAsync<BudgetTx>(), Times.Never());
        } 

        [TestMethod]
        public async Task ClearBogus()
        {
            // When: Sending bogus id to clear database
            await dbadmin.ClearDatabaseAsync("bogus-1234");

            // Then: The context received no clear calls
            context.Verify(x=>x.ClearAsync<Transaction>(), Times.Never());
            context.Verify(x=>x.ClearAsync<Payee>(), Times.Never());
            context.Verify(x=>x.ClearAsync<BudgetTx>(), Times.Never());
        } 
    }
}