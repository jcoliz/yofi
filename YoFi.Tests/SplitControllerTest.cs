using Common.AspNet.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Tests
{
    [TestClass]
    public class SplitControllerTest
    {
        private ControllerTestHelper<Split, SplitsController> helper = null;

        SplitsController controller => helper.controller;
        ApplicationDbContext context => helper.context;

#if false
        // Here if we need them for later
        List<Split> Items => helper.Items;
        DbSet<Split> dbset => helper.dbset;
#endif

        public static List<Split> SplitItems
        {
            get
            {
                return new List<Split>()
                {
                    new Split() { Category = "B", SubCategory = "A", Memo = "3", Amount = 300m },
                    new Split() { Category = "A", SubCategory = "A", Memo = "2", Amount = 200m },
                    new Split() { Category = "C", SubCategory = "A", Memo = "5", Amount = 500m },
                    new Split() { Category = "A", SubCategory = "A", Memo = "1", Amount = 100m },
                    new Split() { Category = "B", SubCategory = "B", Memo = "4", Amount = 400m }
                };
            }
        }

        [TestInitialize]
        public void SetUp()
        {
            helper = new ControllerTestHelper<Split, SplitsController>();
            helper.SetUp();
            helper.controller = new SplitsController(helper.context);

            helper.Items.AddRange(SplitItems);

            helper.dbset = helper.context.Splits;

            // Sample data items will use 'Name' as a unique sort idenfitier
            helper.KeyFor = (x => x.Memo);
        }
        [TestCleanup]
        public void Cleanup() => helper.Cleanup();
        [TestMethod]
        public void Empty() => helper.Empty();
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task IndexEmpty() => await helper.IndexEmpty();
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task IndexSingle() => await helper.IndexSingle();
        //[TestMethod]
        // IndexMany doesn't make sense for splits
        public async Task IndexMany() => await helper.IndexMany();
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task DetailsFound() => await helper.DetailsFound();
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task DetailsNotFound() => await helper.DetailsNotFound();
        [TestMethod]
        public async Task EditFound() => await helper.EditFound();
        [TestMethod]
        public async Task EditNotFound() => await helper.EditNotFound();
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task Create() => await helper.Create();
        //[TestMethod]
        public async Task EditObjectValues() => await helper.EditObjectValues();
        [TestMethod]
        public async Task DeleteFound() => await helper.DeleteFound();
        [TestMethod]
        public async Task DeleteConfirmed() => await helper.DeleteConfirmed("Edit");
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

        [TestMethod]
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
            context.Entry(item).State = EntityState.Detached;

            var updated = new Split() { ID = id, TransactionID = item.ID, Amount = 75m, Category = "C", SubCategory = "D" };

            var result = await controller.Edit(id, updated);
            var redirresult = result as RedirectToActionResult;

            Assert.AreEqual("Edit", redirresult.ActionName);

            // Now let's check our transaction.

            var tx = context.Transactions.Single();

            Assert.AreEqual(updated.Amount, tx.Splits.Single().Amount);
        }

        [TestMethod]
        public async Task DeleteLastSplitReplacesCategory()
        {
            // Bug 1003: Delete split leaves category blank in index

            // Given: A transaction with 1 splits in the database
            var splits = new List<Split>();
            var expected = "A";
            var initial = new Split() { Amount = 25m, Category = expected, SubCategory = "B" };
            splits.Add(initial);

            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = splits };

            context.Transactions.Add(item);
            context.SaveChanges();

            // When: Deleting that split
            await controller.DeleteConfirmed(initial.ID);

            // Then: The transaction now has the category from the split
            var actual = context.Transactions.Single();
            Assert.AreEqual(expected, actual.Category);
        }

        [TestMethod]
        public async Task DeleteNotLastSplitDoesntReplaceCategory()
        {
            // Given: A transaction with 2 splits in the database
            var splits = new List<Split>();
            var initial = new Split() { Amount = 25m, Category = "A", SubCategory = "B" };
            splits.Add(initial);
            splits.Add(new Split() { Amount = 75m, Category = "C" });

            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = splits };

            context.Transactions.Add(item);
            context.SaveChanges();

            // When: Deleting one split
            await controller.DeleteConfirmed(initial.ID);

            // Then: The transaction still has null category
            var actual = context.Transactions.Single();
            Assert.IsNull(actual.Category);
        }
    }
}
