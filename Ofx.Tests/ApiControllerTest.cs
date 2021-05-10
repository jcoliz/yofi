using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
            context.Transactions.Add(new Transaction() { Category = "BB", SubCategory = "AA", Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m });
            context.Transactions.Add(new Transaction() { Category = "AA", SubCategory = "AA", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m });
            context.Transactions.Add(new Transaction() { Category = "CC", SubCategory = "AA", Payee = "5", Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Amount = 300m });
            context.Transactions.Add(new Transaction() { Category = "BB", SubCategory = "AA", Payee = "1", Timestamp = new DateTime(DateTime.Now.Year, 01, 05), Amount = 400m });
            context.Transactions.Add(new Transaction() { Category = "BB", SubCategory = "BB", Payee = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m });
            
            await context.SaveChangesAsync();
        }

        async Task AddFivePayees()
        {
            context.Payees.Add(new Payee() { Category = "Y", SubCategory = "E", Name = "3" });
            context.Payees.Add(new Payee() { Category = "X", SubCategory = "E", Name = "2" });
            context.Payees.Add(new Payee() { Category = "Z", SubCategory = "E", Name = "5" });
            context.Payees.Add(new Payee() { Category = "X", SubCategory = "E", Name = "1" });
            context.Payees.Add(new Payee() { Category = "Y", SubCategory = "F", Name = "4" });

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
        [TestMethod]
        public async Task GetIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x=>x.ID);

            var json = await controller.Get(maxid + 1);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task HideId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();

            var json = await controller.Hide(expected.ID);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(true == expected.Hidden);
        }
        [TestMethod]
        public async Task HideIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var json = await controller.Hide(maxid + 1);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task ShowId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();
            expected.Hidden = true;
            await context.SaveChangesAsync();

            var json = await controller.Show(expected.ID);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(false == expected.Hidden);
        }
        [TestMethod]
        public async Task ShowIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var json = await controller.Show(maxid + 1);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task SelectId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();

            var json = await controller.Select(expected.ID);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(true == expected.Selected);
        }
        [TestMethod]
        public async Task SelectIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var json = await controller.Select(maxid + 1);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task DeselectId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();
            expected.Selected = true;
            await context.SaveChangesAsync();

            var json = await controller.Deselect(expected.ID);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(false == expected.Selected);
        }
        [TestMethod]
        public async Task DeselectIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var json = await controller.Deselect(maxid + 1);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task SelectPayeeId()
        {
            await AddFivePayees();
            var expected = await context.Payees.FirstAsync();

            var json = await controller.SelectPayee(expected.ID);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(true == expected.Selected);
        }
        [TestMethod]
        public async Task SelectPayeeIdFails()
        {
            await AddFivePayees();
            var maxid = await context.Payees.MaxAsync(x => x.ID);

            var json = await controller.SelectPayee(maxid + 1);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task DeselectPayeeId()
        {
            await AddFivePayees();
            var expected = await context.Payees.FirstAsync();
            expected.Selected = true;
            await context.SaveChangesAsync();

            var json = await controller.DeselectPayee(expected.ID);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(false == expected.Selected);
        }
        [TestMethod]
        public async Task DeselectPayeeIdFails()
        {
            await AddFivePayees();
            var maxid = await context.Payees.MaxAsync(x => x.ID);

            var json = await controller.DeselectPayee(maxid + 1);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task AddPayee()
        {
            var expected = new Payee() { Category = "B", SubCategory = "A", Name = "3" };
            
            var json = await controller.AddPayee(expected);
            var result = JsonConvert.DeserializeObject<ApiPayeeResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(expected, result.Payee);
        }
        [TestMethod]
        public async Task ApplyPayee()
        {
            await AddFivePayees();
            await AddFiveTransactions();

            // Pick an aribtrary transaction
            var tx = await context.Transactions.LastAsync();           

            var json = await controller.ApplyPayee(tx.ID);
            var result = JsonConvert.DeserializeObject<ApiPayeeResult>(json);

            Assert.IsTrue(result.Ok);

            var expected = await context.Payees.Where(x => x.Name == tx.Payee).SingleAsync();

            Assert.AreEqual(expected, result.Payee);

            Assert.AreEqual(expected.Category, tx.Category);
            Assert.AreEqual(expected.SubCategory, tx.SubCategory);
        }
        [TestMethod]
        public async Task ApplyPayeeFailsNoTxId()
        {
            await AddFivePayees();
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var json = await controller.ApplyPayee(maxid + 1);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }

        [TestMethod]
        public async Task ApplyPayeeFailsNoPayee()
        {
            await AddFivePayees();
            await AddFiveTransactions();

            // Pick an aribtrary transaction
            var tx = await context.Transactions.LastAsync();

            // Now remove the matching payee
            var payee = await context.Payees.Where(x => x.Name == tx.Payee).SingleAsync();
            context.Payees.Remove(payee);
            await context.SaveChangesAsync();

            var json = await controller.ApplyPayee(tx.ID);
            var result = JsonConvert.DeserializeObject<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task Edit()
        {
            await AddFiveTransactions();
            var original = await context.Transactions.FirstAsync();

            // detach the original so we have an unmodified copy around
            context.Entry(original).State = EntityState.Detached;

            var newtx = new Transaction() { ID = original.ID, Payee = "I have edited you!", SubCategory = original.SubCategory, Timestamp = original.Timestamp, Amount = original.Amount };

            var json = await controller.Edit(original.ID, false, newtx);
            var result = JsonConvert.DeserializeObject<ApiTransactionResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(newtx, result.Transaction);
            Assert.AreNotEqual(original, result.Transaction);

            var actual = await context.Transactions.Where(x => x.ID == original.ID).SingleAsync();
            Assert.AreEqual(newtx, actual);
            Assert.AreNotEqual(original, actual);
        }
        [TestMethod]
        public async Task EditDuplicate()
        {
            await AddFiveTransactions();
            var original = await context.Transactions.FirstAsync();

            // detach the original so we have an unmodified copy around
            context.Entry(original).State = EntityState.Detached;

            var newtx = new Transaction() { ID = original.ID, Payee = "I have edited you!", SubCategory = original.SubCategory, Timestamp = original.Timestamp, Amount = original.Amount };

            var json = await controller.Edit(original.ID, true, newtx);
            var result = JsonConvert.DeserializeObject<ApiTransactionResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(newtx, result.Transaction);
            Assert.AreNotEqual(original, result.Transaction);

            var unmodified = await context.Transactions.Where(x => x.ID == original.ID).SingleAsync();
            Assert.AreEqual(original, unmodified);

            var modified = await context.Transactions.Where(x => x.Payee == newtx.Payee).SingleAsync();
            Assert.AreEqual(newtx, modified);
        }
        [TestMethod]
        public async Task EditPayee()
        {
            await AddFivePayees();
            var original = await context.Payees.FirstAsync();

            // detach the original so we have an unmodified copy around
            context.Entry(original).State = EntityState.Detached;

            var newitem = new Payee() { ID = original.ID, Name = "I have edited you!", SubCategory = original.SubCategory, Category = original.Category };

            var json = await controller.EditPayee(original.ID, false, newitem);
            var result = JsonConvert.DeserializeObject<ApiPayeeResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(newitem, result.Payee);
            Assert.AreNotEqual(original, result.Payee);

            var actual = await context.Payees.Where(x => x.ID == original.ID).SingleAsync();
            Assert.AreEqual(newitem, actual);
            Assert.AreNotEqual(original, actual);
        }
        [TestMethod]
        public async Task EditPayeeDuplicate()
        {
            await AddFivePayees();
            var original = await context.Payees.FirstAsync();

            // detach the original so we have an unmodified copy around
            context.Entry(original).State = EntityState.Detached;

            var newitem = new Payee() { ID = original.ID, Name = "I have edited you!", SubCategory = original.SubCategory, Category = original.Category };

            var json = await controller.EditPayee(original.ID, true, newitem);
            var result = JsonConvert.DeserializeObject<ApiPayeeResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(newitem, result.Payee);
            Assert.AreNotEqual(original, result.Payee);

            var unmodified = await context.Payees.Where(x => x.ID == original.ID).SingleAsync();
            Assert.AreEqual(original, unmodified);

            var modified = await context.Payees.Where(x => x.Name == newitem.Name).SingleAsync();
            Assert.AreEqual(newitem, modified);
        }
    }
}
