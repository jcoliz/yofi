using AngleSharp.Html.Dom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    [TestClass]
    public class BudgetTxControllerTest: IntegrationTest
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

        [TestCleanup]
        public void Cleanup()
        {
            // Clean out database
            context.Set<BudgetTx>().RemoveRange(context.Set<BudgetTx>());
            context.SaveChanges();
        }

        #endregion

        #region Helpers

        protected override IEnumerable<T> GivenFakeItems<T>(int num) =>
            Enumerable.Range(1, num).Select(x => new BudgetTx() { Timestamp = new DateTime(2000, 1, 1) + TimeSpan.FromDays(x), Amount = x * 100m, Memo = $"Memo {x}", Category = $"Category:{x}" }) as IEnumerable<T>;

        private void ThenResultsAreEqualByMemo(IHtmlDocument document, IEnumerable<BudgetTx> chosen)
        {
            ThenResultsAreEqual(document, chosen.Select(i => i.Memo).OrderBy(n => n), "[data-test-id=memo]");
        }
        #endregion

        #region Tests

        [TestMethod]
        public async Task IndexEmpty()
        {
            // Given: Empty database

            // When: Getting the index
            var document = await WhenGetAsync("/BudgetTxs/");

            // Then: No items are returned
            ThenResultsAreEqualByMemo(document, Enumerable.Empty<BudgetTx>());
        }

        [TestMethod]
        public async Task IndexMany()
        {
            // Given: Many items in the database
            var items = await GivenFakeDataInDatabase<BudgetTx>(20);

            // When: Getting the index
            var document = await WhenGetAsync("/BudgetTxs/");

            // Then: The expected items are returned
            ThenResultsAreEqualByMemo(document, items);
        }

        [TestMethod]
        public async Task IndexSingle()
        {
            // Given: There is one item in the database
            var items = await GivenFakeDataInDatabase<BudgetTx>(1);

            // When: Getting the index
            var document = await WhenGetAsync("/BudgetTxs/");

            // Then: The expected items are returned
            ThenResultsAreEqualByMemo(document, items);
        }


        #endregion
    }
}
