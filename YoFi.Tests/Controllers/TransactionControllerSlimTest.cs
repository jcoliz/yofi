using Common.DotNet;
using Common.DotNet.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Core.SampleGen;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Controllers.Slim
{
    [TestClass]
    public class TransactionControllerSlimTest //: BaseControllerSlimTest<Transaction>
    {
        // Note that I would like to derive from BaseControllerSlimTest, however it will mean implementing
        // a bunch of stuff in mock transaction repository. Task for a later date!

        private TestClock clock;
        private MockTransactionRepository repository;
        private TransactionsController controller;

        [TestInitialize]
        public void SetUp()
        {
            clock = new TestClock() { Now = new DateTime(2022, 1, 1) };
            repository = new MockTransactionRepository();
            controller = new TransactionsController(repository as ITransactionRepository, clock);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public async Task Seed()
        {
            // Given: A mock data loader
            var loader = new Mock<ISampleDataLoader>();
            loader.Setup(x => x.SeedAsync(It.IsAny<string>(),It.IsAny<bool>()));

            // When: Seeding with a given ID
            var id = "hello";
            var actionresult = await controller.Seed(id,loader.Object);

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
            var loader = new Mock<ISampleDataLoader>();
            var message = "words";
            loader.Setup(x => x.SeedAsync(It.IsAny<string>(), It.IsAny<bool>())).Throws(new ApplicationException(message));

            // When: Seeding with a given ID
            var id = "hello";
            var actionresult = await controller.Seed(id, loader.Object);

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
            var loader = new Mock<ISampleDataLoader>();
            var message = "words";
            loader.Setup(x => x.SeedAsync(It.IsAny<string>(), It.IsAny<bool>())).Throws(new AppDomainUnloadedException(message));

            // When: Seeding with a given ID
            var id = "hello";
            var actionresult = await controller.Seed(id, loader.Object);

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
            var dbadmin = new Mock<IDatabaseAdministration>();
            dbadmin.Setup(x => x.ClearDatabaseAsync(It.IsAny<string>()));

            // When: Calling DatabaseDelete with a given ID
            var id = "hello";
            var actionresult = await controller.DatabaseDelete(id, dbadmin.Object);

            // Then: The database administration was called with that ID
            dbadmin.Verify(x => x.ClearDatabaseAsync(id), Times.Once);

            // And: The actionresult is redirect to "/Admin"
            var rpresult = Assert.That.IsOfType<RedirectToPageResult>(actionresult);
            Assert.AreEqual("/Admin", rpresult.PageName);
        }

        [TestMethod]
        public async Task ReceiptActionOther() =>
            Assert.IsTrue(await controller.ReceiptAction(1, string.Empty) is RedirectToActionResult);
    }
}
