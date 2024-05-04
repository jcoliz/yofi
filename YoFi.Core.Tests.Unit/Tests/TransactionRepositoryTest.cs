using Common.DotNet;
using Common.DotNet.Test;
using DocumentFormat.OpenXml.Office2010.Excel;
using jcoliz.FakeObjects;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Core.Repositories.Wire;
using YoFi.Tests.Helpers;

namespace YoFi.Core.Tests.Unit
{
    [TestClass]
    public class TransactionRepositoryTest : BaseRepositoryTest<Transaction>, IFakeObjectsSaveTarget
    {
        #region Fields

        private TestAzureStorage storage;
        private TestClock clock;
        private PayeeRepository payees;
        private ITransactionRepository transactionRepository => repository as ITransactionRepository;

        #endregion

        #region Helpers

        public new void AddRange(System.Collections.IEnumerable objects)
        {
            if (objects is IEnumerable<Transaction> txs)
            {
                repository.AddRangeAsync(txs).Wait();
            }
            else
            {
                context.AddRange(objects.Cast<object>());
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

            // TODO: It would be better to MOCK this dependency, however for expediency,
            // we will extend the test into payee repository
            payees = new PayeeRepository(context);
            var splits = new BaseRepository<Split>(context);

            // This is the time supposed by the FakeObjects filler
            clock.Now = new DateTime(2001, 12, 31);

            repository = new TransactionRepository(context, clock, payees, splits, storage);
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

        [DataRow("")]
        [DataRow("Bogus")]
        [DataRow("Bogus [Loan]")]
        [DataRow("Bogus [Loan] More Bogus [Loan] Even More")]
        [DataRow("Bogus [Loan] Very Bogus")]
        [DataRow("Bogus [Loan] {}")]
        [DataRow("Mortgage Interest [Loan] { \"principal\": \"Mortgage Interest\", \"origination\": \"1/1/2000\" } ")]
        [DataRow("Mortgage Interest [Loan] { \"principal\": \"Mortgage Interest\" } ")]
        [DataTestMethod]
        public void CalculateLoanSplitsError(string rule)
        {
            var item = FakeObjects<Transaction>.Make(1).Single();
            var result = transactionRepository.CalculateCustomSplitRules(item, rule);

            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public async Task AssignBankReferences()
        {
            // Given: Many transactions in the repository, some with a bankref, others not
            var fakeref = "ABC123";
            var data = FakeObjects<Transaction>.Make(12, x => x.BankReference = null).Add(8, x => x.BankReference = fakeref).SaveTo(this);
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

        [DataRow("c=CCC,p=222,y=2100", 0, 1)]
        [DataRow("p=222,y=2100", 0, 2)]
        [DataRow("c=BBB,p=444", 4, 5)]
        [DataRow("m=Wut,y=2100", 1, 3)]
        [DataRow("Wut,y=2100", 1, 4)]
        [DataTestMethod]
        public async Task DownloadQComplex(string q, int from, int to)
        {
            // Given: A mix of transactions, in differing years
            // And: some with '{word}' in their category, memo, or payee and some without
            var all = GivenComplexDataInDatabase();
            var chosen = all.Groups(from..to).OrderBy(x => x.ID);

            // When: Downloading transactions with q='{word},{key}={value}' in various combinations
            var stream = await transactionRepository.AsSpreadsheetAsync(chosen.First().Timestamp.Year, false, q);

            // And: Loading it as a spreadsheet
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var items = ssr.Deserialize<Transaction>().OrderBy(x => x.ID);

            // Then: Only the transactions with '{word}' in their category, memo, or payee AND matching the supplied {key}={value} are returned
            Assert.IsTrue(items.SequenceEqual(chosen));
        }

        [TestMethod]
        public async Task FinalizeImport()
        {
            // Given: A mix of transactions, some are not imported, some imported are not selected, some imported are selected
            var chosen = FakeObjects<Transaction>
                .Make(5)
                .Add(6, x => x.Imported = x.Selected = true)
                .Add(7, x => x.Imported = true)
                .SaveTo(this)
                .Groups(0..2);

            // When: Finalizing the import
            await transactionRepository.FinalizeImportAsync();

            // Then: The remaining items are the not imported plus imported selected items
            Assert.IsTrue(transactionRepository.All.SequenceEqual(chosen));

            // And: None are imported or selected
            Assert.IsTrue(transactionRepository.All.All(x => x.Selected != true && x.Imported != true));
        }

        [TestMethod]
        public async Task CancelImport()
        {
            // Given: A mix of transactions, some are not imported, some imported are not selected, some imported are selected
            var chosen = FakeObjects<Transaction>
                .Make(5)
                .Add(6, x => x.Imported = x.Selected = true)
                .Add(7, x => x.Imported = true)
                .SaveTo(this)
                .Group(0);

            // When: Cancelling the import
            await transactionRepository.CancelImportAsync();

            // Then: The remaining items are the not imported items only
            Assert.IsTrue(transactionRepository.All.SequenceEqual(chosen));

            // And: None are imported or selected
            Assert.IsTrue(transactionRepository.All.All(x => x.Selected != true && x.Imported != true));
        }

        [TestMethod]
        public async Task CategoryAutoComplete()
        {
            // Given: A mix of transactions, some with '{word}' in their category, memo, or payee and some without
            var word = "CategoryAutoComplete";
            var data = FakeObjects<Transaction>
                .Make(2)
                .Add(2, x => x.Category += word)
                .Add(2, x => x.Splits = FakeObjects<Split>.Make(2, s => s.Category += word).Group(0))
                .Add(2, x => x.Memo += word)
                .Add(2, x => x.Splits = FakeObjects<Split>.Make(2, s => s.Memo += word).Group(0))
                .Add(2, x => x.Payee += word)
                .SaveTo(this);

            // And: Current day is latest of data
            clock.Now = data.Max(x => x.Timestamp);

            // When: Asking for the category autocomplete for {word}
            var list = await transactionRepository.CategoryAutocompleteAsync(word);

            var expected = data
                .Group(1)
                .Select(x => x.Category)
                .Concat(
                    data
                        .Group(2)
                        .SelectMany(x => x.Splits)
                        .Select(x => x.Category)
                )
                .Distinct()
                .OrderBy(x => x);
            Assert.IsTrue(list.SequenceEqual(expected));
        }

        [TestMethod]
        public async Task CategoryAutoCompleteNotFound()
        {
            // Given: A mix of transactions, NONE of which have '{word}' in their category
            var word = "CategoryAutoComplete";
            _ = FakeObjects<Transaction>
                .Make(5)
                .SaveTo(this);

            // When: Asking for the category autocomplete for {word}
            var list = await transactionRepository.CategoryAutocompleteAsync(word);

            // Then: Empty set returned
            Assert.IsFalse(list.Any());
        }
        [TestMethod]
        public async Task CategoryAutoCompleteEmpty()
        {
            // Given: A mix of transactions, NONE of which have '{word}' in their category
            _ = FakeObjects<Transaction>
                .Make(5)
                .SaveTo(this);

            // When: Asking for the category autocomplete for empty string
            var list = await transactionRepository.CategoryAutocompleteAsync(string.Empty);

            // Then: Empty set returned
            Assert.IsFalse(list.Any());
        }

        [TestMethod]
        public async Task CategoryAutoCompleteError()
        {
            // Given: An error-generating setup
            repository = new TransactionRepository(null, null, null, null, null);

            // When: Asking for the category autocomplete for anything
            var list = await transactionRepository.CategoryAutocompleteAsync("anything");

            // Then: Empty set returned
            Assert.IsFalse(list.Any());
        }

        [TestMethod]
        public async Task BulkEdit()
        {
            // Given: Many items in the data set, some of which are selected
            var data = FakeObjects<Transaction>.Make(5).Add(4, x => x.Selected = true).SaveTo(this);
            var untouched = data.Group(0);
            var selected = data.Group(1);

            // When: Applying bulk edit to the repository using a new category
            var newcategory = "New Category";
            await transactionRepository.BulkEditAsync(newcategory);

            // Then: The selected items have the new category
            Assert.IsTrue(selected.All(x => x.Category == newcategory));

            // And: The other items are unchanged
            Assert.IsTrue(untouched.All(x => x.Category != newcategory));

            // And: All items are unselected
            Assert.IsTrue(data.All(x => x.Selected != true));
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task Select(bool value)
        {
            // Given: There are 5 items in the database, one of which we care about
            var id = FakeObjects<Transaction>.Make(4).Add(1, (x => x.Selected = !value)).SaveTo(this).Last().ID;

            // When: Selecting the item
            await transactionRepository.SetSelectedAsync(id, value);

            // Then: Item selection matches value
            var actual = context.Get<Transaction>().Where(x => x.ID == id).Single();
            Assert.AreEqual(value, actual.Selected);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task Hide(bool value)
        {
            // Given: There are 5 items in the database, one of which we care about
            var id = FakeObjects<Transaction>.Make(4).Add(1, (x => x.Hidden = !value)).SaveTo(this).Last().ID;

            // When: Hiding the item via AJAX
            await transactionRepository.SetHiddenAsync(id, value);

            // Then: Item hidden matches value
            var actual = context.Get<Transaction>().Where(x => x.ID == id).Single();
            Assert.AreEqual(value, actual.Hidden);
        }

        [TestMethod]
        public async Task ApplyPayee()
        {
            // Given : More than five payees, one of which matches the name of the transaction we care about
            var payee = FakeObjects<Payee>.Make(15).SaveTo(this).Last();
            await payees.LoadCacheAsync();

            // Given: Five transactions, one of which has no category, and has "payee" matching name of chosen payee
            var id = FakeObjects<Transaction>.Make(4).Add(1, x => { x.Category = null; x.Payee = payee.Name; }).SaveTo(this).Last().ID;

            // When: Applying the payee to the transaction's ID
            var apiresult = await transactionRepository.ApplyPayeeAsync(id);

            // Then: The result is the applied category
            Assert.AreEqual(payee.Category, apiresult);

            // And: The chosen transaction has the chosen payee's category
            var actual = context.Get<Transaction>().Where(x => x.ID == id).Single();
            Assert.AreEqual(payee.Category, actual.Category);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public async Task ApplyPayeeNotFound()
        {
            // Given: Many payees
            _ = FakeObjects<Payee>.Make(15).SaveTo(this);

            // Given: Five transactions, one of which has no category, and has "payee" matching NONE of the payees in the DB
            var id = FakeObjects<Transaction>.Make(4).Add(1, x => { x.Category = null; x.Payee = "notfound"; }).SaveTo(this).Last().ID;

            // When: Applying the payee to the transaction's ID
            var apiresult = await transactionRepository.ApplyPayeeAsync(id);

            // Then: KeyNotFoundException
        }

        [DataTestMethod]
        [DataRow("1234567 Bobby XN April 2021 5 wks")]
        [DataRow("1234567 Bobby MAR XN")]
        [DataRow("1234567 Jan XN ")]
        public async Task ApplyPayeeRegex_Pbi871(string name)
        {
            // Product Backlog Item 871: Match payee on regex, optionally

            // Given: A payee with a regex for its name
            var expectedpayee = new Payee() { Category = "Y", Name = "/1234567.*XN/" };
            AddRange(new[] { expectedpayee });
            await payees.LoadCacheAsync();

            // And: A transaction which should match it
            var expected = new Transaction() { Payee = name, Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m };
            AddRange(new[] { expected });

            // When: Applying the payee to the transaction's ID
            var apiresult = await transactionRepository.ApplyPayeeAsync(expected.ID);

            // Then: The result is the applied category
            Assert.AreEqual(expectedpayee.Category, apiresult);

            // And: The chosen transaction has the chosen payee's category
            var actual = context.Get<Transaction>().Where(x => x.ID == expected.ID).Single();
            Assert.AreEqual(expectedpayee.Category, actual.Category);
        }


        [TestMethod]
        public async Task ApplyPayeeLoanMatch()
        {
            // Given: A set of loan details
            var inmonth = 133;
            var interest = -359.32m;
            var principal = -1328.39m;
            var payment = -1687.71m;
            var year = 2000 + (inmonth - 1) / 12;
            var month = 1 + (inmonth - 1) % 12;
            var principalcategory = "Mortgage Principal";
            var interestcategory = "Mortgage Interest";
            var rule = $"{principalcategory} [Loan] {{ \"interest\": \"{interestcategory}\", \"amount\": 200000, \"rate\": 6, \"term\": 180, \"origination\": \"1/1/2000\" }} ";
            var payeename = "Mortgage Lender";

            // Given: A test transaction in the database which is a payment for that loan
            var transaction = new Transaction() { Payee = payeename, Amount = payment, Timestamp = new DateTime(year, month, 1) };
            AddRange(new[] { transaction });

            // And: A payee matching rule for that loan
            var payee = new Payee() { Name = payeename, Category = rule };
            AddRange(new[] { payee });

            // When: Applying the payee to the transaction's ID
            var apiresult = await transactionRepository.ApplyPayeeAsync(transaction.ID);

            // And: The returned text is "SPLIT"
            Assert.AreEqual("SPLIT", apiresult);

            // And: The item now has 2 splits which match the expected loan details
            var actual = context.Get<Transaction>().Where(x => x.ID == transaction.ID).Single();
            Assert.IsNull(actual.Category);
            Assert.AreEqual(2, actual.Splits.Count);
            Assert.AreEqual(interest, actual.Splits.Where(x => x.Category == interestcategory).Single().Amount);
            Assert.AreEqual(principal, actual.Splits.Where(x => x.Category == principalcategory).Single().Amount);
        }

        [TestMethod]
        public async Task CategoryAutocomplete()
        {
            // Given: Many recent transactions, some with {word} in their category, some not
            var word = "WORD";
            var chosen = FakeObjects<Transaction>.Make(10).Add(5, x => { x.Category += word; x.Timestamp = DateTime.Now; }).SaveTo(this).Group(1);

            // When: Calling CategoryAutocomplete with '{word}'
            var apiresult = await transactionRepository.CategoryAutocompleteAsync(word);

            // Then: All of the categories from given items which contain '{word}' are returned
            Assert.IsTrue(apiresult.OrderBy(x => x).SequenceEqual(chosen.Select(x => x.Category).OrderBy(x => x)));
        }

        [TestMethod]
        public async Task Edit()
        {
            // Given: There are 5 items in the database, one of which we care about, plus an additional item to be use as edit values
            var data = FakeObjects<Transaction>.Make(4).SaveTo(this).Add(1);
            var id = data.Group(0).Last().ID;
            var newvalues = data.Group(1).Single();

            // When: posting changed values to /Ajax/Payee/Edit/
            var apiresult = await transactionRepository.EditAsync(id, newvalues);

            // Then: The result is what we expect (ApiItemResult in JSON with the item returned to us)
            // Note that AjaxEdit ONLY allows changes to Memo,Payee,Category, so that's all we can verify
            Assert.AreEqual(newvalues.Memo, apiresult.Memo);
            Assert.AreEqual(newvalues.Category, apiresult.Category);
            Assert.AreEqual(newvalues.Payee, apiresult.Payee);

            // And: The item was changed
            var actual = context.Get<Transaction>().Where(x => x.ID == id).Single();
            Assert.AreEqual(newvalues.Memo, actual.Memo);
            Assert.AreEqual(newvalues.Category, actual.Category);
            Assert.AreEqual(newvalues.Payee, actual.Payee);
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
            var word = "IndexQCategory";
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
        public async Task IndexQDefaultsToLast12mo()
        {
            // [Scenario] Default Search
            // Given: Many transactions stretching back 3 + years
            var now = clock.Now = new DateTime(2010, 12, 1);
            var items = FakeObjects<Transaction>
                .Make(5, x => x.Timestamp = now - TimeSpan.FromDays(30))
                .Add(6, x => x.Timestamp = now - TimeSpan.FromDays(500))
                .Add(7, x => x.Timestamp = now - TimeSpan.FromDays(600))
                .SaveTo(this); 

            // When: Searching for a term which will only be in some of the transactions
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"C=Category" });

            // Then: Only items containing that term AND from the previous 12 months are returned
            ThenResultsAreEqualByTestKey(document, items.Group(0));
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
                .Add(3, x => x.Amount = amount / 100)
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
            var chosen = FakeObjects<Transaction>.Make(5).Add(3, x => x.Category += word).SaveTo(this).Group(1);

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
            var date = new DateTime(clock.Now.Year, (int)(number / 100m), (int)(number % 100m));
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
            var dates = numbers.Select(x => new DateTime(clock.Now.Year, (int)(x / 100m), (int)(x % 100m))).ToList();

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

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task IndexQImported(bool i)
        {
            // Given: A mix of transactions, some imported others not
            var data = FakeObjects<Transaction>.Make(6).Add(4, x => x.Imported = true).SaveTo(this);

            // When: Calling index q=i={i}
            var intvalue = i ? 1 : 0;
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"i={intvalue}" });

            // Then: Only imported (or not) items shown
            ThenResultsAreEqualByTestKey(document, data.Group(intvalue));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task IndexQUnknown()
        {
            // When: Calling index with an unknown query key q='!=1'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = "!=1" });

            // Then: Exception
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task IndexQImportedUnknown()
        {
            // When: Calling index with an unknown query key q='i=2'
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = "i=2" });

            // Then: Exception
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task IndexOrderUnknown()
        {
            // When: Calling index with an unknown order key o='xyz'
            var document = await WhenGettingIndex(new WireQueryParameters() { Order = "xyz" });

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
        [DataRow("p=222,y=2100", 0, 2)]
        [DataRow("c=BBB,p=444", 4, 5)]
        [DataRow("m=Wut,y=2100", 1, 3)]
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

        public static IEnumerable<object[]> IndexSortOrderTestData
        {
            get
            {
                return new[]
                {
                    new object[] { new { Key = "aa" , Ascending = true, Predicate = (Func<Transaction, string>)(x=>x.Amount.ToString("0000000.00")) } },
                    new object[] { new { Key = "ad" , Ascending = false, Predicate = (Func<Transaction, string>)(x=>x.Amount.ToString("0000000.00")) } },
                    new object[] { new { Key = "pa" , Ascending = true, Predicate = (Func<Transaction, string>)(x=>x.Payee) } },
                    new object[] { new { Key = "ca" , Ascending = true, Predicate = (Func<Transaction, string>)(x=>x.Category) } },
                    new object[] { new { Key = "da" , Ascending = true, Predicate = (Func<Transaction, string>)(x=>x.Timestamp.ToOADate().ToString()) } },
                    new object[] { new { Key = "pd" , Ascending = false, Predicate = (Func<Transaction, string>)(x=>x.Payee) } },
                    new object[] { new { Key = "cd" , Ascending = false, Predicate = (Func<Transaction, string>)(x=>x.Category) } },
                    new object[] { new { Key = "dd" , Ascending = false, Predicate = (Func<Transaction, string>)(x=>x.Timestamp.ToOADate().ToString()) } },
                    new object[] { new { Key = "ra" , Ascending = true, Predicate = (Func<Transaction, string>)(x=>x.BankReference) } },
                    new object[] { new { Key = "rd" , Ascending = false, Predicate = (Func<Transaction, string>)(x=>x.BankReference) } },
                };
            }
        }

        [DynamicData(nameof(IndexSortOrderTestData))]
        [DataTestMethod]
        public async Task IndexSortOrder(dynamic item)
        {
            // Given: A set of items, where the data set produces a different composition
            // when ordered by each property.
            // Note that this is NOT the usual way fake data is made.
            var items = FakeObjects<Transaction>.Make(20,
                x =>
                {
                    int index = (int)(x.Amount / 100m);
                    x.Payee = (index % 2).ToString() + x.Payee;
                    x.Category = (index % 3).ToString() + x.Category;
                    x.Amount += (index % 4) * 10000m;
                    x.BankReference = (index % 5).ToString() + index.ToString();
                })
                .SaveTo(this);

            // When: Calling Index with a defined sort order
            // When: Getting the index
            var document = await WhenGettingIndex(new WireQueryParameters() { Order = item.Key });

            // Then: The items are returned sorted in that order
            var predicate = item.Predicate as Func<Transaction, string>;
            List<Transaction> expected = null;
            if (item.Ascending)
                expected = items.OrderBy(predicate).ToList();
            else
                expected = items.OrderByDescending(predicate).ToList();

            Assert.IsTrue(document.Items.SequenceEqual(expected));
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
            Assert.AreEqual(2, expected.Splits.Count);

            // And: New split has no category
            Assert.IsNull(expected.Splits.Last().Category);
        }

        [TestMethod]
        public async Task DownloadSplits()
        {
            // Given: Many items in the data set, some of which have splits
            var data = FakeObjects<Transaction>
                .Make(5)
                .Add(2, x => x.Splits = FakeObjects<Split>.Make(5).Group(0))
                .SaveTo(this);

            var expected = data
                .Group(1)
                .SelectMany(x => x.Splits);

            // When: Downloading the items as a spreadsheet
            using var stream = await transactionRepository.AsSpreadsheetAsync(2000,true,null);

            // And: Deserializing SPLITS from the spreadsheet
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            var actual = ssr.Deserialize<Split>("Split");

            // Then: The received items match the data set
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public async Task DeleteSplit()
        {
            // TODO: This test misses the case where the transaction had two splits but now has
            // only one, so the category is transferred to the parent transaction.

            // Given: There are two items in the database, one of which we care about
            int nextid = 1;
            var id = FakeObjects<Split>.Make(2, x => x.ID = nextid++).SaveTo(this).Last().ID;

            // When: Deleting the selected item
            var result = transactionRepository.RemoveSplitAsync(id);

            // Then: Now is only one item in database
            Assert.AreEqual(1, context.Get<Split>().Count());

            // And: The deleted item cannot be found
            Assert.IsFalse(context.Get<Split>().Any(x => x.ID == id));
        }

        #endregion

        #region Receipts

        [TestMethod]
        public async Task CreatePage()
        {
            // Given: It's a certain time
            var now = new DateTime(2003, 07, 15);
            clock.Now = now;

            // When: Asking for the page to create a new item
            var actual = await transactionRepository.CreateAsync();

            // Then: The "timestamp" is filled in with the correct time
            Assert.AreEqual(now.Date, actual.Timestamp);
        }


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
                .Add(1, x => x.ReceiptUrl = filename)
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

        [TestMethod]
        public async Task GetReceiptAsPdfNow()
        {
            // Given: A transaction with a receipt
            var filename = "1234";
            var expected = FakeObjects<Transaction>
                .Make(4)
                .Add(1, x => x.ReceiptUrl = filename)
                .SaveTo(this)
                .Group(1)
                .Single();

            var id = expected.ID;

            storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = filename, InternalFile = "budget-white-60x.png", ContentType = "application/octet-stream" });

            // When: Getting the receipt
            var (stream, contenttypeout, name) = await transactionRepository.GetReceiptAsync(expected);

            // Then: The receipt is returned
            Assert.AreEqual(filename, name);
            Assert.AreEqual("application/pdf", contenttypeout);
        }

        [TestMethod]
        public async Task GetReceiptNoReceipt()
        {
            // Given: A transaction without a receipt
            var expected = FakeObjects<Transaction>
                .Make(5)
                .SaveTo(this)
                .Last();

            // When: Getting the receipt
            var result = await transactionRepository.GetReceiptAsync(expected);

            // Then: Result is all nulls
            Assert.AreEqual((null,null,null),result);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public async Task GetReceiptNoStorage()
        {
            // Given: A transaction with a receipt
            var filename = "1234";
            var expected = FakeObjects<Transaction>
                .Make(4)
                .Add(1, x => x.ReceiptUrl = filename)
                .SaveTo(this)
                .Group(1)
                .Single();

            // And: An error-generating setup
            repository = new TransactionRepository(null, null, null, null, null);

            // When: Getting the receipt
            _ = await transactionRepository.GetReceiptAsync(expected);

            // Then: Exception
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public async Task UpReceiptNoStorage()
        {
            // Given: A transaction with no receipt
            var items = FakeObjects<Transaction>.Make(5).SaveTo(this);
            var expected = items.Last();
            var id = expected.ID;

            // And: An image file
            var contenttype = "image/png";
            var length = 25;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());

            // And: An error-generating setup
            repository = new TransactionRepository(null, null, null, null, null);

            // When: Uploading it as a receipt
            await transactionRepository.UploadReceiptAsync(expected, stream, contenttype);

            // Then: Exception
        }

        [TestMethod]
        public async Task UpReceiptId()
        {
            // Given: There are 5 items in the database, one of which we care about
            var id = FakeObjects<Transaction>.Make(5).SaveTo(this).Last().ID;

            // And: An image file
            var contenttype = "image/png";
            var length = 25;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());

            // When: Uploading it as a receipt
            await transactionRepository.UploadReceiptAsync(id, stream, contenttype);

            // Then: The request is successful
            // (No exception thrown)

            // And: The database was updated with a receipt url
            var actual = context.Get<Transaction>().Where(x => x.ID == id).Single();
            Assert.AreEqual(id.ToString(), actual.ReceiptUrl);

            // And: The receipt is contained in storage
            Assert.AreEqual(1, storage.BlobItems.Count());
            Assert.AreEqual(contenttype, storage.BlobItems.Single().ContentType);
            Assert.AreEqual(id.ToString(), storage.BlobItems.Single().FileName);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public async Task UpReceiptIdAgainFails()
        {
            // Given: There are 5 items in the database, one of which we care about
            var id = FakeObjects<Transaction>.Make(5).SaveTo(this).Last().ID;

            // And: An image file
            var contenttype = "image/png";
            var length = 25;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());

            // And: It has already been uploaded
            await transactionRepository.UploadReceiptAsync(id, stream, contenttype);

            // When: Uploading it again
            var newstream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());
            await transactionRepository.UploadReceiptAsync(id, newstream, contenttype);

            // Then: ApplicationException
        }
        #endregion

    }
}
