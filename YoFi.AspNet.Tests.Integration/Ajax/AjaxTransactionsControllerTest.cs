using jcoliz.FakeObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.AspNet.Tests.Integration.Ajax
{
    [TestClass]
    public class AjaxTransactionsControllerTest: IntegrationTest
    {
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

        #region Tests

        [TestMethod]
        public async Task Edit()
        {
            // Given: There are 5 items in the database, one of which we care about, plus an additional item to be use as edit values
            var data = FakeObjects<Transaction>.Make(4).SaveTo(this).Add(1);
            var id = data.Group(0).Last().ID;
            var newvalues = data.Group(1).Single();

            // And: When posting changed values to /Ajax/Payee/Edit/
            var formData = new Dictionary<string, string>(FormDataFromObject(newvalues))
            {
                { "ID", id.ToString() },
            };
            var response = await WhenGettingAndPostingForm("/Transactions/Index/", d => $"/ajax/tx/edit/{id}", formData);
            response.EnsureSuccessStatusCode();

            // Then: The result is what we expect (ApiItemResult in JSON with the item returned to us)
            // Note that AjaxEdit ONLY allows changes to Memo,Payee,Category, so that's all we can verify
            var apiresult = await JsonSerializer.DeserializeAsync<Transaction>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            Assert.AreEqual(newvalues.Memo, apiresult.Memo);
            Assert.AreEqual(newvalues.Category, apiresult.Category);
            Assert.AreEqual(newvalues.Payee, apiresult.Payee);

            // And: The item was changed
            var actual = context.Set<Transaction>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(newvalues.Memo, actual.Memo);
            Assert.AreEqual(newvalues.Category, actual.Category);
            Assert.AreEqual(newvalues.Payee, actual.Payee);
        }

        [TestMethod]
        public async Task CategoryAutocomplete()
        {
            // Given: Many recent transactions, some with {word} in their category, some not
            var word = "WORD";
            var chosen = FakeObjects<Transaction>.Make(10).Add(5, x => { x.Category += word; x.Timestamp = DateTime.Now; }).SaveTo(this).Group(1);

            // When: Calling CategoryAutocomplete with '{word}'
            var response = await client.GetAsync($"/ajax/tx/cat-ac?q={word}");
            response.EnsureSuccessStatusCode();

            // Then: All of the categories from given items which contain '{word}' are returned
            var apiresult = await JsonSerializer.DeserializeAsync<List<string>>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            Assert.IsTrue(apiresult.OrderBy(x=>x).SequenceEqual(chosen.Select(x=>x.Category).OrderBy(x=>x)));
        }

        [TestMethod]
        public async Task UpReceipt()
        {
            // Given: There are 5 items in the database, one of which we care about
            var id = FakeObjects<Transaction>.Make(5).SaveTo(this).Last().ID;

            // And: An image file
            var length = 25;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());

            // When: Uploading it as a receipt for this ID uprcpt
            var filename = "img.png";
            var content = new MultipartFormDataContent
            {
                { new StreamContent(stream), "file", filename }
            };
            var response = await WhenUploading(content, $"/Transactions/Index/", $"/ajax/tx/uprcpt/{id}");

            // Then: The request is successful
            response.EnsureSuccessStatusCode();

            // And: The receipt was uploaded to storage
            Assert.AreEqual(1, integrationcontext.storage.BlobItems.Count);
            Assert.AreEqual(id.ToString(), integrationcontext.storage.BlobItems.Single().FileName);

            // And: The database was updated with a receipt url
            var actual = context.Set<Transaction>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(id.ToString(), actual.ReceiptUrl);
        }

        [TestMethod]
        public async Task UpReceiptAgainFails()
        {
            // Given: There are 5 items in the database, one of which we care about
            var id = FakeObjects<Transaction>.Make(5).SaveTo(this).Last().ID;

            // And: An image file
            var length = 25;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());

            // And: It has already been uploaded
            var filename = "img.png";
            var content = new MultipartFormDataContent
            {
                { new StreamContent(stream), "file", filename }
            };
            var response = await WhenUploading(content, $"/Transactions/Index/", $"/ajax/tx/uprcpt/{id}");
            response.EnsureSuccessStatusCode();

            // When: Uploading it again
            response = await WhenUploading(content, $"/Transactions/Index/", $"/ajax/tx/uprcpt/{id}");

            // Then: Bad request
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
        #endregion
    }
}
