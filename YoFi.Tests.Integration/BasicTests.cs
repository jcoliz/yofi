using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    [TestClass]
    public class BasicTests: IntegrationTest
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
    }
}
