using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    [TestClass]
    public class ReportBuilderTest: IntegrationTest
    {
        [ClassInitialize]
        public static void InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);

            var txs = LoadTransactions();
            context.Transactions.AddRange(txs);
            context.SaveChanges();
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            integrationcontext.Dispose();
        }

        static IEnumerable<Transaction> Transactions1000;

        protected static IEnumerable<Transaction> LoadTransactions()
        {
            if (null == Transactions1000)
            {
                string json;

                using (var stream = SampleData.Open("Transactions1000.json"))
                using (var reader = new StreamReader(stream))
                    json = reader.ReadToEnd();

                var txs = System.Text.Json.JsonSerializer.Deserialize<List<Transaction>>(json);

                Transactions1000 = txs;
            }
            return Transactions1000;
        }

        [TestMethod]
        public void Empty()
        {

        }

        private async Task<IHtmlDocument> WhenGettingReport(string url)
        {
            // First get the outer layout
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Then find the url to the inner report
            var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());
            var element = document.QuerySelector("div.loadr");
            var endpoint = element.GetAttribute("data-endpoint")?.Trim();

            // Finally, get the inner report
            response = await client.GetAsync(endpoint);

            // Then: It's OK
            response.EnsureSuccessStatusCode();

            document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());

            h2 = document.QuerySelector("H2").TextContent.Trim();
            table = document.QuerySelector("table");
            testid = table.GetAttribute("data-test-id").Trim();
            total = table.QuerySelector("tr.report-row-total td.report-col-total").TextContent.Trim();
            cols = table.QuerySelectorAll("tr.report-row-total td.report-col-amount");
            rows = table.QuerySelectorAll("tbody tr");

            return document;
        }

        private string h2 = default;
        private IElement table = default;
        private string testid;
        private string total = default;
        private IHtmlCollection<IElement> cols;
        private IHtmlCollection<IElement> rows;

        [TestMethod]
        public async Task All()
        {
            // When: Getting the report
            var document = await WhenGettingReport("/Report/all?year=2020");

            // Then: On the expected page
            Assert.AreEqual("All Transactions", h2);

            // And: Showing the correct report
            Assert.AreEqual("report-all",testid);

            // Then: Report has the correct total
            Assert.AreEqual(Transactions1000.Sum(x => x.Amount).ToString("C0"), total);

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(13, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(21, rows.Count());
        }
    }
}
