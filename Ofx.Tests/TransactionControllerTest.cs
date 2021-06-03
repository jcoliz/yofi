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
using Common.AspNetCore.Test;

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

        [TestMethod]
        public async Task UploadDuplicate()
        {
            // Start out with one item in the DB. We are picking the ONE item that Upload doesn't upload.
            var initial = Items[0];
            context.Add(initial);
            await context.SaveChangesAsync();

            // Need to detach the entity we originally created, to set up the same state the controller would be
            // in with not already haveing a tracked object.
            context.Entry(initial).State = EntityState.Detached;

            Items[0].ID = 0;

            // Now upload all the items. What should happen here is that only items 1-4 (not 0) get
            // uploaded, because item 0 is already there, so it gets removed as a duplicate.
            // Can't use the helper's upload bevause Transaction upload does not return the uploaded items.
            var result = await helper.DoUpload(Items);
            var redirectooactionresult = result as RedirectToActionResult;

            Assert.AreEqual("Import", redirectooactionresult.ActionName);

            // What's expected here? The new item IS uploaded, BUT it's deselected, where the rest are selected, right?
            var indexmany = await dbset.OrderBy(x => x.Payee).ToListAsync();

            Assert.AreEqual(1 + Items.Count(), indexmany.Count);

            // We expect that FIVE items were imported.

            var wasimported = dbset.Where(x => true == x.Imported);

            Assert.AreEqual(5, wasimported.Count());

            // Out of those, we expect that ONE is not selected, because it's a dupe of the initial.

            var actual = wasimported.Where(x => false == x.Selected).Single();

            Assert.AreEqual(initial, actual);
        }

        [TestMethod]
        public async Task Bug839()
        {
            // Bug 839: Imported items are selected automatically :(
            await helper.DoUpload(Items);

            await controller.Import("ok");

            var numimported = await dbset.Where(x => x.Imported == true).CountAsync();
            var numselected = await dbset.Where(x => x.Selected == true).CountAsync();

            Assert.AreEqual(0, numimported);
            Assert.AreEqual(0, numselected);
        }

        [TestMethod]
        public async Task Bug846()
        {
            // Bug 846: Save edited item overwrites uploaded receipt

            // So what is happening here is we are calling Edit(id) to get the item first, which has no receipturl

            await helper.AddFiveItems();
            var expected = Items[3];
            var result = await controller.Edit(expected.ID);
            var viewresult = result as ViewResult;
            var editing = viewresult.Model as Transaction;

            // Detach so our edits won't show up
            context.Entry(editing).State = EntityState.Detached;

            // Then separately we are committing a change to set the receipturl

            var dbversion = dbset.Where(x => x.ID == editing.ID).Single();
            dbversion.ReceiptUrl = "SET";
            context.SaveChanges();

            // Detach so the editing operation can work on this item
            context.Entry(dbversion).State = EntityState.Detached;

            // And THEN we are posting the original item :P
            await controller.Edit(editing.ID, false, editing);

            // What SHOULD happen is that the "blank" recepturl in the updated object does not overwrite
            // the receitpurl we set above.

            var actual = dbset.Where(x => x.ID == editing.ID).Single();

            Assert.AreEqual("SET", actual.ReceiptUrl);
        }

        [TestMethod]
        public async Task CreateSplit()
        {
            await helper.AddFiveItems();
            var item = Items[3];
            var expectedcategory = new String(item.Category);
            var result = await controller.CreateSplit(item.ID);
            var viewresult = result as ViewResult;
            var actual = viewresult.Model as Split;

            Assert.AreEqual(item.ID, actual.TransactionID);
            Assert.AreEqual(item.Amount, actual.Amount);
            Assert.AreEqual(expectedcategory, actual.Category);
            Assert.IsNull(actual.Transaction.Category);
            Assert.IsNull(actual.Transaction.SubCategory);
        }

        [TestMethod]
        public async Task CreateSecondSplit()
        {
            var splits = new List<Split>();
            splits.Add(new Split() { Amount = 25m, Category = "A", SubCategory = "B" });

            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = splits };

            context.Transactions.Add(item);
            context.SaveChanges();

            var result = await controller.CreateSplit(item.ID);
            var viewresult = result as ViewResult;
            var actual = viewresult.Model as Split;

            Assert.AreEqual(item.ID, actual.TransactionID);
            Assert.AreEqual(75m, actual.Amount);

            // Second split shouldn't have a category/subcat
            Assert.IsNull(actual.Category);
            Assert.IsNull(actual.SubCategory);
        }

        [TestMethod]
        public async Task SplitsShownInEdit()
        {
            // Copied from SplitTest.Includes()

            var splits = new List<Split>();
            splits.Add(new Split() { Amount = 25m, Category = "A", SubCategory = "B" });
            splits.Add(new Split() { Amount = 75m, Category = "C", SubCategory = "D" });

            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = splits };

            context.Transactions.Add(item);
            context.SaveChanges();

            // Not using this line from SplitTest.Includes(), instead we'll test the controller
            // edit.
            //var actual = await context.Transactions.Include("Splits").ToListAsync();

            // Copied from ControllerTestHelper.EditFound()
            var result = await controller.Edit(item.ID);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as Transaction;

            Assert.AreEqual(2, model.Splits.Count);
            Assert.AreEqual(75m, model.Splits.Where(x => x.Category == "C").Single().Amount);
        }

        //
        // Long list of TODO tests!!
        //
        // TODO: Import (ok/cancel/deselect)
        // TODO: OFX Upload
        // TODO: Upload w/ date cutoff
        // TODO: Edit, duplicate = true
        // TODO: Index sort order
        // TODO: Index payee search
        // TODO: Index cat/subcat search
        // TODO: Index pagination
        // TODO: Bulk Edit
        // TODO: Apply Payee
        // TODO: UpReceipt
        // TODO: DeleteReceipt
        // TODO: GetReceipt
        // TODO: Reports (yikes!)
    }
}
