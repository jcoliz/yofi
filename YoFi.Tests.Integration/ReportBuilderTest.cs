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
        public static async Task InitialSetup(TestContext tcontext)
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

        [TestMethod]
        public async Task All()
        {
            // When: Getting the report

            // First get the outer layout
            var response = await client.GetAsync("/Report/all?year=2020");
            response.EnsureSuccessStatusCode();

            // Then find the url to the inner report
            var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());
            var element = document.QuerySelector("div.loadr");
            var endpoint = element.GetAttribute("data-endpoint")?.Trim();

            // Finally, get the inner report
            response = await client.GetAsync(endpoint);

            // Then: It's OK
            response.EnsureSuccessStatusCode();

            // And: On the expected page
            var expected = "All Transactions";
            document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());
            var h2 = document.QuerySelector("H2");
            Assert.AreEqual(expected, h2.TextContent.Trim());

            // And: Showing the correct report
            var table = document.QuerySelector("table");
            var testid = table.GetAttribute("data-test-id").Trim();
            Assert.AreEqual("report-all",testid);

            // Then: Report has the correct total
            var sum = Transactions1000.Sum(x => x.Amount);
            var total = table.QuerySelector("tr.report-row-total td.report-col-total").TextContent.Trim();
            Assert.AreEqual(sum.ToString("C0"), total);

            // And: Report has the correct # columns (One for each month plus total)
            var cols = table.QuerySelectorAll("tr.report-row-total td.report-col-amount");
            Assert.AreEqual(13, cols.Count());

            // And: Report has the correct # rows
            var rows = table.QuerySelectorAll("tbody tr");
            Assert.AreEqual(21, rows.Count());
        }
    }
}
