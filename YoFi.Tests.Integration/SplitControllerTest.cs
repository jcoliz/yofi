using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    [TestClass]
    public class SplitControllerTest: IntegrationTest
    {
        #region Fields

        protected const string urlroot = "/Splits";

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
            context.Set<Split>().RemoveRange(context.Set<Split>());
            context.SaveChanges();
        }

        #endregion

        #region Helpers

        protected override IEnumerable<T> GivenFakeItems<T>(int num) =>
            Enumerable.Range(1, num).Select(x => new Split() { Amount = x * 100m, Memo = $"Memo {x}", Category = $"Category:{x}" }) as IEnumerable<T>;

        #endregion

        #region Tests

        [TestMethod]
        public async Task Edit()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<Split>(5, 1);
            var id = chosen.Single().ID;

            // When: Editing the chosen item
            var expected = GivenFakeItems<Split>(100).Last();
            var formData = new Dictionary<string, string>()
            {
                { "ID", id.ToString() },
                { "Amount", expected.Amount.ToString() },
                { "Category", expected.Category },
                { "Memo", expected.Memo }
            };

            var response = await WhenGettingAndPostingForm($"{urlroot}/Edit/{id}", d => d.QuerySelector("form").Attributes["action"].TextContent, formData);
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: Redirected to "/Transactions/Edit"
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual("/Transactions/Edit/0", redirect);

            // And: The item was changed
            var actual = context.Set<Split>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task Delete()
        {
            // Given: There are two items in the database, one of which we care about
            (var items, var selected) = await GivenFakeDataInDatabase<Split>(2, 1);
            var id = selected.Single().ID;

            // When: Deleting the selected item
            var formData = new Dictionary<string, string>()
            {
                { "ID", id.ToString() }
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Delete/{id}", d => d.QuerySelector("form").Attributes["action"].TextContent, formData);
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: Redirected to "/Transactions/Edit"
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual("/Transactions/Edit/0", redirect);

            // And: Now is only one item in database
            Assert.AreEqual(1, context.Set<Split>().Count());

            // And: The deleted item cannot be found;
            Assert.IsFalse(context.Set<Split>().Any(x => x.ID == id));
        }

        #endregion
    }
}
