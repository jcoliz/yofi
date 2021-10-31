using Common.AspNet.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Data;
using YoFi.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNet;
using YoFi.Core.Repositories;

namespace YoFi.Tests
{
    [TestClass]
    public class PayeeControllerTest
    {
        private ControllerTestHelper<Payee, PayeesController> helper = null;

        PayeesController controller => helper.controller;
        ApplicationDbContext context => helper.context;
        List<Payee> Items => helper.Items;
        DbSet<Payee> dbset => helper.dbset;

        public static List<Payee> PayeeItems
        {
            get
            {
                return  new List<Payee>()
                {
                    new Payee() { Category = "B", Name = "3" },
                    new Payee() { Category = "A", Name = "2" },
                    new Payee() { Category = "C", Name = "5" },
                    new Payee() { Category = "A", Name = "1" },
                    new Payee() { Category = "B", Name = "4" },

                    new Payee() { Category = "ABCD", Name = "3" },
                    new Payee() { Category = "X", Name = "2" },
                    new Payee() { Category = "ZABCZZ", Name = "5" },
                    new Payee() { Category = "X", Name = "1ABC" },
                    new Payee() { Category = "Y", Name = "4" }
                };
            }
        }

        IEnumerable<Payee> ItemsLong;

        IEnumerable<Payee> GetItemsLong()
        {
            if (null == ItemsLong)
                ItemsLong = Enumerable.Range(1,200).Select(x => new Payee() { Category = x.ToString(), Name = x.ToString() }).ToList();
            return ItemsLong;
        }

