using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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

        #endregion

        #region Bulk Edit

        [TestMethod]
        public async Task BulkEdit()
        {
            // Given: 10 items in the database, 7 of which are marked "selected"
            (var items, var selected) = await GivenFakeDataInDatabase<Transaction>(10, 7, x => { x.Selected = true; return x; });
            var ids = selected.Select(x => x.ID).ToList();

            // When: Calling BulkEdit with a new category
            var category = "Edited Category";
            var formData = new Dictionary<string, string>()
            {
                { "Category", category },
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Index/", d => $"{urlroot}/BulkEdit", formData);

            // Then: Redirected to index
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: All of the edited items have the new category
            var edited = context.Set<Transaction>().Where(x => ids.Contains(x.ID)).AsNoTracking().ToList();
            Assert.IsTrue(edited.All(x => x.Category == category));

            // And: None of the un-edited items have the new category
            var unedited = context.Set<Transaction>().Where(x => !ids.Contains(x.ID)).AsNoTracking().OrderBy(TestKeyOrder<Transaction>()).ToList();
            Assert.IsTrue(items.Except(selected).SequenceEqual(unedited));

        }

        [TestMethod]
        public async Task BulkEditParts()
        {
            // Given: A list of items with varying categories, some of which match the pattern *:B:*

            var categories = new string[] { "AB:Second:E", "AB:Second:E:F", "AB:Second:A:B:C", "G H:Second:KLM NOP" };
            context.Transactions.AddRange(categories.Select(x => new Transaction() { Category = x, Amount = 100m, Timestamp = new DateTime(2001, 1, 1), Selected = true }));
            context.SaveChanges();

            // When: Calling Bulk Edit with a new category which includes positional wildcards
            var category = "(1):New Category:(3+)";
            var formData = new Dictionary<string, string>()
            {
                { "Category", category },
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Index/", d => $"{urlroot}/BulkEdit", formData);

            // Then: Redirected to index
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // Then: All previously-selected items are now correctly matching the expected category
            Assert.IsTrue(categories.Select(x => x.Replace("Second", "New Category")).OrderBy(x=>x).SequenceEqual(context.Set<Transaction>().Select(x => x.Category).OrderBy(x=>x)));
        }


        [TestMethod]
        public async Task BulkEditCancel()
        {
            // Given: 10 items in the database, 7 of which are marked "selected"
            (var items, var selected) = await GivenFakeDataInDatabase<Transaction>(10, 7, x => { x.Selected = true; return x; });

            // When: Calling Bulk Edit with blank category
            var formData = new Dictionary<string, string>()
            {
                { "Category", string.Empty },
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Index/", d => $"{urlroot}/BulkEdit", formData);

            // Then: Redirected to index
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // Then: No items remain selected
            Assert.IsFalse(context.Set<Transaction>().Any(x => x.Selected == true));
        }

        #endregion

        #region Splits

        [TestMethod]
        public async Task SplitsShownInIndex()
        {
            // Given: Single transaction with balanced splits
            (var items, _) = await GivenFakeDataInDatabase<Transaction>(1, 1,
                x =>
                {
                    x.Splits = GivenFakeItems<Split>(2).ToList();
                    x.Amount = x.Splits.Sum(x => x.Amount);
                    return x;
                });

            // When: Getting the index
            var document = await WhenGettingIndex(new WireQueryParameters());

            // Then: Shown as having splits
            var display_category_span = document.QuerySelector("td.display-category span");
            Assert.IsNotNull(display_category_span);
            var category = display_category_span.TextContent.Trim();
            Assert.AreEqual("SPLIT", category);
        }
        [TestMethod]
        public async Task SplitsShownInIndexSearchCategory()
        {
            // Given: Some transactions, one of which has splits
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 1,
                x =>
                {
                    x.Splits = GivenFakeItems<Split>(2, x=> { x.Category += " Split"; return x; }).ToList();
                    x.Amount = x.Splits.Sum(x => x.Amount);
                    return x;
                });
            var expected = chosen.Single();

            // When: Getting the index, searching for the split category
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"c={expected.Splits.First().Category}" });

            // Then: One item returned, shown as having splits
            var display_category_span = document.QuerySelectorAll("td.display-category span");
            Assert.AreEqual(1, display_category_span.Count());
            var category = display_category_span.Single().TextContent.Trim();
            Assert.AreEqual("SPLIT", category);
        }

        [TestMethod]
        public async Task SplitsShownInEdit()
        {
            // Given: Single transaction with balanced splits
            (_, var chosen) = await GivenFakeDataInDatabase<Transaction>(1, 1,
                x =>
                {
                    x.Splits = GivenFakeItems<Split>(2).ToList();
                    x.Amount = x.Splits.Sum(x => x.Amount);
                    return x;
                });
            var id = chosen.Single().ID;

            // When: Getting the edit page for the transaction
            var document = await WhenGetAsync($"{urlroot}/Edit/{id}");

            // Then: Splits are shown
            var splits = document.QuerySelectorAll("table[data-test-id=splits] tbody tr");
            Assert.AreEqual(2, splits.Count());

            // And: Amounts are correct
            var amounts = splits.Select(x => x.QuerySelector("td[data-test-id=split-amount]").TextContent.Trim()).OrderBy(x=>x);
            var expectedamounts = chosen.Single().Splits.Select(x => x.Amount.ToString()).OrderBy(x=>x);
            Assert.IsTrue(amounts.SequenceEqual(expectedamounts));

            // And: They're OK (not flagged as problematic)
            var splits_not_ok = document.QuerySelector("div[data-test-id=splits-not-ok]");
            Assert.IsNull(splits_not_ok);
        }

        [TestMethod]
        public async Task SplitsDontAddUpInEdit()
        {
            // Given: Single transaction with NOT balanced splits
            (_, var chosen) = await GivenFakeDataInDatabase<Transaction>(1, 1,
                x =>
                {
                    x.Splits = GivenFakeItems<Split>(2).ToList();
                    x.Amount = 2 * x.Splits.Sum(x => x.Amount);
                    return x;
                });
            var id = chosen.Single().ID;

            // When: Getting the edit page for the transaction
            var document = await WhenGetAsync($"{urlroot}/Edit/{id}");

            // Then: Splits are shown
            var splits = document.QuerySelectorAll("table[data-test-id=splits] tbody tr");
            Assert.AreEqual(2, splits.Count());

            // And: They're NOT OK (yes flagged as problematic)
            var splits_not_ok = document.QuerySelector("div[data-test-id=splits-not-ok]");
            Assert.IsNotNull(splits_not_ok);
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
