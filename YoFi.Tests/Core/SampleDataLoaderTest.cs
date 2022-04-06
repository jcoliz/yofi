using Common.DotNet;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.SampleData;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class SampleDataLoaderTest
    {
        ISampleDataProvider loader;
        MockDataContext context;
        TestClock clock;

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            clock = new TestClock() { Now = new DateTime(2022, 1, 1) };
            var dir = Environment.CurrentDirectory + "/SampleData";
            var config = new Mock<ISampleDataConfiguration>();
            config.Setup(x => x.Directory).Returns(dir);
            loader = new SampleDataProvider(context, clock, config.Object);
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
            var offerings = await loader.ListDownloadOfferingsAsync();

            // Then: At least one is returned
            Assert.IsTrue(offerings.Count() > 0);

            // And: its ID is as expected
            Assert.AreEqual(1, offerings.Count(x => x.ID == "full"));
        }

        [TestMethod]
        public async Task GetPrimaryOfferings()
        {
            // When: Requesting download offerings
            var offerings = await loader.ListDownloadOfferingsAsync();

            // Then: At least one is returned
            Assert.IsTrue(offerings.Count() >= 3);

            // And: its ID is as expected
            Assert.IsTrue(offerings.Count(x => x.Kind == SampleDataDownloadOfferingKind.Primary) >= 3);
        }

        [TestMethod]
        public async Task GetMonthlyOfferings()
        {
            // When: Requesting download offerings
            var offerings = await loader.ListDownloadOfferingsAsync();

            // Then: Many are returned
            Assert.AreEqual(24, offerings.Count(x => x.Kind == SampleDataDownloadOfferingKind.Monthly));
        }

        [TestMethod]
        public async Task DownloadSingleOffering()
        {
            // When: Requesting the "full" download
            var result = await loader.DownloadSampleDataAsync("full");

            // Then: A spreadsheet is returned
            using var ssr = new SpreadsheetReader();
            ssr.Open(result);

            // Which: Contains all types of data
            var sheets = ssr.SheetNames;
            var expectedsheets = new[] { nameof(Transaction), nameof(Split), nameof(Payee), nameof(BudgetTx) };
            Assert.IsTrue(expectedsheets.All(x=>sheets.Contains(x)));

            // And which: Contains lots of transactions
            var txs = ssr.Deserialize<Transaction>();
            Assert.AreEqual(889, txs.Count());
        }

        [DataRow(nameof(BudgetTx))]
        [DataRow(nameof(Payee))]
        [DataTestMethod]
        public async Task DownloadOtherPrimaryOfferings(string what)
        {
            // When: Requesting the "full" download
            var result = await loader.DownloadSampleDataAsync(what);

            // Then: A spreadsheet is returned
            using var ssr = new SpreadsheetReader();
            ssr.Open(result);

            // Which: Contains {what} kind of data
            var sheets = ssr.SheetNames;
            Assert.IsTrue(sheets.Contains(what));
        }

        [TestMethod]
        public async Task DownloadMontlhyXLSXOfferings()
        {
            // Given: Known set of monthly XLSX offerings
            var offerings = await loader.ListDownloadOfferingsAsync();
            var desired = offerings.Where(x => x.Kind == SampleDataDownloadOfferingKind.Monthly && x.FileType == SampleDataDownloadFileType.XLSX);

            // For: Each offering
            foreach(var offering in desired)
            {
                // When: Requesting the download
                var result = await loader.DownloadSampleDataAsync(offering.ID);

                // Then: A spreadsheet is returned
                using var ssr = new SpreadsheetReader();
                ssr.Open(result);

                // Which: Contains ONLY Transactions and/or Splits
                var sheets = ssr.SheetNames;
                var expectedsheets = new[] { nameof(Transaction), nameof(Split) };
                Assert.IsTrue(sheets.All(x => expectedsheets.Contains(x)));

                // And: About the right amount of transactions
                var txs = ssr.Deserialize<Transaction>();
                var count = txs.Count();
                Assert.IsTrue(count < 100);
                Assert.IsTrue(count > 20);
            }
        }

        [TestMethod]
        public async Task DownloadMontlhyOFXOfferings()
        {
            // Given: Known set of monthly OFX offerings
            var offerings = await loader.ListDownloadOfferingsAsync();
            var desired = offerings.Where(x => x.Kind == SampleDataDownloadOfferingKind.Monthly && x.FileType == SampleDataDownloadFileType.OFX);

            // For: Each offering
            foreach (var offering in desired)
            {
                // When: Requesting the download
                var result = await loader.DownloadSampleDataAsync(offering.ID);

                // Then: An OFX file is returned
                var Document = await OfxSharp.OfxDocumentReader.FromSgmlFileAsync(result);

                // Which: Contains about the right amount of transactions
                var txs = Document.Statements.SelectMany(x => x.Transactions);
                var count = txs.Count();
                Assert.IsTrue(count < 100);
                Assert.IsTrue(count > 20);
            }
        }

        [ExpectedException(typeof(ApplicationException))]
        [TestMethod]
        public async Task BogusSampleDataFails()
        {
            // When: Requesting download with a bogus ID
            await loader.DownloadSampleDataAsync("Bogus");

            // Then: Exception is thrown
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public async Task BogusSampleDataFileTypeFails()
        {
            // When: Requesting download with a bogus ID
            await loader.DownloadSampleDataAsync("Bogus-1");

            // Then: Exception is thrown
        }

        [ExpectedException(typeof(ApplicationException))]
        [TestMethod]
        public async Task SampleDataFileTypeNoneFails()
        {
            // When: Requesting download with a bogus ID
            await loader.DownloadSampleDataAsync("None-1");

            // Then: Exception is thrown
        }

        [TestMethod]
        public async Task GetSingleSeedOffering()
        {
            // When: Requesting download offerings
            var offerings = await loader.ListSeedOfferingsAsync();

            // Then: At least one is returned
            Assert.IsTrue(offerings.Count() > 0);

            // And: its ID is as expected
            Assert.AreEqual(1, offerings.Count(x => x.ID == "today"));
        }

        [TestMethod]
        public async Task GetAllSeedOffering()
        {
            // When: Requesting download offerings
            var offerings = await loader.ListSeedOfferingsAsync();

            // Then: At least one is returned
            Assert.IsTrue(offerings.Count() >= 6);
        }

        [TestMethod]
        public async Task AllSeedOfferingAvailable()
        {
            // When: Requesting download offerings
            var offerings = await loader.ListSeedOfferingsAsync();

            // Then: All are returned
            Assert.AreEqual(6,offerings.Count(x=>x.IsAvailable));
        }

        [TestMethod]
        public async Task SeedOfferingTransactionsUnAvailable()
        {
            // Given: One transaction in the database
            context.Add(new Transaction());

            // When: Requesting download offerings
            var offerings = await loader.ListSeedOfferingsAsync();

            // Then: correct number are returned
            Assert.AreEqual(4, offerings.Count(x => x.IsAvailable));
        }

        [TestMethod]
        public async Task SeedOfferingPayeeAvailable()
        {
            // Given: One transaction in the database
            context.Add(new Payee());

            // When: Requesting download offerings
            var offerings = await loader.ListSeedOfferingsAsync();

            // Then: correct number are returned
            Assert.AreEqual(3, offerings.Count(x => x.IsAvailable));
        }

        [DataRow("payee")]
        [DataRow("today")]
        [DataRow("budget")]
        [DataRow("txyear")]
        [DataRow("all")]
        [DataRow("txtoday")]
        [DataTestMethod]
        public async Task ApplySeedOffering(string id)
        {
            // Given: Empty Database
            //...

            // And: It's April 1st
            clock.IsLocked = true; // Need to lock so that "timestamp == now" calculation in RulesOK works ok
            clock.Now = new DateTime(clock.Now.Year, 3, 22);

            // When: Seeding with the chosen offering {id}
            await loader.SeedAsync(id);

            // Then: That item is no longer allowed
            var offerings = await loader.ListSeedOfferingsAsync();
            var chosen = offerings.Where(x => x.ID == id).Single();
            Assert.IsFalse(chosen.IsAvailable);

            // And: The correct number and type of items are in the database
            if (chosen.Rules.Contains("Today"))
            {
                Assert.AreEqual(202, context.Get<Transaction>().Count());
                Assert.IsFalse(context.Get<Transaction>().Any(x => x.Timestamp > clock.Now));
            }
            if (chosen.Rules.Contains(nameof(Transaction)))
                Assert.AreEqual(889, context.Get<Transaction>().Count());
            if (chosen.Rules.Contains(nameof(Payee)))
                Assert.AreEqual(40, context.Get<Payee>().Count());
            if (chosen.Rules.Contains(nameof(BudgetTx)))
                Assert.AreEqual(46, context.Get<BudgetTx>().Count());
        }

        [TestMethod]
        public async Task ApplySeedOfferingTxTodayTwice()
        {
            // Given: Already seeded months into the year
            clock.Now = new DateTime(clock.Now.Year, 4, 1);
            await loader.SeedAsync("txtoday");

            // And: Now it's the end of the year
            clock.Now = new DateTime(clock.Now.Year, 12, 31);

            // When: Seeding again with the offering id "txtoday"
            await loader.SeedAsync("txtoday");

            // Then: The correct number and type of items are in the database
            Assert.AreEqual(889, context.Get<Transaction>().Count());
        }

        [ExpectedException(typeof(ApplicationException))]
        [TestMethod]
        public async Task IDNotFound()
        {
            // When: Seeding with a bogus ID
            await loader.SeedAsync("bogus");

            // Then: Throws exception
        }

        [ExpectedException(typeof(ApplicationException))]
        [TestMethod]
        public async Task NotAvailable()
        {
            // Given: Already seeded with the chosen offering {id}
            var id = "payee";
            await loader.SeedAsync(id);

            // When: Seeding with it again
            await loader.SeedAsync(id);

            // Then: Throws exception
        }
    }
}
