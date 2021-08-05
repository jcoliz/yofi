using Common.AspNetCore.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp.Controllers.Reports;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ofx.Tests
{
    [TestClass]
    public class ReportBuilderTest
    {
        public ApplicationDbContext context = null;
        public ReportBuilder builder = null;

        IEnumerable<Transaction> Transactions1000;

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);

            builder = new ReportBuilder(context);

            var txs = LoadTransactions();
            context.Transactions.AddRange(txs);
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

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(13, report.ColumnLabels.Count());

            // And: Report has the correct # rows
            var rowset = new int[] { 9, 21, 24, 26 };
            Assert.AreEqual(rowset[level-1], report.RowLabels.Count());

            // Report has the right levels
            Assert.AreEqual(level - 1, report.RowLabels.Max(x => x.Level));
        }

        decimal SumOfTopCategory(string category)
        {
            return
                Transactions1000.Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount) +
                Transactions1000.Where(x => x.HasSplits).SelectMany(x => x.Splits).Where(x => !string.IsNullOrEmpty(x.Category) && x.Category.Contains(category)).Sum(x => x.Amount);
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
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "expenses", year = 2020, showmonths = showmonths});

            // Then: Report has the correct total
            var expected = Transactions1000.Sum(x => x.Amount) - SumOfTopCategory("Taxes") - SumOfTopCategory("Savings") - SumOfTopCategory("Income");
            Assert.AreEqual(expected, report[report.TotalColumn, report.TotalRow]);

            // And: Report has the correct # columns (12 months, plus Total & pct total)
            Assert.AreEqual(showmonths? 14 : 2, report.ColumnLabels.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(12, report.RowLabels.Count());
        }

        // Only enable this if need to generate more sample data
        [TestMethod]
        public void GenerateData()
        {
            // Generates a huge dataset
            const int numtx = 1000;

            var random = new Random();

            string[] categories = new string[] { "A", "A:B:C", "A:B:C:D", "E", "E:F", "E:F:G", "H", "H:I", "J", "Income:K", "Income:L", "Taxes:M", "Taxes:N", "Savings:O", "Savings:P", string.Empty };

            var transactions = new List<Transaction>();
            int i = numtx;
            while(i-- > 0)
            {
                var year = 2020;
                var month = random.Next(1, 13);
                var day = random.Next(1, 1+DateTime.DaysInMonth(year,month));

                var tx = new Transaction() { Timestamp = new DateTime(year,month,day), Payee = i.ToString() };

                // Half the transactions will have splits
                if (random.Next(0,2) == 1)
                {
                    tx.Amount = ((decimal)random.Next(-100000, 0)) / 100m;
                    tx.Category = categories[random.Next(0, categories.Length)];
                }
                else
                {
                    tx.Splits = Enumerable.Range(0, random.Next(2, 7)).Select(x => new Split()
                    {
                        Amount = ((decimal)random.Next(-100000, 0)) / 100m,
                        Category = categories[random.Next(0, categories.Length)],
                        Memo = x.ToString()
                    })
                    .ToList();
                    tx.Amount = tx.Splits.Sum(x => x.Amount);
                }

                transactions.Add(tx);
            }

            // Serialize to JSON

            using (var stream = System.IO.File.OpenWrite("Transactions1000.json"))
            {
                Console.WriteLine($"Writing {stream.Name}...");
                using (var writer = new StreamWriter(stream))
                {
                    var output = System.Text.Json.JsonSerializer.Serialize(transactions, options: new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = null, WriteIndented = true });
                    writer.Write(output);
                }
            }
        }
    }
}
