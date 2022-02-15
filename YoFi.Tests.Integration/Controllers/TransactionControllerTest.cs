using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            context.Set<Payee>().RemoveRange(context.Set<Payee>());
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


        [TestMethod]
        public async Task SplitsShownDownload()
        {
            // NOTE: This currently cannot be done as a repository test, because it relies on the relational
            // foreign key mappings that EF Core puts in place.

            // Given: Single transaction with balanced splits
            (_, var chosen) = await GivenFakeDataInDatabase<Transaction>(1, 1,
                x =>
                {
                    x.Timestamp = DateTime.Now;
                    x.Splits = GivenFakeItems<Split>(2).ToList();
                    x.Amount = x.Splits.Sum(x => x.Amount);
                    return x;
                });
            var expected = chosen.Single();

            // When: Downloading them
            var formData = new Dictionary<string, string>()
            {
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Index/", d => $"{urlroot}/Download", formData);

            // Then: Response is OK
            response.EnsureSuccessStatusCode();

            // And: It's a stream
            Assert.IsInstanceOfType(response.Content, typeof(StreamContent));
            var streamcontent = response.Content as StreamContent;

            // And: The stream contains a spreadsheet
            using var ssr = new SpreadsheetReader();
            ssr.Open(await streamcontent.ReadAsStreamAsync());

            // And: Deserializing items from the spreadsheet
            var ssr_txs = ssr.Deserialize<Transaction>();
            var ssr_splits = ssr.Deserialize<Split>().OrderBy(x => x.Category);

            // Then: The received splits match the expected splits
            Assert.IsTrue(expected.Splits.SequenceEqual(ssr_splits));

            // And: The IDs match
            var txid = ssr_splits.First().TransactionID;
            var tx = ssr_txs.Where(x => x.ID == txid).Single();
            Assert.AreEqual(tx, expected);
        }

        #endregion

        #region Download

        [TestMethod]
        public async Task DownloadAllYears()
        {
            // Given: A mix of transactions over many years
            (var items, _) = await GivenFakeDataInDatabase<Transaction>(30, 30,
                x =>
                {
                    x.Timestamp = new DateTime(2020 - x.Timestamp.Day, x.Timestamp.Month, x.Timestamp.Day);
                    return x;
                });

            // When: Downloading ALL YEARS of data
            var formData = new Dictionary<string, string>()
            {
                {  "allyears", "true" }
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Index/", d => $"{urlroot}/Download", formData);

            // Then: Response is OK
            response.EnsureSuccessStatusCode();

            // And: It's a stream
            Assert.IsInstanceOfType(response.Content, typeof(StreamContent));
            var streamcontent = response.Content as StreamContent;

            // And: The stream contains a spreadsheet
            using var ssr = new SpreadsheetReader();
            ssr.Open(await streamcontent.ReadAsStreamAsync());

            // And: Deserializing items from the spreadsheet
            var ssr_txs = ssr.Deserialize<Transaction>().OrderBy(x=>x.Timestamp);

            // Then: The received items match the expected items
            Assert.IsTrue(items.SequenceEqual(ssr_txs));
        }

        [TestMethod]
        public async Task DownloadNoSplits_Bug895()
        {
            // Bug 895: Transaction download appears corrupt if no splits

            // Given: A mix of transactions over many years, with no splits
            (var items, _) = await GivenFakeDataInDatabase<Transaction>(30, 30,
                x =>
                {
                    x.Timestamp = new DateTime(2020 - x.Timestamp.Day, x.Timestamp.Month, x.Timestamp.Day);
                    return x;
                });

            // When: Downloading ALL YEARS of data
            var formData = new Dictionary<string, string>()
            {
                {  "allyears", "true" }
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Index/", d => $"{urlroot}/Download", formData);

            // Then: Response is OK
            response.EnsureSuccessStatusCode();

            // And: It's a stream
            Assert.IsInstanceOfType(response.Content, typeof(StreamContent));
            var streamcontent = response.Content as StreamContent;

            // And: The stream contains a spreadsheet
            using var ssr = new SpreadsheetReader();
            ssr.Open(await streamcontent.ReadAsStreamAsync());
            var sheetnames = ssr.SheetNames.ToList();

            Assert.AreEqual(1, sheetnames.Count());
            Assert.AreEqual("Transaction", sheetnames.Single());
        }

        [TestMethod]
        public async Task DownloadTooMuchSplits_Bug1172()
        {
            // Bug 1172: [Production Bug] Download transactions with splits overfetches

            // Transactions Index
            // Search for "p=mort"
            // Actions > Export > Current Year
            // Load downloaded file in Excel
            // Notice that the transactions page correctly includes only the transactions from the search results
            // Notice that the splits tab additionally includes paycheck splits too, which are not related to the transactions

            // Originally I thought that this bug can be triggered at the repository level, however, it seems to be tied with
            // relational DB behaviour of splits in transactions. So moving it here for noe.

            // Given: Two transactions, each with two different splits
            var transactions = new List<Transaction>()
            {
                new Transaction()
                {
                    Payee = "One", Amount = 100m, Timestamp = new DateTime(DateTime.Now.Year,1,1),
                    Splits = new List<Split>()
                    {
                        new Split() { Category = "A:1", Amount = 25m },
                        new Split() { Category = "A:2", Amount = 75m },
                    }
                },
                new Transaction()
                {
                    Payee = "Two", Amount = -500m , Timestamp = new DateTime(DateTime.Now.Year,1,1),
                    Splits = new List<Split>()
                    {
                        new Split() { Category = "B:1", Amount = -100m, Memo = string.Empty },
                        new Split() { Category = "B:2", Amount = -400m, Memo = string.Empty },
                    }
                },
            };
            context.Transactions.AddRange(transactions);
            context.SaveChanges();

            // When: Downloading a spreadsheet for just one of the transactions
            var selected = transactions.First();
            var formData = new Dictionary<string, string>()
            {
                { "allyears", "true" },
                { "q", selected.Payee }
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Index/", d => $"{urlroot}/Download", formData);

            // Then: Response is OK
            response.EnsureSuccessStatusCode();

            // And: It's a stream
            Assert.IsInstanceOfType(response.Content, typeof(StreamContent));
            var streamcontent = response.Content as StreamContent;

            // And: The stream contains a spreadsheet
            using var ssr = new SpreadsheetReader();
            ssr.Open(await streamcontent.ReadAsStreamAsync());

            // And: The spreadsheet includes splits for only the selected transaction
            var actual = ssr.Deserialize<Split>().OrderBy(x=>x.Category);
            Assert.IsTrue(actual.SequenceEqual(selected.Splits));
        }

        [TestMethod]
        public async Task DownloadPartial()
        {
            // Note this test will also test that setting the session year works

            // Given: Having first set the session year with a call to reports
            var year = 2004;
            List<string> urls = new List<string>() { $"/Report/all?year={year}" };

            // When: Calling Download Partial (in the same session)
            urls.Add($"{urlroot}/DownloadPartial");
            var document = await WhenGetAsyncSession(urls);

            // Then: The year turns up as the default download year
            var label = document.QuerySelector("label[for=year]").TextContent.Trim();
            Assert.IsTrue(label.Contains(year.ToString()));
        }

        #endregion

        #region Edit

        [TestMethod]
        public async Task EditModal()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 1);
            var expected = chosen.Single();
            var id = expected.ID;

            // When: Asking for the modal edit partial view
            var document = await WhenGetAsync($"{urlroot}/EditModal/{id}");

            // Then: That item is shown
            var testkey = FindTestKey<Transaction>().Name;
            var actual = document.QuerySelector($"input[name={testkey}]").GetAttribute("value").Trim();
            Assert.AreEqual(TestKeyOrder<Transaction>()(expected), actual);
        }

        [DataRow("Edit")]
        [DataRow("EditModal")]
        [DataTestMethod]
        public async Task EditPayeeMatch(string endpoint)
        {
            // Given: A transaction with no category
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 1, x=> { x.Category = null; return x; });
            var expected = chosen.Single();
            var id = expected.ID;

            // And: A payee which matches the category payee
            (_, var payees) = await GivenFakeDataInDatabase<Payee>(1, 1, x => { x.Name = expected.Payee; return x; });
            var payee = payees.Single();

            // When: Asking for the modal edit partial view
            var document = await WhenGetAsync($"{urlroot}/{endpoint}/{id}");

            // Then: The transaction gets the matching payee category
            var actual = document.QuerySelector($"input[name=Category]").GetAttribute("value").Trim();
            Assert.AreEqual(payee.Category, actual);
        }

        [DataRow("Edit")]
        [DataRow("EditModal")]
        [DataTestMethod]
        public async Task EditNoPayeeMatch(string endpoint)
        {
            // Given: A transaction with an existing category
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 1);
            var expected = chosen.Single();
            var id = expected.ID;

            // And: A payee which matches the transaction payee, but has a different category
            var unexpectedcategory = "Unexpected";
            (_, var payees) = await GivenFakeDataInDatabase<Payee>(1, 1, x => { x.Name = expected.Payee; x.Category = unexpectedcategory; return x; });
            var payee = payees.Single();

            // When: Asking for the modal edit partial view
            var document = await WhenGetAsync($"{urlroot}/{endpoint}/{id}");

            // Then: The transaction DID NOT get the matching payee category
            var actual = document.QuerySelector($"input[name=Category]").GetAttribute("value").Trim();
            Assert.AreNotEqual(payee.Category, actual);
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
