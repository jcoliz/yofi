using AngleSharp.Html.Dom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using YoFi.Core.Models;
using YoFi.Core.Repositories.Wire;
using YoFi.Tests.Integration.Helpers;
using Dto = YoFi.Core.Models.Transaction; // YoFi.AspNet.Controllers.TransactionsIndexPresenter.TransactionIndexDto;
using Transaction = YoFi.Core.Models.Transaction;

namespace YoFi.Tests.Integration.Controllers
{
    [TestClass]
    public class TransactionControllerTest : ControllerTest<Transaction>
    {
        protected override string urlroot => "/Transactions";

        #region Init/Cleanup

        [ClassInitialize]
        public static void InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            integrationcontext.Dispose();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean out database
            context.Set<Transaction>().RemoveRange(context.Set<Transaction>());
            context.SaveChanges();
        }

        #endregion

        #region Index Tests

        public static IEnumerable<object[]> IndexSortOrderTestData
        {
            get
            {
                return new[]
                {
                    new object[] { new { Key = "aa" , Ascending = true, Predicate = (Func<Dto, string>)(x=>x.Amount.ToString("0000000.00")) } },
                    new object[] { new { Key = "ad" , Ascending = false, Predicate = (Func<Dto, string>)(x=>x.Amount.ToString("0000000.00")) } },
                    new object[] { new { Key = "pa" , Ascending = true, Predicate = (Func<Dto, string>)(x=>x.Payee) } },
                    new object[] { new { Key = "ca" , Ascending = true, Predicate = (Func<Dto, string>)(x=>x.Category) } },
                    new object[] { new { Key = "da" , Ascending = true, Predicate = (Func<Dto, string>)(x=>x.Timestamp.ToOADate().ToString()) } },
                    new object[] { new { Key = "pd" , Ascending = false, Predicate = (Func<Dto, string>)(x=>x.Payee) } },
                    new object[] { new { Key = "cd" , Ascending = false, Predicate = (Func<Dto, string>)(x=>x.Category) } },
                    new object[] { new { Key = "dd" , Ascending = false, Predicate = (Func<Dto, string>)(x=>x.Timestamp.ToOADate().ToString()) } },
                    new object[] { new { Key = "ra" , Ascending = true, Predicate = (Func<Dto, string>)(x=>x.BankReference) } },
                    new object[] { new { Key = "rd" , Ascending = false, Predicate = (Func<Dto, string>)(x=>x.BankReference) } },
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
            (var _, var items) = await GivenFakeDataInDatabase<Transaction>(20,20,
                x=> 
                {
                    int index = (int)(x.Amount / 100m);
                    x.Payee = (index % 2).ToString() + x.Payee;
                    x.Category = (index % 3).ToString() + x.Category;
                    x.Amount += (index % 4) * 10000m;
                    x.BankReference = (index % 5).ToString() + index.ToString();
                    return x; 
                });

            // When: Calling Index with a defined sort order
            // When: Getting the index
            var document = await WhenGettingIndex(new WireQueryParameters() { Order = item.Key });

            // Then: The items are returned sorted in that order
            var predicate = item.Predicate as Func<Dto, string>;
            List<Dto> expected = null;
            if (item.Ascending)
                expected = items.OrderBy(predicate).ToList();
            else
                expected = items.OrderByDescending(predicate).ToList();

            ThenResultsAreEqualByTestKeyOrdered(document, expected);
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

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task IndexShowHidden(bool ishidden)
        {
            // Given: A set of items, some of which are hidden
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 3, x => { x.Hidden = true; return x; });

            // When: Calling Index with indirect search term for hidden items
            var searchterm = ishidden ? "H" : null;
            var document = await WhenGettingIndex(new WireQueryParameters() { View = searchterm });

            // Then: Only the items with a matching hidden state are returned
            if (ishidden)
                ThenResultsAreEqualByTestKey(document, items);
            else
                ThenResultsAreEqualByTestKey(document, items.Except(chosen));

            // And: The hide checkbox is available (or not, as expected)
            var checkboxshown = !(document.QuerySelector($"th[data-test-id=hide]") is null);
            Assert.AreEqual(ishidden, checkboxshown);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task IndexShowSelected(bool isselected)
        {
            // Given: There are some items in the database
            var items = await GivenFakeDataInDatabase<Transaction>(5);

            // When: Calling index with view set to 'selected'
            var searchterm = isselected ? "S" : null;
            var document = await WhenGettingIndex(new WireQueryParameters() { View = searchterm });

            // Then: The selection checkbox is available (or not, as expected)
            var checkboxshown = !(document.QuerySelector($"th[data-test-id=select]") is null);
            Assert.AreEqual(isselected, checkboxshown);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataRow(null)]
        [DataTestMethod]
        public async Task IndexShowHasReceipt(bool? hasreceipt)
        {
            // Given: A set of items, some with receipts some not
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 3, x => { x.ReceiptUrl = "Has receipt"; return x; });

            // When: Calling Index with receipt search term
            string query = string.Empty;
            if (hasreceipt.HasValue)
                query = hasreceipt.Value ? "R=1" : "R=0";
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = query });

            // Then: Only the items with a matching receipt state are returned
            if (hasreceipt.HasValue)
            {
                if (hasreceipt.Value)
                    ThenResultsAreEqualByTestKey(document, chosen);
                else
                    ThenResultsAreEqualByTestKey(document, items.Except(chosen));
            }
            else
                ThenResultsAreEqualByTestKey(document, items);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataRow(null)]
        [DataTestMethod]
        public async Task IndexPayeesWithReceipt(bool? hasreceipt)
        {
            // Given: A set of items, with two different payees, some with receipts some not
            (var _, var items) = await GivenFakeDataInDatabase<Transaction>(6, 6, 
                x => 
                { 
                    x.ReceiptUrl = (x.Amount%200m == 0) ? "Has receipt" : null;
                    x.Payee = (x.Amount % 300m).ToString("0000");
                    return x; 
                });

            // When: Calling Index with combined search term for payee AND with/without a receipt
            string payee = "0100";
            string query = $"P={payee}";
            if (hasreceipt.HasValue)
                query += hasreceipt.Value ? ",R=1" : ",R=0";
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = query });

            // Then: Only the items with a matching payee AND receipt state are returned
            IEnumerable<Dto> expected;
            if (hasreceipt.HasValue)
            {
                if (hasreceipt.Value)
                    expected = items.Where(x => x.Payee.Contains(payee) && x.ReceiptUrl != null);
                else
                    expected = items.Where(x => x.Payee.Contains(payee) && x.ReceiptUrl == null);
            }
            else
                expected = items.Where(x => x.Payee.Contains(payee));

            ThenResultsAreEqualByTestKey(document, expected);
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
            (_, var chosen) = await GivenFakeDataInDatabase<Transaction>(24, 12, 
                x => 
                {
                    int index = (int)(x.Amount / 100m);
                    if (index % 4 == 0)
                        x.Category += word;
                    if (index % 4 == 1)
                        x.Memo += word;
                    if (index % 4 == 2)
                        x.Payee += word;
                    if (index % 4 == 3)
                        x.Splits = GivenFakeItems<Split>(2, x => { x.Category += word; return x; }).ToList();
                    return x; 
                });

            // When: Calling index q={word}
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = word });

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
            (_, var chosen) = await GivenFakeDataInDatabase<Transaction>(24, 12,
                x =>
                {
                    int index = (int)(x.Amount / 100m);
                    if (index % 2 == 0)
                        x.Memo += word;
                    if (index % 2 == 1)
                        x.Splits = GivenFakeItems<Split>(2, x => { x.Memo += word; return x; }).ToList();
                    return x;
                });

            // When: Calling index q={word}
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = word });

            // Then: Only the transactions with '{word}' in their memo are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        #endregion

        #region Hiding Tests

        // Need to hide download test. Works differently for transactions
        public override Task Download()
        {
           return Task.CompletedTask;
        }

        public override Task Upload()
        {
            return Task.CompletedTask;
        }

        public override Task UploadDuplicate()
        {
            return Task.CompletedTask;
        }

        public override Task UploadFailsNoFile()
        {
            return Task.CompletedTask;
        }

#endregion
    }
}
