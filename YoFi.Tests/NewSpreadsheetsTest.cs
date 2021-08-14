using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YoFi.AspNet.Common;

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
                       Key.Equals(item.Key);
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

        private void WhenReadAsOldSpreadsheet<T>(MemoryStream stream, string name, List<SimpleItem<T>> actual, List<string> sheets)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using (var reader = new SpreadsheetReader())
            {
                reader.Open(stream);
                actual.AddRange(reader.Read<SimpleItem<T>>(name,includeids:true));
                sheets.AddRange(reader.SheetNames.ToList());
            }
        }

        [TestMethod]
        public void SimpleWriteString()
        {
            // Given: A very simple item
            var Items = new SimpleItem<string>[] { new SimpleItem<string>() { Key = "Hello, world!" } };

            // When: Writing it to a spreadsheet using the new methods
            var name = "SimpleWriteString";
            using(var stream = new MemoryStream())
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

    }
}
