using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
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
        public async Task IndexSearch()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<BudgetTx>(5, 1);

            // When: Searching the index for the focused item's name
            var document = await WhenGetAsync($"{urlroot}/?q={chosen.Single().Category}");

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task IndexPage1()
        {
            // Given: A long set of items, which is longer than one page, but not as long as two pages 
            var pagesize = BaseRepository<BudgetTx>.DefaultPageSize;
            (var items, var p1) = await GivenFakeDataInDatabase<BudgetTx>(pagesize * 3 / 2, pagesize);

            // When: Getting the Index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: Only one page of items returned, which are the LAST group, cuz it's sorted by time
            ThenResultsAreEqualByTestKey(document, p1);
        }

        [TestMethod]
        public async Task IndexPage2()
        {
            // Given: A long set of items, which is longer than one page, but not as long as two pages 
            var pagesize = BaseRepository<BudgetTx>.DefaultPageSize;
            (var items, var p1) = await GivenFakeDataInDatabase<BudgetTx>(pagesize * 3 / 2, pagesize);

            // When: Getting the Index for page 2
            var document = await WhenGetAsync($"{urlroot}/?p=2");

            // Then: Only 2nd page items returned
            ThenResultsAreEqualByTestKey(document, items.Except(p1));
        }

        [TestMethod]
        public async Task Details()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<BudgetTx>(5, 1);
            var expected = chosen.Single();
            var id = chosen.Single().ID;

            // When: Getting details for the chosen item
            var document = await WhenGetAsync($"{urlroot}/Details/{id}");

            // Then: That item is shown
            var actual = document.QuerySelector("[data-test-id=memo]").TextContent.Trim();
            Assert.AreEqual(expected.Memo, actual);
        }

        [TestMethod]
        public async Task Edit()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<BudgetTx>(5, 1);
            var id = chosen.Single().ID;

            // When: Editing the chosen item
            var expected = GivenFakeItem<BudgetTx>(100);
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
            var expected = GivenFakeItem<BudgetTx>(70);
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
        public async Task UploadDuplicate()
        {
            // Given: One item in the database
            var initial = await GivenFakeDataInDatabase<BudgetTx>(1);

            // And: A spreadsheet containing 5 items, including a duplicate of the item in the database
            var items = GivenFakeItems<BudgetTx>(5).OrderBy(x => x.Memo);
            var stream = GivenSpreadsheetOf(items);
            // Note that "GivenFakeItems" will restart at 1, so item Name 01 is the dupe

            // When: Uploading the spreadsheet
            var document = await WhenUploadingSpreadsheet(stream, $"{urlroot}/", $"{urlroot}/Upload");

            // Then: Only the non-duplicate items were returned
            ThenResultsAreEqualByTestKey(document, items.Skip(1));

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

            // And: It's a spreadsheet containing our items
            await ThenIsSpreadsheetContaining(response.Content, items);
        }

        #endregion
    }
}
