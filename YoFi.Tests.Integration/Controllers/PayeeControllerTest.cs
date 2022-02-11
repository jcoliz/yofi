using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    [TestClass]
    public class PayeeControllerTest : ControllerTest<Payee>
    {
        protected override string urlroot => "/Payees";

        #region Init/Cleanup

        [ClassInitialize]
        public static void InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            integrationcontext.Dispose();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean out database
            context.Set<Payee>().RemoveRange(context.Set<Payee>());
            context.SaveChanges();
        }

        #endregion

        #region Tests

        [TestMethod]
        public async Task EditModal()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<Payee>(5, 1);
            var expected = chosen.Single();
            var id = expected.ID;

            // When: Asking for the modal edit partial view
            var document = await WhenGetAsync($"{urlroot}/EditModal/{id}");

            // Then: That item is shown
            var testkey = FindTestKey<Payee>().Name;
            var actual = document.QuerySelector($"input[name={testkey}]").GetAttribute("value").Trim();
            Assert.AreEqual(TestKeyOrder<Payee>()(expected), actual);
        }

#if false

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

#endif


        #endregion
    }
}
