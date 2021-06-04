using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Models;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNetCore.Test;
using System;
using OfxWeb.Asp.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Ofx.Tests
{
    [TestClass]
    public class SplitControllerTest
    {
        private ControllerTestHelper<Split, SplitsController> helper = null;

        SplitsController controller => helper.controller;
        ApplicationDbContext context => helper.context;
        List<Split> Items => helper.Items;
        DbSet<Split> dbset => helper.dbset;

        [TestInitialize]
        public void SetUp()
        {
            helper = new ControllerTestHelper<Split, SplitsController>();
            helper.SetUp();
            helper.controller = new SplitsController(helper.context);

            helper.Items.Add(new Split() { Category = "B", SubCategory = "A", Memo = "3", Amount = 300m });
            helper.Items.Add(new Split() { Category = "A", SubCategory = "A", Memo = "2", Amount = 200m });
            helper.Items.Add(new Split() { Category = "C", SubCategory = "A", Memo = "5", Amount = 500m });
            helper.Items.Add(new Split() { Category = "A", SubCategory = "A", Memo = "1", Amount = 100m });
            helper.Items.Add(new Split() { Category = "B", SubCategory = "B", Memo = "4", Amount = 400m });

            helper.dbset = helper.context.Splits;

            // Sample data items will use 'Name' as a unique sort idenfitier
            helper.KeyFor = (x => x.Memo);
        }
        [TestCleanup]
        public void Cleanup() => helper.Cleanup();
        [TestMethod]
        public void Empty() => helper.Empty();
        [TestMethod]
        public async Task IndexEmpty() => await helper.IndexEmpty();
        [TestMethod]
        public async Task IndexSingle() => await helper.IndexSingle();
        //[TestMethod]
        // IndexMany doesn't make sense for splits
        public async Task IndexMany() => await helper.IndexMany();
        [TestMethod]
        public async Task DetailsFound() => await helper.DetailsFound();
        [TestMethod]
        public async Task DetailsNotFound() => await helper.DetailsNotFound();
        [TestMethod]
        public async Task EditFound() => await helper.EditFound();
        [TestMethod]
        public async Task EditNotFound() => await helper.EditNotFound();
        [TestMethod]
        public async Task Create() => await helper.Create();
        //[TestMethod]
        public async Task EditObjectValues() => await helper.EditObjectValues();
        [TestMethod]
        public async Task DeleteFound() => await helper.DeleteFound();
        [TestMethod]
        public async Task DeleteConfirmed() => await helper.DeleteConfirmed();
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task Download() => await helper.Download();
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task Upload() => await helper.Upload();
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task UploadWithID() => await helper.UploadWithID();
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task UploadDuplicate() => await helper.UploadDuplicate();

        //[TestMethod]
        public async Task EditObjectValuesShowsInTransaction()
        {
            var splits = new List<Split>();
            var initial = new Split() { Amount = 25m, Category = "A", SubCategory = "B" };
            splits.Add(initial);

            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = splits };

            context.Transactions.Add(item);
            context.SaveChanges();
            var id = initial.ID;

            // Need to detach the entity we originally created, to set up the same state the controller would be
            // in with not already haveing a tracked object.
            context.Entry(initial).State = EntityState.Detached;

            var updated = new Split() { ID = id, TransactionID = item.ID, Amount = 75m, Category = "C", SubCategory = "D" };

            var result = await controller.Edit(id, updated);
            var redirresult = result as RedirectToActionResult;

            Assert.AreEqual("Edit", redirresult.ActionName);

            // Now let's check our transaction.

            Assert.AreEqual(updated.Amount, item.Splits.Single().Amount);

            /*
            var initial = Items[3];
            context.Add(initial);
            await context.SaveChangesAsync();
            var id = initial.ID;

            // Need to detach the entity we originally created, to set up the same state the controller would be
            // in with not already haveing a tracked object.
            context.Entry(initial).State = EntityState.Detached;

            var updated = Items[1];
            updated.ID = id;
            var result = await controller.Edit(id, updated);
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Index", actual.ActionName);
            */
        }

    }
}
