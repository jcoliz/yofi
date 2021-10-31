using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        string apikey;

        protected readonly string Site = "http://localhost:50419/api/";

        [TestInitialize]
        public void SetUp()
        {
            var config = new ConfigurationBuilder().AddUserSecrets(Assembly.GetAssembly(typeof(ApiKeyTest))).Build();
            apikey = config["Api:Key"];

            client = new HttpClient();

            var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:{apikey}"));
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {b64}");
        }

        [TestMethod]
        public async Task Get()
        {
            // When: Calling API with no parameters
            var response = await client.GetAsync(Site);

            // Then: Response is OK
            Assert.IsTrue(response.IsSuccessStatusCode);

            // And: Returns an empty response
            var stream = await response.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            var ok = root.GetProperty("Ok").GetBoolean();
            Assert.IsTrue(ok);
        }

        [TestMethod]
        public async Task GetTxi()
        {
            // When: Get transactions with a query parameter
            var q = "Tim";
            var response = await client.GetAsync(Site+$"txi/?q={q}");

            // Then: Response is OK
            Assert.IsTrue(response.IsSuccessStatusCode);

            // And: Returns 58 items
            var stream = await response.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            Assert.AreEqual(58, root.GetArrayLength());

            // And: All items contain the query parameter in the payee
            var payees = root.EnumerateArray().Select(x => x.GetProperty("Payee").GetString());
            Assert.IsTrue(payees.All(x => x.Contains(q)));
        }
    }
}
