using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace YoFi.AspNet.Tests.Functional
{
    [TestClass]
    public class ApiKeyTest
    {
        protected HttpClient client = null;

        protected TestConfigProperties Properties = null;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void SetUp() => SetUp(TestContext);

        public void SetUp(TestContext context)
        {
            var Properties = new TestConfigProperties(context.Properties);
            client = new HttpClient() { BaseAddress = new Uri(Properties.Url + "api/") };

            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:{Properties.ApiKey}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }

        [TestMethod]
        public async Task GetTxi()
        {
            // When: Get transactions with a query parameter, which will be found only in payees
            // Note: Be careful not to include a term that will also show up in categories, or 
            // will have to refactor this test
            var q = "Ralphs";
            var response = await WhenRequesting($"txi/?q={q}");

            // NOTE: This needs to be something that's stable across database resets. Which means
            // it needs to be something that's NOT a payee in a multi-payee sample data pattern.

            // Then: Returns 52 items
            Assert.AreEqual(52, response.GetArrayLength());

            // And: All items contain the query parameter in the payee
            var payees = response.EnumerateArray().Select(x => x.GetProperty("Payee").GetString());
            Assert.IsTrue(payees.All(x => x.Contains(q)));
        }

        [TestMethod]
        public async Task GetNotFound()
        {
            // When: Calling the API with a transaction ID that doesn't exist
            var response = await client.GetAsync("0");

            // Then: The result code is "404 not found"
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound,response.StatusCode);

            // And: The response body is empty
            Assert.AreEqual(0, response.Content.Headers.ContentLength);
        }

        [TestMethod]
        public async Task ReportNotFound()
        {
            // When: Calling the API with a transaction ID that doesn't exist
            var response = await client.GetAsync("ReportV2/notfound");

#if false
            var stream = await response.Content.ReadAsStreamAsync();
            using var sr = new StreamReader(stream);
            var body = await sr.ReadToEndAsync();
#endif
            
            // Then: The result code is "404 not found"
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);

            // And: The response body is empty
            Assert.AreEqual(0, response.Content.Headers.ContentLength);
        }

        [TestMethod]
        public async Task AuthFails()
        {
            // Given: A bad auth header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "Wrong");

            // When: Calling an API that otherwise works
            var q = "Ralphs";
            var response = await client.GetAsync($"txi/?q={q}");

            // Then: The response is 401 "Unauthorized"
            Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);

            // And: The body is a helpful error message containing the error code "E5"
            var stream = await response.Content.ReadAsStreamAsync();
            using var sr = new StreamReader(stream);
            var body = await sr.ReadToEndAsync();
            Assert.IsTrue(body.Contains("E5"));
        }

        public async Task<JsonElement> WhenRequesting(string url)
        {
            // When: Requesting {url} from server
            var response = await client.GetAsync(url);

            // Then: Response is OK
            Assert.IsTrue(response.IsSuccessStatusCode);

            // And: Returns a json document for further inspection
            var stream = await response.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            return root;
        }

        public async Task WhenPosting(string url, HttpContent content)
        {
            // When: Requesting {url} from server
            var response = await client.PostAsync(url,content);

            // Then: Response is OK
            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [DataRow("payee")]
        [DataTestMethod]
        public async Task ClearTestData(string what)
        {
            // When: Requesting to clear test data
            await WhenPosting("ClearTestData/" + what, null);

            // Then: Response is OK
        }
    }
}
