using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Reports;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Reports
{
    [TestClass]
    public class ReportBuilderApiTest: IntegrationTest
    {
        #region Fields

        protected static SampleDataStore data;
        private JsonElement.ArrayEnumerator rows;
        private JsonElement totalrow;
        private IEnumerable<JsonProperty> cols;

        #endregion

        #region Init/Cleanup

        [ClassInitialize]
        public static async Task InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);

            // Given: A large database of transactions and budgettxs

            await SampleDataStore.LoadPartialAsync();
            data = SampleDataStore.Single;

            context.Transactions.AddRange(data.Transactions);
            context.BudgetTxs.AddRange(data.BudgetTxs);
            context.SaveChanges();
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            integrationcontext.Dispose();
        }

        [TestInitialize]
        public void SetUp()
        {
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:{integrationcontext.apiconfig.Key}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Remove ephemeral items
            context.BudgetTxs.RemoveRange(context.BudgetTxs.Where(x => x.Memo == "__TEST__"));
            context.SaveChanges();
        }

        #endregion

        #region Helpers

        protected async Task WhenGettingReport(ReportParameters parameters)
        {
            // Given: A URL which encodes these report parameters

            var start = $"/api/ReportV2/{parameters.slug}?year={parameters.year}";
            var builder = new StringBuilder(start);

            if (parameters.showmonths.HasValue)
                builder.Append($"&showmonths={parameters.showmonths}");
            if (parameters.month.HasValue)
                builder.Append($"&month={parameters.month}");
            if (parameters.level.HasValue)
                builder.Append($"&level={parameters.level}");

            var url = builder.ToString();

            // When: Getting the report
            var response = await client.GetAsync(url);

            // Then: Response is OK
            response.EnsureSuccessStatusCode();

            // And: Parse for further checking
            var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            rows = root.EnumerateArray();
            totalrow = rows.Where(x => x.GetProperty("IsTotal").GetBoolean()).SingleOrDefault();
            var lookrow = (totalrow.ValueKind == JsonValueKind.Object) ? totalrow : rows.FirstOrDefault();
            cols = 
                (lookrow.ValueKind == JsonValueKind.Object)
                ? lookrow.EnumerateObject().Where(x => x.Name.StartsWith("ID:") || x.Name.StartsWith("Name:") || x.Name == "TOTAL") 
                : default;
        }

        private void ThenReportHasTotal(decimal expected)
        {
            var totalvalue = totalrow.GetProperty("TOTAL").GetDecimal();
            Assert.AreEqual(expected, totalvalue);
        }

        protected decimal SumOfCategory(string category)
        {
            return
                data.Transactions.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category == category).Sum(x => x.Amount) +
                data.Transactions.Where(x => x.HasSplits).SelectMany(x => x.Splits).Where(x => !string.IsNullOrEmpty(x.Category) && x.Category == category).Sum(x => x.Amount);
        }

        protected decimal SumOfTopCategory(string category)
        {
            return
                data.Transactions.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount) +
                data.Transactions.Where(x => x.HasSplits).SelectMany(x => x.Splits).Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
        }

        protected decimal SumOfBudgetTxsTopCategory(string category)
        {
            return
                data.BudgetTxs.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
        }

        protected decimal SumOfManagedBudgetTxsTopCategory(string category)
        {
            return
                data.ManagedBudgetTxs.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
        }

        protected double GetCell(string colname, string rowname)
        {
            var row = rows.Where(x => x.GetProperty("Name").GetString() == rowname).Single();
            var col = row.EnumerateObject().Where(x => x.Name.EndsWith(colname)).Single().Value.GetDouble();

            return col;
        }

        #endregion

        #region Tests

        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        public async Task All(bool showmonths)
        {
            // Given: A large database of transactions
            // (Assembled on ClassInitialize)

            // When: Getting the report
            await WhenGettingReport(new ReportParameters() { slug = "all", year = 2020, showmonths = showmonths });

            // And: Report has the correct total
            ThenReportHasTotal(data.Transactions.Sum(x => x.Amount));

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(showmonths ? 13 : 1, cols.Count());

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
            await WhenGettingReport(new ReportParameters() { slug = "all", year = 2020, showmonths = true, level = level });

            // And: Report has the correct total
            ThenReportHasTotal(data.Transactions.Sum(x => x.Amount));

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(13, cols.Count());

            // And: Report has the correct # rows
            var rowset = new int[] { 9, 21, 24, 26 };
            Assert.AreEqual(rowset[level - 1], rows.Count());

            // And: Report has the right levels
            var levels = rows.Select(x => x.GetProperty("Level").GetInt32()).Distinct();
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
            await WhenGettingReport(new ReportParameters() { slug = "all", year = 2020, showmonths = true, month = month });

            // And: Report has the correct total
            var expected = data.Transactions.Where(x => x.Timestamp.Month <= month).Sum(x => x.Amount);
            ThenReportHasTotal(expected);

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
            await WhenGettingReport(new ReportParameters() { slug = category.ToLowerInvariant(), year = 2020 });

            // Then: Report has the correct total
            ThenReportHasTotal(SumOfTopCategory(category));

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
            await WhenGettingReport(new ReportParameters() { slug = "expenses-detail", year = 2020, showmonths = showmonths });

            // Then: Report has the correct total
            ThenReportHasTotal(data.Transactions.Sum(x => x.Amount) - SumOfTopCategory("Taxes") - SumOfTopCategory("Savings") - SumOfTopCategory("Income"));

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
            await WhenGettingReport(new ReportParameters() { slug = "budget", year = 2020 });

            // Then: Report has the correct total
            ThenReportHasTotal(data.BudgetTxs.Sum(x => x.Amount));

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
            await WhenGettingReport(new ReportParameters() { slug = "expenses-budget", year = 2020 });

            // Then: Report has the correct total
            ThenReportHasTotal(data.BudgetTxs.Sum(x => x.Amount) - SumOfBudgetTxsTopCategory("Taxes") - SumOfBudgetTxsTopCategory("Savings") - SumOfBudgetTxsTopCategory("Income"));

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
            await WhenGettingReport(new ReportParameters() { slug = "expenses-v-budget", year = 2020 });

            // Then: Report has the correct total budget
            var expected = data.BudgetTxs.Sum(x => x.Amount) - SumOfBudgetTxsTopCategory("Taxes") - SumOfBudgetTxsTopCategory("Savings") - SumOfBudgetTxsTopCategory("Income");
            var budgettotal = totalrow.GetProperty("ID:Budget").GetDecimal();
            Assert.AreEqual(expected, budgettotal);

            // And: Report has the correct actual total
            expected = data.Transactions.Sum(x => x.Amount) - SumOfTopCategory("Taxes") - SumOfTopCategory("Savings") - SumOfTopCategory("Income");
            var actualtotal = totalrow.GetProperty("ID:Actual").GetDecimal();
            Assert.AreEqual(expected, actualtotal);

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
            await WhenGettingReport(new ReportParameters() { slug = "all-v-budget", year = 2020 });

            // Then: Report has the correct total budget
            var expected = data.BudgetTxs.Sum(x => x.Amount);
            var budgettotal = totalrow.GetProperty("ID:Budget").GetDecimal();
            Assert.AreEqual(expected, budgettotal);

            // And: Report has the correct actual total
            expected = data.Transactions.Sum(x => x.Amount);
            var actualtotal = totalrow.GetProperty("ID:Actual").GetDecimal();
            Assert.AreEqual(expected, actualtotal);

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
            context.BudgetTxs.AddRange(data.ManagedBudgetTxs);
            context.SaveChanges();

            // When: Building the 'managed-budget' report for the correct year
            await WhenGettingReport(new ReportParameters() { slug = "managed-budget", year = 2020 });

            // Then: Report has the correct values 

            var expected = (double)SumOfManagedBudgetTxsTopCategory("Income");
            Assert.AreEqual(expected, GetCell("Budget", "Income"),1e-5);

            expected = (double)SumOfManagedBudgetTxsTopCategory("J");
            Assert.AreEqual(expected, GetCell("Budget", "J"), 1e-5);

            expected = (double)SumOfTopCategory("Income");
            Assert.AreEqual(expected, GetCell("Actual", "Income"), 1e-5);

            expected = (double)SumOfTopCategory("J");
            Assert.AreEqual(expected, GetCell("Actual", "J"), 1e-5);

            // And: Report has the correct # displayed columns: budget, actual, progress, remaining
            Assert.AreEqual(4, cols.Count());

            // And: Report has the correct # rows: just the 2 managed budgets
            Assert.AreEqual(2, rows.Count());
        }

        [TestMethod]
        public async Task Bug1185()
        {
            // Bug 1185: Managed budget report looks crazy if no monthly transactions

            // Given: A database of transactions and budgettx, but
            // CRITICALLY no monthly items
            // So we can use the setup assembled on Initialize

            // When: Building the 'managed-budget' report for the correct year
            await WhenGettingReport(new ReportParameters() { slug = "managed-budget", year = 2020 });

            // Then: The report is totally blank
            Assert.IsFalse(rows.Any());
        }

        [TestMethod]
        public async Task ReportV2export()
        {
            // When: Building the export report for the correct year
            await WhenGettingReport(new ReportParameters() { slug = "export", year = 2020 });

            // Then: Selected totals match
            var expected = (double)SumOfCategory("Income:K");
            Assert.AreEqual(expected, GetCell("Actual", "Income:K"), 1e-5);

            expected = (double)SumOfCategory("J");
            Assert.AreEqual(expected, GetCell("Actual", "J"), 1e-5);

            // And: Report has the correct # displayed columns: budget, actual
            Assert.AreEqual(2, cols.Count());

            // And: Report has the correct # rows: all the categories
            Assert.AreEqual(18, rows.Count());
        }

        [TestMethod]
        public async Task ReportV2exportEmpty()
        {
            // When: Building the export report for a year when we have no data
            await WhenGettingReport(new ReportParameters() { slug = "export", year = 2023 });

            // Then: The report is totally blank
            Assert.IsFalse(rows.Any());

        }

        #endregion
    }

}
