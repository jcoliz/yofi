using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Controllers
{
    [TestClass]
    public class ReceiptControllerTest: IntegrationTest
    {
        protected string urlroot => "/Receipts";

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

        [TestCleanup]
        public void Cleanup()
        {
            // Clean out database
            var rs = context.Set<Receipt>();
            foreach (var r in rs)
                context.Remove(r);
            context.SaveChanges();
        }

        #endregion

        #region Tests

        [TestMethod]
        public async Task IndexEmpty()
        {
            // Given: Empty database

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: No items are returned
            ThenResultsAreEqualByTestKey(document, Enumerable.Empty<Receipt>());
        }


        // Fails [TestMethod]
        public async Task IndexMany()
        {
            // Given: Many items in the database
            var items = FakeObjects<Receipt>.Make(20).SaveTo(this);

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, items);
        }

        // Fails [TestMethod]
        public async Task IndexSingle()
        {
            // Given: There is one item in the database
            var items = FakeObjects<Receipt>.Make(1).SaveTo(this);

            // When: Getting the index
            var document = await WhenGetAsync($"{urlroot}/");

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, items);
        }

        #endregion
    }
}
