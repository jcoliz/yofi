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
    public class ReportBuilderTest : ReportBuilderTestBase
    {
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
                    .SelectMany(row => row.ClassList)       // Extract all the classes
                    .Select(@class => regex.Match(@class))  // Look for the classes which match our pattern
                    .Where(match => match.Success)          // Take only the successful matches
                    .Select(match => match.Groups.Values.Last().Value)  // Extract the ([0-9]+) group out of the class name
                    .Select(value => int.Parse(value))      // Turn it into an int (probably not needed now)
                    .Distinct();                            // Boil down to only unique values

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

        [TestMethod]
        public async Task Bug1185()
        {
            // Bug 1185: Managed budget report looks crazy if no monthly transactions

            // Given: A database of transactions and budgettx, but
            // CRITICALLY no monthly items
            // So we can use the setup assembled on Initialize

            // When: Building the 'managed-budget' report for the correct year
            await WhenGettingReport(new ReportParameters() { id = "managed-budget", year = 2020 });

            // Then: The report is totally blank
            Assert.IsNull(table);

            // And: The "There is no data" message is present
            var nodata = document.QuerySelector("[data-test-id=no-data]");
            Assert.IsNotNull(nodata);
        }

        #endregion

        #region Utilities

        // Only enable this if need to generate more sample data
        //[TestMethod]
        public void GenerateData()
        {
            // Generates a large dataset of transactions

            const int numtx = 1000;
            var year = 2020;
            var random = new Random();

            string[] categories = new string[] { "A", "A:B:C", "A:B:C:D", "E", "E:F", "E:F:G", "H", "H:I", "J", "Income:K", "Income:L", "Taxes:M", "Taxes:N", "Savings:O", "Savings:P", string.Empty };

            var transactions = new List<Transaction>();
            int i = numtx;
            while (i-- > 0)
            {
                var month = random.Next(1, 13);
                var day = random.Next(1, 1 + DateTime.DaysInMonth(year, month));

                var tx = new Transaction() { Timestamp = new DateTime(year, month, day), Payee = i.ToString() };

                // Half the transactions will have splits
                if (random.Next(0, 2) == 1)
                {
                    tx.Amount = nextamount(1000);
                    tx.Category = categories[random.Next(0, categories.Length)];
                }
                else
                {
                    tx.Splits = Enumerable.Range(0, random.Next(2, 7)).Select(x => new Split()
                    {
                        Amount = nextamount(1000),
                        Category = categories[random.Next(0, categories.Length)],
                        Memo = x.ToString()
                    })
                    .ToList();
                    tx.Amount = tx.Splits.Sum(x => x.Amount);
                }

                transactions.Add(tx);
            }

            // Serialize to JSON

            {
                using var stream = File.OpenWrite("Transactions1000.json");
                Console.WriteLine($"Writing {stream.Name}...");
                using var writer = new StreamWriter(stream);
                var output = System.Text.Json.JsonSerializer.Serialize(transactions, options: new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = null, WriteIndented = true });
                writer.Write(output);
            }

            // Generate annual budget txs

            string[] budgetcategories = new string[] { "A:B", "E:F", "H:I", "Taxes", "Taxes:N", "Savings:O", "Savings:P" };

            var budgettxs = budgetcategories.Select(x => new BudgetTx() { Timestamp = new DateTime(year, 1, 1), Amount = nextamount(100000), Category = x });

            // Serialize to JSON

            {
                using var stream = File.OpenWrite("BudgetTxs.json");
                Console.WriteLine($"Writing {stream.Name}...");
                using var writer = new StreamWriter(stream);
                var output = System.Text.Json.JsonSerializer.Serialize(budgettxs, options: new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = null, WriteIndented = true });
                writer.Write(output);
            }

            // Generate managed budget txs

            string[] managedbudgetcategories = new string[] { "J", "Income" };

            var managedbudgettxs = managedbudgetcategories.SelectMany(
                c => Enumerable.Range(1, 12).Select(
                    mo => new BudgetTx() { Timestamp = new DateTime(year, mo, 1), Amount = nextamount(10000), Category = c }
                    )
            );

            // Serialize to JSON

            {
                using var stream = File.OpenWrite("BudgetTxsManaged.json");
                Console.WriteLine($"Writing {stream.Name}...");
                using var writer = new StreamWriter(stream);
                var output = System.Text.Json.JsonSerializer.Serialize(managedbudgettxs, options: new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = null, WriteIndented = true });
                writer.Write(output);
            }

            decimal nextamount(decimal x) => ((decimal)random.Next(-(int)(x * 100m), 0)) / 100m;
        }
        #endregion

    }
}
