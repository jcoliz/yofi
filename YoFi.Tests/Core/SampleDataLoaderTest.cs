using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using YoFi.Core.SampleGen;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core.SampleGen
{
    [TestClass]
    public class SampleDataLoaderTest
    {
        ISampleDataLoader loader;
        MockDataContext context;

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            loader = new SampleDataLoader(context, string.Empty);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(loader);
        }
    }
}
