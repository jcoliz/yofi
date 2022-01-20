using Common.DotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.SampleGen;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core.SampleGen
{
    [TestClass]
    public class SampleDataLoaderTest
    {
        ISampleDataLoader loader;
        MockDataContext context;
        TestClock clock;

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            clock = new TestClock() { Now = new DateTime(2022, 1, 1) };
            loader = new SampleDataLoader(context, clock, string.Empty);
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(loader);
        }

        [TestMethod]
        public async Task GetSingleOfferings()
        {
            // When: Requesting download offerings
            var offerings = await loader.GetDownloadOfferingsAsync();

            // Then: At least one is returned
            Assert.IsTrue(offerings.Count() > 0);

            // And: its ID is as expected
            Assert.AreEqual(1, offerings.Count(x => x.ID == "full"));
        }

        public async Task GetDownloadOfferings()
        {
            // When: Requesting download offerings
            var offerings = await loader.GetDownloadOfferingsAsync();

            // Then: Many are returned
            Assert.IsTrue(offerings.Count() > 20);
        }
    }
}
