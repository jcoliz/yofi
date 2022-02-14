using Common.DotNet;
using Common.NET.Test;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Core.Repositories.Wire;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class TransactionRepositoryTest : BaseRepositoryTest<Transaction>
    {
        private TestClock clock;

        protected override List<Transaction> Items
        {
            get
            {
                // Need to make a new one every time we ask for it, because the old items
                // tracked IDs for a previous test
                return new List<Transaction>()
                {
                    new Transaction() { ID = 1, Category = "B", Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m, BankReference = "C" },
                    new Transaction() { ID = 2, Category = "A", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m, BankReference = "D" },
                    new Transaction() { ID = 3, Category = "C", Payee = "5", Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Amount = 300m, BankReference = "B" },
                    new Transaction() { ID = 4, Category = "B", Payee = "1", Timestamp = new DateTime(DateTime.Now.Year, 01, 05), Amount = 400m, BankReference = "E" },
                    new Transaction() { ID = 5, Category = "B", Payee = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m, BankReference = "A" },
                    new Transaction() { Category = "B", Payee = "34", Memo = "222", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m, BankReference = "J" },
                    new Transaction() { Category = "B", Payee = "1234", Memo = "Wut", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m, BankReference = "I" },
                    new Transaction() { Category = "C", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m, BankReference = "G" },
                    new Transaction() { Category = "ABC", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m, BankReference = "H" },
                    new Transaction() { Category = "DE:CAF", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m, BankReference = "F" },
                    new Transaction() { Category = "GH:CAF", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "DE:RGB", Payee = "2", Memo = "CAFE", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:RGB", Payee = "2", Memo = "CONCACAF", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:XYZ", Payee = "2", Memo = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:RGB", Payee = "2", Memo = "Wut", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "DE:RGB", Payee = "CAFE", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:RGB", Payee = "CONCACAF", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:XYZ", Payee = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Category = "GH:RGB", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m },
                    new Transaction() { Amount = 123m },
                    new Transaction() { Amount = 1.23m },
                    new Transaction() { Memo = "123" },
                };
            }
        }

        protected override int CompareKeys(Transaction x, Transaction y) => x.Payee.CompareTo(y.Payee);

        private ITransactionRepository transactionRepository => repository as ITransactionRepository;

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            var storage = new TestAzureStorage();
            clock = new TestClock();
            repository = new TransactionRepository(context, clock, storage:storage);            
        }

        public static IEnumerable<object[]> CalculateLoanSplits_Data =>
            new[]
            {
                new object[] { 1,-1000m,-687.71m },
                new object[] { 2,-996.56m,-691.15m },
                new object[] { 3,-993.11m,-694.6m },
                new object[] { 4,-989.63m,-698.08m },
                new object[] { 5,-986.14m,-701.57m },
                new object[] { 6,-982.63m,-705.08m },
                new object[] { 7,-979.11m,-708.6m },
                new object[] { 8,-975.57m,-712.14m },
                new object[] { 9,-972.01m,-715.7m },
                new object[] { 10,-968.43m,-719.28m },
                new object[] { 20,-931.64m,-756.07m },
                new object[] { 21,-927.86m,-759.85m },
                new object[] { 34,-876.96m,-810.75m },
                new object[] { 53,-796.37m,-891.34m },
                new object[] { 70,-717.5m,-970.21m },
                new object[] { 90,-615.73m,-1071.98m },
                new object[] { 111,-497.36m,-1190.35m },
                new object[] { 133,-359.32m,-1328.39m },
                new object[] { 148,-256.12m,-1431.59m },
                new object[] { 180,-8.4m,-1679.31m },
            };

        [DynamicData(nameof(CalculateLoanSplits_Data))]
        [DataTestMethod]
        public void CalculateLoanSplits(int inmonth, decimal interest, decimal principal)
        {
            // https://www.calculator.net/amortization-calculator.html?cloanamount=200000&cloanterm=15&cinterestrate=6&printit=0&x=69&y=17
            // Loan amount: 200k
            // Term: 15 years (180mo)
            // Rate = 6.0 %/yr
            // Payment = 1687.71

            // Given: A transaction

            var year = 2000 + (inmonth-1) / 12;
            var month = 1 + (inmonth-1) % 12;

            var item = new Transaction() { Amount = -1687.71m, Timestamp = new DateTime(year, month, 1) };

            // And: A loan rule definition
            var rule = "Mortgage Principal [Loan] { \"interest\": \"Mortgage Interest\", \"amount\": 200000, \"rate\": 6, \"term\": 180, \"origination\": \"1/1/2000\" } ";

            // When: Calculatiung loan splits
            var splits = transactionRepository.CalculateCustomSplitRules(item, rule);

            // Then: Two splits are created
            Assert.AreEqual(2, splits.Count());

            // And: The splits are as expected (using an excel amortization table for source)
            Assert.AreEqual(interest, splits.Where(x => x.Category == "Mortgage Interest").Single().Amount);
            Assert.AreEqual(principal, splits.Where(x => x.Category == "Mortgage Principal").Single().Amount);
        }

        [TestMethod]
        public async Task AssignBankReferences()
        {
            // Given: Many transactions in the repository, some with a bankref, others not
            await repository.AddRangeAsync(Items.Take(20));

            // When: Calling AssignBankReferences()
            await transactionRepository.AssignBankReferences();

            // Then: All transactions have a bankref now
            Assert.IsFalse(repository.All.Any(x=>string.IsNullOrEmpty(x.BankReference)));

            // And: Those which already had a bankref did not have theirs changed
            Assert.AreEqual(10, repository.All.Count(x => x.BankReference.Length == 1));
        }

        [TestMethod]
        public async Task BulkEditParts()
        {
            // Given: A list of items with varying categories, some of which match the pattern *:B:*

            var categories = new string[] { "AB:Second:E", "AB:Second:E:F", "AB:Second:A:B:C", "G H:Second:KLM NOP" };
            await repository.AddRangeAsync(categories.Select(x => new Transaction() { Category = x, Amount = 100m, Timestamp = new DateTime(2001, 1, 1), Selected = true }));

            // When: Calling Bulk Edit with a new category which includes positional wildcards
            var newcategory = "(1):New Category:(3+)";
            await transactionRepository.BulkEditAsync(newcategory);

            // Then: All previously-selected items are now correctly matching the expected category
            Assert.IsTrue(categories.Select(x => x.Replace("Second", "New Category")).SequenceEqual(repository.All.Select(x => x.Category)));
        }

        [TestMethod]
        public async Task IndexPayeeSearch()
        {
            // Given: A set of items, some of which have a certain payee
            var word = "Fibbledy-jibbit";
            (var _, var chosen) = await GivenFakeDataInDatabase<Transaction>(7, 2, x => { x.Payee += word; return x; });

            // When: Calling Index with payee search term
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"p={word}" });

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexCategorySearch()
        {
            // Given: A set of items, some of which have a certain category
            var word = "Fibbledy-jibbit";
            (var _, var chosen) = await GivenFakeDataInDatabase<Transaction>(8, 3, x => { x.Category += word; return x; });

            // When: Calling Index with category search term
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"c={word}" });

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }


        [TestMethod]
        public async Task IndexQPayeeAny()
        {
            // Given: A set of items, some of which have a certain category
            var word = "Fibbledy-jibbit";
            (var _, var chosen) = await GivenFakeDataInDatabase<Transaction>(8, 3, x => { x.Category += word; return x; });

            // When: Calling index q={word}
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = word });

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQAny()
        {
            // Given: A mix of transactions, some with '{word}' in their category, memo, or payee and some without
            var word = "CAF";
            (var _, var c1) = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Payee += word; return x; });
            (var _, var c2) = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Memo += word; return x; });
            (var _, var c3) = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Category += word; return x; });
            var chosen = c1.Concat(c2).Concat(c3);

            // When: Calling index q={word}
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = word });

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQPayee()
        {
            // Given: A mix of transactions, some with '{word}' in their category, memo, or payee and some without
            var word = "CAF";
            (var _, var chosen) = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Payee += word; return x; });
            _ = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Memo += word; return x; });
            _ = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Category += word; return x; });

            // When: Calling index q='p={word}'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"P={word}" });

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQCategory()
        {
            // Given: A mix of transactions, some with '{word}' in their category, memo, or payee and some without
            var word = "CAF";
            (var _, var chosen) = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Category += word; return x; });
            _ = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Memo += word; return x; });
            _ = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Payee += word; return x; });

            // When: Calling index q='c={word}'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"C={word}" });

            // Then: Only the transactions with '{word}' in their category are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQCategoryBlank()
        {
            // Given: A mix of transactions, some with null category
            (var _, var chosen) = await GivenFakeDataInDatabase<Transaction>(8, 3, x => { x.Category = null; return x; });

            // When: Calling index q='c=[blank]'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"C=[blank]" });

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQMemo()
        {
            // Given: A mix of transactions, some with '{word}' in their category, memo, or payee and some without
            var word = "CAF";
            (var _, var chosen) = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Memo += word; return x; });
            _ = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Category += word; return x; });
            _ = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Payee += word; return x; });

            // When: Calling index q='m={word}'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"M={word}" });

            // Then: Only the transactions with '{word}' in their memo are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task IndexQReceipt(bool with)
        {
            // Given: A mix of transactions, some with receipts, some without
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 3, x => { x.ReceiptUrl = "Has receipt"; return x; });

            // When: Calling index q='r=1' (or r=0)
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"R={(with ? '1' : '0')}" });

            // Then: Only the transactions with (or without) receipts are returned
            if (with)
                ThenResultsAreEqualByTestKey(document, chosen);
            else
                ThenResultsAreEqualByTestKey(document, items.Except(chosen));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task IndexQReceiptWrong()
        {
            // Given: A mix of transactions, some with receipts, some without
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 3, x => { x.ReceiptUrl = "Has receipt"; return x; });

            // When: Calling index with q='r=text'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = "R=bogus" });

            // Then: Throws exception
        }

        [TestMethod]
        public async Task IndexQYear()
        {
            // Given: A mix of transactions, in differing years
            int year = 1992;
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 3, x => { x.Timestamp = new DateTime(year,1,1); return x; });

            // When: Calling index q='y={year}'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"Y={year}" });

            // Then: Only the transactions in {year} are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task IndexQAmountText()
        {
            // Given: A mix of transactions, with differing amounts
            await GivenFakeDataInDatabase<Transaction>(10);

            // When: Calling index with q='a=text'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = "A=text" });

            // Then: Throws exception
        }

        [TestMethod]
        public async Task IndexQAmountInteger()
        {
            // Given: A mix of transactions, some with a certain amount, others not
            var amount = 123m;
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 3, x => { x.Amount = amount; return x; });
            // TODO: Also need to add some which are amount/100

            // When: Calling index with q='a=###'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"A={amount:0}" });

            // Then: Only transactions with amounts #.## and ###.00 are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQAmountDouble()
        {
            // Given: A mix of transactions, some with a certain amount, others not
            var amount = 1.23m;
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 3, x => { x.Amount = amount; return x; });

            // When: Calling index with q='a=#.##'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"A={amount:0.00}" });

            // Then: Only transactions with amounts #.## are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }


        [TestMethod]
        public async Task IndexQCategoryAny()
        {
            // Given: A mix of transactions, some with '{word}' in their category and some without
            var word = "CAF";
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(10, 4, x => { x.Category += word; return x; });

            // When: Calling index q={word}
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = word });

            // Then: Only the transactions with '{word}' in their category are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQCategorySplitsAny()
        {
            // Given: A mix of transactions, some with splits, some without; some with '{word}' in their category, memo, or payee, or splits category and some without
            var word = "CAF";
            _               = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Memo += word; return x; });
            (var _, var c1) = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Category += word; return x; });
            _               = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Payee += word; return x; });
            
            (var _, var c2) = await GivenFakeDataInDatabase<Transaction>(4, 2, 
                                x => 
                                {
                                    x.Splits = GivenFakeItems<Split>(2, x => { x.Category += word; return x; }).ToList();
                                    return x; 
                                });
            _               = await GivenFakeDataInDatabase<Transaction>(4, 2,
                                x =>
                                {
                                    x.Splits = GivenFakeItems<Split>(2).ToList();
                                    return x;
                                });
            var chosen = c1.Concat(c2);

            // When: Calling index q={word}
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"c={word}" });

            // Then: Only the transactions with '{word}' in their category are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }
        [TestMethod]
        public async Task IndexQMemoAny()
        {
            // Given: A mix of transactions, some with '{word}' in their memo and some without
            var word = "CAF";
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(10, 4, x => { x.Memo += word; return x; });

            // When: Calling index q={word}
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = word });

            // Then: Only the transactions with '{word}' in their memo are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }
        [TestMethod]
        public async Task IndexQMemoSplitsAny()
        {
            // Given: A mix of transactions, some with '{word}' in their memo and some without
            var word = "CAF";
            (var _, var c1) = await GivenFakeDataInDatabase<Transaction>(4, 2, x => { x.Memo += word; return x; });
            (var _, var c2) = await GivenFakeDataInDatabase<Transaction>(4, 2,
                                x =>
                                {
                                    x.Splits = GivenFakeItems<Split>(2, x => { x.Memo += word; return x; }).ToList();
                                    return x;
                                });
            _ = await GivenFakeDataInDatabase<Transaction>(4, 2,
                                x =>
                                {
                                    x.Splits = GivenFakeItems<Split>(2).ToList();
                                    return x;
                                });
            var chosen = c1.Concat(c2);

            // When: Calling index q={word}
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = word });

            // Then: Only the transactions with '{word}' in their memo are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataTestMethod]
        public async Task IndexQDate(int day)
        {
            // Given: A mix of transactions, with differing dates, within the current year
            var items = await GivenFakeDataInDatabase<Transaction>(22);
            clock.Now = items.Min(x => x.Timestamp);

            // When: Calling index with q='d=#/##'
            var target = items.Min(x=>x.Timestamp).AddDays(day);
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"D={target.Month}/{target.Day}" });

            // Then: Only transactions on that date or the following 7 days in the current year are returned
            var expected = items.Where(x => x.Timestamp >= target && x.Timestamp < target.AddDays(7));
            ThenResultsAreEqualByTestKey(document, expected);
        }

        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataTestMethod]
        public async Task IndexQDateInteger(int day)
        {
            // Given: A mix of transactions, with differing dates, within the current year
            var items = await GivenFakeDataInDatabase<Transaction>(22);
            clock.Now = items.Min(x => x.Timestamp);

            // When: Calling index with q='d=#/##'
            var target = items.Min(x => x.Timestamp).AddDays(day);
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"D={target.Month}{target.Day:00}" });

            // Then: Only transactions on that date or the following 7 days in the current year are returned
            var expected = items.Where(x => x.Timestamp >= target && x.Timestamp < target.AddDays(7));
            ThenResultsAreEqualByTestKey(document, expected);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task IndexQDateText()
        {
            // Given: A mix of transactions, with differing dates
            var items = await GivenFakeDataInDatabase<Transaction>(22);

            // When: Calling index with q='d=text'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"D=text" });

            // Then: Exception
        }
    }
}
