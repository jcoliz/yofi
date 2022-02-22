using Common.DotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YoFi.Core.Models;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class ReceiptTest
    {
        private TestClock clock;

        [TestInitialize]
        public void SetUp()
        {
            clock = new TestClock() { Now = new DateTime(2010, 6, 1) };
        }

        #region From Filename

        [TestMethod]
        public void MatchNameOnly()
        {
            // Given: A filename with only a payee name in it
            var name = "Multiplex Cable Services";
            var filename = $"{name}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename, clock);

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
            var receipt = Receipt.FromFilename(filename, clock);

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
            var receipt = Receipt.FromFilename(filename, clock);

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
            var receipt = Receipt.FromFilename(filename, clock);

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
            var receipt = Receipt.FromFilename(filename, clock);

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
            var receipt = Receipt.FromFilename(filename, clock);

            // Then: The timestamp is set with correct month & date values
            var expected = DateTime.Parse(date).Date;
            Assert.AreEqual(expected.Month, receipt.Timestamp.Value.Month);
            Assert.AreEqual(expected.Day, receipt.Timestamp.Value.Day);
        }

        [DataRow("1-1","1-1-2010")]
        [DataRow("6-1", "6-1-2010")]
        [DataRow("6-2", "6-2-2009")]
        [DataRow("12-1","12-1-2009")]
        [DataRow("9-30","9-30-2009")]
        [DataRow("12-10","12-10-2009")]
        [TestMethod]
        public void MatchDatePrecise(string date, string expectedstr)
        {
            // Given: A filename with a date in it
            var filename = $"{date}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename, clock);

            // Then: The timestamp is set with correct month & date values
            var expected = DateTime.Parse(expectedstr);
            Assert.AreEqual(expected.Date, receipt.Timestamp.Value.Date);
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
            var receipt = Receipt.FromFilename(filename, clock);

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
            var receipt = Receipt.FromFilename(pdf, clock);

            // Then: Components set as expected
            if (date.HasValue)
            {
                Assert.AreEqual(date.Value.Month, receipt.Timestamp.Value.Month);
                Assert.AreEqual(date.Value.Day, receipt.Timestamp.Value.Day);
            }
            if (amount.HasValue)
                Assert.AreEqual(amount, receipt.Amount);
            if (name != null)
                Assert.AreEqual(name, receipt.Name);
            if (memo != null)
                Assert.AreEqual(memo, receipt.Memo);
        }

        #endregion

        #region Match Transaction

        [TestMethod]
        public void MatchTxNameOnly()
        {
            // Given: A transaction
            var item = FakeObjects<Transaction>.Make(1).Single();

            // And: A receipt which substring matches the name
            var receipt = new Receipt() { Name = item.Payee[0..7] };

            // When: Testing for a match
            var match = receipt.MatchesTransaction(item);

            // Then: This is a 100-point match
            Assert.AreEqual(100, match);
        }

        [TestMethod]
        public void MatchTxAmountOnly()
        {
            // Given: A transaction
            var item = FakeObjects<Transaction>.Make(1).Single();

            // And: A receipt which exactly matches the amount
            var receipt = new Receipt() { Amount = item.Amount };

            // When: Testing for a match
            var match = receipt.MatchesTransaction(item);

            // Then: This is a 100-point match
            Assert.AreEqual(100, match);
        }

        [TestMethod]
        public void MatchTxAmountAndName()
        {
            // Given: A transaction
            var item = FakeObjects<Transaction>.Make(1).Single();

            // And: A receipt which substring matches the name and matches the amount
            var receipt = new Receipt() { Name = item.Payee[0..7], Amount = item.Amount };

            // When: Testing for a match
            var match = receipt.MatchesTransaction(item);

            // Then: This is a 200-point match
            Assert.AreEqual(200, match);
        }

        [TestMethod]
        public void MatchDateOnlyFailes()
        {
            // Given: A transaction
            var item = FakeObjects<Transaction>.Make(1).Single();

            // And: A receipt which matches the date
            var receipt = new Receipt() { Timestamp = item.Timestamp };

            // When: Testing for a match
            var match = receipt.MatchesTransaction(item);

            // Then: This is not a match
            Assert.AreEqual(0, match);
        }

        [TestMethod]
        public void MatchDateAndAmount()
        {
            // Given: A transaction
            var item = FakeObjects<Transaction>.Make(1).Single();

            // And: A receipt which matches the date and amount
            var receipt = new Receipt() { Timestamp = item.Timestamp, Amount = item.Amount };

            // When: Testing for a match
            var match = receipt.MatchesTransaction(item);

            // Then: This is a 200 point match
            Assert.AreEqual(200, match);
        }

        [TestMethod]
        public void MatchDateAndAlmostAmount()
        {
            // Given: A transaction
            var item = FakeObjects<Transaction>.Make(1).Single();

            // And: A receipt which matches the date with a week and amount
            var receipt = new Receipt() { Timestamp = item.Timestamp + TimeSpan.FromDays(7), Amount = item.Amount };

            // When: Testing for a match
            var match = receipt.MatchesTransaction(item);

            // Then: This is a 193 point match
            Assert.AreEqual(193, match);
        }

        #endregion
    }
}
