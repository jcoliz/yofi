using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using YoFi.Core.Models;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class ReceiptTest
    {
        [TestMethod]
        public void MatchNameOnly()
        {
            // Given: A filename with only a payee name in it
            var name = "Multiplex Cable Services";
            var filename = $"{name}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename);

            // Then: The name is set
            Assert.AreEqual(name, receipt.Name);
        }

        [TestMethod]
        public void MatchMemoOnly()
        {
            // Given: A filename with only a payee name in it
            var memo = "Hello,there";
            var filename = $"({memo}).pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename);

            // Then: The memo is set
            Assert.AreEqual(memo, receipt.Memo);
        }

        [TestMethod]
        public void MatchMemoAndAmount()
        {
            // Given: A filename with only a payee name in it
            var memo = "Hello,there";
            var amount = 1234.56m;
            var filename = $"${amount} ({memo}).pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename);

            // Then: The memo is set
            Assert.AreEqual(memo, receipt.Memo);

            // Then: The amount is set
            Assert.AreEqual(amount, receipt.Amount);
        }

        [DataRow("0.01")]
        [DataRow("0")]
        [DataRow("1234")]
        [DataRow("1234.56")]
        [DataTestMethod]
        public void MatchAmountOnly(string input)
        {
            // Given: A filename with only an amount in it
            var amount = decimal.Parse(input);
            var filename = $"${amount}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename);

            // Then: The amount is set
            Assert.AreEqual(amount, receipt.Amount);
        }

        [TestMethod]
        public void MatchNameAndAmount()
        {
            // Given: A filename with a name and amount in it
            var name = "Multiplex Cable Services";
            var amount = 1234.56m;
            var filename = $"{name} ${amount}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename);

            // Then: The name is set
            Assert.AreEqual(name, receipt.Name);

            // Then: The amount is set
            Assert.AreEqual(amount, receipt.Amount);
        }

        [DataRow("1-1")]
        [DataRow("12-1")]
        [DataRow("9-30")]
        [DataRow("12-10")]
        [TestMethod]
        public void MatchDate(string date)
        {
            // Given: A filename with a date in it
            var filename = $"{date}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename);

            // Then: The timestamp is set with correct month & date values
            var expected = DateTime.Parse(date).Date;
            Assert.AreEqual(expected.Month, receipt.Timestamp.Month);
            Assert.AreEqual(expected.Date, receipt.Timestamp.Date);
        }

        [TestMethod]
        public void MatchNameAmountAndMemo()
        {
            // Given: A filename with a name and amount in it
            var name = "Multiplex Cable Services";
            var memo = "Dog F00ds";
            var amount = 1234.56m;
            var filename = $"{name} ${amount} {memo}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename);

            // Then: The name is set
            Assert.AreEqual(name, receipt.Name);

            // Then: The memo is set
            Assert.AreEqual(memo, receipt.Memo);

            // Then: The amount is set
            Assert.AreEqual(amount, receipt.Amount);
        }

        [DataRow("a=1000.01,d=10-1,n=Some Name")]
        [DataRow("a=1000.01,d=10-1")]
        [DataRow("n=Your Name Here,d=10-1")]
        [DataRow("n=Your Name Here,a=1")]
        [DataRow("a=1000.01,n=Some Name,d=10-1,m=Take a memo")]
        [DataRow("n=Your Name Here,d=10-1,m=Take a memo")]
        [DataRow("n=Your Name Here,a=1,m=Take a memo")]
        [DataTestMethod]
        public void MatchMany(string input)
        {
            // Given: A filename from varied inputs
            var terms = input.Split(",");
            var filename = new List<string>();

            string name = null;
            string memo = null;
            decimal? amount = null;
            DateTime? date = null;

            foreach(var term in terms)
            {
                var kv = term.Split('=');
                if (kv[0] == "a")
                {
                    amount = decimal.Parse(kv[1]);
                    filename.Add($"${amount}");
                }
                if (kv[0] == "n")
                {
                    name = kv[1];
                    filename.Add(name);
                }
                if (kv[0] == "m")
                {
                    memo = kv[1];
                    filename.Add(memo);
                }
                if (kv[0] == "d")
                {
                    date = DateTime.Parse(kv[1]);
                    filename.Add($"{date.Value.Month}-{date.Value.Day}");
                }
            }

            // When: Constructing a receipt object from it
            var pdf = String.Join(' ', filename) + ".pdf";
            var receipt = Receipt.FromFilename(pdf);

            // Then: Components set as expected
            if (date.HasValue)
            {
                Assert.AreEqual(date.Value.Month, receipt.Timestamp.Month);
                Assert.AreEqual(date.Value.Date, receipt.Timestamp.Date);
            }
            if (amount.HasValue)
                Assert.AreEqual(amount, receipt.Amount);
            if (name != null)
                Assert.AreEqual(name, receipt.Name);
            if (memo != null)
                Assert.AreEqual(memo, receipt.Memo);
        }
    }
}
