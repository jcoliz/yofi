using AngleSharp.Html.Dom;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    [TestClass]
    public class BudgetTxControllerTest: IntegrationTest
    {
        #region Fields

        protected const string urlroot = "/BudgetTxs";

        #endregion

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
            context.Set<BudgetTx>().RemoveRange(context.Set<BudgetTx>());
            context.SaveChanges();
        }

        #endregion

        #region Helpers

        protected override IEnumerable<T> GivenFakeItems<T>(int num) =>
            Enumerable.Range(1, num).Select(x => new BudgetTx() { Timestamp = new DateTime(2000, 1, 1) + TimeSpan.FromDays(x), Amount = x * 100m, Memo = $"Memo {x}", Category = $"Category:{x}" }) as IEnumerable<T>;

        private void ThenResultsAreEqualByMemo(IHtmlDocument document, IEnumerable<BudgetTx> chosen)
        {
            ThenResultsAreEqual(document, chosen.Select(i => i.Memo).OrderBy(n => n), "[data-test-id=memo]");
        }
        #endregion

        #region Tests

        [TestMethod]
        public async Task IndexEmpty()
        {
            // Given: Empty database

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: No items are returned
            ThenResultsAreEqualByMemo(document, Enumerable.Empty<BudgetTx>());
        }

        [TestMethod]
        public async Task IndexMany()
        {
            // Given: Many items in the database
            var items = await GivenFakeDataInDatabase<BudgetTx>(20);

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: The expected items are returned
            ThenResultsAreEqualByMemo(document, items);
        }

        [TestMethod]
        public async Task IndexSingle()
        {
            // Given: There is one item in the database
            var items = await GivenFakeDataInDatabase<BudgetTx>(1);

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: The expected items are returned
            ThenResultsAreEqualByMemo(document, items);
        }


        [TestMethod]
        public async Task Edit()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<BudgetTx>(5, 1);
            var id = chosen.Single().ID;

            // When: Editing the chosen item
            var expected = GivenFakeItems<BudgetTx>(100).Last();
            var formData = new Dictionary<string, string>()
            {
                { "ID", id.ToString() },
                { "Amount", expected.Amount.ToString() },
                { "Category", expected.Category },
                { "Timestamp", expected.Timestamp.ToString("MM/dd/yyyy") },
                { "Memo", expected.Memo }
            };

            var response = await WhenGettingAndPostingForm($"{urlroot}/Edit/{id}", d => d.QuerySelector("form").Attributes["action"].TextContent, formData);
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: Redirected to index
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: The item was changed
            var actual = context.Set<BudgetTx>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task Delete()
        {
            // Given: There are two items in the database, one of which we care about
            (var items, var selected) = await GivenFakeDataInDatabase<BudgetTx>(2, 1);
            var id = selected.Single().ID;

            // When: Deleting the selected item
            var formData = new Dictionary<string, string>()
            {
                { "ID", id.ToString() }
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Delete/{id}", d => d.QuerySelector("form").Attributes["action"].TextContent, formData);
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: Redirected to index
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: Now is only one item in database
            Assert.AreEqual(1, context.Set<BudgetTx>().Count());

            // And: The deleted item cannot be found;
            Assert.IsFalse(context.Set<BudgetTx>().Any(x => x.ID == id));
        }

        [TestMethod]
        public async Task DeleteNoIdFails()
        {
            // When: Calling delete without sending an ID
            var response = await client.GetAsync($"{urlroot}/Delete/");

            // Then: Bad Request
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task DeleteTextIdFails()
        {
            // When: Calling delete with a text id
            var response = await client.GetAsync($"{urlroot}/Delete/BOGUS");

            // Then: Bad Request
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Upload()
        {
            // Given: A spreadsheet of items
            var items = GivenFakeItems<BudgetTx>(15).OrderBy(x => x.Memo);
            var stream = GivenSpreadsheetOf(items);

            // When: Uploading it
            var document = await WhenUploadingSpreadsheet(stream,$"{urlroot}/",$"{urlroot}/Upload");

            // Then: The uploaded items are returned
            ThenResultsAreEqualByMemo(document, items);

            // And: The database now contains the items
            items.SequenceEqual(context.Set<BudgetTx>().OrderBy(x => x.Memo));
        }

        #endregion
    }
}
