using Common.DotNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
            var testkey = FindTestKey<Receipt>().Name.ToLowerInvariant();
            var actual = document.QuerySelector($"dd[data-test-id={testkey}]").TextContent.Trim();
            Assert.AreEqual(TestKeyOrder<Receipt>()(expected), actual);
        }

        [TestMethod]
        public async Task DetailsWithMatches()
        {
            // Given: There are 5 items in the database, one of which we care about, which matches ALL the transactions
            var amount = 12.34m;
            var txs = FakeObjects<Transaction>.Make(3,x=>x.Amount = amount).SaveTo(this);
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

        #endregion
    }
}
