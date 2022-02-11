using Microsoft.EntityFrameworkCore;
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
    public class AjaxTransactionsControllerTest: IntegrationTest
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
            context.Set<Transaction>().RemoveRange(context.Set<Transaction>());
            context.SaveChanges();
        }

        #endregion

        #region Tests

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task Select(bool value)
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 1, (x => { x.Selected = !value; return x; }));
            var id = chosen.Single().ID;

            // When: Selecting the item via AJAX
            var formData = new Dictionary<string, string>()
            {
                { "value", value.ToString() },
            };
            var response = await WhenGettingAndPostingForm("/Transactions/Index/", d => $"/ajax/tx/select/{id}", formData);

            // Then: Item selection matches value
            var actual = context.Set<Transaction>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(value, actual.Selected);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task Hide(bool value)
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 1, (x => { x.Hidden = !value; return x; }));
            var id = chosen.Single().ID;

            // When: Selecting the item via AJAX
            var formData = new Dictionary<string, string>()
            {
                { "value", value.ToString() },
            };
            var response = await WhenGettingAndPostingForm("/Transactions/Index/", d => $"/ajax/tx/hide/{id}", formData);

            // Then: Item hidden matches value
            var actual = context.Set<Transaction>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(value, actual.Hidden);
        }

        #endregion

    }
}
