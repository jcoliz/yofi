using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YoFi.AspNet.Data;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    public abstract class IntegrationTest
    {
        #region Fields

        protected static IntegrationContext integrationcontext;
        protected static HtmlParser parser => integrationcontext.parser;
        protected static HttpClient client => integrationcontext.client;
        protected static ApplicationDbContext context => integrationcontext.context;

        #endregion

        #region Properties

        public TestContext TestContext { get; set; }

        #endregion

        #region Helpers

        protected virtual IEnumerable<T> GivenFakeItems<T>(int num) => throw new NotImplementedException();

        protected async Task<(IEnumerable<T>, IEnumerable<T>)> GivenFakeDataInDatabase<T>(int total, int selected, Func<T, T> func = null) where T : class
        {
            var all = GivenFakeItems<T>(total);
            var needed = all.Skip(total - selected).Take(selected).Select(func ?? (x => x));
            var items = all.Take(total - selected).Concat(needed).ToList();
            var wasneeded = items.Skip(total - selected).Take(selected);

            context.Set<T>().AddRange(items);
            await context.SaveChangesAsync();

            return (items, wasneeded);
        }

        protected async Task<IEnumerable<T>> GivenFakeDataInDatabase<T>(int total) where T : class
        {
            (var result, _) = await GivenFakeDataInDatabase<T>(total, 0);
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
    }
}
