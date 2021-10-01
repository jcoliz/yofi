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

        private void WhenReadAsNewSpreadsheet<T>(MemoryStream stream, List<T> actual, List<string> sheets) where T : class, new()
        {
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new OpenXmlSpreadsheetReader();
            reader.Open(stream);
            actual.AddRange(reader.Read<T>(TestContext.TestName));
            sheets.AddRange(reader.SheetNames.ToList());
        }

        public void WriteThenReadBack<T>(IEnumerable<T> items, bool writetodisk = true) where T : class, new()
        {
            // Given: Some items

            // When: Writing it to a spreadsheet using the new methods
            using var stream = new MemoryStream();
            WhenWritingToNewSpreadsheet(stream, items, writetodisk);

            // And: Reading it back to a spreadsheet
            var actual = new List<T>();
            var sheets = new List<string>();

            WhenReadAsNewSpreadsheet<T>(stream, actual, sheets);

            // Then: The spreadsheet is valid, and contains the expected item
            Assert.AreEqual(1, sheets.Count());
            Assert.AreEqual(TestContext.TestName, sheets.Single());
            Assert.IsTrue(actual.SequenceEqual(items));
        }

        [TestMethod]
        public void SimpleWriteString()
        {
            // Given: A very simple string item
            var Items = new List<SimpleItem<string>>() { new SimpleItem<string>() { Key = "Hello, world!" } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }

        [TestMethod]
        public void SimpleWriteStringNull()
        {
            // Given: A small list of simple string items, one with null key
            var Items = new List<SimpleItem<string>>() { new SimpleItem<string>(), new SimpleItem<string>() { Key = "Hello, world!" } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }

        [TestMethod]
        public void SimpleWriteDateTime()
        {
            // Given: A very simple item w/ DateTime member
            var Items = new List<SimpleItem<DateTime>>() { new SimpleItem<DateTime>() { Key = new DateTime(2021,06,08) } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }

        [TestMethod]
        public void SimpleWriteInt32()
        {
            // Given: A very simple item w/ Int32 member
            var Items = new List<SimpleItem<Int32>>() { new SimpleItem<Int32>() { Key = 12345 } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }

        [TestMethod]
        public void SimpleWriteDecimal()
        {
            // Given: A very simple item w/ decimal member
            var Items = new List<SimpleItem<decimal>>() { new SimpleItem<decimal>() { Key = 123.45m } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }

        [TestMethod]
        public void SimpleWriteBoolean()
        {
            // Given: A very simple item w/ boolean member
            var Items = new List<SimpleItem<Boolean>>() { new SimpleItem<Boolean>() { Key = true } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }

        [TestMethod]
        public void OnePayee()
        {
            // Given: A single empty transaction
            // Note that an empty timestamp does not serialize well
            var Items = new List<Payee>() { new Payee() { ID = 1, Category = "A", Name = "C", Selected = true } };

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
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
            WriteThenReadBack(Items);
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
            WriteThenReadBack(Items);
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
            WriteThenReadBack(Items);
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
            WriteThenReadBack(Items);
        }

        [TestMethod]
        public async Task TransactionItemsFew()
        {
            // Given: A small number of transactions
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()).Take(2) /*.ToList()*/;

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
        }

        [TestMethod]
        public async Task TransactionItems20()
        {
            // Given: A ton of transactions
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()).Take(20) /*.ToList()*/;

            // When: Writing it to a spreadsheet using the new methods
            // And: Reading it back to a spreadsheet using the old methods
            // Then: The spreadsheet is valid, and contains the expected item
            WriteThenReadBack(Items);
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
            WriteThenReadBack<Transaction>(Items, writetodisk:false);
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
            using var stream = new MemoryStream();
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
            reader = new OpenXmlSpreadsheetReader();
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

        [TestMethod]
        public async Task LoadAnyName()
        {
            // User Story 1042: Upload spreadsheet shouldn't be worried about name of sheet

            // Given: A file created with an arbitrary non-confirming sheet name
            var Items = (await TransactionControllerTest.GetTransactionItemsLong()).Take(20) /*.ToList()*/;
            using var stream = new MemoryStream();
            WhenWritingToNewSpreadsheet(stream, Items, writetodisk:true);

            // When: Loading the file, without specifying the sheet name
            var actual = new List<Transaction>();
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new OpenXmlSpreadsheetReader();
            reader.Open(stream);
            actual.AddRange(reader.Read<Transaction>());

            // Then: Data is loaded as expected.
            Assert.IsTrue(actual.SequenceEqual(Items));
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void CustomColumnNullFails()
        {
            var writer = new OpenXmlSpreadsheetWriter();
            var sheets = writer.SheetNames;
        }

        public class ThirtyMembers
        {
            public int Member_01 { get; set; }
            public int Member_02 { get; set; }
            public int Member_03 { get; set; }
            public int Member_04 { get; set; }
            public int Member_05 { get; set; }
            public int Member_06 { get; set; }
            public int Member_07 { get; set; }
            public int Member_08 { get; set; }
            public int Member_09 { get; set; }
            public int Member_10 { get; set; }
            public int Member_11 { get; set; }
            public int Member_12 { get; set; }
            public int Member_13 { get; set; }
            public int Member_14 { get; set; }
            public int Member_15 { get; set; }
            public int Member_16 { get; set; }
            public int Member_17 { get; set; }
            public int Member_18 { get; set; }
            public int Member_19 { get; set; }
            public int Member_20 { get; set; }
            public int Member_21 { get; set; }
            public int Member_22 { get; set; }
            public int Member_23 { get; set; }
            public int Member_24 { get; set; }
            public int Member_25 { get; set; }
            public int Member_26 { get; set; }
            public int Member_27 { get; set; }
            public int Member_28 { get; set; }
            public int Member_29 { get; set; }
            public int Member_30 { get; set; }

            public override bool Equals(object obj)
            {
                return obj is ThirtyMembers members &&
                       Member_01 == members.Member_01 &&
                       Member_02 == members.Member_02 &&
                       Member_03 == members.Member_03 &&
                       Member_04 == members.Member_04 &&
                       Member_05 == members.Member_05 &&
                       Member_06 == members.Member_06 &&
                       Member_07 == members.Member_07 &&
                       Member_08 == members.Member_08 &&
                       Member_09 == members.Member_09 &&
                       Member_10 == members.Member_10 &&
                       Member_11 == members.Member_11 &&
                       Member_12 == members.Member_12 &&
                       Member_13 == members.Member_13 &&
                       Member_14 == members.Member_14 &&
                       Member_15 == members.Member_15 &&
                       Member_16 == members.Member_16 &&
                       Member_17 == members.Member_17 &&
                       Member_18 == members.Member_18 &&
                       Member_19 == members.Member_19 &&
                       Member_20 == members.Member_20 &&
                       Member_21 == members.Member_21 &&
                       Member_22 == members.Member_22 &&
                       Member_23 == members.Member_23 &&
                       Member_24 == members.Member_24 &&
                       Member_25 == members.Member_25 &&
                       Member_26 == members.Member_26 &&
                       Member_27 == members.Member_27 &&
                       Member_28 == members.Member_28 &&
                       Member_29 == members.Member_29 &&
                       Member_30 == members.Member_30;
            }

            public override int GetHashCode()
            {
                HashCode hash = new HashCode();
                hash.Add(Member_01);
                hash.Add(Member_02);
                hash.Add(Member_03);
                hash.Add(Member_04);
                hash.Add(Member_05);
                hash.Add(Member_06);
                hash.Add(Member_07);
                hash.Add(Member_08);
                hash.Add(Member_09);
                hash.Add(Member_10);
                hash.Add(Member_11);
                hash.Add(Member_12);
                hash.Add(Member_13);
                hash.Add(Member_14);
                hash.Add(Member_15);
                hash.Add(Member_16);
                hash.Add(Member_17);
                hash.Add(Member_18);
                hash.Add(Member_19);
                hash.Add(Member_20);
                hash.Add(Member_21);
                hash.Add(Member_22);
                hash.Add(Member_23);
                hash.Add(Member_24);
                hash.Add(Member_25);
                hash.Add(Member_26);
                hash.Add(Member_27);
                hash.Add(Member_28);
                hash.Add(Member_29);
                hash.Add(Member_30);
                return hash.ToHashCode();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void SheetNamesFails()
        {
            var writer = new OpenXmlSpreadsheetWriter();
            var sheets = writer.SheetNames;
        }

        [TestMethod]
        public void WriteLongClass()
        {
            var items = new List<ThirtyMembers>()
            {
                new ThirtyMembers() { Member_23 = 23, Member_30 = 30 }
            };

            WriteThenReadBack(items);
        }
    }
}
