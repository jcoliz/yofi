using Common.NET.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoFi.AspNet.Controllers.Reports;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using YoFi.Tests.Helpers;
using System.Threading.Tasks;

namespace YoFi.Tests
{
    [TestClass]
    public class ReportBuilderTest
    {
        public ApplicationDbContext context = null;
        public ReportBuilder builder = null;

        IEnumerable<Transaction> Transactions1000;
        IEnumerable<BudgetTx> BudgetTxs;
        IEnumerable<BudgetTx> ManagedBudgetTxs;

        public ILoggerFactory logfact = LoggerFactory.Create(builder =>
        {
            builder
            .AddConsole((options) => { })
            .AddFilter((category, level) =>
                    category == "Microsoft.EntityFrameworkCore.Query"
                    && level == LogLevel.Debug);
        });

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                //.UseLoggerFactory(logfact)
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);

            builder = new ReportBuilder(context);

            var txs = LoadTransactions();
            context.Transactions.AddRange(txs);
            var btxs = LoadBudgetTxs();
            context.BudgetTxs.AddRange(btxs);
            context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Didn't actually solve anything. Keep it around for possible future problem
            //DetachAllEntities();

            // https://stackoverflow.com/questions/33490696/how-can-i-reset-an-ef7-inmemory-provider-between-unit-tests
            context?.Database.EnsureDeleted();
            context = null;
        }

        public IEnumerable<Transaction> LoadTransactions()
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

        public IEnumerable<BudgetTx> LoadBudgetTxs()
        {
            if (null == BudgetTxs)
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
        public IEnumerable<BudgetTx> LoadManagedBudgetTxs()
        {
            if (null == ManagedBudgetTxs)
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

        RowLabel GetRow(Report report, Func<RowLabel, bool> predicate)
        {
            var result = report.RowLabels.Where(predicate).SingleOrDefault();

            Assert.IsNotNull(result);

            return result;
        }
        ColumnLabel GetColumn(Report report,Func<ColumnLabel, bool> predicate)
        {
            var result = report.ColumnLabels.Where(predicate).SingleOrDefault();

            Assert.IsNotNull(result);

            return result;
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void All(bool showmonths)
        {
            // Given: A large database of transactions
            // (Assembled on Initialize)

            // When: Building the 'All' report for the correct year
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "all", year = 2020, showmonths = showmonths });

            // Then: Report has the correct total
            var expected = Transactions1000.Sum(x => x.Amount);
            Assert.AreEqual(expected, report[report.TotalColumn, report.TotalRow]);

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(showmonths? 13 : 1, report.ColumnLabels.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(21, report.RowLabels.Count());
        }

        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataTestMethod]
        public void AllLevels(int level)
        {
            // Given: A large database of transactions
            // (Assembled on Initialize)

            // When: Building the 'All' report for the correct year, with level at '{level}'
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "all", year = 2020, level = level });

            // Then: Report has the correct total
            var expected = Transactions1000.Sum(x => x.Amount);
            Assert.AreEqual(expected, report[report.TotalColumn, report.TotalRow]);

            // And: Report has the correct # columns (all months and total)
            Assert.AreEqual(13, report.ColumnLabels.Count());

            // And: Report has the correct # rows
            var rowset = new int[] { 9, 21, 24, 26 };
            Assert.AreEqual(rowset[level-1], report.RowLabels.Count());

            // Report has the right levels
            Assert.AreEqual(level - 1, report.RowLabels.Max(x => x.Level));
        }

        [DataRow(1)]
        [DataRow(3)]
        [DataRow(6)]
        [DataRow(9)]
        [DataRow(12)]
        [DataTestMethod]
        public void AllMonths(int month)
        {
            // Given: A large database of transactions
            // (Assembled on Initialize)

            // When: Building the 'All' report for the correct year, with level at '{level}'
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "all", year = 2020, month = month });

            // Then: Report has the correct total
            var expected = Transactions1000.Where(x=>x.Timestamp.Month <= month).Sum(x => x.Amount);
            Assert.AreEqual(expected, report[report.TotalColumn, report.TotalRow]);

            // And: Report has the correct # columns (Just total)
            Assert.AreEqual(month+1, report.ColumnLabels.Count());

            // And: Report has the correct # rows
            var rowset = new int[] { 9, 21, 24, 26 };
            Assert.AreEqual(21, report.RowLabels.Count());
        }

        [DataRow(1)]
        [DataRow(3)]
        [DataRow(6)]
        [DataRow(9)]
        [DataRow(12)]
        [DataTestMethod]
        public async Task AllMonthsNewData(int month)
        {
            // Given: The generated sample dataset
            await SampleDataStore.LoadSingleAsync();
            context.Transactions.RemoveRange(context.Transactions);
            context.Transactions.AddRange(SampleDataStore.Single.Transactions);
            context.SaveChanges();

            // When: Building the 'All' report for the correct year, with level at '{level}'
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "all", year = 2021, month = month });

            // Then: Report has the correct total
            var expected = SampleDataStore.Single.Transactions.Where(x => x.Timestamp.Month <= month).Sum(x => x.Amount);
            Assert.AreEqual(expected, report[report.TotalColumn, report.TotalRow]);

            // And: Report has the correct # columns (Just total)
            Assert.AreEqual(month + 1, report.ColumnLabels.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(43, report.RowLabels.Count());
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

        [DataRow("Income")]
        [DataRow("Taxes")]
        [DataRow("Savings")]
        [DataTestMethod]
        public void SingleTop(string category)
        {
            // Given: A large database of transactions
            // (Assembled on Initialize)

            // When: Building the '{Category}' report for the correct year
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = category.ToLowerInvariant(), year = 2020 });

            // Then: Report has the correct total
            var expected = SumOfTopCategory(category);
            Assert.AreEqual(expected, report[report.TotalColumn, report.TotalRow]);

            // And: Report has the correct # columns (Total & pct total)
            Assert.AreEqual(2, report.ColumnLabels.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(3, report.RowLabels.Count());
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void Expenses(bool showmonths)
        {
            // Given: A large database of transactions
            // (Assembled on Initialize)

            // When: Building the '{Category}' report for the correct year
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "expenses-detail", year = 2020, showmonths = showmonths});

            // Then: Report has the correct total
            var expected = Transactions1000.Sum(x => x.Amount) - SumOfTopCategory("Taxes") - SumOfTopCategory("Savings") - SumOfTopCategory("Income");
            Assert.AreEqual(expected, report[report.TotalColumn, report.TotalRow]);

            // And: Report has the correct # columns (12 months, plus Total & pct total)
            Assert.AreEqual(showmonths? 14 : 2, report.ColumnLabels.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(12, report.RowLabels.Count());
        }

        [TestMethod]
        public void Budget()
        {
            // Given: A large database of transactions and budgettxs
            // (Assembled on Initialize)

            // When: Building the 'budget' report for the correct year
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "budget", year = 2020 });

            // Then: Report has the correct total
            var expected = BudgetTxs.Sum(x => x.Amount);
            Assert.AreEqual(expected, report[report.TotalColumn, report.TotalRow]);

            // And: Report has the correct # columns, just 1 the budget itself
            Assert.AreEqual(1, report.ColumnLabels.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(13, report.RowLabels.Count());
        }

        [TestMethod]
        public void ExpensesBudget()
        {
            // Given: A large database of transactions and budgettxs
            // (Assembled on Initialize)

            // When: Building the 'expenses-budget' report for the correct year
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "expenses-budget", year = 2020 });

            // Then: Report has the correct total
            var expected = BudgetTxs.Sum(x => x.Amount) - SumOfBudgetTxsTopCategory("Taxes") - SumOfBudgetTxsTopCategory("Savings") - SumOfBudgetTxsTopCategory("Income");
            Assert.AreEqual(expected, report[report.TotalColumn, report.TotalRow]);

            // And: Report has the correct # columns, just 1 the budget itself
            Assert.AreEqual(1, report.ColumnLabels.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(7, report.RowLabels.Count());

        }

        [TestMethod]
        public void Expenses_V_Budget()
        {
            // Given: A large database of transactions and budgettxs
            // (Assembled on Initialize)

            // When: Building the 'expenses-v-budget' report for the correct year
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "expenses-v-budget", year = 2020 });

            // Then: Report has the correct total budget
            var BudgetCol = GetColumn(report,x=>x.Name == "Budget");
            var expected = BudgetTxs.Sum(x => x.Amount) - SumOfBudgetTxsTopCategory("Taxes") - SumOfBudgetTxsTopCategory("Savings") - SumOfBudgetTxsTopCategory("Income");
            Assert.AreEqual(expected, report[BudgetCol, report.TotalRow]);

            // And: Report has the correct actual total
            var ActualCol = GetColumn(report, x => x.Name == "Actual");
            expected = Transactions1000.Sum(x => x.Amount) - SumOfTopCategory("Taxes") - SumOfTopCategory("Savings") - SumOfTopCategory("Income");
            Assert.AreEqual(expected, report[ActualCol, report.TotalRow]);

            // And: Report has the correct # visible columns, budget, actual, progress
            Assert.AreEqual(3, report.ColumnLabelsFiltered.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(12, report.RowLabels.Count());
        }

        [TestMethod]
        public void All_V_Budget()
        {
            // Given: A large database of transactions and budgettxs
            // (Assembled on Initialize)

            // When: Building the 'all-v-budget' report for the correct year
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "all-v-budget", year = 2020 });

            // Then: Report has the correct total budget
            var BudgetCol = GetColumn(report, x => x.Name == "Budget");
            var expected = BudgetTxs.Sum(x => x.Amount);
            Assert.AreEqual(expected, report[BudgetCol, report.TotalRow]);

            // And: Report has the correct actual total
            var ActualCol = GetColumn(report, x => x.Name == "Actual");
            expected = Transactions1000.Sum(x => x.Amount);
            Assert.AreEqual(expected, report[ActualCol, report.TotalRow]);

            // And: Report has the correct # visible columns, budget, actual, progress
            Assert.AreEqual(3, report.ColumnLabelsFiltered.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(22, report.RowLabels.Count());
        }

        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataTestMethod]
        public void YoY(int level)
        {
            // Given: A large database of transactions, over many years

            // Clear out existing transactions
            context.Transactions.RemoveRange(context.Transactions);
            context.SaveChanges();

            // Transform into a 10-year timespan
            context.Transactions.AddRange(Transactions1000.Select(x=> 
            {
                var index = Convert.ToInt32(x.Payee);
                var adjust = index % 10;
                x.Timestamp = x.Timestamp.AddYears(-adjust); 
                return x;
            }));
            context.SaveChanges();

            // When: Building the 'yoy' report 
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "yoy", level = level });

            // Then: Report has the correct total
            var expected = Transactions1000.Sum(x => x.Amount);
            Assert.AreEqual(expected, report[report.TotalColumn, report.TotalRow]);

            // And: Report has the correct # columns: 10 years plus total
            Assert.AreEqual(11, report.ColumnLabels.Count());

            // And: Report has the correct # rows
            var rowset = new int[] { 9, 21, 24, 26 };
            Assert.AreEqual(rowset[level - 1], report.RowLabels.Count());

            // And: Report has the right levels
            Assert.AreEqual(level - 1, report.RowLabels.Max(x => x.Level));
        }

        [TestMethod]
        public void ManagedBudget()
        {
            // Given: A large database of transactions and budgettxs, including a mix of monthly and yearly budget txs
            // Most are Assembled on Initialize, but we need to add managed txs
            context.BudgetTxs.AddRange(LoadManagedBudgetTxs());
            context.SaveChanges();

            // When: Building the 'managed-budget' report for the correct year
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "managed-budget", year = 2020 });

            // Then: Report has the correct values 
            var BudgetCol = GetColumn(report, x => x.Name == "Budget");
            var ActualCol = GetColumn(report, x => x.Name == "Actual");
            var IncomeRow = GetRow(report, x => x.Name == "Income");
            var JRow = GetRow(report, x => x.Name == "J");

            Assert.AreEqual(SumOfManagedBudgetTxsTopCategory("Income"), report[BudgetCol, IncomeRow]);
            Assert.AreEqual(SumOfManagedBudgetTxsTopCategory("J"), report[BudgetCol, JRow]);
            Assert.AreEqual(SumOfTopCategory("Income"), report[ActualCol, IncomeRow]);
            Assert.AreEqual(SumOfTopCategory("J"), report[ActualCol, JRow]);

            // And: Report has the correct # displayed columns: budget, actual, progress, remaining
            Assert.AreEqual(4, report.ColumnLabelsFiltered.Count());

            // And: Report has the correct # rows: just the 2 managed budgets
            Assert.AreEqual(2, report.RowLabels.Count());
        }

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
            while(i-- > 0)
            {
                var month = random.Next(1, 13);
                var day = random.Next(1, 1+DateTime.DaysInMonth(year,month));

                var tx = new Transaction() { Timestamp = new DateTime(year,month,day), Payee = i.ToString() };

                // Half the transactions will have splits
                if (random.Next(0,2) == 1)
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

            var budgettxs = budgetcategories.Select(x=>new BudgetTx() { Timestamp = new DateTime(year,1,1), Amount = nextamount(100000), Category = x});

            // Serialize to JSON

            {
                using var stream = File.OpenWrite("BudgetTxs.json");
                Console.WriteLine($"Writing {stream.Name}...");
                using var writer = new StreamWriter(stream);
                var output = System.Text.Json.JsonSerializer.Serialize(budgettxs, options: new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = null, WriteIndented = true });
                writer.Write(output);
            }

            // Generate managed budget txs

            string[] managedbudgetcategories = new string[] {"J", "Income" };

            var managedbudgettxs = managedbudgetcategories.SelectMany(
                c => Enumerable.Range(1,12).Select(
                    mo => new BudgetTx() { Timestamp = new DateTime(year,mo,1), Amount = nextamount(10000), Category = c }
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
    }
}
