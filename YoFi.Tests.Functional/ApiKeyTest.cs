using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
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
            client = new HttpClient();

            var config = new ConfigurationBuilder().AddUserSecrets(Assembly.GetAssembly(typeof(ApiKeyTest))).Build();
            apikey = config["Api:Key"];
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
            var result = await System.Text.Json.JsonSerializer.DeserializeAsync<ApiResult>(stream);
            Assert.IsTrue(result.Ok);
        }

        public class ApiResult
        {
            public bool Ok { get; set; }

            public object Item { get; private set; }

            public string Error { get; private set; }
        }
    }
}
