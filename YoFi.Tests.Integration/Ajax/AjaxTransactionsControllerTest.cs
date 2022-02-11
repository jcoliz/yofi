using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

        [TestMethod]
        public async Task Edit()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 1);
            var id = chosen.Single().ID;

            // And: When posting changed values to /Ajax/Payee/Edit/
            var expected = GivenFakeItem<Transaction>(90);
            var formData = new Dictionary<string, string>(FormDataFromObject(expected))
            {
                { "ID", id.ToString() },
            };
            var response = await WhenGettingAndPostingForm("/Transactions/Index/", d => $"/ajax/tx/edit/{id}", formData);
            response.EnsureSuccessStatusCode();

            // Then: The result is what we expect (ApiItemResult in JSON with the item returned to us)
            // Note that AjaxEdit ONLY allows changes to Memo,Payee,Category, so that's all we can verify
            var apiresult = await JsonSerializer.DeserializeAsync<Transaction>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            Assert.AreEqual(expected.Memo, apiresult.Memo);
            Assert.AreEqual(expected.Category, apiresult.Category);
            Assert.AreEqual(expected.Payee, apiresult.Payee);

            // And: The item was changed
            var actual = context.Set<Transaction>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(expected.Memo, actual.Memo);
            Assert.AreEqual(expected.Category, actual.Category);
            Assert.AreEqual(expected.Payee, actual.Payee);
        }

        [TestMethod]
        public async Task ApplyPayee()
        {
            // Given : More than five payees, one of which matches the name of the transaction we care about
            (_, var payeeschosen) = await GivenFakeDataInDatabase<Payee>(15, 1);
            var payee = payeeschosen.Single();

            // Given: Five transactions, one of which has no category, and has "payee" matching name of chosen payee
            (_, var txchosen) = await GivenFakeDataInDatabase<Transaction>(5, 1, x => { x.Category = null; x.Payee = payee.Name; return x; });
            var id = txchosen.Single().ID;

            // When: Applying the payee to the transaction's ID
            var response = await WhenGettingAndPostingForm($"/Transactions/Index/", d => $"/ajax/tx/applypayee/{id}", new Dictionary<string, string>());
            response.EnsureSuccessStatusCode();

            // Then: The result is the applied category
            var apiresult = await JsonSerializer.DeserializeAsync<string>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            Assert.AreEqual(payee.Category, apiresult);

            // And: The chosen transaction has the chosen payee's category
            var actual = context.Set<Transaction>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(payee.Category, actual.Category);
        }

        #endregion
    }
}
