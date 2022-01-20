﻿using Common.DotNet;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
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
            using var ssr = new SpreadsheetReader();
            ssr.Open(result);

            // Which: Contains all types of data
            var sheets = ssr.SheetNames;
            var expectedsheets = new[] { "Transaction", "Split", "Payee", "BudgetTx" };
            Assert.IsTrue(expectedsheets.All(x=>sheets.Contains(x)));

            // And which: Contains lots of transactions
            var txs = ssr.Deserialize<Transaction>();
            Assert.AreEqual(889, txs.Count());
        }

        [DataRow("BudgetTx")]
        [DataRow("Payee")]
        [DataTestMethod]
        public async Task DownloadOtherPrimaryOfferings(string what)
        {
            // When: Requesting the "full" download
            var result = await loader.DownloadSampleDataAsync(what.ToLowerInvariant());

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
            var offerings = await loader.GetDownloadOfferingsAsync();
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
                var expectedsheets = new[] { "Transaction", "Split" };
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
            var offerings = await loader.GetDownloadOfferingsAsync();
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
    }
}