        [TestInitialize]
        public void SetUp()
        {
            helper = new ControllerTestHelper<Payee, PayeesController>();
            helper.SetUp();
            helper.controller = new PayeesController(new PayeeRepository(helper.context));
            helper.Items.AddRange(PayeeItems.Take(5));
            helper.dbset = helper.context.Payees;
            helper.KeyFor = (x => x.Name);
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
        [TestMethod]
        public async Task Download() => await helper.Download();
        [TestMethod]
        public async Task Upload() => await helper.Upload();
        [TestMethod]
        public async Task UploadWithID() => await helper.UploadWithID();
        [TestMethod]
        public async Task UploadDuplicate() => await helper.UploadDuplicate();
        [TestMethod]
        public async Task UploadEmpty() => await helper.UploadEmpty();

        [TestMethod]
        public async Task EditModal()
        {
            await helper.AddFiveItems();
            var expected = Items[3];
            var result = await controller.EditModal(expected.ID);
            var actual = result as PartialViewResult;
            var model = actual.Model as Payee;

            Assert.AreEqual("EditPartial", actual.ViewName);
            Assert.AreEqual(expected, model);
        }
        [TestMethod]
        public async Task BulkEdit()
        {
            await helper.AddFiveItems();
            Items[2].Selected = true;
            Items[4].Selected = true;
            await context.SaveChangesAsync();

            var result = await controller.BulkEdit("Category");
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Index", actual.ActionName);

            // Note that we can still use the 'items' objects here because they are tracking the DB

            var lookup = Items.ToLookup(x => x.Category, x => x);

            var changeditems = lookup["Category"];

            Assert.AreEqual(2, changeditems.Count());

            Assert.AreEqual("Category", Items[2].Category);
            Assert.AreEqual("Category", Items[4].Category);
        }
        [TestMethod]
        public async Task CreateFromTx()
        {
            var tx = new Transaction() { Payee = "A", Category = "C" };
            context.Add(tx);
            await context.SaveChangesAsync();

            var result = await controller.Create(tx.ID);
            var actual = result as ViewResult;
            var model = actual.Model as Payee;

            Assert.AreEqual(tx.Payee, model.Name);
            Assert.AreEqual(tx.Category, model.Category);
        }

        [TestMethod]
        public async Task CreateModalFromTx()
        {
            var tx = new Transaction() { Payee = "A", Category = "C" };
            context.Add(tx);
            await context.SaveChangesAsync();

            var result = await controller.CreateModal(tx.ID);
            var actual = result as PartialViewResult;
            var model = actual.Model as Payee;

            Assert.AreEqual("CreatePartial", actual.ViewName);
            Assert.AreEqual(tx.Payee, model.Name);
            Assert.AreEqual(tx.Category, model.Category);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task IndexShowSelected(bool isselected)
        {
            // When: Calling index with view set to 'selected'
            var searchterm = isselected ? "S" : null;
            await controller.Index(v: searchterm);

            // Then: The "show selected" state is transmitted through to the view in the view data
            Assert.AreEqual(isselected, controller.ViewData["ShowSelected"]);
        }

        [TestMethod]
        public async Task IndexPage1()
        {
            // Given: A very long set of items 
            var items = GetItemsLong();
            dbset.AddRange(items);
            context.SaveChanges();

            // When: Calling Index page 1
            var result = await controller.Index(p: 1);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as List<Payee>;

            // Then: Only one page's worth of items are returned
            Assert.AreEqual(PayeesController.PageSize, model.Count);

            // And: Page Item values are as expected
            var pages = viewresult.ViewData[nameof(PageDivider)] as PageDivider;
            Assert.AreEqual(1, pages.PageFirstItem);
            Assert.AreEqual(PayeesController.PageSize, pages.PageLastItem);
            Assert.AreEqual(items.Count(), pages.PageTotalItems);
        }

        [TestMethod]
        public async Task IndexPage2()
        {
            // Given: A long set of items, which is longer than one page, but not as long as two pages 
            var itemcount = PayeesController.PageSize + PayeesController.PageSize / 2;
            dbset.AddRange(GetItemsLong().Take(itemcount));
            context.SaveChanges();

            // When: Calling Index page 2
            var result = await controller.Index(p: 2);
            var viewresult = result as ViewResult;
            var model = viewresult.Model as List<Payee>;

            // Then: Only items after one page's worth of items are returned
            Assert.AreEqual(TransactionsController.PageSize / 2, model.Count);

            // And: Page Item values are as expected
            var pages = viewresult.ViewData[nameof(PageDivider)] as PageDivider;
            Assert.AreEqual(1 + TransactionsController.PageSize, pages.PageFirstItem);
            Assert.AreEqual(itemcount, pages.PageLastItem);
            Assert.AreEqual(itemcount, pages.PageTotalItems);
        }

        [TestMethod]
        public async Task IndexQAny()
        {
            // Given: A mix of transactions, some with '{word}' in their category, memo, or payee and some without
            var items = PayeeItems.Take(10);
            dbset.AddRange(items);
            context.SaveChanges();

            // When: Calling index q={word}
            var word = "ABC";
            var result = await controller.Index(q: word);
            var actual = result as ViewResult;
            var model = actual.Model as List<Payee>;

            // Then: Only the items with '{word}' in their category or payee are returned
            var expected = items.Where(x => x.Name.Contains(word) || x.Category.Contains(word)).ToList();
            CollectionAssert.AreEquivalent(expected, model);
        }

        [TestMethod]
        public async Task CreateEmpty()
        {
            // When: Calling Creat with null value
            int? value = null;
            var result = await controller.Create(value);

            // Then: It returns an empty model
            var viewresult = result as ViewResult;
            Assert.IsNull(viewresult.Model);
        }

        [TestMethod]
        public async Task CreateZero() =>
            Assert.IsTrue(await controller.Create(0) is Microsoft.AspNetCore.Mvc.NotFoundResult);

        [TestMethod]
        public async Task CreateModalZero() =>
            Assert.IsTrue(await controller.CreateModal(0) is Microsoft.AspNetCore.Mvc.BadRequestObjectResult);

        [TestMethod]
        public async Task CreateModalNotFound() =>
            Assert.IsTrue(await controller.CreateModal(1) is Microsoft.AspNetCore.Mvc.NotFoundResult);

        [TestMethod]
        public async Task EditNullNotFound() =>
            Assert.IsTrue(await controller.Edit(null) is Microsoft.AspNetCore.Mvc.NotFoundResult);

        [TestMethod]
        public async Task EditModalNullNotFound() =>
            Assert.IsTrue(await controller.EditModal(null) is Microsoft.AspNetCore.Mvc.NotFoundResult);

        [TestMethod]
        public async Task DeleteNullNotFound() =>
            Assert.IsTrue(await controller.Delete(null) is Microsoft.AspNetCore.Mvc.NotFoundResult);

        [TestMethod]
        public async Task DetailsNullNotFound() =>
            Assert.IsTrue(await controller.Details(null) is Microsoft.AspNetCore.Mvc.NotFoundResult);

        [TestMethod]
        public async Task DeleteConfirmedNotFound() =>
            Assert.IsTrue(await controller.DeleteConfirmed(-1) is Microsoft.AspNetCore.Mvc.NotFoundResult);

        // TODO: Upload duplicate where ONLY the NAME is the same
        // TODO: Upload payee name stripping
    }
}
