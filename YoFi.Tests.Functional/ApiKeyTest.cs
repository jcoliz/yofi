using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    [TestClass]
    public class ApiKeyTest
    {
        HttpClient client;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void SetUp() => SetUp(TestContext);

        public void SetUp(TestContext context)
        {
            var site = context.Properties["webAppUrl"];
            client = new HttpClient() { BaseAddress = new Uri(site + "api/") };

            var config = new ConfigurationBuilder().AddUserSecrets(Assembly.GetAssembly(typeof(ApiKeyTest))).Build();
            var apikey = config["Api:Key"];
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:{apikey}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }

        [TestMethod]
        public async Task Get()
        {
            // When: Calling API with no parameters
            var response = await WhenRequesting(string.Empty);

            // Then: Returns an OK response
            var ok = response.GetProperty("Ok").GetBoolean();
            Assert.IsTrue(ok);
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
        public async Task MaxId()
        {
            // When: Get max id for payees
            var response = await WhenRequesting($"maxid/payees");

            // Then: Returns 58 items
            var maxid = response.GetProperty("Item").GetInt32();
            Assert.IsTrue(maxid >= 40);
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

        public async Task<JsonElement> WhenPosting(string url, HttpContent content)
        {
            // When: Requesting {url} from server
            var response = await client.PostAsync(url,content);

            // Then: Response is OK
            Assert.IsTrue(response.IsSuccessStatusCode);

            // And: Returns a json document for further inspection
            var stream = await response.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            return root;
        }

        [DataRow("payee")]
        [DataTestMethod]
        public async Task ClearTestData(string what)
        {
            var response = await WhenPosting("ClearTestData/" + what, null);

            // Then: Returns an OK response
            var ok = response.GetProperty("Ok").GetBoolean();
            Assert.IsTrue(ok);
        }
    }
}
