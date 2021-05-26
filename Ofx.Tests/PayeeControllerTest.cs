using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Models;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNetCore.Tests;

namespace Ofx.Tests
{
    [TestClass]
    public class PayeeControllerTest
    {
        private ControllerTestHelper<Payee, PayeesController> helper = null;

        [TestInitialize]
        public void SetUp()
        {
            helper = new ControllerTestHelper<Payee, PayeesController>();
            helper.SetUp();
            helper.controller = new PayeesController(helper.context);

            helper.Items.Add(new Payee() { Category = "B", SubCategory = "A", Name = "3" });
            helper.Items.Add(new Payee() { Category = "A", SubCategory = "A", Name = "2" });
            helper.Items.Add(new Payee() { Category = "C", SubCategory = "A", Name = "5" });
            helper.Items.Add(new Payee() { Category = "A", SubCategory = "A", Name = "1" });
            helper.Items.Add(new Payee() { Category = "B", SubCategory = "B", Name = "4" });

            helper.dbset = helper.context.Payees;

            // Sample data items will use 'Name' as a unique sort idenfitier
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
        public async Task EditModal()
        {
            await helper.AddFiveItems();
            var expected = helper.Items[3];
            var result = await helper.controller.EditModal(expected.ID);
            var actual = result as PartialViewResult;
            var model = actual.Model as Payee;

            Assert.AreEqual("EditPartial", actual.ViewName);
            Assert.AreEqual(expected, model);
        }
        [TestMethod]
        public async Task BulkEdit()
        {
            await helper.AddFiveItems();
            helper.Items[2].Selected = true;
            helper.Items[4].Selected = true;
            await helper.context.SaveChangesAsync();

            var result = await helper.controller.BulkEdit("Category", "SubCategory");
            var actual = result as RedirectToActionResult;

            Assert.AreEqual("Index", actual.ActionName);

            // Note that we can still use the 'items' objects here because they are tracking the DB

            var lookup = helper.Items.ToLookup(x => x.Category, x => x);

            var changeditems = lookup["Category"];

            Assert.AreEqual(2, changeditems.Count());

            Assert.AreEqual("Category", helper.Items[2].Category);
            Assert.AreEqual("Category", helper.Items[4].Category);
        }
        [TestMethod]
        public async Task CreateFromTx()
        {
            var tx = new Transaction() { Payee = "A", SubCategory = "B", Category = "C" };
            helper.context.Add(tx);
            await helper.context.SaveChangesAsync();

            var result = await helper.controller.Create(tx.ID);
            var actual = result as ViewResult;
            var model = actual.Model as Payee;

            Assert.AreEqual(tx.Payee, model.Name);
            Assert.AreEqual(tx.Category, model.Category);
            Assert.AreEqual(tx.SubCategory, model.SubCategory);
        }

        [TestMethod]
        public async Task CreateModalFromTx()
        {
            var tx = new Transaction() { Payee = "A", SubCategory = "B", Category = "C" };
            helper.context.Add(tx);
            await helper.context.SaveChangesAsync();

            var result = await helper.controller.CreateModal(tx.ID);
            var actual = result as PartialViewResult;
            var model = actual.Model as Payee;

            Assert.AreEqual("CreatePartial", actual.ViewName);
            Assert.AreEqual(tx.Payee, model.Name);
            Assert.AreEqual(tx.Category, model.Category);
            Assert.AreEqual(tx.SubCategory, model.SubCategory);
        }

        // TODO: Upload duplicate where ONLY the NAME is the same
        // TODO: Upload payee name stripping
    }
}
