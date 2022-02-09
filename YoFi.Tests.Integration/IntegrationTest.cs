using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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

        protected virtual IEnumerable<T> GivenFakeItems<T>(int num) where T: class, new()
        {
            return Enumerable.Range(1, num).Select(x => GivenFakeItem<T>(x));
        }

        protected virtual T GivenFakeItem<T>(int index) where T: class, new()
        {
            var result = new T();
            var properties = typeof(T).GetProperties();
            var chosen = properties.Where(x => x.CustomAttributes.Any(y => y.AttributeType == typeof(YoFi.Core.Models.Attributes.EditableAttribute)));

            foreach(var property in chosen)
            {
                var t = property.PropertyType;
                object o = default;

                if (t == typeof(string))
                    o = $"{property.Name} {index}";
                else if (t == typeof(decimal))
                    o = index * 100m;
                else if (t == typeof(DateTime))
                    o = new DateTime(2000, 1, 1) + TimeSpan.FromDays(index);
                else
                    throw new NotImplementedException();

                property.SetValue(result,o);
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

        protected Dictionary<string,string> FormDataFromObject<T>(T item)
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

        protected async Task<IHtmlDocument> WhenUploadingSpreadsheet(Stream stream, string fromurl, string tourl)
        {
            // First, we have to "get" the page we upload "from"
            var getresponse = await client.GetAsync(fromurl);

            // Pull out the antiforgery values
            var getdocument = await parser.ParseDocumentAsync(await getresponse.Content.ReadAsStreamAsync());
            var token = AntiForgeryTokenExtractor.ExtractAntiForgeryToken(getdocument);
            var cookie = AntiForgeryTokenExtractor.ExtractAntiForgeryCookieValueFrom(getresponse);

            var content = new MultipartFormDataContent
            {
                { new StreamContent(stream), "files", "Items.xlsx" },
                { new StringContent(token.Value), token.Key }
            };
            var postRequest = new HttpRequestMessage(HttpMethod.Post, tourl);
            postRequest.Headers.Add("Cookie", cookie.ToString());
            postRequest.Content = content;
            var response = await client.SendAsync(postRequest);

            response.EnsureSuccessStatusCode();
            var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());

            return document;
        }

        protected void ThenResultsAreEqual(IHtmlDocument document, IEnumerable<string> chosen, string selector)
        {
            // Then: The expected items are returned
            var results = document.QuerySelectorAll("table[data-test-id=results] tbody tr");
            var names = results.Select(x => x.QuerySelector(selector).TextContent.Trim());
            Assert.IsTrue(chosen.SequenceEqual(names));
        }

        protected void ThenResultsAreEqualByTestKey<T>(IHtmlDocument document, IEnumerable<T> expected)
        {
            var property = FindTestKey<T>();
            var testid = $"[data-test-id={property.Name.ToLowerInvariant()}]";

            ThenResultsAreEqual(document, expected.Select(i => (string)property.GetValue(i)).OrderBy(n => n), testid);
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

        private PropertyInfo FindTestKey<T>()
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


        #endregion
    }
}
