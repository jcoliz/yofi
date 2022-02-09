using AngleSharp.Html.Dom;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        private void ThenResultsAreEqualByTestKey(IHtmlDocument document, IEnumerable<BudgetTx> chosen)
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
            ThenResultsAreEqualByTestKey(document, Enumerable.Empty<BudgetTx>());
        }

        [TestMethod]
        public async Task IndexMany()
        {
            // Given: Many items in the database
            var items = await GivenFakeDataInDatabase<BudgetTx>(20);

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, items);
        }

        [TestMethod]
        public async Task IndexSingle()
        {
            // Given: There is one item in the database
            var items = await GivenFakeDataInDatabase<BudgetTx>(1);

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, items);
        }

        [TestMethod]
        public async Task Edit()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<BudgetTx>(5, 1);
            var id = chosen.Single().ID;

            // When: Editing the chosen item
            var expected = GivenFakeItems<BudgetTx>(100).Last();
            var formData = new Dictionary<string, string>(FormDataFromObject(expected))
            {
                { "ID", id.ToString() },
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
        public async Task Create()
        {
            // Given: There is one item in the database
            _ = await GivenFakeDataInDatabase<BudgetTx>(1);

            // When: Creating a new item
            var expected = GivenFakeItems<BudgetTx>(70).Last();
            var formData = new Dictionary<string, string>(FormDataFromObject(expected));
            var response = await WhenGettingAndPostingForm($"{urlroot}/Create", d => d.QuerySelector("form").Attributes["action"].TextContent, formData);

            // Then: Redirected to index
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: Now are two items in database
            Assert.AreEqual(2, context.Set<BudgetTx>().Count());

            var actual = context.Set<BudgetTx>().Where(x => x.Memo == expected.Memo).AsNoTracking().Single();
            Assert.AreEqual(expected, actual);
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
            ThenResultsAreEqualByTestKey(document, items);

            // And: The database now contains the items
            items.SequenceEqual(context.Set<BudgetTx>().OrderBy(x => x.Memo));
        }

        [TestMethod]
        public async Task Download()
        {
            // Given: Many items in the database
            var items = await GivenFakeDataInDatabase<BudgetTx>(20);

            // When: Downloading them
            var response = await client.GetAsync($"{urlroot}/Download");

            // Then: Response is OK
            response.EnsureSuccessStatusCode();

            // And: It's a stream
            Assert.IsInstanceOfType(response.Content, typeof(StreamContent));
            var streamcontent = response.Content as StreamContent;

            // And: The stream contains a spreadsheet
            using var ssr = new SpreadsheetReader();
            ssr.Open(await streamcontent.ReadAsStreamAsync());

            // And: The spreadsheet contains all our items
            var actual = ssr.Deserialize<BudgetTx>();
            Assert.IsTrue(items.OrderBy(x => x.Memo).SequenceEqual(actual.OrderBy(x => x.Memo)));
        }

        #endregion
    }
}
