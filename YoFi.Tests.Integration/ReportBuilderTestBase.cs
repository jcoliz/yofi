using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Reports;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    public abstract class ReportBuilderTestBase: IntegrationTest
    {
        #region Fields

        protected static IEnumerable<Transaction> Transactions1000;
        protected static IEnumerable<BudgetTx> BudgetTxs;
        protected IEnumerable<BudgetTx> ManagedBudgetTxs;
        protected static readonly CultureInfo culture = new CultureInfo("en-US");

        protected string h2 = default;
        protected IElement table = default;
        protected string testid;
        protected string total = default;
        protected IEnumerable<IElement> cols;
        protected IHtmlCollection<IElement> rows;
        protected IHtmlDocument document;

        #endregion

        #region Helpers

        protected static IEnumerable<Transaction> Given1000Transactions()
        {
            if (Transactions1000 is null)
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

        protected static IEnumerable<BudgetTx> GivenSampleBudgetTxs()
        {
            if (BudgetTxs is null)
            {
                string json;

                using (var stream = SampleData.Open("BudgetTxs.json"))
                using (var reader = new StreamReader(stream))
                    json = reader.ReadToEnd();

                var txs = System.Text.Json.JsonSerializer.Deserialize<List<BudgetTx>>(json);

                BudgetTxs = txs;
            }
            return BudgetTxs;
        }

        protected IEnumerable<BudgetTx> GivenSampleManagedBudgetTxs()
        {
            if (ManagedBudgetTxs is null)
            {
                string json;

                using (var stream = SampleData.Open("BudgetTxsManaged.json"))
                using (var reader = new StreamReader(stream))
                    json = reader.ReadToEnd();

                var txs = System.Text.Json.JsonSerializer.Deserialize<List<BudgetTx>>(json);

                ManagedBudgetTxs = txs;
            }
            return ManagedBudgetTxs;
        }


        protected async Task WhenGettingReport(string url)
        {
            // First get the outer layout
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Then find the url to the inner report
            document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());
            var element = document.QuerySelector("div.loadr");
            var endpoint = element.GetAttribute("data-endpoint")?.Trim();

            // Finally, get the inner report
            response = await client.GetAsync(endpoint);

            // Then: It's OK
            response.EnsureSuccessStatusCode();

            document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());

            h2 = document.QuerySelector("H2")?.TextContent.Trim();
            var tables = document.QuerySelectorAll("table").ToDictionary(x=>x.GetAttribute("data-test-id").Trim(),x=>x);
            if (tables.Count() == 1)
            {
                var item = tables.Single();
                table = item.Value;
                testid = item.Key;
                total = table.QuerySelector("tr.report-row-total td.report-col-total")?.TextContent.Trim();
                cols = table.QuerySelectorAll("th").Skip(1);
                rows = table.QuerySelectorAll("tbody tr");
            }
        }

        protected async Task WhenGettingReport(ReportParameters parameters)
        {
            // Given: A URL which encodes these report parameters

            var builder = new StringBuilder($"/Report/{parameters.id}?year={parameters.year}");

            if (parameters.showmonths.HasValue)
                builder.Append($"&showmonths={parameters.showmonths}");
            if (parameters.month.HasValue)
                builder.Append($"&month={parameters.month}");
            if (parameters.level.HasValue)
                builder.Append($"&level={parameters.level}");

            var url = builder.ToString();

            // When: Getting a report at that URL from the system
            await WhenGettingReport(url);

            // Then: Is showing the correct report
            if (table != null)
                Assert.AreEqual($"report-{parameters.id}", testid);

            // And: Return to the caller for futher checks
        }

        protected decimal SumOfTopCategory(string category)
        {
            return
                Transactions1000.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount) +
                Transactions1000.Where(x => x.HasSplits).SelectMany(x => x.Splits).Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
        }

        protected decimal SumOfBudgetTxsTopCategory(string category)
        {
            return
                BudgetTxs.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
        }

        protected decimal SumOfManagedBudgetTxsTopCategory(string category)
        {
            return
                ManagedBudgetTxs.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
        }

        protected string GetCell(string col, string row)
        {
            // Note that finding an arbitrary cell in the table is more involved. I didn't want
            // to mark up EVERY cell with a data-test-id. Instead I marked up the headers. So I
            // need to figure out which column## has the data I want, and then look for it in
            // the row cols

            var index = cols.Index(cols.Where(x => x.GetAttribute("data-test-id") == $"col-{col}").Single());
            var result = table.QuerySelectorAll($"tr[data-test-id=row-{row}] td.report-col-amount")[index].TextContent.Trim();

            return result;
        }

        #endregion    
    }
}
