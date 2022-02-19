using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.AspNet.Data;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    public abstract class IntegrationTest: IFakeObjectsSaveTarget
    {
        #region Fields

        protected static IntegrationContext integrationcontext;
        protected static HtmlParser parser => integrationcontext.parser;
        protected static HttpClient client => integrationcontext.client;
        protected static ApplicationDbContext context => integrationcontext.context;

        protected readonly Func<IHtmlDocument, string> FormAction = d => d.QuerySelector("form").Attributes["action"].TextContent;

        #endregion

        #region Properties

        public TestContext TestContext { get; set; }

        #endregion

        #region Helpers

        #region Helpers

        public void AddRange(System.Collections.IEnumerable objects)
        {
            if (objects is IEnumerable<Payee> p)
                context.AddRange(p);
            if (objects is IEnumerable<Transaction> t)
                context.AddRange(t);
            if (objects is IEnumerable<BudgetTx> b)
                context.AddRange(b);

            context.SaveChanges();
        }

        #endregion

        protected virtual IEnumerable<TItem> GivenFakeItems<TItem>(int num, Func<TItem, TItem> func = null, int from = 1) where TItem : class, new()
        {
            return Enumerable
                .Range(from, num)
                .Select(x => GivenFakeItem<TItem>(x))
                .Select(func ?? (x => x));
        }

        protected virtual T GivenFakeItem<T>(int index) where T : class, new()
        {
            var result = new T();
            var properties = typeof(T).GetProperties();
            var chosen = properties.Where(x => x.CustomAttributes.Any(y => y.AttributeType == typeof(YoFi.Core.Models.Attributes.EditableAttribute)));

            foreach (var property in chosen)
            {
                var t = property.PropertyType;
                object o = default;

                if (t == typeof(string))
                    o = $"{property.Name} {index:D5}";
                else if (t == typeof(decimal))
                    o = index * 100m;
                else if (t == typeof(DateTime))
                    // Note that datetimes should descend, because anything which sorts by a datetime
                    // will typically sort descending
                    o = new DateTime(2001, 12, 31) - TimeSpan.FromDays(index);
                else
                    throw new NotImplementedException();

                property.SetValue(result, o);
            }

            return result;
        }

        protected async Task<(IEnumerable<T>, IEnumerable<T>)> GivenFakeDataInDatabase<T>(int total, int selected, Func<T, T> func = null) where T : class, new()
        {
            var all = GivenFakeItems<T>(total);
            var needed = all.Skip(total - selected).Take(selected).Select(func ?? (x => x));
            var items = all.Take(total - selected).Concat(needed).ToList();
            var wasneeded = items.Skip(total - selected).Take(selected);

            context.Set<T>().AddRange(items);
            await context.SaveChangesAsync();

            return (items, wasneeded);
        }

        protected async Task<IEnumerable<T>> GivenFakeDataInDatabase<T>(int total) where T : class, new()
        {
            (var result, _) = await GivenFakeDataInDatabase<T>(total, 0);
            return result;
        }

        protected Stream GivenSpreadsheetOf<T>(IEnumerable<T> items) where T : class
        {
            var stream = new MemoryStream();
            using (var ssw = new SpreadsheetWriter())
            {
                ssw.Open(stream);
                ssw.Serialize<T>(items);
            }
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        protected Dictionary<string, string> FormDataFromObject<T>(T item)
        {
            var result = new Dictionary<string, string>();

            var properties = typeof(T).GetProperties();
            var chosen = properties.Where(x => x.CustomAttributes.Any(y => y.AttributeType == typeof(YoFi.Core.Models.Attributes.EditableAttribute)));

            foreach (var property in chosen)
            {
                var t = property.PropertyType;
                object o = property.GetValue(item);
                string s = string.Empty;

                if (t == typeof(string))
                    s = (string)o;
                else if (t == typeof(decimal))
                    s = ((decimal)o).ToString();
                else if (t == typeof(DateTime))
                    s = ((DateTime)o).ToString("MM/dd/yyyy");
                else
                    throw new NotImplementedException();

                result[property.Name] = s;
            }

            return result;
        }

        protected async Task<IHtmlDocument> WhenGetAsync(string url)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());
            return document;
        }

        protected async Task<IHtmlDocument> WhenGetAsyncSession(IEnumerable<string> urls)
        {
            if (!urls.Any())
                throw new ArgumentException("URLs are required", nameof(urls));

            // Get the first one
            var response = await client.GetAsync(urls.First());
            response.EnsureSuccessStatusCode();

            foreach(var url in urls.Skip(1))
            {
                // Extract session cookie
                CookieHeaderValue sessioncookie = null;
                var setcookie = response
                    .Headers
                    .GetValues("Set-Cookie")
                    .FirstOrDefault(x => x.Contains("AspNetCore.Session"));

                if (setcookie is null)
                    throw new ApplicationException("No session cookie found");

                var setcookiehv = SetCookieHeaderValue.Parse(setcookie);
                sessioncookie = new CookieHeaderValue(setcookiehv.Name, setcookiehv.Value);

                // Get the next one
                response = await client.GetAsync(urls.First());

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Cookie", sessioncookie.ToString());
                response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();
            }

            var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());
            return document;
        }

        protected async Task<HttpResponseMessage> WhenGettingAndPostingForm(string url, Func<IHtmlDocument, string> selector, Dictionary<string, string> fields)
        {
            // First, we have to "get" the page
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

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

        protected async Task<HttpResponseMessage> WhenUploading(MultipartFormDataContent content, string fromurl, string tourl)
        {
            // First, we have to "get" the page we upload "from"
            var getresponse = await client.GetAsync(fromurl);

            // Pull out the antiforgery values
            var getdocument = await parser.ParseDocumentAsync(await getresponse.Content.ReadAsStreamAsync());
            var token = AntiForgeryTokenExtractor.ExtractAntiForgeryToken(getdocument);
            var cookie = AntiForgeryTokenExtractor.ExtractAntiForgeryCookieValueFrom(getresponse);

            content.Add(new StringContent(token.Value), token.Key);

            var postRequest = new HttpRequestMessage(HttpMethod.Post, tourl);
            postRequest.Headers.Add("Cookie", cookie.ToString());
            postRequest.Content = content;
            var response = await client.SendAsync(postRequest);

            return response;
        }

        protected async Task<IHtmlDocument> WhenUploadingFile(Stream stream, string name, string filename, string fromurl, string tourl)
        {
            var content = new MultipartFormDataContent
            {
                { new StreamContent(stream), name, filename }
            };

            var response = await WhenUploading(content, fromurl, tourl);

            response.EnsureSuccessStatusCode();
            var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());

            return document;
        }

        protected Task<IHtmlDocument> WhenUploadingSpreadsheet(Stream stream, string fromurl, string tourl)
        {
            return WhenUploadingFile(stream, "files", "Items.xlsx", fromurl, tourl);
        }

        protected async Task<HttpResponseMessage> WhenUploadingEmpty(string fromurl, string tourl)
        {
            var response = await WhenUploading(new MultipartFormDataContent(), fromurl, tourl);
            return response;
        }

        protected void ThenResultsAreEqual(IHtmlDocument document, IEnumerable<string> chosen, string selector, bool ordered = false)
        {
            // Then: The expected items are returned
            var results = document.QuerySelectorAll("table[data-test-id=results] tbody tr");
            var names = results.Select(x => x.QuerySelector(selector).TextContent.Trim());
            if (!ordered)
                names = names.OrderBy(x => x);
            Assert.IsTrue(chosen.SequenceEqual(names));
        }

        protected void ThenResultsAreEqualByTestKey<T>(IHtmlDocument document, IEnumerable<T> expected)
        {
            var property = FindTestKey<T>();
            var testid = $"[data-test-id={property.Name.ToLowerInvariant()}]";

            ThenResultsAreEqual(document, expected.Select(i => (string)property.GetValue(i)), testid);
        }

        protected void ThenResultsAreEqualByTestKeyOrdered<T>(IHtmlDocument document, IEnumerable<T> expected)
        {
            var property = FindTestKey<T>();
            var testid = $"[data-test-id={property.Name.ToLowerInvariant()}]";

            ThenResultsAreEqual(document, expected.Select(i => (string)property.GetValue(i)), testid, ordered:true);
        }

        protected async Task ThenIsSpreadsheetContaining<T>(HttpContent content, IEnumerable<T> items) where T: class, new()
        {
            // Then: It's a stream
            Assert.IsInstanceOfType(content, typeof(StreamContent));
            var streamcontent = content as StreamContent;

            // And: The stream contains a spreadsheet
            using var ssr = new SpreadsheetReader();
            ssr.Open(await streamcontent.ReadAsStreamAsync());

            // And: The spreadsheet contains all our items
            var actual = ssr.Deserialize<T>();
            var property = FindTestKey<T>();
            Assert.IsTrue(items.OrderBy(x => property.GetValue(x)).SequenceEqual(actual.OrderBy(x => property.GetValue(x))));
        }

        protected PropertyInfo FindTestKey<T>()
        {
            // Find the test key on the object
            var properties = typeof(T).GetProperties();
            var chosen = properties.Where(x => x.CustomAttributes.Any(y => y.AttributeType == typeof(Core.Models.Attributes.TestKeyAttribute)));
            if (!chosen.Any())
                throw new ApplicationException("Test Key not found");
            if (chosen.Skip(1).Any())
                throw new ApplicationException("More than one Test Key found");
            var property = chosen.Single();

            return property;
        }

        protected Func<T,object> TestKeyOrder<T>()
        {
            return x => FindTestKey<T>().GetValue(x);
        }

        protected async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
        {
            var apiresult = await JsonSerializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            return apiresult;
        }

        #endregion
    }
}
