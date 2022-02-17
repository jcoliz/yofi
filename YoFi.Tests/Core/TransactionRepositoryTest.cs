using Common.DotNet;
using Common.NET.Test;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Core.Repositories.Wire;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class TransactionRepositoryTest : BaseRepositoryTest<Transaction>, IFakeObjectsSaveTarget
    {
        #region Fields

        private TestAzureStorage storage;
        private TestClock clock;
        private ITransactionRepository transactionRepository => repository as ITransactionRepository;

        #endregion

        #region Helpers

        public new void AddRange(System.Collections.IEnumerable objects)
        {
            if (objects is IEnumerable<Transaction> txs)
            {
                repository.AddRangeAsync(txs).Wait();
            }
        }

        protected IFakeObjects<Transaction> GivenComplexDataInDatabase()
        {
            // Given: A mix of transactions, in differing years
            // And: some with '{word}' in their category, memo, or payee and some without
            // And: some with receipts, some without
            var chosen = FakeObjects<Transaction>
                .Make(2, x =>
                {
                    x.Category += "CCC";
                    x.Payee += "222";
                    x.Timestamp = new DateTime(2100, 1, 1);
                })
                .Add(2, x => 
                {
                    x.Category += "BBB";
                    x.Payee += "222";
                    x.Memo += "Wut";
                    x.Timestamp = new DateTime(2100, 1, 1);
                })
                .Add(2, x => 
                {
                    x.Memo += "Wut";
                    x.Timestamp = new DateTime(2100, 1, 1);
                })
                .Add(2, x => 
                {
                    x.Payee += "Wut";
                    x.Timestamp = new DateTime(2100, 1, 1);
                })
                .Add(2, x => 
                {
                    x.Category += "BBB";
                    x.Payee += "444";
                    x.Payee += "Wut";
                })
                .SaveTo(this);

            return chosen;
        }

        #endregion

        #region Init/Cleanup

        [TestInitialize]
        public void SetUp()
        {
            context = new MockDataContext();
            storage = new TestAzureStorage();
            clock = new TestClock();
            repository = new TransactionRepository(context, clock, storage);
        }

        #endregion

        #region Tests

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

            var year = 2000 + (inmonth - 1) / 12;
            var month = 1 + (inmonth - 1) % 12;

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
            var fakeref = "ABC123";
            var data = FakeObjects<Transaction>.Make(12, x => x.BankReference = null).Add(8, x => x.BankReference = fakeref).SaveTo(this) ;
            var hadref = data.Group(1);

            // When: Calling AssignBankReferences()
            await transactionRepository.AssignBankReferences();

            // Then: All transactions have a bankref now
            Assert.IsFalse(repository.All.Any(x => string.IsNullOrEmpty(x.BankReference)));

            // And: Those which already had a bankref did not have theirs changed
            Assert.AreEqual(hadref.Count, repository.All.Count(x => x.BankReference == fakeref));
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

        [DataRow("c=CCC,p=222,y=2100", 0,1)]
        [DataRow("p=222,y=2100", 0,2)]
        [DataRow("c=BBB,p=444", 4,5)]
        [DataRow("m=Wut,y=2100", 1,3)]
        [DataRow("Wut,y=2100", 1,4)]
        [DataTestMethod]
        public async Task DownloadQComplex(string q, int from, int to)
        {
            // Given: A mix of transactions, in differing years
            // And: some with '{word}' in their category, memo, or payee and some without
            var all = GivenComplexDataInDatabase();
            var chosen = all.Groups(from..to).OrderBy(x=>x.ID);

            // When: Downloading transactions with q='{word},{key}={value}' in various combinations
            var stream = await transactionRepository.AsSpreadsheetAsync(chosen.First().Timestamp.Year, false, q);

            // And: Loading it as a spreadsheet
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var items = ssr.Deserialize<Transaction>().OrderBy(x=>x.ID);

            // Then: Only the transactions with '{word}' in their category, memo, or payee AND matching the supplied {key}={value} are returned
            Assert.IsTrue(items.SequenceEqual(chosen));
        }

        #endregion

        #region Index Tests

        [TestMethod]
        public async Task IndexPayeeSearch()
        {
            // Given: A set of items, some of which have a certain payee
            var word = "Fibbledy-jibbit";
            var chosen = FakeObjects<Transaction>.Make(5).Add(2, x => x.Payee += word).SaveTo(this).Group(1);

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
            var chosen = FakeObjects<Transaction>.Make(5).Add(2, x => x.Category += word).SaveTo(this).Group(1);

            // When: Calling Index with category search term
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"c={word}" });

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQPayeeAny()
        {
            // Given: A set of items, some of which have a certain payee
            var word = "Fibbledy-jibbit";
            var chosen = FakeObjects<Transaction>.Make(5).Add(3, x => x.Payee += word).SaveTo(this).Group(1);

            // When: Calling index q={word}
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = word });

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQAny()
        {
            // Given: A mix of transactions, some with '{word}' in their category, memo, or payee and some without
            // (Note, I am prototyping a new way to handle multiple kinds of fake data)
            // (Unfortunately, I feel like this is LESS expressive still)
            var word = "CAF";
            var chosen = FakeObjects<Transaction>
                .Make(2)
                .Add(2, x => x.Payee += word)
                .Add(2, x => x.Memo += word)
                .Add(2, x => x.Category += word)
                .SaveTo(this)
                .Groups(1..);

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
            var chosen = FakeObjects<Transaction>
                .Make(2)
                .Add(2, x => x.Payee += word)
                .Add(2, x => x.Memo += word)
                .Add(2, x => x.Category += word)
                .SaveTo(this)
                .Group(1);

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
            var chosen = FakeObjects<Transaction>
                .Make(2)
                .Add(2, x => x.Category += word)
                .Add(2, x => x.Payee += word)
                .Add(2, x => x.Memo += word)
                .SaveTo(this)
                .Group(1);

            // When: Calling index q='c={word}'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"C={word}" });

            // Then: Only the transactions with '{word}' in their category are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQCategoryBlank()
        {
            // Given: A mix of transactions, some with null category
            var chosen = FakeObjects<Transaction>.Make(5).Add(3, x => x.Category = null).SaveTo(this).Group(1);

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
            var chosen = FakeObjects<Transaction>
                .Make(2)
                .Add(2, x => x.Memo += word)
                .Add(2, x => x.Category += word)
                .Add(2, x => x.Payee += word)
                .SaveTo(this)
                .Group(1);

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
            var items = FakeObjects<Transaction>.Make(5).Add(3, x => x.ReceiptUrl = "Has receipt").SaveTo(this);
            var chosen = items.Group(1);

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
            _ = FakeObjects<Transaction>.Make(5).Add(3, x => x.ReceiptUrl = "Has receipt").SaveTo(this);

            // When: Calling index with q='r=text'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = "R=bogus" });

            // Then: Throws exception
        }

        [TestMethod]
        public async Task IndexQYear()
        {
            // Given: A mix of transactions, in differing years
            int year = 1992;
            var chosen = FakeObjects<Transaction>.Make(5).Add(3, x => x.Timestamp = new DateTime(year, 1, 1)).SaveTo(this).Group(1);

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
            _ = FakeObjects<Transaction>.Make(10).SaveTo(this);

            // When: Calling index with q='a=text'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = "A=text" });

            // Then: Throws exception
        }

        [TestMethod]
        public async Task IndexQAmountInteger()
        {
            // Given: A mix of transactions, some with a certain amount, others not
            var amount = 123m;
            var chosen = FakeObjects<Transaction>
                .Make(2)
                .Add(3, x => x.Amount = amount)
                .Add(3, x => x.Amount = amount/100)
                .SaveTo(this)
                .Groups(1..);

            // When: Calling index with q='a=###'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"A={amount:0}" });

            // Then: Only transactions with amounts #.## and ###.00 are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQAmountAny()
        {
            // Given: A mix of transactions, some with a certain amount, others not
            var amount = 599m;
            var chosen = FakeObjects<Transaction>
                .Make(2)
                .Add(3, x => x.Amount = amount)
                .Add(3, x => x.Amount = amount / 100)
                .SaveTo(this)
                .Groups(1..);

            // When: Calling index with q='###'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"{amount:0}" });

            // Then: Only transactions with amounts #.## and ###.00 are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQAmountDouble()
        {
            // Given: A mix of transactions, some with a certain amount, others not
            var amount = 1.23m;
            var chosen = FakeObjects<Transaction>.Make(5).Add(3, x => x.Amount = amount).SaveTo(this).Group(1);

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
            var chosen = FakeObjects<Transaction>.Make(5).Add(3, x=>x.Category += word).SaveTo(this).Group(1);

            // When: Calling index q={word}
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = word });

            // Then: Only the transactions with '{word}' in their category are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task IndexQCategorySplits(bool any)
        {
            // This test tests BOTH the case where we supply the word with or without the "c=" specifier.
            // The "Any=true" case is NOT specifying it, in which case we're ensuring that the result INCLUDES the desired items
            // The "Any=false" case is YES specifying it, in which case we're ensuring that the result EXACTLY includes the desired items

            // Given: A mix of transactions, some with splits, some without; some with '{word}' in their category, memo, or payee, or splits category and some without
            var word = "CAF";

            var items = FakeObjects<Transaction>
                .Make(5)
                .Add(2, x => x.Category += word)
                .Add(2, x => x.Splits = FakeObjects<Split>.Make(2, s => s.Category += word).Group(0))
                .Add(2, x => x.Payee += word)
                .Add(2, x => x.Memo += word)
                .Add(2, x => x.Splits = FakeObjects<Split>.Make(2).Group(0))
                .SaveTo(this);

            var chosen = any ? items.Groups(1..5) : items.Groups(1..3);

            // When: Calling index q={word} OR q=c={word}
            var q = any ? word : $"c={word}";            
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = q });

            // Then: Only the transactions with '{word}' in their category (or everywhere) are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQMemoAny()
        {
            // Given: A mix of transactions, some with '{word}' in their memo and some without
            var word = "CAF";
            var chosen = FakeObjects<Transaction>.Make(6).Add(4, x => x.Memo += word).SaveTo(this).Group(1);

            // When: Calling index q={word}
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = word });

            // Then: Only the transactions with '{word}' in their memo are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task IndexQMemoSplitsAny(bool any)
        {
            // Given: A mix of transactions, some with '{word}' in their memo and some without
            var word = "CAF";
            var items = FakeObjects<Transaction>
                .Make(5)
                .Add(2, x => x.Memo += word)
                .Add(2, x => x.Splits = FakeObjects<Split>.Make(2, s => s.Memo += word).Group(0))
                .Add(2, x => x.Category += word)
                .Add(2, x => x.Splits = FakeObjects<Split>.Make(2, s => s.Category += word).Group(0))
                .Add(2, x => x.Payee += word)
                .Add(2, x => x.Splits = FakeObjects<Split>.Make(2).Group(0))
                .SaveTo(this);

            var chosen = any ? items.Groups(1..6) : items.Groups(1..3);

            // When: Calling index q={word}
            var q = any ? word : $"m={word}";
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = q });

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
            var items = FakeObjects<Transaction>.Make(22).SaveTo(this);

            clock.Now = items.Min(x => x.Timestamp);

            // When: Calling index with q='d=#/##'
            var target = items.Min(x => x.Timestamp).AddDays(day);
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
            var items = FakeObjects<Transaction>.Make(22).SaveTo(this);
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
            var items = FakeObjects<Transaction>.Make(22).SaveTo(this);

            // When: Calling index with q='d=text'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"D=text" });

            // Then: Exception
        }

        [TestMethod]
        public async Task IndexQIntAny()
        {
            // Given: A mix of transactions, with differing amounts, dates, and payees
            var number = 123m;
            var number00 = number / 100m;
            var date = new DateTime(DateTime.Now.Year, (int)(number / 100m), (int)(number % 100m));
            var word = number.ToString("0");

            var chosen = FakeObjects<Transaction>
                .Make(5)
                .Add(2, x => x.Category += word)
                .Add(2, x => x.Memo += word)
                .Add(2, x => x.Payee += word)
                .Add(2, x => x.Amount = number)
                .Add(2, x => x.Amount = number00)
                .Add(2, x => x.Timestamp = date)
                .SaveTo(this)
                .Groups(1..)
                .OrderBy(x => x.Payee);

            // When: Calling index q={word}
            // (Note that we are ordering just so we can ensure easy comparison later)
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = word, Order = "pa" });

            // Then: Transactions with {###} in the memo or payee are returned AS WELL AS
            // transactions on or within a week of #/## AS WELL AS transactions with amounts
            // of ###.00 and #.##.
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexQIntAnyMultiple()
        {
            // Given: A mix of transactions, with differing amounts, dates, and payees
            var numbers = new[] { 123m, 501m };
            var words = numbers.Select(x => x.ToString("0")).ToList();
            var number = numbers[0];
            var number00 = number / 100m;
            var dates = numbers.Select(x => new DateTime(DateTime.Now.Year, (int)(x / 100m), (int)(x % 100m))).ToList();

            var chosen = FakeObjects<Transaction>
                .Make(5)
                .Add(2, x => { x.Memo += words[0]; x.Timestamp = dates[1]; })
                .Add(2, x => { x.Amount = number00; x.Category += words[1]; })
                .Add(2, x => x.Category += words[0])
                .Add(2, x => x.Payee += words[0])
                .Add(2, x => x.Amount = number)
                .Add(2, x => x.Timestamp = dates[0])
                .SaveTo(this)
                .Groups(1..3)
                .OrderBy(x => x.Payee);

            // When: Calling index q={words}
            // (Note that we are ordering just so we can ensure easy comparison later)
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = string.Join(",", words), Order = "pa" });

            // Then: Only the transactions with BOTH '{words}' somewhere in their searchable terms are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task IndexQUnknown()
        {
            // When: Calling index with an unknown query key q='!=1'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = "!=1" });

            // Then: Exception
        }

        [DataTestMethod]
        public async Task IndexVHidden()
        {
            // Given: A mix of transactions, some hidden, some not
            var items = FakeObjects<Transaction>.Make(2).Add(3, x => x.Hidden = true).SaveTo(this);

            // When: Calling index v='h'
            var document = await WhenGettingIndex(new WireQueryParameters() { View = "H" });

            // Then: All transactions are returned
            ThenResultsAreEqualByTestKey(document, items);
        }

        [TestMethod]
        public async Task IndexNoHidden()
        {
            // Given: A mix of transactions, some hidden, some not
            var items = FakeObjects<Transaction>.Make(2).Add(3, x => x.Hidden = true).SaveTo(this);

            // When: Calling index without qualifiers
            var document = await WhenGettingIndex(new WireQueryParameters());

            // Then: Only non-hidden transactions are returned
            ThenResultsAreEqualByTestKey(document, items.Group(0));
        }

        [DataRow("c=CCC,p=222,y=2100", 0, 1)]
        [DataRow("p=222,y=2100", 0, 2 )]
        [DataRow("c=BBB,p=444", 4, 5 )]
        [DataRow("m=Wut,y=2100", 1, 3 )]
        [DataRow("Wut,y=2100", 1, 4)]
        [DataTestMethod]
        public async Task IndexQComplex(string q, int from, int to)
        {
            // Given: A mix of transactions, in differing years
            // And: some with '{word}' in their category, memo, or payee and some without
            // And: some with receipts, some without
            var all = GivenComplexDataInDatabase();
            var chosen = all.Groups(from..to);

            // When: Calling index q='{word},{key}={value}' in various combinations
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = q });

            // Then: Only the transactions with '{word}' in their category, memo, or payee AND matching the supplied {key}={value} are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        #endregion

        #region Splits Tests

        [TestMethod]
        public async Task CreateSplit()
        {
            // Given: There are 5 items in the database, one of which we care about
            var items = FakeObjects<Transaction>.Make(5).SaveTo(this);
            var expected = items.Last();
            var id = expected.ID;
            var category = expected.Category;

            // When: Adding a split to that item
            var newid = await transactionRepository.AddSplitToAsync(id);

            // Then: Split has expected values
            var actual = expected.Splits.Single();

            Assert.AreEqual(expected.Amount, actual.Amount);
            Assert.AreEqual(category, actual.Category);
            Assert.IsNull(expected.Category);
        }

        [TestMethod]
        public async Task CreateSecondSplit()
        {
            // Given: There are 5 items in the database, one of which already has a split
            var expected = FakeObjects<Transaction>
                .Make(4)
                .Add(1, x => 
                {
                    x.Category = null;
                    x.Splits = new List<Split>()
                    {
                        new Split()
                        {
                            Amount = 25m,
                            Category = "A"
                        }
                    };
                })
                .SaveTo(this)
                .Group(1)
                .Single();

            var id = expected.ID;

            // When: Adding a split to that item
            var newid = await transactionRepository.AddSplitToAsync(id);

            // Then: Splits are now balanced
            Assert.IsTrue(expected.IsSplitsOK);
            Assert.AreEqual(2,expected.Splits.Count);

            // And: New split has no category
            Assert.IsNull(expected.Splits.Last().Category);
        }

        #endregion

        #region Receipts

        [TestMethod]
        public async Task UpReceipt()
        {
            // Given: A transaction with no receipt
            var items = FakeObjects<Transaction>.Make(5).SaveTo(this);
            var expected = items.Last();
            var id = expected.ID;

            // And: An image file
            var contenttype = "image/png";
            var length = 25;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());

            // When: Uploading it as a receipt
            await transactionRepository.UploadReceiptAsync(expected, stream, contenttype);

            // Then: The transaction displays as having a receipt
            Assert.IsFalse(string.IsNullOrEmpty(expected.ReceiptUrl));

            // And: The receipt is contained in storage
            Assert.AreEqual(1, storage.BlobItems.Count());
            Assert.AreEqual(contenttype, storage.BlobItems.Single().ContentType);
            Assert.AreEqual(id.ToString(), storage.BlobItems.Single().FileName);
        }

        [TestMethod]
        public async Task GetReceipt()
        {
            // Given: A transaction with a receipt
            var filename = "1234";
            var expected = FakeObjects<Transaction>
                .Make(4)
                .Add(1, x => x.ReceiptUrl = filename )
                .SaveTo(this)
                .Group(1)
                .Single();

            var id = expected.ID;
            var contenttype = "image/png";

            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

            // When: Getting the receipt
            var (stream, contenttypeout, name) = await transactionRepository.GetReceiptAsync(expected);

            // Then: The receipt is returned
            Assert.AreEqual(filename, name);
            Assert.AreEqual(contenttype, contenttypeout);
        }

        #endregion

    }
}
