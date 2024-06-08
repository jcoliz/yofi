using Common.DotNet;
using Common.DotNet.Test;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using YoFi.AspNet.Boilerplate.Models;
using YoFi.AspNet.Controllers;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Core.Repositories.Wire;
using YoFi.Core.SampleData;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Controllers.Slim
{
    [TestClass]
    public class TransactionControllerSlimTest: BaseControllerSlimTest<Transaction>
    {
        // Note that I would like to derive from BaseControllerSlimTest, however it will mean implementing
        // a bunch of stuff in mock transaction repository. Task for a later date!

        private TestClock clock;
        private TransactionsController itemController => base.controller as TransactionsController;
        private MockTransactionRepository itemRepository => base.repository as MockTransactionRepository;

        [TestInitialize]
        public void SetUp()
        {
            clock = new TestClock() { Now = new DateTime(2022, 1, 1) };
            repository = new MockTransactionRepository();
            controller = new TransactionsController(repository as ITransactionRepository, clock);
        }

        [TestMethod]
        public async Task Seed()
        {
            // Given: A mock data loader
            var loader = new Mock<ISampleDataProvider>();
            loader.Setup(x => x.SeedAsync(It.IsAny<string>(),It.IsAny<bool>()));

            // When: Seeding with a given ID
            var id = "hello";
            var actionresult = await itemController.Seed(id,loader.Object);

            // Then: The data loader was called with that ID
            loader.Verify(x => x.SeedAsync(id,false), Times.Once);

            // And: The actionresult is "Completed"
            var pvresult = Assert.That.IsOfType<PartialViewResult>(actionresult);
            (var result, var details) = ((string, string))pvresult.Model;
            Assert.AreEqual("Completed", result);
        }

        [TestMethod]
        public async Task SeedAppException()
        {
            // Given: A mock data loader which will produce an ApplicationException
            var loader = new Mock<ISampleDataProvider>();
            var message = "words";
            loader.Setup(x => x.SeedAsync(It.IsAny<string>(), It.IsAny<bool>())).Throws(new ApplicationException(message));

            // When: Seeding with a given ID
            var id = "hello";
            var actionresult = await itemController.Seed(id, loader.Object);

            // Then: The data loader was called with that ID
            loader.Verify(x => x.SeedAsync(id, false), Times.Once);

            // And: The actionresult is "Sorry"
            var pvresult = Assert.That.IsOfType<PartialViewResult>(actionresult);
            (var result, var details) = ((string, string))pvresult.Model;
            Assert.AreEqual("Sorry", result);

            // And: Details contains {message}
            Assert.IsTrue(details.Contains(message));

            // And: Details contains "E1"
            Assert.IsTrue(details.Contains("E1"));
        }

        [TestMethod]
        public async Task SeedAnyException()
        {
            // Given: A mock data loader which will produce a strangeException
            var loader = new Mock<ISampleDataProvider>();
            var message = "words";
            loader.Setup(x => x.SeedAsync(It.IsAny<string>(), It.IsAny<bool>())).Throws(new AppDomainUnloadedException(message));

            // When: Seeding with a given ID
            var id = "hello";
            var actionresult = await itemController.Seed(id, loader.Object);

            // Then: The data loader was called with that ID
            loader.Verify(x => x.SeedAsync(id, false), Times.Once);

            // And: The actionresult is "Sorry"
            var pvresult = Assert.That.IsOfType<PartialViewResult>(actionresult);
            (var result, var details) = ((string, string))pvresult.Model;
            Assert.AreEqual("Sorry", result);

            // And: Details contains {message}
            Assert.IsTrue(details.Contains(message));

            // And: Details contains "E2"
            Assert.IsTrue(details.Contains("E2"));
        }

        [TestMethod]
        public async Task DatabaseDelete()
        {
            // Given: A mock database administration
            var dbadmin = new Mock<IDataAdminProvider>();
            dbadmin.Setup(x => x.ClearDatabaseAsync(It.IsAny<string>()));

            // When: Calling DatabaseDelete with a given ID
            var id = "hello";
            var actionresult = await itemController.DatabaseDelete(id, dbadmin.Object);

            // Then: The database administration was called with that ID
            dbadmin.Verify(x => x.ClearDatabaseAsync(id), Times.Once);

            // And: The actionresult is redirect to "/Admin"
            var rpresult = Assert.That.IsOfType<RedirectToPageResult>(actionresult);
            Assert.AreEqual("/Admin", rpresult.PageName);
        }

        [TestMethod]
        public async Task ReceiptActionOther() =>
            Assert.IsTrue(await itemController.ReceiptAction(1, string.Empty) is RedirectToActionResult);

        [TestMethod]
        public void Error()
        {
            var expected = "Bah, humbug!";
            var httpcontext = new DefaultHttpContext() { TraceIdentifier = expected };
            itemController.ControllerContext.HttpContext = httpcontext;
            var actionresult = itemController.Error();
            var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
            var model = Assert.That.IsOfType<ErrorViewModel>(viewresult.Model);
            Assert.AreEqual(expected, model.RequestId);
        }

        [TestMethod]
        public async Task CreateSplit()
        {
            // Given: A mocked repository
            var repo = new Mock<ITransactionRepository>();
            repo.Setup(x => x.AddSplitToAsync(98)).Returns(Task.FromResult(99));
            controller = new TransactionsController(repo.Object, clock);

            // When: Creating a split
            var actionresult = await itemController.CreateSplit(98);

            // Then: Redirected to edit page for that
            var redirect = Assert.That.IsOfType<RedirectToActionResult>(actionresult);
            Assert.AreEqual("Edit", redirect.ActionName);
            Assert.AreEqual("Splits", redirect.ControllerName);
            Assert.AreEqual(99, redirect.RouteValues["id"]);
        }
        [TestMethod]
        public async Task IndexBadArgs()
        {
            // Given: A mocked repository
            var repo = new Mock<ITransactionRepository>();
            repo.Setup(x => x.GetByQueryAsync(It.IsAny<IWireQueryParameters>())).Throws(new ArgumentException());
            controller = new TransactionsController(repo.Object, clock);

            // When: Calling Index with bad arguments
            var actionresult = await itemController.Index();

            // Then: Badrequest
            var redirect = Assert.That.IsOfType<BadRequestResult>(actionresult);
        }

        // TODO: Move to unit tests
        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task EditPayeeMatch(bool modal)
        {
            // Given: Mocked repositories
            var txrepo = new Mock<ITransactionRepository>();
            txrepo.Setup(x => x.GetWithSplitsByIdAsync(99)).Returns(Task.FromResult(new Transaction() { Payee = "PayeeName" } ));
            controller = new TransactionsController(txrepo.Object, clock);

            var payrepo = new Mock<IPayeeRepository>();
            payrepo.Setup(x => x.GetCategoryMatchingPayeeAsync("PayeeName")).Returns(Task.FromResult("PayeeCategory"));

            // When: Calling Edit with a category that should match
            Transaction model;
            if (modal)
            {
                var actionresult = await itemController.EditModal(99, payrepo.Object);
                var viewresult = Assert.That.IsOfType<PartialViewResult>(actionresult);
                model = Assert.That.IsOfType<Transaction>(viewresult.Model);
            }
            else
            {
                var actionresult = await itemController.Edit(99, payrepo.Object,null);
                var viewresult = Assert.That.IsOfType<ViewResult>(actionresult);
                model = Assert.That.IsOfType<Transaction>(viewresult.Model);
            }

            // Then: Resulting transaction has expected category
            Assert.AreEqual("PayeeCategory", model.Category);
        }

        [TestMethod]
        public async Task BulkEdit()
        {
            // Given: Mocked repositories
            var category = "BulkEdit";
            var txrepo = new Mock<ITransactionRepository>();
            txrepo.Setup(x => x.BulkEditAsync(category));
            controller = new TransactionsController(txrepo.Object, clock);

            // When: BulkEdit with a chosen category
            var actionresult = await itemController.BulkEdit(category);

            // Then: BulkEdit operation was taken
            txrepo.Verify(x => x.BulkEditAsync(category), Times.Once());
            Assert.IsTrue(true);
        }

        // Upload from transactions controller is not done. All transaction upload is via importer page now.
        public override Task Upload() => Task.CompletedTask;

    }
}
