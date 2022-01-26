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
            context.Setup(x=>x.Transactions).Returns(Enumerable.Empty<Transaction>().AsQueryable());
            context.Setup(x=>x.Payees).Returns(Enumerable.Empty<Payee>().AsQueryable());
            context.Setup(x=>x.BudgetTxs).Returns(Enumerable.Empty<BudgetTx>().AsQueryable());
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

        [TestMethod]
        public async Task GetDatabaseStatus_NotEmpty()
        {
            // Given: Database is not empty
            context.Setup(x=>x.CountAsync<Transaction>(It.IsAny<IQueryable<Transaction>>())).Returns(Task.FromResult(10));
            context.Setup(x=>x.CountAsync<Payee>(It.IsAny<IQueryable<Payee>>())).Returns(Task.FromResult(20));
            context.Setup(x=>x.CountAsync<BudgetTx>(It.IsAny<IQueryable<BudgetTx>>())).Returns(Task.FromResult(30));
            dbadmin  = new DatabaseAdministration(context.Object,clock.Object);

            // When: Asking for database status
            var status = await dbadmin.GetDatabaseStatus();

            // Then: The values are as expected
            Assert.AreEqual(10,status.NumTransactions);
            Assert.AreEqual(20,status.NumPayees);
            Assert.AreEqual(30,status.NumBudgetTxs);

            // And: Database is shown to be non-empty
            Assert.IsFalse(status.IsEmpty);
        }

        [TestMethod]
        public async Task GetDatabaseStatus_Empty()
        {
            // Given: Database is not empty
            context.Setup(x=>x.CountAsync<Transaction>(It.IsAny<IQueryable<Transaction>>())).Returns(Task.FromResult(0));
            context.Setup(x=>x.CountAsync<Payee>(It.IsAny<IQueryable<Payee>>())).Returns(Task.FromResult(0));
            context.Setup(x=>x.CountAsync<BudgetTx>(It.IsAny<IQueryable<BudgetTx>>())).Returns(Task.FromResult(0));
            dbadmin  = new DatabaseAdministration(context.Object,clock.Object);

            // When: Asking for database status
            var status = await dbadmin.GetDatabaseStatus();

            // Then: The values are as expected
            Assert.AreEqual(0,status.NumTransactions);
            Assert.AreEqual(0,status.NumPayees);
            Assert.AreEqual(0,status.NumBudgetTxs);

            // And: Database is shown to be empty
            Assert.IsTrue(status.IsEmpty);
        }

    }
}