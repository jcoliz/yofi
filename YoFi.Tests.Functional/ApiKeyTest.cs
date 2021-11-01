﻿using Microsoft.Extensions.Configuration;
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

        protected readonly string Site = "http://localhost:50419/api/"; //50419

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
            var response = await WhenRequesting(Site);

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
            var q = "Tim";
            var response = await WhenRequesting(Site+$"txi/?q={q}");

            // Then: Returns 58 items
            Assert.AreEqual(58, response.GetArrayLength());

            // And: All items contain the query parameter in the payee
            var payees = response.EnumerateArray().Select(x => x.GetProperty("Payee").GetString());
            Assert.IsTrue(payees.All(x => x.Contains(q)));
        }

        [TestMethod]
        public async Task MaxId()
        {
            // When: Get max id for payees
            var response = await WhenRequesting(Site + $"maxid/payees");

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
            var response = await WhenPosting(Site + "ClearTestData/" + what, null);

            // Then: Returns an OK response
            var ok = response.GetProperty("Ok").GetBoolean();
            Assert.IsTrue(ok);
        }
    }
}