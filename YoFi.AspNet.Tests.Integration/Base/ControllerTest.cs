using AngleSharp.Html.Dom;
using jcoliz.FakeObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using YoFi.Core.Models;
using YoFi.Core.Repositories.Wire;

namespace YoFi.AspNet.Tests.Integration.Controllers
{
    [TestClass]

    public abstract class ControllerTest<T>: IntegrationTest where T: class, IID, new()
    {
        #region Properties

        protected abstract string urlroot { get; }

        #endregion

        #region Helpers

        protected Task<IHtmlDocument> WhenGettingIndex(IWireQueryParameters parms)
        {
            var terms = new List<string>();

            if (!string.IsNullOrEmpty(parms.Query))
            {
                terms.Add($"q={HttpUtility.UrlEncode(parms.Query)}");
            }
            if (!string.IsNullOrEmpty(parms.Order))
            {
                terms.Add($"o={HttpUtility.UrlEncode(parms.Order)}");
            }
            if (!string.IsNullOrEmpty(parms.View))
            {
                terms.Add($"v={HttpUtility.UrlEncode(parms.View)}");
            }
            if (parms.Page.HasValue)
            {
                terms.Add($"p={parms.Page.Value}");
            }

            var urladd = (terms.Any()) ? "?" + string.Join("&", terms) : string.Empty;

            return WhenGetAsync($"{urlroot}/{urladd}");
        }

        #endregion

        #region Tests

        [TestMethod]
        public async Task IndexEmpty()
        {
            // Given: Empty database

            // When: Getting the index
            var document = await WhenGettingIndex(new WireQueryParameters());

            // Then: No items are returned
            ThenResultsAreEqualByTestKey(document, Enumerable.Empty<T>());
        }


        [TestMethod]
        public async Task IndexMany()
        {
            // Given: Many items in the database
            var items = FakeObjects<T>.Make(20).SaveTo(this);

            // When: Getting the index
            var document = await WhenGettingIndex(new WireQueryParameters());

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, items);
        }

