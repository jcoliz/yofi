using Common.NET.Test;
using jcoliz.OfficeOpenXml.Easy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
            var stream = TestData.Open("TestData1.xlsx");

            // When: Loading them
            generator.LoadDefinitions(stream);

            // Then: They are all loaded
            Assert.AreEqual(32, generator.Definitions.Count);

            // And: Quick spot check of schemes looks good
            Assert.AreEqual(6, generator.Definitions.Count(x => x.Scheme == SchemeEnum.Quarterly));
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
            using var reader = new OpenXmlSpreadsheetReader();
            reader.Open(stream);
            var actual = reader.Read<Transaction>().ToList();

            // Then: The file contains all the transactions
            Assert.AreEqual(435, actual.Count);
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
            using var reader = new OpenXmlSpreadsheetReader();
            reader.Open(stream);
            var actual = reader.Read<CategoryAmount>().ToList();

            // Then: The file contains all the splits
            Assert.AreEqual(312, actual.Count);
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
            using var reader = new OpenXmlSpreadsheetReader();
            reader.Open(stream);
            var transactions = reader.Read<Transaction>();
            var splits = reader.Read<CategoryAmount>();

            // And: Matching the spits up to their transaction
            var lookup = splits.Where(x => x.TransactionID != 0).ToLookup(x => x.TransactionID);
            foreach(var group in lookup)
            {
                transactions.Where(x => x.ID == group.Key).Single().Splits = group.ToList();
            }

            // Then: They all match
            Assert.AreEqual(36, transactions.Count(x => x.HasMultipleSplits));
        }
    }
}
