using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    [TestClass]
    public class AjaxPayeeControllerTest: IntegrationTest
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
            context.Payees.RemoveRange(context.Payees);
            context.SaveChanges();
        }

        #endregion

        #region Helpers

        private IEnumerable<Payee> GivenFakeItems(int num) =>
            Enumerable.Range(1, num).Select(x => new Payee() { Name = $"Name {x}", Category = $"Category:{x}" });

        private async Task<(IEnumerable<Payee>, IEnumerable<Payee>)> GivenFakeDataInDatabase(int total, int selected, Func<Payee, Payee> func = null)
        {
            var all = GivenFakeItems(total);
            var needed = all.Skip(total - selected).Take(selected).Select(func ?? (x => x));
            var items = all.Take(total - selected).Concat(needed).ToList();
            var wasneeded = items.Skip(total - selected).Take(selected);

            context.AddRange(items);
            await context.SaveChangesAsync();

            return (items, wasneeded);
        }

        private async Task<IEnumerable<Payee>> GivenFakeDataInDatabase(int total)
        {
            (var result, _) = await GivenFakeDataInDatabase(total, 0);
            return result;
        }

        protected async Task<IHtmlDocument> WhenGetAsync(string url)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());
            return document;
        }

        protected async Task<HttpResponseMessage> WhenGettingAndPostingForm(string url, Func<IHtmlDocument, string> selector, Dictionary<string, string> fields)
        {
            // First, we have to "get" the page
            var response = await client.GetAsync(url);

            // Pull out the antiforgery values
            var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());
            var token = AntiForgeryTokenExtractor.ExtractAntiForgeryToken(document);
            var cookie = AntiForgeryTokenExtractor.ExtractAntiForgeryCookieValueFrom(response);

            // Figure out the form action
            var action = selector(document);

            var formData = fields.Concat(new[] { token });
            var postRequest = new HttpRequestMessage(HttpMethod.Post, action);
            postRequest.Headers.Add("Cookie", cookie.ToString());
            postRequest.Content = new FormUrlEncodedContent(formData);
            var outresponse = await client.SendAsync(postRequest);

            return outresponse;
        }

        #endregion

        #region Tests

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task Select(bool value)
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase(5, 1, (x => { x.Selected = !value; return x; }));
            var id = chosen.Single().ID;

            // When: Selecting the item via AJAX
            var formData = new Dictionary<string, string>()
            {
                { "value", value.ToString() },
            };
            var response = await WhenGettingAndPostingForm("/Payees/Index/", d => $"/ajax/payee/select/{id}", formData);

            // Then: Item selection matches value
            var actual = context.Payees.Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(value,actual.Selected);
        }

        [TestMethod]
        public async Task Add()
        {
            // When: Adding a new item via AJAX
            var expected = new Payee() { Category = "B", Name = "3" };
            var formData = new Dictionary<string, string>()
            {
                { "Category", expected.Category },
                { "Name", expected.Name },
            };
            var response = await WhenGettingAndPostingForm("/Payees/Index/", d => $"/ajax/payee/add", formData);

            // Then: There is one payee in the database
            Assert.AreEqual(1, context.Payees.Count());

            // And: It matches what we sent in
            var actual = context.Payees.Single();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task Edit()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase(5, 1);
            var id = chosen.Single().ID;

            // And: When posting changed values to /Ajax/Payee/Edit/
            var expected = new Payee() { Category = "Edited Category", Name = "Edited Name" };
            var formData = new Dictionary<string, string>()
            {
                { "ID", id.ToString() },
                { "Name", expected.Name },
                { "Category", expected.Category },
            };
            var response = await WhenGettingAndPostingForm("/Payees/Index/", d => $"/ajax/payee/edit/{id}", formData);
            response.EnsureSuccessStatusCode();

            // Then: The result is what we expect (ApiItemResult in JSON with the item returned to us)
            var apiresult = await JsonSerializer.DeserializeAsync<Payee>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            Assert.AreEqual(expected, apiresult);

            // And: The item was changed
            var actual = context.Payees.Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(expected, actual);
        }

        #endregion
    }
}
