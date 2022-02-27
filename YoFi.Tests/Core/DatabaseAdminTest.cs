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
            context.Setup(x => x.Get<Transaction>()).Returns(Enumerable.Empty<Transaction>().AsQueryable());
            context.Setup(x=>x.Get<Payee>()).Returns(Enumerable.Empty<Payee>().AsQueryable());
            context.Setup(x => x.Get<BudgetTx>()).Returns(Enumerable.Empty<BudgetTx>().AsQueryable());
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

        [TestMethod]
        public async Task UnHide()
        {
            // Given: A set of transactions of varying dates
            var transactions = Enumerable.Range(1,10).Select(x=>new Transaction() { Timestamp = new System.DateTime(2022,1,x), Hidden = x > 3 }).ToList();
            context.Setup(x=>x.Get<Transaction>()).Returns(transactions.AsQueryable());
            context.Setup(x=>x.AnyAsync<Transaction>(It.IsAny<IQueryable<Transaction>>())).Returns(Task.FromResult(true));

            // And: Current time is in the middle of those
            clock = new Mock<IClock>();
            clock.Setup(x=>x.Now).Returns(new System.DateTime(2022,1,8));
            dbadmin  = new DatabaseAdministration(context.Object,clock.Object);

            // When: Unhiding up to today
            await dbadmin.UnhideTransactionsToToday();

            // Then: The correct items were changed
            Assert.IsFalse(transactions[3].Hidden); // Jan 4
            Assert.IsFalse(transactions[4].Hidden); // Jan 5
            Assert.IsFalse(transactions[5].Hidden); // Jan 6
            Assert.IsFalse(transactions[6].Hidden); // Jan 7
            Assert.IsFalse(transactions[7].Hidden); // Jan 8
            Assert.IsTrue(transactions[8].Hidden); // Jan 9
            Assert.IsTrue(transactions[9].Hidden); // Jan 10
        }

        [DataRow("payee","p")]
        [DataRow("budgettx", "b")]
        [DataRow("trx", "t")]
        [DataRow("all", "tbp")]
        [DataTestMethod]
        public async Task ClearTestData(string id, string expected)
        {
            // Given: A fresh dbadmin using a mock data context
            var mycontext = new MockDataContext();
            dbadmin = new DatabaseAdministration(mycontext, clock.Object);

            // Given: A mix of transactions, payees, and budgettx with and without the testmarker, in the database
            var tdata = FakeObjects<Transaction>
                .Make(5)
                .Add(6, x => x.Category += DatabaseAdministration.TestMarker)
                .SaveTo(mycontext);
            var bdata = FakeObjects<BudgetTx>.Make(5).Add(6, x => x.Category += DatabaseAdministration.TestMarker).SaveTo(mycontext);
            var pdata = FakeObjects<Payee>.Make(5).Add(6, x => x.Category += DatabaseAdministration.TestMarker).SaveTo(mycontext);

            // When: Clearing test data using the selected {id}
            await dbadmin.ClearTestDataAsync(id);

            // Then: The {expected} data has been removed of testdata
            if (expected.Contains("t"))
                Assert.IsTrue(mycontext.Get<Transaction>().SequenceEqual(tdata.Group(0)));
            else
                Assert.IsTrue(mycontext.Get<Transaction>().SequenceEqual(tdata));

            if (expected.Contains("b"))
                Assert.IsTrue(mycontext.Get<BudgetTx>().SequenceEqual(bdata.Group(0)));
            else
                Assert.IsTrue(mycontext.Get<BudgetTx>().SequenceEqual(bdata));

            if (expected.Contains("p"))
                Assert.IsTrue(mycontext.Get<Payee>().SequenceEqual(pdata.Group(0)));
            else
                Assert.IsTrue(mycontext.Get<Payee>().SequenceEqual(pdata));

        }
    }
}