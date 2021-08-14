using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        [TestMethod]
        public void SimpleWriteString()
        {

            // Given: A ton of transactions
            var Items = new List<SimpleItem<string>>() { new SimpleItem<string>() { Key = "Hello, world!" } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteNewReadOld("SimpleWriteString", Items);
        }

        [TestMethod]
        public void SimpleWriteStringNull()
        {
            // Given: A very simple item
            var Items = new SimpleItem<string>[] { new SimpleItem<string>(), new SimpleItem<string>() { Key = "Second" } };

            // When: Writing it to a spreadsheet using the new methods
            var name = "SimpleWriteStringNull";
            using (var stream = new MemoryStream())
            {
                WhenWritingToNewSpreadsheet(stream, Items, name);

                List<SimpleItem<string>> actual = new List<SimpleItem<string>>();
                List<string> sheets = new List<string>();
                WhenReadAsOldSpreadsheet(stream, name, actual, sheets);

                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(1, sheets.Count());
                Assert.AreEqual(name, sheets.Single());
                CollectionAssert.AreEqual(Items, actual);
            }
        }

        [TestMethod]
        public void SimpleWriteDateTime()
        {
            // Given: A very simple item
            var Items = new SimpleItem<DateTime>[] { new SimpleItem<DateTime>() { Key = new DateTime(2021,06,08) } };

            // When: Writing it to a spreadsheet using the new methods
            var name = "SimpleWriteDateTime";
            using (var stream = new MemoryStream())
            {
                WhenWritingToNewSpreadsheet(stream, Items, name);

                List<SimpleItem<DateTime>> actual = new List<SimpleItem<DateTime>>();
                List<string> sheets = new List<string>();
                WhenReadAsOldSpreadsheet(stream, name, actual, sheets);

                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(1, sheets.Count());
                Assert.AreEqual(name, sheets.Single());
                CollectionAssert.AreEqual(Items, actual);
            }
        }

        [TestMethod]
        public void SimpleWriteInt32()
        {
            // Given: A very simple item
            var Items = new SimpleItem<Int32>[] { new SimpleItem<Int32>() { Key = 12345 } };

            // When: Writing it to a spreadsheet using the new methods
            var name = "SimpleWriteInt32";
            using (var stream = new MemoryStream())
            {
                WhenWritingToNewSpreadsheet(stream, Items, name);

                List<SimpleItem<Int32>> actual = new List<SimpleItem<Int32>>();
                List<string> sheets = new List<string>();
                WhenReadAsOldSpreadsheet(stream, name, actual, sheets);

                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(1, sheets.Count());
                Assert.AreEqual(name, sheets.Single());
                CollectionAssert.AreEqual(Items, actual);
            }
        }

        [TestMethod]
        public void SimpleWriteDecimal()
        {
            // Given: A very simple item
            var Items = new SimpleItem<decimal>[] { new SimpleItem<decimal>() { Key = 123.45m } };

            // When: Writing it to a spreadsheet using the new methods
            var name = "SimpleWriteDecimal";
            using (var stream = new MemoryStream())
            {
                WhenWritingToNewSpreadsheet(stream, Items, name);

                List<SimpleItem<decimal>> actual = new List<SimpleItem<decimal>>();
                List<string> sheets = new List<string>();
                WhenReadAsOldSpreadsheet(stream, name, actual, sheets);

                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(1, sheets.Count());
                Assert.AreEqual(name, sheets.Single());
                CollectionAssert.AreEqual(Items, actual);
            }
        }
        [TestMethod]
        public void SimpleWriteBoolean()
        {
            // Given: A very simple item
            var Items = new SimpleItem<Boolean>[] { new SimpleItem<Boolean>() { Key = true } };

            // When: Writing it to a spreadsheet using the new methods
            var name = "SimpleWriteBoolean";
            using (var stream = new MemoryStream())
            {
                WhenWritingToNewSpreadsheet(stream, Items, name);

                var actual = new List<SimpleItem<Boolean>>();
                var sheets = new List<string>();
                WhenReadAsOldSpreadsheet(stream, name, actual, sheets);

                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(1, sheets.Count());
                Assert.AreEqual(name, sheets.Single());
                CollectionAssert.AreEqual(Items, actual);
            }
        }

        [TestMethod]
        public void OnePayee()
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = new List<Payee>() { new Payee() { ID = 1, Category = "A", SubCategory = "B", Name = "C", Selected = true } };

            // When: Writing it to a spreadsheet using the new methods
            var name = "OnePayee";
            using (var stream = new MemoryStream())
            {
                WhenWritingToNewSpreadsheet(stream, Items, name);

                var actual = new List<Payee>();
                var sheets = new List<string>();
                WhenReadAsOldSpreadsheet<Payee>(stream, name, actual, sheets);

                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(1, sheets.Count());
                Assert.AreEqual(name, sheets.Single());
                CollectionAssert.AreEqual(Items, actual);
            }
        }


        [TestMethod]
        public void OneTransactionEmpty()
        {
            // NOTE: This fails to load in excel, whereas the previous ones DO work.

            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = new List<Transaction>() { new Transaction() { Timestamp = new DateTime(2021, 01, 03) } };

            // When: Writing it to a spreadsheet using the new methods
            var name = "OneTransactionEmpty";
            using (var stream = new MemoryStream())
            {
                WhenWritingToNewSpreadsheet(stream, Items, name);

                var actual = new List<Transaction>();
                var sheets = new List<string>();
                WhenReadAsOldSpreadsheet<Transaction>(stream, name, actual, sheets);

                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(1, sheets.Count());
                Assert.AreEqual(name, sheets.Single());
                CollectionAssert.AreEqual(Items, actual);
            }
        }

        [TestMethod]
        public async Task TransactionItemsFew()
        {
            // Given: A ton of transactions
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()).Take(2).ToList();

            // When: Writing it to a spreadsheet using the new methods
            var name = "TransactionItemsFew";
            using (var stream = new MemoryStream())
            {
                WhenWritingToNewSpreadsheet(stream, Items, name);

                var actual = new List<Transaction>();
                var sheets = new List<string>();
                WhenReadAsOldSpreadsheet<Transaction>(stream, name, actual, sheets);

                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(1, sheets.Count());
                Assert.AreEqual(name, sheets.Single());
                CollectionAssert.AreEqual(Items, actual);
            }
        }
        [TestMethod]
        public async Task TransactionItems20()
        {
            // Given: A ton of transactions
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()).Take(20).ToList();

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteNewReadOld("TransactionItems20", Items);
        }

        public void WriteNewReadOld<T>(string name, List<T> items) where T : class, new()
        {
            // Given: Some items

            // When: Writing it to a spreadsheet using the new methods
            using (var stream = new MemoryStream())
            {
                WhenWritingToNewSpreadsheet(stream, items, name);

                // And: Reading it back to a spreadsheet using the old methods
                var actual = new List<T>();
                var sheets = new List<string>();
                WhenReadAsOldSpreadsheet<T>(stream, name, actual, sheets);

                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(1, sheets.Count());
                Assert.AreEqual(name, sheets.Single());
                CollectionAssert.AreEqual(items, actual);
            }
       }
    }
}
