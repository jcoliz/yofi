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

namespace YoFi.AspNet.Tests.Integration.Reports
{
    [TestClass]
    public class ReportBuilderTest : ReportBuilderTestBase
    {
        #region Fields

        protected static SampleDataStore data;

        #endregion

        #region Init/Cleanup

        [ClassInitialize]
        public static async Task InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);

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

        [TestCleanup]
        public new void Cleanup()
        {
            // Reset base class
            base.Cleanup();

            // Remove ephemeral items
            context.Transactions.RemoveRange(context.Transactions.Where(x => x.Memo == "__TEST__"));
            context.BudgetTxs.RemoveRange(context.BudgetTxs.Where(x=>x.Memo == "__TEST__"));
            context.SaveChanges();
        }

        #endregion

        #region Helpers

        protected static decimal SumOfTopCategory(string category)
        {
            return
                data.Transactions.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount) +
                data.Transactions.Where(x => x.HasSplits).SelectMany(x => x.Splits).Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
        }

        protected static decimal SumOfBudgetTxsTopCategory(string category)
        {
            return
                data.BudgetTxs.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
        }

        protected static decimal SumOfManagedBudgetTxsTopCategory(string category)
        {
            return
                data.ManagedBudgetTxs.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
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
            await WhenGettingReport( new ReportParameters() { slug = "all", year = 2020, showmonths = showmonths } );

            // Then: On the expected page
            Assert.AreEqual("All Transactions", h2);

            // And: Report has the correct total
            ThenReportHasTotal(data.Transactions.Sum(x => x.Amount));

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(showmonths?13:1, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(21, rows.Length);
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

            // Then: On the expected page
            Assert.AreEqual("All Transactions", h2);

            // And: Report has the correct total
            ThenReportHasTotal(data.Transactions.Sum(x => x.Amount));

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(13, cols.Count());

            // And: Report has the correct # rows
            var rowset = new int[] { 9, 21, 24, 26 };
            Assert.AreEqual(rowset[level - 1], rows.Length);

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
            await WhenGettingReport(new ReportParameters() { slug = "all", year = 2020, showmonths = true, month = month });

            // Then: On the expected page
            Assert.AreEqual("All Transactions", h2);

            // And: Showing the correct report
            Assert.AreEqual($"report-{report}", testid);

            // And: Report has the correct total
            var expected = data.Transactions.Where(x => x.Timestamp.Month <= month).Sum(x => x.Amount);
            ThenReportHasTotal(expected);

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(month + 1, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(21, rows.Length);
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
            Assert.AreEqual(3, rows.Length);
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
            Assert.AreEqual(12, rows.Length);
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
            Assert.AreEqual(13, rows.Length);
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
            Assert.AreEqual(7, rows.Length);
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
            var budgettotal = table.QuerySelector("td[data-test-id=total-Budget]").TextContent.Trim();
            Assert.AreEqual(expected.ToString("C0", culture), budgettotal);

            // And: Report has the correct actual total
            expected = data.Transactions.Sum(x => x.Amount) - SumOfTopCategory("Taxes") - SumOfTopCategory("Savings") - SumOfTopCategory("Income");
            var actualtotal = table.QuerySelector("td[data-test-id=total-Actual]").TextContent.Trim();
            Assert.AreEqual(expected.ToString("C0", culture), actualtotal);

            // And: Report has the correct # visible columns, budget, actual, progress
            Assert.AreEqual(4, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(12, rows.Length);
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
            var budgettotal = table.QuerySelector("td[data-test-id=total-Budget]").TextContent.Trim();
            Assert.AreEqual(expected.ToString("C0", culture), budgettotal);

            // And: Report has the correct actual total
            expected = data.Transactions.Sum(x => x.Amount);
            var actualtotal = table.QuerySelector("td[data-test-id=total-Actual]").TextContent.Trim();
            Assert.AreEqual(expected.ToString("C0", culture), actualtotal);

            // And: Report has the correct # visible columns, budget, actual, progress
            Assert.AreEqual(3, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(22, rows.Length);
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
            Assert.AreEqual(2, rows.Length);
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
            Assert.IsFalse(tables.Any());

            // And: The "There is no data" message is present
            var nodata = document.QuerySelector("[data-test-id=no-data]");
            Assert.IsNotNull(nodata);
        }

        [TestMethod]
        public async Task Bug1405()
        {
            // Bug 1405: Blank split causes reports to fail

            /*
                On Transactions Index
                Edit a transaction (with no splits to start)
                Add a split, accept it. This creates a split with the full amouint and category of parent
                Add another split, accept it. This creates the "blank split" referred to in this bug
                Go to Reports page, then click ">>" under expenses.
                (May need to ensure months is set to include the transaction edited above e.g. https://localhost:44364/Report/expenses-detail?month=12)
                Result: Error dialog
                Expected: Reports work as normal
            */

            // Given: A transaction with an empty split in the database
            var tx = new Transaction()
            {
                Timestamp = new DateTime(2020, 1, 1),
                Payee = "Payee",
                Amount = 100m,
                Memo = "__TEST__", // So it gets cleaned up
                Splits = new Split[]
                {
                    new Split()
                    {
                        Amount = 100m,
                        Category = "Income"
                    },
                    new Split() // <-- This is the problem
                }
            };
            context.Transactions.Add(tx);
            context.SaveChanges();

            // When: Building the 'expenses' report for the year including that year
            await WhenGettingReport(new ReportParameters() { slug = "expenses", year = 2020 });

            // Then: The report builds without error
        }

#if false
        [TestMethod]
        public void ReportSetsYear()
        {
            // Given: Current time is 1/1/2002
            var now = new DateTime(2002, 1, 1);
            controller.Now = now;
            var year = 2000;

            // And: First calling report with a defined year
            controller.Report(new ReportBuilder.Parameters() { year = year });

            // When: Later calling report with no year
            var result = controller.Report(new ReportBuilder.Parameters());
            var viewresult = result as ViewResult;
            var model = viewresult.Model as Report;

            // Then: The year from the first call is used
            Assert.AreEqual(12, viewresult.ViewData["month"]);
            Assert.IsTrue(model.Description.Contains(year.ToString()));
        }

        [TestMethod]
        public void ReportNotFound() =>
            Assert.IsTrue(controller.Report(new ReportBuilder.Parameters() { id = "notfound" }) is Microsoft.AspNetCore.Mvc.NotFoundObjectResult);
#endif

        #endregion

        #region Utilities

        // Only enable this if need to generate more sample data
        //[TestMethod]
        public static void GenerateData()
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
