using Common.NET.Test;
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

        public TestContext TestContext { get; set; }

        private static TestContext _testContext;

        [ClassInitialize]
        public static void SetupTests(TestContext testContext)
        {
            _testContext = testContext;
        }

        void WhenWritingToNewSpreadsheet<T>(Stream stream,IEnumerable<T> items,bool writetodisk = true) where T: class
        {
            using (var writer = new OpenXmlSpreadsheetWriter())
            {
                writer.Open(stream);
                writer.Write(items, TestContext.TestName);
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

        private void WhenReadAsOldSpreadsheet<T>(MemoryStream stream,  List<T> actual, List<string> sheets) where T: class, new()
        {
#if EPPLUS
            stream.Seek(0, SeekOrigin.Begin);
            using (var reader = new EPPlusSpreadsheetReader())
            {
                reader.Open(stream);
                actual.AddRange(reader.Read<T>(name));
                sheets.AddRange(reader.SheetNames.ToList());
            }
#else
            throw new ApplicationException("Old spreadsheets not included. Define EPPLUS to include them.");
#endif
        }

        private void WhenReadAsNewSpreadsheet<T>(MemoryStream stream, List<T> actual, List<string> sheets) where T : class, new()
        {
            stream.Seek(0, SeekOrigin.Begin);
            using (var reader = new OpenXmlSpreadsheetReader())
            {
                reader.Open(stream);
                actual.AddRange(reader.Read<T>(TestContext.TestName));
                sheets.AddRange(reader.SheetNames.ToList());
            }
        }

        public void WriteThenReadBack<T>(IEnumerable<T> items, bool newreader = false, bool writetodisk = true) where T : class, new()
        {
            // Given: Some items

            // When: Writing it to a spreadsheet using the new methods
            using (var stream = new MemoryStream())
            {
                WhenWritingToNewSpreadsheet(stream, items, writetodisk);

                // And: Reading it back to a spreadsheet using the old methods
                var actual = new List<T>();
                var sheets = new List<string>();

                if (newreader)
                    WhenReadAsNewSpreadsheet<T>(stream, actual, sheets);
#if EPPLUS
                else
                    WhenReadAsOldSpreadsheet<T>(stream, name, actual, sheets);
#else
                else
                    return;
#endif
                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(1, sheets.Count());
                Assert.AreEqual(TestContext.TestName, sheets.Single());
                Assert.IsTrue(actual.SequenceEqual(items));
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
            WriteThenReadBack(Items, newreader);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void SimpleWriteStringNull(bool newreader)
        {
            // Given: A small list of simple string items, one with null key
            var Items = new List<SimpleItem<string>>() { new SimpleItem<string>(), new SimpleItem<string>() { Key = "Hello, world!" } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items, newreader);
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
            WriteThenReadBack(Items, newreader);
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
            WriteThenReadBack(Items, newreader);
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
            WriteThenReadBack(Items, newreader);
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
            WriteThenReadBack(Items, newreader);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void OnePayee(bool newreader)
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = new List<Payee>() { new Payee() { ID = 1, Category = "A", Name = "C", Selected = true } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items, newreader);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void AllPayees(bool newreader)
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = PayeeControllerTest.PayeeItems;

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items, newreader);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void AllSplits(bool newreader)
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = SplitControllerTest.SplitItems;

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items, newreader);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void AllBudgetTxs(bool newreader)
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = BudgetTxControllerTest.BudgetTxItems;

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items, newreader);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void OneTransactionEmpty(bool newreader)
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = new List<Transaction>() { new Transaction() { Timestamp = new DateTime(2021, 01, 03) } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items, newreader);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task TransactionItemsFew(bool newreader)
        {
            // Given: A small number of transactions
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()).Take(2) /*.ToList()*/;

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items, newreader);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task TransactionItems20(bool newreader)
        {
            // Given: A ton of transactions
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()).Take(20) /*.ToList()*/;

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items, newreader);
        }


        // This is really slow, so not running by default
        [TestMethod]
        public async Task TransactionItems1000()
        {
            // Given: A ton of transactions
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()) /*.ToList()*/;

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack<Transaction>(Items, newreader:true, writetodisk:false);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task MultipleDataSeries(bool newreader)
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
                using (var writer = new OpenXmlSpreadsheetWriter())
                {
                    writer.Open(stream);
                    writer.Write(TxItems);
                    writer.Write(SplitItems);
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
                if (newreader)
                    reader = new OpenXmlSpreadsheetReader();
#if EPPLUS
                else
                    reader = new EPPlusSpreadsheetReader();
                using (reader)
                {
                    reader.Open(stream);
                    actual_t.AddRange(reader.Read<Transaction>());
                    actual_s.AddRange(reader.Read<Split>());
                    sheets.AddRange(reader.SheetNames.ToList());
                }

                // Then: The spreadsheet is valid, and contains the expected item
                Assert.AreEqual(2, sheets.Count());
                Assert.IsTrue(sheets.Contains("Transaction"));
                Assert.IsTrue(sheets.Contains("Split"));
                CollectionAssert.AreEqual(TxItems, actual_t);
                CollectionAssert.AreEqual(SplitItems, actual_s);
#endif
            }
        }

        [TestMethod]
        public void LoadFromFile()
        {
            // Given: An existing file with splits
            var instream = SampleData.Open("Splits-Test.xlsx");

            // When: Loading this file
            IEnumerable<Split> actual;
            using(var reader = new OpenXmlSpreadsheetReader())
            {
                reader.Open(instream);
                actual = reader.Read<Split>();
            }

            // Then: Contents are as expected
            Assert.AreEqual(7, actual.Count());
            Assert.AreEqual(1, actual.Count(x => x.Memo == "Memo 4"));
            Assert.AreEqual(3, actual.Count(x => x.Category.Contains("A")));
            // Last item is a total row
            Assert.AreEqual(actual.TakeLast(1).Single().Amount, actual.SkipLast(1).Sum(x => x.Amount));
        }
    }
}
