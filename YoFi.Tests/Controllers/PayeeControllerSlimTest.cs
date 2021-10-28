using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Common;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Controllers.Slim
{
    [TestClass]
    public class PayeeControllerSlimTest: BaseControllerSlimTest<Payee>
    {
        private PayeesController payeeController => base.controller as PayeesController;
        private MockPayeeRepository payeeRepository => base.repository as MockPayeeRepository;

        [TestInitialize]
        public void SetUp()
        {
            repository = new MockPayeeRepository();
            controller = new PayeesController(repository as IPayeeRepository);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task CreateFromTx(bool ok)
        {
            // Given: Repository in the given state
            repository.Ok = ok;

            // When: Creating a payee from a given transaction id
            var txid = 1234;
            var actionresult = await payeeController.Create(txid);
            ThenSucceedsOrFailsAsExpected(actionresult, ok);
            if (!ok) return;

            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<Payee>(viewresult.Model);

            // Then: The item is as we expect it to be
            Assert.AreEqual(txid.ToString(),model.Name);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task CreateModalFromTx(bool ok)
        {
            // Given: Repository in the given state
            repository.Ok = ok;

            // When: Creating a payee from a given transaction id
            var txid = 1234;
            var actionresult = await payeeController.CreateModal(txid);
            ThenSucceedsOrFailsAsExpected(actionresult, ok);
            if (!ok) return;

            var viewresult = Assert.That.IsOfType<PartialViewResult>(actionresult);
            var model = Assert.That.IsOfType<Payee>(viewresult.Model);

            // Then: The item is as we expect it to be
            Assert.AreEqual(txid.ToString(), model.Name);
        }

        [TestMethod]
        public async Task CreateFromTxNotFound()
        {
            // When: Creating a payee from a non-existant id
            var actionresult = await payeeController.Create(0);
            ThenSucceedsOrFailsAsExpected(actionresult, ok:true);

            // Then: The result is 'not found'
            Assert.That.IsOfType<NotFoundResult>(actionresult);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task EditModalDetailsFound(bool ok)
        {
            // Given: Five items in the respository
            repository.AddItems(5);

            // And: Repository in the given state
            repository.Ok = ok;

            // When: Retrieving details for a selected item to edit it
            var selected = repository.All.Skip(1).First();
            var actionresult = await payeeController.EditModal(selected.ID);
            ThenSucceedsOrFailsAsExpected(actionresult, ok);
            if (!ok) return;

            var viewresult = Assert.That.IsOfType<PartialViewResult>(actionresult);
            var model = Assert.That.IsOfType<Payee>(viewresult.Model);

            // Then: The selected item is the one returned
            Assert.AreEqual(selected, model);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task BulkEdit(bool ok)
        {
            // Given: Repository in the given state
            repository.Ok = ok;

            // When: Calling BukeEdit
            var actionresult = await payeeController.BulkEdit("Test");
            ThenSucceedsOrFailsAsExpected(actionresult, ok);
            if (!ok) return;

            // Then: Returns a redirection to Index
            var redirresult = Assert.That.IsOfType<RedirectToActionResult>(actionresult);
            Assert.AreEqual("Index", redirresult.ActionName);

            // And: Bulk edit operation was performed
            Assert.IsTrue(payeeRepository.WasBulkEditCalled);
        }

    }
}
