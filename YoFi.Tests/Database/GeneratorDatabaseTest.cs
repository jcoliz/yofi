using Common.NET.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using YoFi.AspNet.Data;
using YoFi.Core.SampleGen;
using CNDSampleData = Common.NET.Data.SampleData;

namespace YoFi.Tests.Database
{
    [TestClass]
    public class GeneratorDatabaseTest
    {
        ApplicationDbContext context;
        SampleDataGenerator generator;

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);

            generator = new SampleDataGenerator();
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

        [TestMethod]
        public async Task GenerateAndAddTransactions()
        {
            // Given: An existing file of defitions
            // And: Loading them
            // And: Generating transactions
            var stream = SampleData.Open("TestData1.xlsx");
            generator.LoadDefinitions(stream);
            generator.GenerateTransactions(addids:false);

            // When: Adding them to the database
            context.Transactions.AddRange(generator.Transactions);
            await context.SaveChangesAsync();

            // Then: They are in the database as expected
            var count = await context.Transactions.CountAsync();
            Assert.AreEqual(435, count);

            var spotcheckcount = await context.Transactions.CountAsync(x => x.Payee == "Big Megacorp");
            Assert.AreEqual(24, spotcheckcount);

            var splitcount = await context.Splits.CountAsync();
            Assert.AreEqual(312, splitcount);
        }

        [TestMethod]
        public async Task GenerateAndAddPayees()
        {
            // Given: An existing file of defitions
            // And: Loading them
            // And: Generating transactions
            var stream = SampleData.Open("TestData1.xlsx");
            generator.LoadDefinitions(stream);
            generator.GeneratePayees();

            // When: Adding them to the database
            context.Payees.AddRange(generator.Payees);
            await context.SaveChangesAsync();

            // Then: They are in the database as expected
            var count = await context.Payees.CountAsync();
            Assert.AreEqual(21, count);
        }

        [TestMethod]
        public async Task GenerateAndAddBudget()
        {
            // Given: An existing file of defitions
            // And: Loading them
            // And: Generating transactions
            var stream = SampleData.Open("TestData1.xlsx");
            generator.LoadDefinitions(stream);
            generator.GenerateBudget();

            // When: Adding them to the database
            context.BudgetTxs.AddRange(generator.BudgetTxs);
            await context.SaveChangesAsync();

            // Then: They are in the database as expected
            var count = await context.BudgetTxs.CountAsync();
            Assert.AreEqual(32, count);
        }

        [TestMethod]
        public async Task GenerateAndAddFullSampleData()
        {
            var instream = CNDSampleData.Open("FullSampleDataDefinition.xlsx");
            generator.LoadDefinitions(instream);
            generator.GenerateTransactions(addids:false);
            generator.GeneratePayees();
            generator.GenerateBudget();

            context.Transactions.AddRange(generator.Transactions);
            context.Payees.AddRange(generator.Payees);
            context.BudgetTxs.AddRange(generator.BudgetTxs);
            await context.SaveChangesAsync();

            // Then: They are in the database as expected
            var count = await context.Transactions.CountAsync();
            Assert.AreEqual(889, count);
            count = await context.Splits.CountAsync();
            Assert.AreEqual(312, count);
            count = await context.Payees.CountAsync();
            Assert.AreEqual(40, count);
            count = await context.BudgetTxs.CountAsync();
            Assert.AreEqual(46, count);
        }
    }
}