        [TestMethod]
        public async Task IndexSingle()
        {
            // Given: There is one item in the database
            var items = FakeObjects<T>.Make(1).SaveTo(this);

            // When: Getting the index
            var document = await WhenGettingIndex(new WireQueryParameters());

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, items);
        }

        [TestMethod]
        public async Task IndexPage1()
        {
            // Given: A long set of items, which is longer than one page, but not as long as two pages 
            var pagesize = 25; // BaseRepository<T>.DefaultPageSize;
            var items = FakeObjects<T>.Make(pagesize).Add(pagesize/2).SaveTo(this);

            // When: Getting the Index
            var document = await WhenGettingIndex(new WireQueryParameters());

            // Then: Only one page of items returned, which are the LAST group, cuz it's sorted by time
            ThenResultsAreEqualByTestKey(document, items.Group(0));
        }

        [TestMethod]
        public async Task IndexPage2()
        {
            // Given: A long set of items, which is longer than one page, but not as long as two pages 
            var pagesize = 25; // BaseRepository<BudgetTx>.DefaultPageSize;
            var items = FakeObjects<T>.Make(pagesize).Add(pagesize/2).SaveTo(this);

            // When: Getting the Index for page 2
            var document = await WhenGettingIndex(new WireQueryParameters() { Page = 2 });

            // Then: Only 2nd page items returned
            ThenResultsAreEqualByTestKey(document, items.Group(1));
        }

        [TestMethod]
        public async Task IndexSearch()
        {
            // Given: There are 5 items in the database, one of which we care about
            var chosen = FakeObjects<T>.Make(5).SaveTo(this).Take(1);

            // When: Searching the index for the focused item's testkey
            var q = (string)TestKey<T>.Order()(chosen.Single());
            var document = await WhenGettingIndex(new WireQueryParameters() { Query = q });

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task Details()
        {
            // Given: There are 5 items in the database, one of which we care about
            var expected = FakeObjects<T>.Make(5).SaveTo(this).Last();
            var id = expected.ID;

            // When: Getting details for the chosen item
            var document = await WhenGetAsync($"{urlroot}/Details/{id}");

            // Then: That item is shown
            var testkey = TestKey<T>.Find().Name.ToLowerInvariant();
            var actual = document.QuerySelector($"[data-test-id={testkey}]").TextContent.Trim();
            Assert.AreEqual(TestKey<T>.Order()(expected), actual);
        }

        [TestMethod]
        public async Task Edit()
        {
            // Given: There are 5 items in the database, one of which we care about
            // Given: There are 5 items in the database, one of which we care about, plus an additional item to be use as edit values
            var data = FakeObjects<T>.Make(4).SaveTo(this).Add(1);
            var id = data.Group(0).Last().ID;
            var newvalues = data.Group(1).Single();

            // When: Editing the chosen item
            var formData = new Dictionary<string, string>(FormDataFromObject(newvalues))
            {
                { "ID", id.ToString() },
            };

            var response = await WhenGettingAndPostingForm($"{urlroot}/Edit/{id}", FormAction, formData);

            // Then: Redirected to index
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: The item was changed
            var actual = context.Set<T>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(newvalues, actual);
        }

        [TestMethod]
        public async Task Delete()
        {
            // Given: There are two items in the database, one of which we care about
            var id = FakeObjects<T>.Make(2).SaveTo(this).Last().ID;

            // When: Deleting the selected item
            var formData = new Dictionary<string, string>()
            {
                { "ID", id.ToString() }
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Delete/{id}", FormAction, formData);
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: Redirected to index
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: Now is only one item in database
            Assert.AreEqual(1, context.Set<T>().Count());

            // And: The deleted item cannot be found;
            Assert.IsFalse(context.Set<T>().Any(x => x.ID == id));
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
            // Given: There is one item in the database, and another one waiting to be created
            var expected = FakeObjects<T>.Make(1).SaveTo(this).Add(1).Last();

            // When: Creating a new item
            var formData = new Dictionary<string, string>(FormDataFromObject(expected));
            var response = await WhenGettingAndPostingForm($"{urlroot}/Create", FormAction, formData);

            // Then: Redirected to index
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: Now are two items in database
            Assert.AreEqual(2, context.Set<T>().Count());

            // And: The last one is the one we just added
            var actual = context.Set<T>().OrderBy(x => x.ID).Last();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public virtual async Task Upload()
        {
            // Given: A spreadsheet of items
            var items = FakeObjects<T>.Make(15);

            var stream = GivenSpreadsheetOf(items);

            // When: Uploading it
            var document = await WhenUploadingSpreadsheet(stream, $"{urlroot}/", $"{urlroot}/Upload");

            // Then: The uploaded items are returned
            ThenResultsAreEqualByTestKey(document, items);

            // And: The database now contains the items
            items.SequenceEqual(context.Set<T>().OrderBy(TestKey<T>.Order()));
        }

        [TestMethod]
        public virtual async Task UploadDuplicate()
        {
            // Given: One item in the database
            // And: A spreadsheet containing 5 items, including a duplicate of the item in the database
            var data = FakeObjects<T>.Make(1).SaveTo(this).Add(4);
            var initial = data.Group(0);
            var items = data;
            var stream = GivenSpreadsheetOf(items);
            // Note that "GivenFakeItems" will restart at 1, so item Name 01 is the dupe

            // When: Uploading the spreadsheet
            var document = await WhenUploadingSpreadsheet(stream, $"{urlroot}/", $"{urlroot}/Upload");

            // Then: Only the non-duplicate items were returned
            ThenResultsAreEqualByTestKey(document, items.Skip(1));

            // And: The database now contains the items
            items.SequenceEqual(context.Set<T>().OrderBy(TestKey<T>.Order()));
        }

        [TestMethod]
        public virtual async Task UploadFailsNoFile()
        {
            // When: Calling upload with no files
            var response = await WhenUploadingEmpty($"{urlroot}/", $"{urlroot}/Upload");

            // Then: Bad Request
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public virtual async Task Download()
        {
            // Given: Many items in the database
            var items = FakeObjects<T>.Make(20).SaveTo(this);

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
