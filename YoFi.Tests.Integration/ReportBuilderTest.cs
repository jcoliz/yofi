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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Reports;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    [TestClass]
    public class ReportBuilderTest: IntegrationTest
    {
        #region Fields

        static IEnumerable<Transaction> Transactions1000;
        static IEnumerable<BudgetTx> BudgetTxs;
        IEnumerable<BudgetTx> ManagedBudgetTxs;
        static readonly CultureInfo culture = new CultureInfo("en-US");

        private string h2 = default;
        private IElement table = default;
        private string testid;
        private string total = default;
        private IEnumerable<IElement> cols;
        private IHtmlCollection<IElement> rows;

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

        public IEnumerable<BudgetTx> GivenSampleManagedBudgetTxs()
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


        private async Task WhenGettingReport(string url)
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
            total = table.QuerySelector("tr.report-row-total td.report-col-total")?.TextContent.Trim();
            cols = table.QuerySelectorAll("th").Skip(1);
            rows = table.QuerySelectorAll("tbody tr");
        }

        private async Task WhenGettingReport(ReportParameters parameters)
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
            Assert.AreEqual($"report-{parameters.id}", testid);

            // And: Return to the caller for futher checks
        }

        decimal SumOfTopCategory(string category)
        {
            return
                Transactions1000.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount) +
                Transactions1000.Where(x => x.HasSplits).SelectMany(x => x.Splits).Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
        }

        decimal SumOfBudgetTxsTopCategory(string category)
        {
            return
                BudgetTxs.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
        }

        decimal SumOfManagedBudgetTxsTopCategory(string category)
        {
            return
                ManagedBudgetTxs.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
        }

        private string GetCell(string col, string row)
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

        #region Init/Cleanup

        [ClassInitialize]
        public static void InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);

            var txs = Given1000Transactions();
            context.Transactions.AddRange(txs);
            var btxs = GivenSampleBudgetTxs();
            context.BudgetTxs.AddRange(btxs);
            context.SaveChanges();
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            integrationcontext.Dispose();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Remove ephemeral items
            context.BudgetTxs.RemoveRange(context.BudgetTxs.Where(x=>x.Memo == "__TEST__"));
            context.SaveChanges();
        }

        #endregion

        #region Tests

        [TestMethod]
        public void Empty()
        {

        }

        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        public async Task All(bool showmonths)
        {
            // Given: A large database of transactions
            // (Assembled on ClassInitialize)

            // When: Getting the report
            await WhenGettingReport( new ReportParameters() { id = "all", year = 2020, showmonths = showmonths } );

            // Then: On the expected page
            Assert.AreEqual("All Transactions", h2);

            // And: Report has the correct total
            Assert.AreEqual(Transactions1000.Sum(x => x.Amount).ToString("C0",culture), total);

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(showmonths?13:1, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(21, rows.Count());
        }

        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataTestMethod]
        public async Task AllLevels(int level)
        {
            // Given: A large database of transactions
            // (Assembled on Initialize)

            // When: Getting the report
            await WhenGettingReport(new ReportParameters() { id = "all", year = 2020, showmonths = true, level = level });

            // Then: On the expected page
            Assert.AreEqual("All Transactions", h2);

            // And: Report has the correct total
            Assert.AreEqual(Transactions1000.Sum(x => x.Amount).ToString("C0", culture), total);

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(13, cols.Count());

            // And: Report has the correct # rows
            var rowset = new int[] { 9, 21, 24, 26 };
            Assert.AreEqual(rowset[level - 1], rows.Count());

            // And: Report has the right levels
            // Note that we are using the report-row-x class
            var regex = new Regex("report-row-([0-9]+)");
            var levels = rows
                    .SelectMany(row => row.ClassList)
                    .Select(@class => regex.Match(@class))
                    .Where(match => match.Success)
                    .Select(match => match.Groups.Values.Last().Value)
                    .Select(value => int.Parse(value))
                    .Distinct();

            Assert.AreEqual(level, levels.Count());
        }

        [DataRow(1)]
        [DataRow(3)]
        [DataRow(6)]
        [DataRow(9)]
        [DataRow(12)]
        [DataTestMethod]
        public async Task AllMonths(int month)
        {
            // Given: A large database of transactions
            // (Assembled on Initialize)

            // When: Building the 'All' report for the correct year, with level at '{level}'
            var report = "all";
            await WhenGettingReport(new ReportParameters() { id = "all", year = 2020, showmonths = true, month = month });

            // Then: On the expected page
            Assert.AreEqual("All Transactions", h2);

            // And: Showing the correct report
            Assert.AreEqual($"report-{report}", testid);

            // And: Report has the correct total
            var expected = Transactions1000.Where(x => x.Timestamp.Month <= month).Sum(x => x.Amount);
            Assert.AreEqual(expected.ToString("C0", culture), total);

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(month + 1, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(21, rows.Count());
        }

        [DataRow("Income")]
        [DataRow("Taxes")]
        [DataRow("Savings")]
        [DataTestMethod]
        public async Task SingleTop(string category)
        {
            // Given: A large database of transactions
            // (Assembled on Initialize)

            // When: Building the '{Category}' report for the correct year
            await WhenGettingReport(new ReportParameters() { id = category.ToLowerInvariant(), year = 2020 });

            // Then: Report has the correct total
            var expected = SumOfTopCategory(category);
            Assert.AreEqual(expected.ToString("C0", culture), total);

            // And: Report has the correct # columns (Total & pct total)
            Assert.AreEqual(2, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(3, rows.Count());
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task ExpensesDetail(bool showmonths)
        {
            // Given: A large database of transactions
            // (Assembled on Initialize)

            // When: Building the 'expenses-detail' report for the correct year
            await WhenGettingReport(new ReportParameters() { id = "expenses-detail", year = 2020, showmonths = showmonths });

            // Then: Report has the correct total
            var expected = Transactions1000.Sum(x => x.Amount) - SumOfTopCategory("Taxes") - SumOfTopCategory("Savings") - SumOfTopCategory("Income");
            Assert.AreEqual(expected.ToString("C0", culture), total);

            // And: Report has the correct # columns (12 months, plus Total & pct total)
            Assert.AreEqual(showmonths ? 14 : 2, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(12, rows.Count());
        }

        [TestMethod]
        public async Task Budget()
        {
            // Given: A large database of transactions and budgettxs
            // (Assembled on Initialize)

            // When: Building the 'budget' report for the correct year
            await WhenGettingReport(new ReportParameters() { id = "budget", year = 2020 });

            // Then: Report has the correct total
            var expected = BudgetTxs.Sum(x => x.Amount);
            Assert.AreEqual(expected.ToString("C0", culture), total);

            // And: Report has the correct # columns, just 1 the budget itself
            Assert.AreEqual(1, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(13, rows.Count());
        }

        [TestMethod]
        public async Task ExpensesBudget()
        {
            // Given: A large database of transactions and budgettxs
            // (Assembled on Initialize)

            // When: Building the 'expenses-budget' report for the correct year
            await WhenGettingReport(new ReportParameters() { id = "expenses-budget", year = 2020 });

            // Then: Report has the correct total
            var expected = BudgetTxs.Sum(x => x.Amount) - SumOfBudgetTxsTopCategory("Taxes") - SumOfBudgetTxsTopCategory("Savings") - SumOfBudgetTxsTopCategory("Income");
            Assert.AreEqual(expected.ToString("C0", culture), total);

            // And: Report has the correct # columns, just 1 the budget itself
            Assert.AreEqual(1, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(7, rows.Count());
        }

        [TestMethod]
        public async Task Expenses_V_Budget()
        {
            // Given: A large database of transactions and budgettxs
            // (Assembled on Initialize)

            // When: Building the 'expenses-v-budget' report for the correct year
            await WhenGettingReport(new ReportParameters() { id = "expenses-v-budget", year = 2020 });

            // Then: Report has the correct total budget
            var expected = BudgetTxs.Sum(x => x.Amount) - SumOfBudgetTxsTopCategory("Taxes") - SumOfBudgetTxsTopCategory("Savings") - SumOfBudgetTxsTopCategory("Income");
            var budgettotal = table.QuerySelector("td[data-test-id=total-Budget]").TextContent.Trim();
            Assert.AreEqual(expected.ToString("C0", culture), budgettotal);

            // And: Report has the correct actual total
            expected = Transactions1000.Sum(x => x.Amount) - SumOfTopCategory("Taxes") - SumOfTopCategory("Savings") - SumOfTopCategory("Income");
            var actualtotal = table.QuerySelector("td[data-test-id=total-Actual]").TextContent.Trim();
            Assert.AreEqual(expected.ToString("C0", culture), actualtotal);

            // And: Report has the correct # visible columns, budget, actual, progress
            Assert.AreEqual(4, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(12, rows.Count());
        }

        [TestMethod]
        public async Task All_V_Budget()
        {
            // Given: A large database of transactions and budgettxs
            // (Assembled on Initialize)

            // When: Building the 'all-v-budget' report for the correct year
            await WhenGettingReport(new ReportParameters() { id = "all-v-budget", year = 2020 });

            // Then: Report has the correct total budget
            var expected = BudgetTxs.Sum(x => x.Amount);
            var budgettotal = table.QuerySelector("td[data-test-id=total-Budget]").TextContent.Trim();
            Assert.AreEqual(expected.ToString("C0", culture), budgettotal);

            // And: Report has the correct actual total
            expected = Transactions1000.Sum(x => x.Amount);
            var actualtotal = table.QuerySelector("td[data-test-id=total-Actual]").TextContent.Trim();
            Assert.AreEqual(expected.ToString("C0", culture), actualtotal);

            // And: Report has the correct # visible columns, budget, actual, progress
            Assert.AreEqual(3, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(22, rows.Count());
        }

        [TestMethod]
        public async Task ManagedBudget()
        {
            // Given: A large database of transactions and budgettxs, including a mix of monthly and yearly budget txs
            // Most are Assembled on Initialize, but we need to add managed txs
            context.BudgetTxs.AddRange(GivenSampleManagedBudgetTxs());
            context.SaveChanges();

            // When: Building the 'managed-budget' report for the correct year
            await WhenGettingReport(new ReportParameters() { id = "managed-budget", year = 2020 });

            // Then: Report has the correct values 

            var expected = SumOfManagedBudgetTxsTopCategory("Income");
            Assert.AreEqual(expected.ToString("C0", culture), GetCell("Budget","Income"));

            expected = SumOfManagedBudgetTxsTopCategory("J");
            Assert.AreEqual(expected.ToString("C0", culture), GetCell("Budget", "J"));

            expected = SumOfTopCategory("Income");
            Assert.AreEqual(expected.ToString("C0", culture), GetCell("Actual", "Income"));

            expected = SumOfTopCategory("J");
            Assert.AreEqual(expected.ToString("C0", culture), GetCell("Actual", "J"));

            // And: Report has the correct # displayed columns: budget, actual, progress, remaining
            Assert.AreEqual(4, cols.Count());

            // And: Report has the correct # rows: just the 2 managed budgets
            Assert.AreEqual(2, rows.Count());
        }

        #endregion
    }
}
