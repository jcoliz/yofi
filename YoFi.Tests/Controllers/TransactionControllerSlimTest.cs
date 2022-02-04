﻿using Common.DotNet;
using Common.DotNet.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
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
    }
}
