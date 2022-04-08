using Common.DotNet.Test;
using jcoliz.FakeObjects;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YoFi.Core.Models;
using YoFi.Tests.Helpers;

namespace YoFi.Core.Tests.Unit
{
    [TestClass]
    public class NewSpreadsheetsTest
    {
        public TestContext TestContext { get; set; }

        void WhenWritingToSpreadsheet<T>(Stream stream,IEnumerable<T> items,bool writetodisk = true) where T: class
        {
            {
                using var writer = new SpreadsheetWriter();
                writer.Open(stream);
                writer.Serialize(items, TestContext.TestName);
            }

            stream.Seek(0, SeekOrigin.Begin);

            if (writetodisk)
            {
                var filename = $"Test-{TestContext.TestName}.xlsx";
                File.Delete(filename);
                using var outstream = File.OpenWrite(filename);
                stream.CopyTo(outstream);
                TestContext.AddResultFile(filename);
            }
        }

        private IEnumerable<T> WhenReadAsSpreadsheet<T>(MemoryStream stream, List<string> sheets) where T : class, new()
        {
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            sheets.AddRange(reader.SheetNames);

            return reader.Deserialize<T>(TestContext.TestName);
        }

        public void WriteThenReadBack<T>(IEnumerable<T> items, bool writetodisk = true) where T : class, new()
        {
            // Given: Some items

            // When: Writing it to a spreadsheet using the new methods
            using var stream = new MemoryStream();
            WhenWritingToSpreadsheet(stream, items, writetodisk);

            // And: Reading it back to a spreadsheet
            var sheets = new List<string>();
            var actual = WhenReadAsSpreadsheet<T>(stream, sheets);

            // Then: The spreadsheet is valid, and contains the expected item
            Assert.AreEqual(1, sheets.Count());
            Assert.AreEqual(TestContext.TestName, sheets.Single());
            Assert.IsTrue(actual.SequenceEqual(items));
        }

        [TestMethod]
        public void OnePayee()
        {
            // Given: A single payee
            var items = FakeObjects<Payee>.Make(1);

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(items);
        }

        // TODO: Need to find a new way to get items
#if false
        [TestMethod]
        public void AllPayees()
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = PayeeControllerTest.PayeeItems;

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }

        [TestMethod]
        public void AllSplits()
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            
            var Items = SplitControllerTest.SplitItems;

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }
        [TestMethod]
        public void AllBudgetTxs()
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = BudgetTxControllerTest.BudgetTxItems;

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }
#endif

        [TestMethod]
        public void OneTransactionEmpty()
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = new List<Transaction>() { new Transaction() { Timestamp = new DateTime(2021, 01, 03) } };

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }

        [TestMethod]
        public void TransactionItemsFew()
        {
            // Given: A small number of transactions
            var items = FakeObjects<Transaction>.Make(2);

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(items);
        }

        [TestMethod]
        public void TransactionItems20()
        {
            // Given: A ton of transactions
            var items = FakeObjects<Transaction>.Make(20);

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(items);
        }

        [TestMethod]
        public void TransactionItems1000()
        {
            // Given: A ton of transactions
            var items = FakeObjects<Transaction>.Make(1000);

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack<Transaction>(items, writetodisk:false);
        }

#if false
        [TestMethod]
        public async Task MultipleDataSeries()
        {
            // Given: Two different item series
            var TxItems = (await TransactionControllerTest.GetTransactionItemsLong()).Take(20).ToList();

            // TODO: Need a new way to generate split items
            var SplitItems = SplitControllerTest.SplitItems;

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: All spreadsheets are valid, and contain the expected items

            // When: Writing it to a spreadsheet using the new methods
            using var stream = new MemoryStream();
            using (var writer = new SpreadsheetWriter())
            {
                writer.Open(stream);
                writer.Serialize(TxItems);
                writer.Serialize(SplitItems);
            }

            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using (var outstream = File.OpenWrite(filename))
            {
                Console.WriteLine($"Writing {outstream.Name}...");
                stream.CopyTo(outstream);
            }

            // And: Reading it back to a spreadsheet using the old methods
            var actual_t = new List<Transaction>();
            var actual_s = new List<Split>();
            var sheets = new List<string>();

            stream.Seek(0, SeekOrigin.Begin);
            ISpreadsheetReader reader;
            reader = new SpreadsheetReader();
        }
#endif

        [TestMethod]
        public void LoadFromFile()
        {
            // Given: An existing file with splits
            var instream = Common.DotNet.Test.SampleData.Open("Splits-Test.xlsx");

            // When: Loading this file
            IEnumerable<Split> actual;
            using(var reader = new SpreadsheetReader())
            {
                reader.Open(instream);
                actual = reader.Deserialize<Split>();
            }

            // Then: Contents are as expected
            Assert.AreEqual(7, actual.Count());
            Assert.AreEqual(1, actual.Count(x => x.Memo == "Memo 4"));
            Assert.AreEqual(3, actual.Count(x => x.Category.Contains("A")));
            // Last item is a total row
            Assert.AreEqual(actual.TakeLast(1).Single().Amount, actual.SkipLast(1).Sum(x => x.Amount));
        }

        [TestMethod]
        public void LoadAnyName()
        {
            // TODO: Review this test! Is it actually doing what the comments say??

            // User Story 1042: Upload spreadsheet shouldn't be worried about name of sheet

            // Given: A file created with an arbitrary non-confirming sheet name
            var items = FakeObjects<Transaction>.Make(20);
            using var stream = new MemoryStream();
            WhenWritingToSpreadsheet(stream, items, writetodisk:true);

            // When: Loading the file, without specifying the sheet name
            var actual = new List<Transaction>();
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            actual.AddRange(reader.Deserialize<Transaction>());

            // Then: Data is loaded as expected.
            Assert.IsTrue(actual.SequenceEqual(items));
        }

    }
}
