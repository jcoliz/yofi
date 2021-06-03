using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ofx.Tests
{
    [TestClass]
    public class SplitTest
    {
        ApplicationDbContext context;

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(context);
        }
        [TestMethod]
        public async Task Includes()
        {
            // Test that we can commit splits with a transaction AND get them back

            var splits = new List<Split>();
            splits.Add(new Split() { Amount = 25m, Category = "A", SubCategory = "B" });
            splits.Add(new Split() { Amount = 75m, Category = "C", SubCategory = "D" });

            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = splits };

            context.Transactions.Add(item);
            context.SaveChanges();

            var actual = await context.Transactions.Include("Splits").ToListAsync();

            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(2, actual[0].Splits.Count);
        }
    }
}
