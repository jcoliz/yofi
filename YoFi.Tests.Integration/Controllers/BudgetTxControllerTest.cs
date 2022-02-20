using AngleSharp.Html.Parser;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Controllers
{
    [TestClass]
    public class BudgetTxControllerTest: ControllerTest<BudgetTx>
    {
        #region Fields

        protected override string urlroot => "/BudgetTxs";

        #endregion

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

        #region Tests

        [TestMethod]
        public async Task UploadAlmostDuplicate()
        {
            // This tests the difference between Equals() and ImportEquals(). We will pass in
            // duplicate items which will pass ImportEquals but fail Equals().

            // The duplicates should NOT be imported.

            // Given: 5 items in the database
            var initial = FakeObjects<BudgetTx>.Make(5).SaveTo(this);

            // When: Upload 8 items, the first 5 of which are slight variations of the initial
            // items in the db
            var items = FakeObjects<BudgetTx>.Make(8).Group(0);
            foreach (var item in items)
                item.Timestamp += TimeSpan.FromDays(1);
            var stream = GivenSpreadsheetOf(items);
            var document = await WhenUploadingSpreadsheet(stream, $"{urlroot}/", $"{urlroot}/Upload");

            // Then: Only the non-duplicate items were returned, which are the last 3
            ThenResultsAreEqualByTestKey(document, items.Skip(5));
        }

        [TestMethod]
        public async Task UploadSmallAmountDiff_Bug890()
        {
            // Bug 890: BudgetTxs upload fails to filter duplicates when source data has >2 digits
            // Hah, this is fixed by getting UploadMinmallyDuplicate() test to properly pass.

            // Given: 5 items in the database
            var initial = FakeObjects<BudgetTx>.Make(5).SaveTo(this);

            // When: Uploading an item which differs in only a small amount from an otherwise
            // overlapping item
            var items = FakeObjects<BudgetTx>.Make(1).Group(0);
            items[0].Amount += 0.001m;
            var stream = GivenSpreadsheetOf(items);
            var document = await WhenUploadingSpreadsheet(stream, $"{urlroot}/", $"{urlroot}/Upload");

            // Then: No items were accepted
            ThenResultsAreEqualByTestKey(document, Enumerable.Empty<BudgetTx>());

            // And: The db still has 5 items
            Assert.AreEqual(5, context.Set<BudgetTx>().Count());
        }

        [TestMethod]
        public async Task BulkDelete()
        {
            // Given: 10 items in the database, 7 of which are marked "selected"
            (var items, var selected) = await GivenFakeDataInDatabase<BudgetTx>(10, 7, x => { x.Selected = true; return x; });

            // When: Calling BulkDelete
            var response = await WhenGettingAndPostingForm($"{urlroot}/Index/", d => $"{urlroot}/BulkDelete", new Dictionary<string, string>());

            // Then: Redirected to index
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: Only the unselected items remain
            var actual = context.Set<BudgetTx>().AsQueryable().OrderBy(TestKeyOrder<BudgetTx>()).ToList();
            Assert.IsTrue(actual.SequenceEqual(items.Except(selected)));
        }

        #endregion
    }
}
