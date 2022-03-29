using AngleSharp.Html.Dom;
using jcoliz.FakeObjects;
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
            // Reset the clock back
            integrationcontext.clock.Reset();

            // Clean out database
            context.Set<Transaction>().RemoveRange(context.Set<Transaction>());
            context.Set<Payee>().RemoveRange(context.Set<Payee>());
            context.Set<Receipt>().RemoveRange(context.Set<Receipt>());
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
            var items = FakeObjects<Transaction>.Make(20,
                x=> 
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

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task IndexShowHidden(bool ishidden)
        {
            // Given: A set of items, some of which are hidden
            var items = FakeObjects<Transaction>.Make(2).Add(3, x => x.Hidden = true).SaveTo(this);

            // When: Calling Index with indirect search term for hidden items
            var searchterm = ishidden ? "H" : null;
            var document = await WhenGettingIndex(new WireQueryParameters() { View = searchterm });

            // Then: Only the items with a matching hidden state are returned
            if (ishidden)
                ThenResultsAreEqualByTestKey(document, items);
            else
                ThenResultsAreEqualByTestKey(document, items.Group(0));

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
            var items = FakeObjects<Transaction>.Make(5).SaveTo(this);

            // When: Calling index with view set to 'selected'
            var searchterm = isselected ? "S" : null;
            var document = await WhenGettingIndex(new WireQueryParameters() { View = searchterm });

            // Then: The selection checkbox is available (or not, as expected)
            var checkboxshown = !(document.QuerySelector($"[data-test-id=select]") is null);
            Assert.AreEqual(isselected, checkboxshown);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataRow(null)]
        [DataTestMethod]
        public async Task IndexShowHasReceipt(bool? hasreceipt)
        {
            // Given: A set of items, some with receipts some not
            var items = FakeObjects<Transaction>.Make(2).Add(3,x => x.ReceiptUrl = "Has receipt").SaveTo(this);

            // When: Calling Index with receipt search term
            string query = string.Empty;
            if (hasreceipt.HasValue)
                query = hasreceipt.Value ? "R=1" : "R=0";
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = query });

            // Then: Only the items with a matching receipt state are returned
            if (hasreceipt.HasValue)
            {
                if (hasreceipt.Value)
                    ThenResultsAreEqualByTestKey(document, items.Group(1));
                else
                    ThenResultsAreEqualByTestKey(document, items.Group(0));
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
            var items = FakeObjects<Transaction>.Make(6,
                x => 
                { 
                    x.ReceiptUrl = (x.Amount % 200m == 0) ? "Has receipt" : null;
                    x.Payee = (x.Amount % 300m).ToString("0000");
                })
                .SaveTo(this);

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

        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataTestMethod]
        public async Task IndexQDate(int day)
        {
            // Given: A mix of transactions, with differing dates, within the current year
            var items = FakeObjects<Transaction>.Make(22,x=>x.Timestamp = new DateTime(DateTime.Now.Year,x.Timestamp.Month,x.Timestamp.Day)).SaveTo(this);

            // When: Calling index with q='d=#/##'
            var target = items.Min(x => x.Timestamp).AddDays(day);
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = $"D={target.Month}/{target.Day}" });

            // Then: Only transactions on that date or the following 7 days in the current year are returned
            var expected = items.Where(x => x.Timestamp >= target && x.Timestamp < target.AddDays(7));
            ThenResultsAreEqualByTestKey(document, expected);
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

        #endregion

        #region Bulk Edit

        [TestMethod]
        public async Task BulkEdit()
        {
            // Given: 10 items in the database, 7 of which are marked "selected"
            var data = FakeObjects<Transaction>.Make(3).Add(7, x => x.Selected = true).SaveTo(this);
            var ids = data.Group(1).Select(x => x.ID);

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
            var unedited = context.Set<Transaction>().Where(x => !ids.Contains(x.ID)).AsNoTracking().OrderBy(TestKey<Transaction>.Order()).ToList();
            Assert.IsTrue(data.Group(0).SequenceEqual(unedited));

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
            _ = FakeObjects<Transaction>.Make(3).Add(7, x => x.Selected = true).SaveTo(this);

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
        public async Task CreateSplit()
        {
            // Given: There are 5 items in the database, one of which we care about
            var items = FakeObjects<Transaction>.Make(5).SaveTo(this);
            var expected = items.Last();
            var id = expected.ID;
            var category = expected.Category;

            // When: Adding a split to that item
            var formData = new Dictionary<string, string>()
            {
                { "id", id.ToString() },
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Edit/{id}", d => $"{urlroot}/CreateSplit", formData);

            // Then: Split has expected values
            var actualtx = context.Set<Transaction>().Include(x=>x.Splits).Where(x => x.ID == id).AsNoTracking().Single();
            var actual = actualtx.Splits.Single();

            Assert.AreEqual(expected.Amount, actual.Amount);
            Assert.AreEqual(category, actual.Category);
            Assert.IsNull(actualtx.Category);
        }

        [TestMethod]
        public async Task SplitsShownInIndex()
        {
            // Given: Single transaction with balanced splits
            _ = FakeObjects<Transaction>
                .Make(1, x =>
                {
                    x.Splits = FakeObjects<Split>.Make(2).Group(0);
                    x.Amount = x.Splits.Sum(x => x.Amount);
                })
                .SaveTo(this);

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
            var expected = FakeObjects<Transaction>
                .Make(5)
                .Add(1,
                    x =>
                    {
                        x.Splits = FakeObjects<Split>.Make(2, s => { s.Category += " Split"; }).Group(0);
                        x.Amount = x.Splits.Sum(s => s.Amount);
                    })
                .SaveTo(this)
                .Last();

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
            var chosen = FakeObjects<Transaction>
                .Make(1, x =>
                {
                    x.Splits = FakeObjects<Split>.Make(2).Group(0);
                    x.Amount = x.Splits.Sum(x => x.Amount);
                })
                .SaveTo(this);
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
            var id = FakeObjects<Transaction>.Make(1,
                x =>
                {
                    x.Splits = FakeObjects<Split>.Make(2).Group(0);
                    x.Amount = 2 * x.Splits.Sum(x => x.Amount);
                })
                .SaveTo(this)
                .Single()
                .ID;

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
            var expected = FakeObjects<Transaction>.Make(1,
                x =>
                {
                    x.Timestamp = DateTime.Now;
                    x.Splits = FakeObjects<Split>.Make(2).Group(0);
                    x.Amount = x.Splits.Sum(x => x.Amount);
                })
                .SaveTo(this)
                .Single();

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

        [TestMethod]
        public async Task UploadSplitsForTransaction()
        {
            // Given: There are 5 items in the database, one of which we care about
            var expected = FakeObjects<Transaction>.Make(5).SaveTo(this).Last();
            var id = expected.ID;

            // And: A spreadsheet of splits which will break down the details of that transaction
            var numsplits = 4;
            var splits = FakeObjects<Split>.Make(numsplits, x => { x.Amount = expected.Amount / numsplits; });
            var stream = GivenSpreadsheetOf(splits);

            // When: Uploading the spreadsheet as splits for the chosen transaction
            var content = new MultipartFormDataContent
            {
                { new StreamContent(stream), "files", "Splits.xlsx" },
                { new StringContent(id.ToString()), "id" }
            };
            var response = await WhenUploading(content, $"{urlroot}/Edit/{id}", $"{urlroot}/UpSplits");

            // Then: Redirected to "/Transactions/Edit"
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"/Transactions/Edit/{id}", redirect);

            // And: The splits are now attached to the transaction in the database
            var actual = context.Set<Transaction>().Include(x=>x.Splits).Where(x => x.ID == id).AsNoTracking().Single();
            Assert.IsTrue(actual.HasSplits);
            Assert.IsTrue(actual.IsSplitsOK);
            Assert.IsTrue(splits.SequenceEqual(actual.Splits.OrderBy(TestKey<Split>.Order())));
        }

        #endregion

        #region Download

        [TestMethod]
        public async Task DownloadAllYears()
        {
            // Given: A mix of transactions over many years
            var items = FakeObjects<Transaction>.Make(30,
                x =>
                    x.Timestamp = new DateTime(2020 - x.Timestamp.Day, x.Timestamp.Month, x.Timestamp.Day)
                )
                .SaveTo(this);

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

            // And: The received items match the expected items
            Assert.IsTrue(items.SequenceEqual(ssr_txs));
        }

        [TestMethod]
        public async Task DownloadNoSplits_Bug895()
        {
            // Bug 895: Transaction download appears corrupt if no splits

            // Given: A mix of transactions over many years, with no splits
            var items = FakeObjects<Transaction>.Make(30,
                x =>
                    x.Timestamp = new DateTime(2020 - x.Timestamp.Day, x.Timestamp.Month, x.Timestamp.Day)
                )
                .SaveTo(this);

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
            var expected = FakeObjects<Transaction>.Make(5).SaveTo(this).Last();
            var id = expected.ID;

            // When: Asking for the modal edit partial view
            var document = await WhenGetAsync($"{urlroot}/EditModal/{id}");

            // Then: That item is shown
            var testkey = TestKey<Transaction>.Find().Name;
            var actual = document.QuerySelector($"input[name={testkey}]").GetAttribute("value").Trim();
            Assert.AreEqual(TestKey<Transaction>.Order()(expected), actual);
        }

        [DataRow("Edit")]
        [DataRow("EditModal")]
        [DataTestMethod]
        public async Task EditPayeeMatch(string endpoint)
        {
            // Given: A transaction with no category
            var tx = FakeObjects<Transaction>.Make(4).Add(1,x => x.Category = null).SaveTo(this).Group(1).Single();

            // And: A payee which matches the category payee
            var payee = FakeObjects<Payee>.Make(1,x => x.Name = tx.Payee).SaveTo(this).Single();

            // When: Asking for the modal edit partial view
            var document = await WhenGetAsync($"{urlroot}/{endpoint}/{tx.ID}");

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
            var tx = FakeObjects<Transaction>.Make(5).SaveTo(this).Last();

            // And: A payee which matches the transaction payee, but has a different category
            var unexpectedcategory = "Unexpected";
            var payee = FakeObjects<Payee>.Make(1,x => { x.Name = tx.Payee; x.Category = unexpectedcategory; }).SaveTo(this).Single();

            // When: Asking for the modal edit partial view
            var document = await WhenGetAsync($"{urlroot}/{endpoint}/{tx.ID}");

            // Then: The transaction DID NOT get the matching payee category
            var actual = document.QuerySelector($"input[name=Category]").GetAttribute("value").Trim();
            Assert.AreNotEqual(payee.Category, actual);
        }

        [TestMethod]
        public async Task EditDuplicate()
        {
            // Given: There are 5 items in the database, one of which we care about, plus an additional item to be use as edit values
            var data = FakeObjects<Transaction>.Make(4).SaveTo(this).Add(1);
            var id = data.Group(0).Last().ID;
            var newvalues = data.Group(1).Single();

            // When: Editing the chosen item, with duplicate = true
            var formData = new Dictionary<string, string>(FormDataFromObject(newvalues))
            {
                { "ID", id.ToString() },
                { "duplicate", "true" },
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Edit/{id}", FormAction, formData);

            // Then: Redirected to index
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: The item was added, so the whole database now is the original items plus the expected
            var actual = context.Set<Transaction>().AsNoTracking().OrderBy(TestKey<Transaction>.Order());
            Assert.IsTrue(actual.SequenceEqual(data));
        }

        [TestMethod]
        public async Task EditReceiptOverride_Bug846()
        {
            // Bug 846: Save edited item overwrites uploaded receipt

            // Given: There are 5 items in the database, one of which we care about
            // Note that this does not have a receipturl, by default
            var data = FakeObjects<Transaction>.Make(4).SaveTo(this).Add(1);
            var original = data.Group(0).Last();
            var id = original.ID;
            var newvalues = data.Group(1).Single();

            // Detach so our edits won't show up
            context.Entry(original).State = EntityState.Detached;

            // And: Separately committing a change to set the receipturl
            // Note that we have not reflected this change in our in-memory version of the object
            var newreceipturl = "SET";
            var dbversion = context.Set<Transaction>().Where(x => x.ID == id).Single();
            dbversion.ReceiptUrl = newreceipturl;
            context.SaveChanges();
            // Detach so the editing operation can work on this item
            context.Entry(dbversion).State = EntityState.Detached;

            // When: Posting an edit to the chosen item using new edited values
            // Note also no receipturl in this object
            var formData = new Dictionary<string, string>(FormDataFromObject(newvalues))
            {
                { "ID", id.ToString() },
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Edit/{id}", FormAction, formData);

            // Then: The in-database 

            // What SHOULD happen is that the "blank" recepturl in the updated object does not overwrite
            // the receitpurl we set above.
            var actual = context.Set<Transaction>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(newreceipturl, actual.ReceiptUrl);
        }

        [TestMethod]
        public async Task EditNoReceipts()
        {
            // Given: One transaction in the system
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: No receipts in the system
            // ...

            // When: Editing a transaction
            var document = await WhenGetAsync($"{urlroot}/Edit/{tx.ID}");

            // Then: No option to match is offered
            var hasreceipts = document.QuerySelector("div[data-test-id=hasreceipts]");
            Assert.IsNull(hasreceipts);
        }

        [TestMethod]
        public async Task EditAnyReceipts()
        {
            // Given: Some receipts in the system
            _ = FakeObjects<Receipt>.Make(5).SaveTo(this);

            // And: One transaction in the system
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // When: Editing the transaction
            var document = await WhenGetAsync($"{urlroot}/Edit/{tx.ID}");

            // Then: Option to match a receipt is offered
            var hasreceipts = document.QuerySelector("div[data-test-id=hasreceipts]");
            Assert.IsNotNull(hasreceipts);
        }

        [TestMethod]
        public async Task EditMatchingReceipt()
        {
            // Given: One transaction in the system
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: Some receipts in the system where exactly one matches the transation
            var r = FakeObjects<Receipt>.Make(1,x=>x.Name = tx.Payee).Add(5,x=>x.Timestamp += TimeSpan.FromDays(100)).SaveTo(this).First();

            // When: Editing the transaction
            var document = await WhenGetAsync($"{urlroot}/Edit/{tx.ID}");

            // Then: Option to apply the matching receipt is offered
            var accept = document.QuerySelector("form[data-test-id=accept]");
            Assert.IsNotNull(accept);

            var rid_str = accept.GetAttribute("data-test-value");
            var rid = int.Parse(rid_str);
            Assert.AreEqual(r.ID,rid);
        }

        [TestMethod]
        public async Task EditBestMatchingReceipt()
        {
            // Given: One transaction in the system
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: Some receipts in the system where many match the transation, butone mattches exactly
            var r = FakeObjects<Receipt>.Make(1,x=>{x.Name = tx.Payee;x.Timestamp = tx.Timestamp;}).Add(5,x=>{ x.Name = tx.Payee; x.Timestamp = tx.Timestamp + TimeSpan.FromDays(1);}).SaveTo(this).First();

            // When: Editing the transaction
            var document = await WhenGetAsync($"{urlroot}/Edit/{tx.ID}");

            // Then: Option to apply the best matching receipt is offered
            var accept = document.QuerySelector("form[data-test-id=accept]");
            Assert.IsNotNull(accept);

            var rid_str = accept.GetAttribute("data-test-value");
            var rid = int.Parse(rid_str);
            Assert.AreEqual(r.ID,rid);
        }

        #endregion

        #region Receipts

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

            integrationcontext.storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = filename, InternalFile = "budget-white-60x.png", ContentType = contenttype });

            // When: Getting the receipt
            var response = await client.GetAsync($"{urlroot}/GetReceipt/{id}");
            response.EnsureSuccessStatusCode();

            // Then: Response is OK
            response.EnsureSuccessStatusCode();

            // And: It's a stream
            Assert.IsInstanceOfType(response.Content, typeof(StreamContent));
            var streamcontent = response.Content as StreamContent;

            // Then: The receipt is returned
            Assert.AreEqual(filename, streamcontent.Headers.ContentDisposition.FileNameStar);
            Assert.AreEqual(contenttype, streamcontent.Headers.ContentType.ToString());
        }

        #endregion

        #region Other Tests

        [TestMethod]
        public async Task CreatePage()
        {
            // Given: It's a certain time
            var now = new DateTime(2003, 07, 15);
            integrationcontext.clock.Now = now;

            // When: Asking for the page to create a new item
            var document = await WhenGetAsync($"{urlroot}/Create");

            // Then: The "timestamp" is filled in with the correct time
            var input = document.QuerySelector("input[name=Timestamp]") as IHtmlInputElement;
            var actual_str = input.DefaultValue;
            var actual = DateTime.Parse(actual_str);
            Assert.AreEqual(now.Date, actual.Date);
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
