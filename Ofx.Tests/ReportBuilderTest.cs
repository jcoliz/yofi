﻿using Common.AspNetCore.Test;
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

        [TestMethod]
        public void All()
        {
            // Given: A large database of transactions
            // (Assembled on Initialize)

            // When: Building the 'All' report for the correct year
            var report = builder.BuildReport(new ReportBuilder.Parameters() { id = "all", year = 2020 });

            // Then: Report has the correct total
            var expected = Transactions1000.Sum(x => x.Amount);
            Assert.AreEqual(expected, report[report.TotalColumn, report.TotalRow]);

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(13, report.ColumnLabels.Count());

            // And: Report has the correct # rows
            // A, A-, AB, E, E-, EF, H, H-, HI, J, Blank, Total
            Assert.AreEqual(12, report.RowLabels.Count());
        }

        // Only enable this if need to generate more sample data
        //[TestMethod]
        public void GenerateData()
        {
            // Generates a huge dataset
            const int numtx = 1000;

            var random = new Random();

            string[] categories = new string[] { "A", "A:B:C", "A:B:C:D", "E", "E:F", "E:F:G", "H", "H:I", "J", string.Empty };

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
