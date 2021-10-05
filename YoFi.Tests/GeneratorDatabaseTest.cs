using Common.NET.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Data;

namespace YoFi.SampleGen.Tests
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
        public async Task GenerateAndAdd()
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
    }
}
