using AngleSharp.Html.Dom;
using Common.DotNet;
using jcoliz.FakeObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Controllers
{
    [TestClass]
    public class ReceiptControllerTest: IntegrationTest
    {
        #region Fields

        protected string urlroot => "/Receipts";
        private IDataContext iDC => integrationcontext.context as IDataContext;

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
            var rs = iDC.Get<Receipt>().ToList();
            foreach (var r in rs)
                iDC.Remove(r);
            context.Set<Transaction>().RemoveRange(context.Set<Transaction>());
            context.SaveChanges();

            // Clean out storage
            integrationcontext.storage.BlobItems.Clear();
        }

        #endregion

        #region Helpers
        private Receipt GivenReceiptInStorage(string filename, string contenttype)
        {

            var item = Receipt.FromFilename(filename, clock: new SystemClock());
            context.Add(item);
            context.SaveChanges();
            integrationcontext.storage.BlobItems.Add(new TestAzureStorage.BlobItem() { FileName = $"{ReceiptRepositoryInDb.Prefix}{item.ID}", InternalFile = "budget-white-60x.png", ContentType = contenttype });

            return item;
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
            ThenResultsAreEqualByTestKey(document, Enumerable.Empty<Receipt>());
        }


        [TestMethod]
        public async Task IndexMany()
        {
            // Given: Many items in the database
            var items = FakeObjects<Receipt>.Make(20).SaveTo(this);

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, items);
        }

        [TestMethod]
        public async Task IndexSingle()
        {
            // Given: There is one item in the database
            var items = FakeObjects<Receipt>.Make(1).SaveTo(this);

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, items);
        }

        [TestMethod]
        public async Task Details()
        {
            // Given: There are 5 items in the database, one of which we care about
            var expected = FakeObjects<Receipt>.Make(5).SaveTo(this).Last();
            var id = expected.ID;

            // When: Getting details for the chosen item
            var document = await WhenGetAsync($"{urlroot}/Details/{id}");

            // Then: That item is shown
            var testkey = TestKey<Receipt>.Find().Name.ToLowerInvariant();
            var actual = document.QuerySelector($"dd[data-test-id={testkey}]").TextContent.Trim();
            Assert.AreEqual(TestKey<Receipt>.Order()(expected), actual);
        }

        [TestMethod]
        public async Task DetailsWithMatches()
        {
            // Given: There are 5 items in the database, one of which we care about, which matches ALL the transactions
            var amount = 12.34m;
            var txs = FakeObjects<Transaction>.Make(3, x => x.Amount = amount).SaveTo(this);
            var t = txs.Last();
            var expected = FakeObjects<Receipt>.Make(4).Add(1,x=> { x.Amount = amount; x.Timestamp = t.Timestamp; }).SaveTo(this).Last();
            var id = expected.ID;

            // When: Getting details for the chosen item
            var document = await WhenGetAsync($"{urlroot}/Details/{id}");

            // Then: The transactions are shown
            ThenResultsAreEqualByTestKey(document, txs);
        }

        [TestMethod]
        public async Task Delete()
        {
            // Given: There are two items in the database, one of which we care about
            var expected = FakeObjects<Receipt>.Make(2).SaveTo(this).Last();
            var id = expected.ID;
            context.Entry(expected).State = EntityState.Detached;

            // When: Deleting the selected item
            var formData = new Dictionary<string, string>()
            {
                { "ID", id.ToString() }
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/",d=>$"{urlroot}/Delete/{id}", formData);
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: Redirected to index
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: Now is only one item in database
            Assert.AreEqual(1, iDC.Get<Receipt>().Count());

            // And: The deleted item cannot be found;
            Assert.IsFalse(iDC.Get<Receipt>().Any(x => x.ID == id));
        }

        [TestMethod]
        public async Task Upload()
        {
            // Given: An image file
            var length = 25;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());

            // When: Uploading it as a receipt
            var name = "Uptown Espresso";
            var amount = 12.24m;
            var filename = $"{name} ${amount}.png";
            var content = new MultipartFormDataContent
            {
                { new StreamContent(stream), "files", filename }
            };
            var response = await WhenUploading(content, $"{urlroot}/", $"{urlroot}/Upload");

            // Then: Redirected to index
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: Now is one item in database
            Assert.AreEqual(1, iDC.Get<Receipt>().Count());

            // And: Its properties match what we expect
            var actual = iDC.Get<Receipt>().Single();
            Assert.AreEqual(name, actual.Name);
            Assert.AreEqual(amount, actual.Amount);

            // And: The receipt was uploaded to storage with the expected name
            Assert.AreEqual(1, integrationcontext.storage.BlobItems.Count);
            Assert.AreEqual(ReceiptRepositoryInDb.Prefix + actual.ID.ToString(), integrationcontext.storage.BlobItems.Single().FileName);
        }

        [TestMethod]
        public async Task AcceptOne()
        {
            // Given: Several transactions, one of which we care about
            // Note: We have to override the timestamp on these to match the clock
            // that the system under test is using, else the transaction wont match the receipt
            // because the years will be off.
            var i = 0;
            var t = FakeObjects<Transaction>.Make(10,x=>x.Timestamp = DateTime.Now - TimeSpan.FromDays(i++)).SaveTo(this).Last();

            // And: One receipt in storage, which will match the transaction we care about
            var filename = $"{t.Payee} ${t.Amount} {t.Timestamp.Month}-{t.Timestamp.Day}.png";
            var contenttype = "image/png";
            var r = GivenReceiptInStorage(filename,contenttype);

            // When: Assigning the receipt to its best match
            var formData = new Dictionary<string, string>()
            {
                { "ID", r.ID.ToString() },
                { "txid", t.ID.ToString() }
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/", d => $"{urlroot}/Accept", formData);

            // Then: Redirected to index
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // Then: The selected transaction has a receipt now, with the expected name
            var expected = $"{ReceiptRepositoryInDb.Prefix}{r.ID}";
            var actual = await context.Set<Transaction>().Where(x => x.ID == t.ID).AsNoTracking().SingleAsync();
            Assert.AreEqual(expected, actual.ReceiptUrl);

            // And: There are no more (unassigned) receipts now
            Assert.IsFalse(iDC.Get<Receipt>().Any());
        }

        [TestMethod]
        public async Task AcceptPick()
        {
            // Given: Several transactions, one of which we care about
            // Note: We have to override the timestamp on these to match the clock
            // that the system under test is using, else the transaction wont match the receipt
            // because the years will be off.
            var i = 0;
            var t = FakeObjects<Transaction>.Make(10,x=>x.Timestamp = DateTime.Now - TimeSpan.FromDays(i++)).SaveTo(this).Last();

            // And: One receipt in storage, which will match the transaction we care about
            var filename = $"{t.Payee} ${t.Amount} {t.Timestamp.Month}-{t.Timestamp.Day}.png";
            var contenttype = "image/png";
            var r = GivenReceiptInStorage(filename,contenttype);

            // When: Assigning the receipt to its best match
            // And: Asking for the redirect (next) to edit transaction
            var formData = new Dictionary<string, string>()
            {
                { "ID", r.ID.ToString() },
                { "txid", t.ID.ToString() },
                { "next", "edittx" }
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/", d => $"{urlroot}/Accept", formData);

            // Then: Redirected to edit transaction
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"/Transactions/Edit/{t.ID}", redirect);

            // Then: The selected transaction has a receipt now, with the expected name
            var expected = $"{ReceiptRepositoryInDb.Prefix}{r.ID}";
            var actual = await context.Set<Transaction>().Where(x => x.ID == t.ID).AsNoTracking().SingleAsync();
            Assert.AreEqual(expected, actual.ReceiptUrl);

            // And: There are no more (unassigned) receipts now
            Assert.IsFalse(iDC.Get<Receipt>().Any());
        }

        [TestMethod]
        public async Task Bug1348()
        {
            //
            // Bug 1348: [Production Bug] Receipt shows matches on index but not on details
            //

            /*
                Create transaction exactly 15 days prior to the current date
                Create two receipts, both which match the transaction by name, and not by amount. One receipt is on the current date. The other receipt is a week prior.
                View the receipts on the receipt index.
                Note that both receipts show matches > 0, so "create" is not shown, but "review" is shown.
                Expected: Current transaction should have 0 matches, and create is shown
                Click review on the current-date transaction
                Note that the details page shows no matches
                Expected: This is correct
            */

            // Given: Transaction exactly 15 days prior
            var today = DateTime.Now.Date;
            var prior = today - TimeSpan.FromDays(15);
            var txs = FakeObjects<Transaction>.Make(1, x => x.Timestamp = prior).SaveTo(this);
            var tx = txs.Single();

            // And: One receipt matches by name, not amount, on today's date
            // And: One receipt matches by name, not amount, on an earlier date
            var r = FakeObjects<Receipt>
                        .Make(1, x => { x.Timestamp = today; x.Amount = tx.Amount * 2; x.Name = tx.Payee; })
                        .Add(1, x => { x.Timestamp = today - TimeSpan.FromDays(7); x.Amount = tx.Amount * 2; x.Name = tx.Payee; })
                        .SaveTo(this)
                        .First();

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: First item shows no matches
            var matches_el = document.QuerySelectorAll("tbody tr td[data-test-id=Matches]");
            var matches_str = matches_el.First().GetAttribute("data-test-value");
            var nmatches = int.Parse(matches_str);
            Assert.AreEqual(0, nmatches);

            // When: Getting details for the first receipt
            document = await WhenGetAsync($"{urlroot}/Details/{r.ID}");

            // Then: Item shows no matches
            ThenResultsAreEqualByTestKey(document, Enumerable.Empty<Transaction>());
        }

        [TestMethod]
        public async Task Bug1351()
        {
            //
            // Bug 1351: Should be able to create a new transaction for a receipt which matches another
            //

            /*
                Given: A transaction
                And: Two receipts, both which match the transaction, but one of them matches better than the other
                When: Getting the receipts index
                Then: Both receipts have a "create" button 
            */

            // Given: A transaction
            var t = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: Two receipts, both which match the transaction, but one of them matches better than the other
            var rs = FakeObjects<Receipt>
                .Make(1, x => { x.Name = t.Payee; x.Amount = t.Amount; x.Timestamp = t.Timestamp; })
                .Add(1, x => { x.Name = t.Payee; x.Amount = t.Amount * 2; x.Timestamp = t.Timestamp; })
                .SaveTo(this);

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: Both receipts have a "create" button 
            var elements = document.QuerySelectorAll("table[data-test-id=results] tbody td[data-test-id=Matches]");
            foreach(var element in elements)
            {
                Assert.AreEqual(1,element.QuerySelectorAll("a").Where(x=>(x as IHtmlAnchorElement).Href.Contains("Transactions/Create")).Count());
            }
        }

        [TestMethod]
        public async Task PickAll()
        {
            // Given: A transaction in the database
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: Many receipts in the database all of which match that transaction
            var rs = FakeObjects<Receipt>
                .Make(5, x => { x.Name = tx.Payee; x.Timestamp = tx.Timestamp; })
                .SaveTo(this);

            // When: Getting the receipt picker for a the transaction
            var document = await WhenGetAsync($"{urlroot}/Pick?txid={tx.ID}");

            // Then: All receipts are included in the results
            ThenResultsAreEqualByTestKey(document, rs);
        }

        [TestMethod]
        public async Task PickSome()
        {
            // Given: A transaction in the database
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: Many receipts in the database some of which match that transaction,
            // but others which do not match
            var rs = FakeObjects<Receipt>
                .Make(5, x => { x.Name = tx.Payee; x.Timestamp = tx.Timestamp; })
                .Add(7, x => { x.Name = "No Match"; x.Amount = tx.Amount * 10; })
                .SaveTo(this);

            // When: Getting the receipt picker for a the transaction
            var document = await WhenGetAsync($"{urlroot}/Pick?txid={tx.ID}");

            // Then: All receipts are included in the results
            ThenResultsAreEqualByTestKey(document, rs);
        }

        [TestMethod]
        public async Task PickNone()
        {
            // Given: A transaction in the database
            var tx = FakeObjects<Transaction>.Make(1).SaveTo(this).Single();

            // And: Many receipts in the database none of which match that transaction
            var rs = FakeObjects<Receipt>
                .Make(5, x => { x.Name = "No Match"; x.Amount = 0; x.Timestamp += TimeSpan.FromDays(100); })
                .SaveTo(this);

            // When: Getting the receipt picker for a the transaction
            var document = await WhenGetAsync($"{urlroot}/Pick?txid={tx.ID}");

            // Then: All receipts are included in the results
            ThenResultsAreEqualByTestKey(document, rs);
        }



        #endregion
    }
}
