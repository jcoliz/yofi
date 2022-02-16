using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Threading.Tasks;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    [TestClass]
    public class SmokeTest: IntegrationTest
    {
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

        /// <summary>
        /// Just testing that the top-level pages load OK on direct load
        /// </summary>
        /// <param name="url"></param>
        [DataRow("/Identity/Account/Register")]
        [DataRow("/Home")]
        [DataRow("/Import")]
        [DataRow("/Reports")]
        [DataRow("/Budget")]
        [DataRow("/Help")]
        [DataTestMethod]
        public async Task GetOK(string url)
        {
            // When: Getting "/"
            var response = await client.GetAsync(url);

            // Then: It's OK
            response.EnsureSuccessStatusCode();
        }

        [TestMethod]
        public async Task NotFound()
        {
            // When: Getting something that won't be found
            var response = await client.GetAsync("/dsfglkjdfglkdgbu");

            // Then: Error 404
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

            // And: Status code information shown
            var document = await parser.ParseDocumentAsync(await response.Content.ReadAsStreamAsync());
            var h1 = document.QuerySelector("h1").TextContent.Trim();
            var h2 = document.QuerySelector("h2").TextContent.Trim();
            Assert.AreEqual("404", h1);
            Assert.AreEqual("Not Found", h2);
        }
    }
}
