using Common.DotNet;
using jcoliz.OfficeOpenXml.Serializer;
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
            loader = new SampleDataLoader(context, clock, Environment.CurrentDirectory + "/SampleData");
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(loader);
        }

        [TestMethod]
        public async Task GetSingleOffering()
        {
            // When: Requesting download offerings
            var offerings = await loader.GetDownloadOfferingsAsync();

            // Then: At least one is returned
            Assert.IsTrue(offerings.Count() > 0);

            // And: its ID is as expected
            Assert.AreEqual(1, offerings.Count(x => x.ID == "full"));
        }

        [TestMethod]
        public async Task GetPrimaryOfferings()
        {
            // When: Requesting download offerings
            var offerings = await loader.GetDownloadOfferingsAsync();

            // Then: At least one is returned
            Assert.IsTrue(offerings.Count() >= 3);

            // And: its ID is as expected
            Assert.IsTrue(offerings.Count(x => x.Kind == SampleDataDownloadOfferingKind.Primary) >= 3);
        }

        [TestMethod]
        public async Task GetMonthlyOfferings()
        {
            // When: Requesting download offerings
            var offerings = await loader.GetDownloadOfferingsAsync();

            // Then: Many are returned
            Assert.AreEqual(24, offerings.Count(x => x.Kind == SampleDataDownloadOfferingKind.Monthly));
        }

        [TestMethod]
        public async Task DownloadSingleOffering()
        {
            // When: Requesting the "full" download
            var result = await loader.DownloadSampleDataAsync("full");

            // Then: A spreadsheet is returned
            // Which: Contains all types of data, and lots of it
            using var ssr = new SpreadsheetReader();
            ssr.Open(result);
            var sheets = ssr.SheetNames;
            var expectedsheets = new[] { "Transaction", "Split", "Payee", "BudgetTx" };
            Assert.IsTrue(expectedsheets.All(x=>sheets.Contains(x)));
        }
    }
}
