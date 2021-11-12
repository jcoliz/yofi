using Common.NET.Test;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Database;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class NewSpreadsheetsTest
    {
        public class SimpleItem<T>
        {
            public T Key { get; set; }

            public override bool Equals(object obj)
            {
                return obj is SimpleItem<T> item &&
                    (
                        (Key == null && item.Key == null)
                        ||
                        (Key?.Equals(item.Key) ?? false)
                     );
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Key);
            }
        }

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
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = new List<Payee>() { new Payee() { ID = 1, Category = "A", Name = "C", Selected = true } };

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }

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
        public async Task TransactionItemsFew()
        {
            // Given: A small number of transactions
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()).Take(2) /*.ToList()*/;

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }

        [TestMethod]
        public async Task TransactionItems20()
        {
            // Given: A ton of transactions
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()).Take(20) /*.ToList()*/;

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }


        // This is really slow, so not running by default
        [TestMethod]
        public async Task TransactionItems1000()
        {
            // Given: A ton of transactions
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()) /*.ToList()*/;

            // When: Writing it to a spreadsheet 
            // And: Reading it back to a spreadsheet
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack<Transaction>(Items, writetodisk:false);
        }

        [TestMethod]
        public async Task MultipleDataSeries()
        {
            // Given: Two different item series
            var TxItems = (await TransactionControllerTest.GetTransactionItemsLong()).Take(20).ToList();
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

        [TestMethod]
        public void LoadFromFile()
        {
            // Given: An existing file with splits
            var instream = SampleData.Open("Splits-Test.xlsx");

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
        public async Task LoadAnyName()
        {
            // User Story 1042: Upload spreadsheet shouldn't be worried about name of sheet

            // Given: A file created with an arbitrary non-confirming sheet name
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()).Take(20) /*.ToList()*/;
            using var stream = new MemoryStream();
            WhenWritingToSpreadsheet(stream, Items, writetodisk:true);

            // When: Loading the file, without specifying the sheet name
            var actual = new List<Transaction>();
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            actual.AddRange(reader.Deserialize<Transaction>());

            // Then: Data is loaded as expected.
            Assert.IsTrue(actual.SequenceEqual(Items));
        }

    }
}
