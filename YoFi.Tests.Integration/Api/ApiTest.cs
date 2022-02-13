using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Api
{
    [TestClass]
    public class ApiTest: IntegrationTest
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

        [TestInitialize]
        public void SetUp()
        {
            var apikey = integrationcontext.apiconfig.Key;
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:{apikey}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean out database
            context.Set<Transaction>().RemoveRange(context.Set<Transaction>());
            context.SaveChanges();
        }

        #endregion

        #region Tests

        [TestMethod]
        public async Task GetId()
        {
            // Given: Many items in the database
            var items = await GivenFakeDataInDatabase<Transaction>(20);
            var expected = items.First();

            // When: Calling Get with '{id}'
            var response = await client.GetAsync($"/api/{expected.ID}");
            response.EnsureSuccessStatusCode();

            // Then: Requested item returned
            var apiresult = await JsonSerializer.DeserializeAsync<Transaction>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            Assert.AreEqual(expected, apiresult);
        }

        #endregion
    }
}
