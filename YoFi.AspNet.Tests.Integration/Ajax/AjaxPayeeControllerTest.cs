using jcoliz.FakeObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Ajax
{
    [TestClass]
    public class AjaxPayeeControllerTest: IntegrationTest
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
            context.Set<Payee>().RemoveRange(context.Set<Payee>());
            context.SaveChanges();
        }

        #endregion

        #region Tests

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task Select(bool value)
        {
            // Given: There are 5 items in the database, one of which we care about, which has the given selected value
            var id = FakeObjects<Payee>.Make(4).Add(1, (x => x.Selected = !value)).SaveTo(this).Last().ID;

            // When: Selecting the item via AJAX
            var formData = new Dictionary<string, string>()
            {
                { "value", value.ToString() },
            };
            var response = await WhenGettingAndPostingForm("/Payees/Index/", d => $"/ajax/payee/select/{id}", formData);

            // Then: Item selection matches value
            var actual = context.Payees.Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(value,actual.Selected);
        }

        [TestMethod]
        public async Task Add()
        {
            // When: Adding a new item via AJAX
            var expected = FakeObjects<Payee>.Make(1).Single();
            var formData = new Dictionary<string, string>(FormDataFromObject(expected));
            var response = await WhenGettingAndPostingForm("/Payees/Index/", d => $"/ajax/payee/add", formData);

            // Then: There is one payee in the database
            Assert.AreEqual(1, context.Payees.Count());

            // And: It matches what we sent in
            var actual = context.Set<Payee>().Single();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task Edit()
        {
            // Given: There are 5 items in the database, one of which we care about, plus an additional item to be use as edit values
            var data = FakeObjects<Payee>.Make(4).SaveTo(this).Add(1);
            var id = data.Group(0).Last().ID;
            var newvalues = data.Group(1).Single();

            // And: When posting changed values to /Ajax/Payee/Edit/
            var formData = new Dictionary<string, string>(FormDataFromObject(newvalues))
            {
                { "ID", id.ToString() },
            };
            var response = await WhenGettingAndPostingForm("/Payees/Index/", d => $"/ajax/payee/edit/{id}", formData);
            response.EnsureSuccessStatusCode();

            // Then: The result is what we expect (ApiItemResult in JSON with the item returned to us)
            var apiresult = await JsonSerializer.DeserializeAsync<Payee>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            Assert.AreEqual(newvalues, apiresult);

            // And: The item was changed
            var actual = context.Set<Payee>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(newvalues, actual);
        }

        #endregion
    }
}
