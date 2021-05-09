using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ofx.Tests
{
    [TestClass]
    public class TransactionControllerTest
    {
        private ControllerTestHelper<Transaction, TransactionsController> helper = null;

        TransactionsController controller => helper.controller;
        ApplicationDbContext context => helper.context;
        List<Transaction> Items => helper.Items;
        DbSet<Transaction> dbset => helper.dbset;

        [TestInitialize]
        public void SetUp()
        {
            helper = new ControllerTestHelper<Transaction, TransactionsController>();
            helper.SetUp();
            helper.controller = new TransactionsController(helper.context, null);

            helper.Items.Add(new Transaction() { Category = "B", SubCategory = "A", Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m });
            helper.Items.Add(new Transaction() { Category = "A", SubCategory = "A", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m });
            helper.Items.Add(new Transaction() { Category = "C", SubCategory = "A", Payee = "5", Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Amount = 300m });
            helper.Items.Add(new Transaction() { Category = "B", SubCategory = "A", Payee = "1", Timestamp = new DateTime(DateTime.Now.Year, 01, 05), Amount = 400m });
            helper.Items.Add(new Transaction() { Category = "B", SubCategory = "B", Payee = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m });

            helper.dbset = helper.context.Transactions;

            // Sample data items will use 'Name' as a unique sort idenfitier
            helper.KeyFor = (x => x.Payee);
        }

        [TestCleanup]
        public void Cleanup() => helper.Cleanup();
        [TestMethod]
        public void Empty() => helper.Empty();
        [TestMethod]
        public async Task IndexEmpty() => await helper.IndexEmpty();
        [TestMethod]
        public async Task IndexSingle() => await helper.IndexSingle();
        [TestMethod]
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
        [TestMethod]
        public async Task EditObjectValues() => await helper.EditObjectValues();
        [TestMethod]
        public async Task DeleteFound() => await helper.DeleteFound();
        [TestMethod]
        public async Task DeleteConfirmed() => await helper.DeleteConfirmed();
        // TODO: Fix failing tests
        [TestMethod]
        public async Task Download() => await helper.Download();
        [TestMethod]
        public async Task Upload()
        {
            // Can't use the helper's upload bevause Transaction upload does not return the uploaded items.
            var result = await helper.DoUpload(Items);

            // Test the status
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Import", actual.ActionName);

            // Now check the state of the DB

            Assert.AreEqual(Items.Count, dbset.Count());
        }
        [TestMethod]
        public async Task UploadWithID()
        {
            // Can't use the helper's upload bevause Transaction upload does not return the uploaded items.

            // Start out with one item in the DB. We are picking the ONE item that Upload doesn't upload.
            var expected = Items[4];
            context.Add(expected);
            await context.SaveChangesAsync();

            // One of the new items has an overlapping ID, but will be different in every way. We expect that
            // The end result is that the database will == items
            Items[0].ID = expected.ID;

            // Just upload the first four items. The fifth, we already did above
            // Can't use the helper's upload bevause Transaction upload does not return the uploaded items.
            var result = await helper.DoUpload(Items.Take(4).ToList());
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Import", actual.ActionName);

            // From here we can just use the Index test, but not add items. There should be the proper "items"
            // all there now.

            var indexmany = await dbset.OrderBy(x => x.Payee).ToListAsync();

            // Sort the original items by Key
            Items.Sort((x, y) => x.Payee.CompareTo(y.Payee));

            // Test that the resulting items are in the same order
            for (int i = 0; i < 5; ++i)
                Assert.AreEqual(Items[i], indexmany[i]);
        }
#if false
        [TestMethod]
        public async Task UploadWithID() => await helper.UploadWithID();
        [TestMethod]
        public async Task UploadDuplicate() => await helper.UploadDuplicate();
#endif
    }
}
