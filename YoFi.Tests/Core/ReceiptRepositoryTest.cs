using Common.DotNet;
using Common.DotNet.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using YoFi.Core.Repositories;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class ReceiptRepositoryTest
    {
        ReceiptRepository repository;
        MockTransactionRepository txrepo;
        TestAzureStorage storage;
        TestClock clock;

        [TestInitialize]
        public void SetUp()
        {
            txrepo = new MockTransactionRepository();
            clock = new TestClock();
            storage = new TestAzureStorage();
            repository = new ReceiptRepository(txrepo,storage,clock);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(repository);
        }

    }
}
