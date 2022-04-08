using Common.DotNet;
using jcoliz.FakeObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using YoFi.Core.Models;

namespace YoFi.Core.Tests.Unit
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
            Assert.AreEqual(expected.Month, receipt.Timestamp.Month);
            Assert.AreEqual(expected.Day, receipt.Timestamp.Day);
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
            var receipt = Receipt.FromFilename(filename, clock);

            // Then: The name is set
            Assert.AreEqual(name, receipt.Name);

            // Then: The memo is set
            Assert.AreEqual(memo, receipt.Memo);

            // Then: The amount is set
            Assert.AreEqual(amount, receipt.Amount);
        }

        [TestMethod]
        public void MatchNameAmountAndMemoFirst()
        {
            // Given: A filename with a name and amount in it
            var name = "Multiplex Cable Services";
            var memo = "DogF00ds";
            var amount = 1234.56m;
            var filename = $"({memo}) {name} ${amount}.pdf";

            // When: Constructing a receipt object from it
            var receipt = Receipt.FromFilename(filename, clock);

            // Then: The name is set
            Assert.AreEqual(name, receipt.Name);

            // Then: The memo is set
            Assert.AreEqual(memo, receipt.Memo);

            // Then: The amount is set
            Assert.AreEqual(amount, receipt.Amount);
        }

        [TestMethod]
        public void MatchNameAmountAndMemoNextToName()
        {
            // Given: A filename with a name and amount in it
            var name = "Multiplex Cable Services";
            var memo = "DogF00ds";
            var amount = 1234.56m;
            var filename = $"{name} ({memo}) ${amount}.pdf";

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
                Assert.AreEqual(date.Value.Month, receipt.Timestamp.Month);
                Assert.AreEqual(date.Value.Day, receipt.Timestamp.Day);
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
        public void MatchTxAmountAndDate()
        {
            // Given: A transaction
            var item = FakeObjects<Transaction>.Make(1).Single();

            // And: A receipt which exactly matches the amount
            var receipt = new Receipt() { Amount = item.Amount, Timestamp = item.Timestamp };

            // When: Testing for a match
            var match = receipt.MatchesTransaction(item);

            // Then: This is a 200-point match
            Assert.AreEqual(200, match);
        }


        [TestMethod]
        public void MatchTxNegativeAmountAndDate()
        {
            // Given: A transaction
            var item = FakeObjects<Transaction>.Make(1,x=>x.Amount = -12.34m ).Single();

            // And: A receipt which exactly matches the amount, but sign is flipped
            var receipt = new Receipt() { Amount = -item.Amount, Timestamp = item.Timestamp };

            // When: Testing for a match
            var match = receipt.MatchesTransaction(item);

            // Then: This is a 200-point match
            Assert.AreEqual(200, match);
        }

        [TestMethod]
        public void MatchTxAmountAndNameAndDate()
        {
            // Given: A transaction
            var item = FakeObjects<Transaction>.Make(1).Single();

            // And: A receipt which substring matches the name and matches the amount
            var receipt = new Receipt() { Name = item.Payee[0..7], Amount = item.Amount, Timestamp = item.Timestamp };

            // When: Testing for a match
            var match = receipt.MatchesTransaction(item);

            // Then: This is a 300-point match
            Assert.AreEqual(300, match);
        }

        [TestMethod]
        public void MatchTxNameAndDate_MixedCase_Bug1341()
        {
            //
            // Bug 1341: Receipt matching is case sensitive
            //

            // Given: A transaction, whose payee is all upper-case
            var item = FakeObjects<Transaction>.Make(1,x=>x.Payee = x.Payee.ToUpperInvariant()).Single();

            // And: A receipt which substring matches the name, except it's lowercase, and matches the date
            var receipt = new Receipt() { Name = item.Payee[0..7].ToLowerInvariant(), Timestamp = item.Timestamp };

            // When: Testing for a match
            var match = receipt.MatchesTransaction(item);

            // Then: This is a 200-point match
            Assert.AreEqual(200, match);
        }

        [TestMethod]
        public void MatchTxNull()
        {
            // Given: A receipt
            var receipt = new Receipt();

            // When: Testing for a match against null transaction
            var match = receipt.MatchesTransaction(null);

            // Then: This is a 0-point match
            Assert.AreEqual(0, match);
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

        [TestMethod]
        public void MatchDateAndNameAndAlmostAmount()
        {
            // Given: A transaction
            var item = FakeObjects<Transaction>.Make(1).Single();

            // And: A receipt which matches the date with a week and amount and the name
            var receipt = new Receipt() { Name = item.Payee[0..7], Timestamp = item.Timestamp + TimeSpan.FromDays(7), Amount = item.Amount };

            // When: Testing for a match
            var match = receipt.MatchesTransaction(item);

            // Then: This is a 293 point match
            Assert.AreEqual(293, match);
        }

        [TestMethod]
        public void NarrowByDate()
        {
            // Given: Many items with a range of dates
            var items = FakeObjects<Transaction>.Make(100).Group(0);

            // And: A small number of receipts which each will match one of those transactions
            var receipts = new List<Receipt>()
            {
                new Receipt() { Name = items[50].Payee, Timestamp = items[50].Timestamp },
                new Receipt() { Name = items[60].Payee, Timestamp = items[60].Timestamp },
            };

            // When: Narrowing the transactions
            var narrow = Receipt.TransactionsForReceipts(items.AsQueryable(), receipts).ToList();

            // Then: List was narrowed to 11 transactions. The original 11 in the window, plus 13 on either side (26 total)
            Assert.AreEqual(37,narrow.Count);

            // And: They match
            Assert.IsTrue(receipts.Select(x => narrow.Any(y => x.MatchesTransaction(y) > 100)).All(x=>x));
        }

        [TestMethod]
        public void HashSet()
        {
            // Given: Three sets of receipts
            var data = FakeObjects<Receipt>.Make(10).Add(20).Add(30);

            // When: Creating a hash table of sets 1 & 2
            var hashset1 = new HashSet<Receipt>(data.Groups(0..2));

            // And: Adding sets 2 and 3
            var hashset2 = new HashSet<Receipt>(data.Groups(1..3));
            hashset1.UnionWith(hashset2);

            // Then: Hashtable has all three sets now exactly
            Assert.AreEqual(data.Count(), hashset1.Count);
            Assert.IsTrue(hashset1.ToList().OrderBy(x => x.Name).SequenceEqual(data));
        }

        [TestMethod]
        public void IDs()
        {
            // Given: Some receipts with IDs, one of which we care about
            int id = 1;
            var data = FakeObjects<Receipt>.Make(10,x=>x.ID = id++);
            var expected = data.Last();

            // When: Making a dictionary by IDs
            var dict = data.ToDictionary(x => x.ID, x => x);

            // Then: Can find by ID
            var actual = dict[expected.ID];
            Assert.AreSame(expected, actual);
        }

        [TestMethod]
        public void AsTransaction()
        {
            // Given: A receipt
            var r = FakeObjects<Receipt>.Make(1).Single();

            // When: Creating a trasnaction out of it
            var t = r.AsTransaction();

            // Then: The transaction matches the receipt maximally
            var match = r.MatchesTransaction(t);
            Assert.AreEqual(300, match);
        }

        #endregion
    }
}
