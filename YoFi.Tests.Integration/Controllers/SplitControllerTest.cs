using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Controllers
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

        #region Tests

        [TestMethod]
        public async Task Edit()
        {
            // Given: There are 5 items in the database, one of which we care about
            // Given: There are 5 items in the database, one of which we care about, plus an additional item to be use as edit values
            var data = FakeObjects<Split>.Make(4).SaveTo(this).Add(1);
            var id = data.Group(0).Last().ID;
            var newvalues = data.Group(1).Single();

            // When: Editing the chosen item
            var formData = new Dictionary<string, string>(FormDataFromObject(newvalues))
            {
                { "ID", id.ToString() },
            };

            var response = await WhenGettingAndPostingForm($"{urlroot}/Edit/{id}", d => d.QuerySelector("form").Attributes["action"].TextContent, formData);
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: Redirected to "/Transactions/Edit"
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual("/Transactions/Edit/0", redirect);

            // And: The item was changed
            var actual = context.Set<Split>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(newvalues, actual);
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

            // And: The deleted item cannot be found
            Assert.IsFalse(context.Set<Split>().Any(x => x.ID == id));
        }

        #endregion
    }
}
