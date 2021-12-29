using Common.DotNet.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Controllers.Slim
{
    [TestClass]
    public class PayeeControllerSlimTest: BaseControllerSlimTest<Payee>
    {
        private PayeesController itemController => base.controller as PayeesController;
        private MockPayeeRepository itemRepository => base.repository as MockPayeeRepository;

        [TestInitialize]
        public void SetUp()
        {
            repository = new MockPayeeRepository();
            controller = new PayeesController(repository as IPayeeRepository, new MockQueryExecution());
        }

        [TestMethod]
        public async Task CreateFromTx()
        {
            // When: Creating a payee from a given transaction id
            var txid = 1234;
            var actionresult = await itemController.Create(txid);

            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<Payee>(viewresult.Model);

            // Then: The item is as we expect it to be
            Assert.AreEqual(txid.ToString(),model.Name);
        }

        [TestMethod]
        public async Task CreateModalFromTx()
        {
            // When: Creating a payee from a given transaction id
            var txid = 1234;
            var actionresult = await itemController.CreateModal(txid);

            var viewresult = Assert.That.IsOfType<PartialViewResult>(actionresult);
            var model = Assert.That.IsOfType<Payee>(viewresult.Model);

            // Then: The item is as we expect it to be
            Assert.AreEqual(txid.ToString(), model.Name);
        }

        [TestMethod]
        public async Task CreateFromTxNotFound()
        {
            // When: Creating a payee from a non-existant id
            var actionresult = await itemController.Create(0);

            // Then: The result is 'not found'
            Assert.That.IsOfType<NotFoundResult>(actionresult);
        }

        [TestMethod]
        public async Task EditModalDetailsFound()
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // When: Retrieving details for a selected item to edit it
            var selected = repository.All.Skip(1).First();
            var actionresult = await itemController.EditModal(selected.ID);

            var viewresult = Assert.That.IsOfType<PartialViewResult>(actionresult);
            var model = Assert.That.IsOfType<Payee>(viewresult.Model);

            // Then: The selected item is the one returned
            Assert.AreEqual(selected, model);
        }

        [TestMethod]
        public async Task BulkEdit()
        {
            // When: Calling BulkEdit
            var actionresult = await itemController.BulkEdit("Test");

            // Then: Returns a redirection to Index
            var redirresult = Assert.That.IsOfType<RedirectToActionResult>(actionresult);
            Assert.AreEqual("Index", redirresult.ActionName);

            // And: Bulk edit operation was performed
            Assert.IsTrue(itemRepository.WasBulkEditCalled);
        }

        [TestMethod]
        public async Task BulkDelete()
        {
            // When: Calling BulkDelete
            var actionresult = await itemController.BulkDelete();

            // Then: Returns a redirection to Index
            var redirresult = Assert.That.IsOfType<RedirectToActionResult>(actionresult);
            Assert.AreEqual("Index", redirresult.ActionName);

            // And: Bulk edit operation was performed
            Assert.IsTrue(itemRepository.WasBulkDeleteCalled);
        }
    }
}
