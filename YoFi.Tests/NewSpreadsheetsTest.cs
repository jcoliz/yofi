﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.AspNet.Common;
using YoFi.AspNet.Models;

namespace YoFi.Tests
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

        void WhenWritingToNewSpreadsheet<T>(Stream stream,IEnumerable<T> items, string name) where T: class
        {
            using (var writer = new NewSpreadsheetWriter())
            {
                writer.Open(stream);
                writer.Write(items, name);
            }

            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-{name}.xlsx";
            File.Delete(filename);
            using (var outstream = File.OpenWrite(filename))
            {
                Console.WriteLine($"Writing {outstream.Name}...");
                stream.CopyTo(outstream);
            }
        }

        private void WhenReadAsOldSpreadsheet<T>(MemoryStream stream, string name, List<T> actual, List<string> sheets) where T: class, new()
        {
            stream.Seek(0, SeekOrigin.Begin);
            using (var reader = new SpreadsheetReader())
            {
                reader.Open(stream);
                actual.AddRange(reader.Read<T>(name,includeids:true));
                sheets.AddRange(reader.SheetNames.ToList());
            }
        }

        private void WhenReadAsNewSpreadsheet<T>(MemoryStream stream, string name, List<T> actual, List<string> sheets) where T : class, new()
        {
            stream.Seek(0, SeekOrigin.Begin);
            using (var reader = new NewSpreadsheetReader())
            {
                reader.Open(stream);
                actual.AddRange(reader.Read<T>(name, includeids: true));
                sheets.AddRange(reader.SheetNames.ToList());
            }
        }

        public void WriteThenReadBack<T>(string name, List<T> items, bool newreader = false) where T : class, new()
        {
            // Given: Some items

            // When: Writing it to a spreadsheet using the new methods
            using (var stream = new MemoryStream())
            {
                WhenWritingToNewSpreadsheet(stream, items, name);

                // And: Reading it back to a spreadsheet using the old methods
                var actual = new List<T>();
                var sheets = new List<string>();

                if (newreader)
                    WhenReadAsNewSpreadsheet<T>(stream, name, actual, sheets);
                else
                    WhenReadAsOldSpreadsheet<T>(stream, name, actual, sheets);

                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(1, sheets.Count());
                Assert.AreEqual(name, sheets.Single());
                CollectionAssert.AreEqual(items, actual);
            }
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void SimpleWriteString(bool newreader)
        {
            // Given: A very simple string item
            var Items = new List<SimpleItem<string>>() { new SimpleItem<string>() { Key = "Hello, world!" } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("SimpleWriteString", Items, newreader);
        }

        [TestMethod]
        public void SimpleWriteStringNull()
        {
            // Given: A small list of simple string items, one with null key
            var Items = new List<SimpleItem<string>>() { new SimpleItem<string>() { Key = "Hello, world!" } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("SimpleWriteStringNull", Items);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void SimpleWriteDateTime(bool newreader)
        {
            // Given: A very simple item w/ DateTime member
            var Items = new List<SimpleItem<DateTime>>() { new SimpleItem<DateTime>() { Key = new DateTime(2021,06,08) } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("SimpleWriteDateTime", Items, newreader);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void SimpleWriteInt32(bool newreader)
        {
            // Given: A very simple item w/ Int32 member
            var Items = new List<SimpleItem<Int32>>() { new SimpleItem<Int32>() { Key = 12345 } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("SimpleWriteInt32", Items, newreader);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void SimpleWriteDecimal(bool newreader)
        {
            // Given: A very simple item w/ decimal member
            var Items = new List<SimpleItem<decimal>>() { new SimpleItem<decimal>() { Key = 123.45m } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("SimpleWriteDecimal", Items, newreader);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void SimpleWriteBoolean(bool newreader)
        {
            // Given: A very simple item w/ boolean member
            var Items = new List<SimpleItem<Boolean>>() { new SimpleItem<Boolean>() { Key = true } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("SimpleWriteBoolean", Items, newreader);
        }

        [TestMethod]
        public void OnePayee()
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = new List<Payee>() { new Payee() { ID = 1, Category = "A", SubCategory = "B", Name = "C", Selected = true } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("OnePayee", Items);
        }

        [TestMethod]
        public void AllPayees()
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = PayeeControllerTest.PayeeItems;

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("AllPayees", Items);
        }

        [TestMethod]
        public void AllSplits()
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = SplitControllerTest.SplitItems;

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("AllSplits", Items);
        }

        [TestMethod]
        public void AllBudgetTxs()
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = BudgetTxControllerTest.BudgetTxItems;

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("AllBudgetTxs", Items);
        }

        [TestMethod]
        public void OneTransactionEmpty()
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = new List<Transaction>() { new Transaction() { Timestamp = new DateTime(2021, 01, 03) } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("OneTransactionEmpty", Items);
        }

        [TestMethod]
        public async Task TransactionItemsFew()
        {
            // Given: A small number of transactions
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()).Take(2).ToList();

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("TransactionItemsFew", Items);
        }
        
        [TestMethod]
        public async Task TransactionItems20()
        {
            // Given: A ton of transactions
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()).Take(20).ToList();

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack("TransactionItems20", Items);
        }

        [TestMethod]
        public async Task MultipleDataSeries()
        {
            // Given: Two different item series
            var TxItems = (await TransactionControllerTest.GetTransactionItemsLong()).Take(20).ToList();
            var SplitItems = SplitControllerTest.SplitItems;

            // When: Writing both to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: All spreadsheets are valid, and contain the expected items

            // When: Writing it to a spreadsheet using the new methods
            using (var stream = new MemoryStream())
            {
                using (var writer = new NewSpreadsheetWriter())
                {
                    writer.Open(stream);
                    writer.Write(TxItems);
                    writer.Write(SplitItems);
                }

                stream.Seek(0, SeekOrigin.Begin);
                var filename = $"Test-MultipleDataSeries.xlsx";
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
                using (var reader = new SpreadsheetReader())
                {
                    reader.Open(stream);
                    actual_t.AddRange(reader.Read<Transaction>(includeids: true));
                    actual_s.AddRange(reader.Read<Split>(includeids: true));
                    sheets.AddRange(reader.SheetNames.ToList());
                }

                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(2, sheets.Count());
                Assert.IsTrue(sheets.Contains("Transaction"));
                Assert.IsTrue(sheets.Contains("Split"));
                CollectionAssert.AreEqual(TxItems, actual_t);
                CollectionAssert.AreEqual(SplitItems, actual_s);
            }

        }

    }
}
