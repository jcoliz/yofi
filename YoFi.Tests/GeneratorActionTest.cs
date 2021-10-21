using Common.NET.Test;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Models;
using YoFi.Tests.Helpers;

namespace YoFi.SampleGen.Tests
{
    [TestClass]
    public class GeneratorActionTest
    {
        public TestContext TestContext { get; set; }

        SampleDataGenerator generator;

        [TestInitialize]
        public void SetUp()
        {
            generator = new SampleDataGenerator();
        }

        [TestMethod]
        public void Loader()
        {
            // Given: An existing file of defitions
            var stream = SampleData.Open("TestData1.xlsx");

            // When: Loading them
            generator.LoadDefinitions(stream);

            // Then: They are all loaded
            Assert.AreEqual(32, generator.Definitions.Count);

            // And: Quick spot check of schemes looks good
            Assert.AreEqual(6, generator.Definitions.Count(x => x.DateFrequency == FrequencyEnum.Quarterly));
            Assert.AreEqual(2, generator.Definitions.Count(x => x.AmountJitter == JitterEnum.Moderate));
        }

        [TestMethod]
        public void Generator()
        {
            // Given: An generator with an existing file of defitions already loaded
            Loader();

            // When: Generating transactions
            generator.GenerateTransactions();

            // Then: They are all generated
            Assert.AreEqual(435, generator.Transactions.Count);
            Assert.AreEqual(24, generator.Transactions.Count(x => x.Payee == "Big Megacorp"));
        }

        [TestMethod]
        public void PayeeGenerator()
        {
            // Given: An generator with an existing file of defitions already loaded
            Loader();

            // When: Generating payees
            generator.GeneratePayees();

            // Then: They are all generated
            Assert.AreEqual(21, generator.Payees.Count);
        }

        [TestMethod]
        public void Writer()
        {
            // Given: An generator with an existing file of defitions already loaded and generated
            Generator();

            // When: Writing them to an output file
            using var stream = new MemoryStream();
            generator.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-Generator-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);

            // And: Reading it back to a list
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            var actual = reader.Deserialize<Transaction>();

            // Then: The file contains all the transactions
            Assert.AreEqual(435, actual.Count());
            Assert.AreEqual(24, actual.Count(x => x.Payee == "Big Megacorp"));
        }

        [TestMethod]
        public void Splits()
        {
            // Given: An generator with an existing file of defitions already loaded and generated
            Generator();

            // When: Writing them to an output file
            using var stream = new MemoryStream();
            generator.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-Generator-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);

            // And: Reading it back to a list
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            var actual = reader.Deserialize<Split>("Split");

            // Then: The file contains all the splits
            Assert.AreEqual(312, actual.Count());
        }

        [TestMethod]
        public void SplitsMatch()
        {
            // Given: An generator with an existing file of defitions already loaded and generated
            Generator();

            // When: Writing them to an output file
            using var stream = new MemoryStream();
            generator.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-Generator-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);

            // And: Reading it back to a lists
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            var transactions = reader.Deserialize<Transaction>();
            var splits = reader.Deserialize<Split>("Split");

            // And: Matching the spits up to their transaction
            var lookup = splits.Where(x => x.TransactionID != 0).ToLookup(x => x.TransactionID);
            foreach(var group in lookup)
            {
                transactions.Where(x => x.ID == group.Key).Single().Splits = group.ToList();
            }

            // Then: They all match
            Assert.AreEqual(36, transactions.Count(x => x.Splits?.Count > 1));
        }

        [TestMethod]
        public void PayeeWriter()
        {
            // Given: An generator with an existing file of defitions already loaded and generated
            Generator();

            // When: Generating payees
            generator.GeneratePayees();

            // And: Writing them to an output file
            using var stream = new MemoryStream();
            generator.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-Generator-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);

            // And: Reading it back to a list of Payees
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            var actual = reader.Deserialize<Payee>();

            // Then: The file contains all the transactions
            Assert.AreEqual(21, actual.Count());
        }

        [TestMethod]
        public void BudgetWriter()
        {
            // Given: An generator with an existing file of defitions already loaded and generated
            Generator();

            // When: Generating budget
            generator.GenerateBudget();

            // And: Writing them to an output file
            using var stream = new MemoryStream();
            generator.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-Generator-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);

            // And: Reading it back to a list of budget line items
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            var actual = reader.Deserialize<BudgetTx>();

            // Then: The file contains all the budget line items
            Assert.AreEqual(87, actual.Count());
        }

        [TestMethod]
        public void GenerateFullSampleData()
        {
            var instream = Common.NET.Data.SampleData.Open("FullSampleDataDefinition.xlsx");
            generator.LoadDefinitions(instream);
            generator.GenerateTransactions();
            generator.GeneratePayees();
            generator.GenerateBudget();

            using var stream = new MemoryStream();
            generator.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-Generator-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);

        }

        [TestMethod]
        public async Task GenerateJson()
        {
            var instream = Common.NET.Data.SampleData.Open("FullSampleDataDefinition.xlsx");
            generator.LoadDefinitions(instream);
            generator.GenerateTransactions(addids:false);
            generator.GeneratePayees();
            generator.GenerateBudget();

            var store = new SampleDataStore()
            {
                Transactions = generator.Transactions,
                Payees = generator.Payees,
                BudgetTxs = generator.BudgetTxs
            };

            var filename = "FullSampleData.json";
            File.Delete(filename);
            {
                using var jsonstream = File.OpenWrite(filename);
                await store.SerializeAsync(jsonstream);
            }
            TestContext.AddResultFile(filename);

            var newstore = new SampleDataStore();
            using var newstream = File.OpenRead(filename);
            await newstore.DeSerializeAsync(newstream);

            CollectionAssert.AreEqual(store.Transactions, newstore.Transactions);
            CollectionAssert.AreEqual(store.Payees, newstore.Payees);
            CollectionAssert.AreEqual(store.BudgetTxs, newstore.BudgetTxs);
        }

        [TestMethod]
        public async Task GenerateOfx()
        {
            // Given: One month of generated Transactions
            Generator();
            var items = generator.Transactions.Where(x => x.Timestamp.Month == 12).ToList();

            // When: Exporting then as OFX
            var filename = "Generated.ofx";
            File.Delete(filename);
            {
                using var writestream = File.OpenWrite(filename);
                SampleDataOfx.WriteToOfx(items, writestream);
            }
            TestContext.AddResultFile(filename);

            // And: Importing them through the OFX reader
            var instream = File.OpenRead(filename);
            var Document = await OfxSharp.OfxDocumentReader.FromSgmlFileAsync(instream);
            var imported = Document.Statements.SelectMany(x => x.Transactions).Select(
                tx => new Transaction()
                {
                    Amount = tx.Amount,
                    Payee = tx.Memo?.Trim(),
                    BankReference = tx.ReferenceNumber?.Trim(),
                    Timestamp = tx.Date.Value.DateTime
                }
            ).ToList();

            // Then: The transactions match
            CollectionAssert.AreEqual(items, imported);
        }
    }
}
