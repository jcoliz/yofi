using Common.DotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
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
    }
}
