using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ofx.Tests
{
    [TestClass]
    public class ApiControllerTest
    {
        public ApiController controller { set; get; } = default(ApiController);

        public ApplicationDbContext context = null;

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);

            controller = new ApiController(context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Didn't actually solve anything. Keep it around for possible future problem
            //DetachAllEntities();

            // https://stackoverflow.com/questions/33490696/how-can-i-reset-an-ef7-inmemory-provider-between-unit-tests
            context?.Database.EnsureDeleted();
            context = null;
            controller = default(ApiController);
        }

        async Task AddFiveTransactions()
        {            
            context.Transactions.Add(new Transaction() { Category = "B", SubCategory = "A", Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m });
            context.Transactions.Add(new Transaction() { Category = "A", SubCategory = "A", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m });
            context.Transactions.Add(new Transaction() { Category = "C", SubCategory = "A", Payee = "5", Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Amount = 300m });
            context.Transactions.Add(new Transaction() { Category = "B", SubCategory = "A", Payee = "1", Timestamp = new DateTime(DateTime.Now.Year, 01, 05), Amount = 400m });
            context.Transactions.Add(new Transaction() { Category = "B", SubCategory = "B", Payee = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m });

            await context.SaveChangesAsync();
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void Get()
        {
            var json = controller.Get();
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsTrue(result.Ok);
        }
        [TestMethod]
        public async Task GetId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();

            var json = await controller.Get(expected.ID);
            var result = JsonConvert.DeserializeObject<ApiTransactionResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(expected, result.Transaction);
        }
    }
}
