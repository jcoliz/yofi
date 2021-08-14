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
        public class SimpleItem
        {
            public string Value { get; set; }

            public override bool Equals(object obj)
            {
                return obj is SimpleItem item &&
                       Value == item.Value;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Value);
            }
        }

        [TestMethod]
        public void SimpleWrite()
        {
            // Given: A very simple item
            var Items = new SimpleItem[] { new SimpleItem() { Value = "Hello, world!" } };

            // When: Writing it to a spreadsheet
            using(var stream = new MemoryStream())
            {
                using(var writer = new NewSpreadsheetWriter())
                {
                    writer.Open(stream);
                    writer.Write(Items);
                }

                stream.Seek(0, SeekOrigin.Begin);
                using (var outstream = File.OpenWrite($"Test-SimpleWrite.xlsx"))
                {
                    Console.WriteLine($"Writing {outstream.Name}...");
                    stream.CopyTo(outstream);
                }

                // Then: The spreadsheet is valid, and contains the expected item
                stream.Seek(0, SeekOrigin.Begin);
                IEnumerable<SimpleItem> actual = null;
                IEnumerable<string> sheets = null;
                using ( var reader = new SpreadsheetReader())
                {
                    reader.Open(stream);
                    actual = reader.Read<SimpleItem>();
                    sheets = reader.SheetNames.ToList();
                }

                Assert.AreEqual(1, sheets.Count());
                Assert.AreEqual("SimpleItem", sheets.Single());
                Assert.AreEqual(Items.First(), actual.First());
            }
        }
    }
}
