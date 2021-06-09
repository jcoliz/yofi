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
using Microsoft.AspNetCore.Http;
using System.IO;
using OfficeOpenXml;

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
        public async Task UploadMatchPayees()
        {
            await AddFivePayees();

            // Strip off the categories, so they'll match on input
            var uploadme = Items.Select(x => { x.Category = null; x.SubCategory = null; return x; }).ToList();

            // Then upload that
            await helper.DoUpload(uploadme);

            // This should have matched ALL the payees

            foreach(var tx in context.Transactions)
            {
                var expectedpayee = context.Payees.Where(x => x.Name == tx.Payee).Single();
                Assert.AreEqual(expectedpayee.Category, tx.Category);
            }
        }

        [TestMethod]
        public async Task Bug880()
        {
            // Bug 880: Import applies substring matches before regex matches

            var regexpayee = new Payee() { Category = "Y", SubCategory = "E", Name = "/DOG.*123/" };
            var substrpayee = new Payee() { Category = "X", SubCategory = "E", Name = "BIGDOG" };

            context.Payees.Add(regexpayee);
            context.Payees.Add(substrpayee);
            await context.SaveChangesAsync();

            // This transaction will match EITHER of the payees. Preference is to match the regex first
            // because the regex is a more precise specification of what we want.
            await helper.DoUpload(new List<Transaction>() { new Transaction() { Payee = "BIGDOG SAYS 1234", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m } });

            var tx = context.Transactions.Single();

            Assert.AreEqual(regexpayee.Category, tx.Category);
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
            var viewresult = result as RedirectToActionResult;
            var routes = viewresult.RouteValues;
            var newid = Convert.ToInt32(routes["id"]);
            var actual = context.Splits.Where(x => x.ID == newid).Single();

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
            var viewresult = result as RedirectToActionResult;
            var routes = viewresult.RouteValues;
            var newid = Convert.ToInt32(routes["id"]);
            var actual = context.Splits.Where(x => x.ID == newid).Single();

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
            Assert.AreEqual(true, viewresult.ViewData["SplitsOK"]);
        }

        [TestMethod]
        public async Task SplitsShownInIndex()
        {
            // Copied from SplitTest.Includes()

            var splits = new List<Split>();
            splits.Add(new Split() { Amount = 25m, Category = "A", SubCategory = "B" });
            splits.Add(new Split() { Amount = 75m, Category = "C", SubCategory = "D" });

            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = splits };

            context.Transactions.Add(item);
            context.SaveChanges();

            var result = await controller.Index(null,null,null,null,null);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as List<Transaction>;

            Assert.IsTrue(model.Single().HasSplits);
            Assert.IsTrue(model.Single().IsSplitsOK);
        }
        [TestMethod]
        public async Task SplitsShownInIndexSearchCategory()
        {
            var splits = new List<Split>();
            splits.Add(new Split() { Amount = 25m, Category = "A", SubCategory = "B" });
            splits.Add(new Split() { Amount = 75m, Category = "C", SubCategory = "D" });

            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = splits };

            context.Transactions.Add(item);
            context.SaveChanges();

            var result = await controller.Index(null, null, null, "A", null);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as List<Transaction>;

            Assert.AreEqual(1, model.Count);
            Assert.IsTrue(model.Single().HasSplits);
            Assert.IsTrue(model.Single().IsSplitsOK);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task SplitsShownInReport(bool usesplits)
        {
            int year = DateTime.Now.Year;
            var expected_ab = 25m;
            var expected_cd = 75m;

            if (usesplits)
            {
                var splits = new List<Split>();
                splits.Add(new Split() { Amount = expected_ab, Category = "A", SubCategory = "B" });
                splits.Add(new Split() { Amount = expected_cd, Category = "C", SubCategory = "D" });

                var item = new Transaction() { Payee = "3", Timestamp = new DateTime(year, 01, 03), Amount = 100m, Splits = splits };

                context.Transactions.Add(item);
            }
            else
            {
                var items = new List<Transaction>();
                items.Add(new Transaction() { Category = "A", SubCategory = "B", Payee = "3", Timestamp = new DateTime(year, 01, 03), Amount = expected_ab });
                items.Add(new Transaction() { Category = "C", SubCategory = "D", Payee = "2", Timestamp = new DateTime(year, 01, 04), Amount = expected_cd });
                context.Transactions.AddRange(items);
            }

            context.SaveChanges();

            var result = await controller.Pivot("all", null, null, year, null);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as PivotTable<Label, Label, decimal>;

            var row_AB = model.RowLabels.Where(x => x.Value == "A" && x.SubValue == "B").Single();
            var col = model.Columns.First();
            var actual_AB = model[col, row_AB];

            Assert.AreEqual(expected_ab, actual_AB);

            var row_CD = model.RowLabels.Where(x => x.Value == "C" && x.SubValue == "D").Single();
            var actual_CD = model[col, row_CD];

            Assert.AreEqual(expected_cd, actual_CD);

            // Make sure the total is correct as well, no extra stuff in there.
            var row_total = model.RowLabels.Where(x => x.Value == "TOTAL").Single();
            var actual_total = model[col, row_total];

            Assert.AreEqual(expected_ab + expected_cd, actual_total);
        }

        [TestMethod]
        public async Task SplitsDontAddUpInEdit()
        {
            // Copied from SplitTest.Includes()

            var splits = new List<Split>();
            splits.Add(new Split() { Amount = 25m, Category = "A", SubCategory = "B" });

            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = splits };

            context.Transactions.Add(item);
            context.SaveChanges();

            // Copied from ControllerTestHelper.EditFound()
            var result = await controller.Edit(item.ID);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as Transaction;

            Assert.AreEqual(false, viewresult.ViewData["SplitsOK"]);
            Assert.IsFalse(model.IsSplitsOK);
        }

        [TestMethod]
        public async Task UploadSplitsForTransaction()
        {
            // Don't add the splits here, we'll upload them
            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m };

            context.Transactions.Add(item);
            context.SaveChanges();

            var splits = new List<Split>();
            splits.Add(new Split() { Amount = 25m, Category = "A", SubCategory = "B" });
            splits.Add(new Split() { Amount = 75m, Category = "C", SubCategory = "D" });

            // Make an HTML Form file containg an excel spreadsheet containing those splits
            var file = ControllerTestHelper<Split, SplitsController>.PrepareUpload(splits);

            // Upload that
            var result = await controller.UpSplits(new List<IFormFile>() { file }, item.ID);
            var redir = result as RedirectToActionResult;

            Assert.IsNotNull(redir);
            Assert.IsTrue(item.HasSplits);
            Assert.IsTrue(item.IsSplitsOK);
        }

        [TestMethod]
        public async Task SplitsShownDownload()
        {
            var splits = new List<Split>();
            splits.Add(new Split() { Amount = 25m, Category = "A", SubCategory = "B" });
            splits.Add(new Split() { Amount = 75m, Category = "C", SubCategory = "D" });

            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, Splits = splits };

            context.Transactions.Add(item);
            context.SaveChanges();

            var result = await controller.Download();
            var fcresult = result as FileContentResult;
            var data = fcresult.FileContents;

            var incoming = new HashSet<Split>();
            using (var stream = new MemoryStream(data))
            {
                var excel = new ExcelPackage(stream);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var sheetname = $"{typeof(Split).Name}s";
                var worksheet = excel.Workbook.Worksheets.Where(x => x.Name == sheetname).Single();
                worksheet.ExtractInto(incoming,includeids:true);
            }

            Assert.AreEqual(2, incoming.Count);
            Assert.AreEqual(item.ID, incoming.First().TransactionID);
            Assert.AreEqual(item.ID, incoming.Last().TransactionID);
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
        // TODO: UpReceipt
        // TODO: DeleteReceipt
        // TODO: GetReceipt
        // TODO: Reports (yikes!)
    }
}
