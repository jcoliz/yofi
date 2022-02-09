using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
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
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<Payee>(5, 1, (x => { x.Selected = !value; return x; }));
            var id = chosen.Single().ID;

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
            var expected = GivenFakeItems<Payee>(100).Last();
            var formData = new Dictionary<string, string>()
            {
                { "Category", expected.Category },
                { "Name", expected.Name },
            };
            var response = await WhenGettingAndPostingForm("/Payees/Index/", d => $"/ajax/payee/add", formData);

            // Then: There is one payee in the database
            Assert.AreEqual(1, context.Payees.Count());

            // And: It matches what we sent in
            var actual = context.Payees.Single();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task Edit()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<Payee>(5, 1);
            var id = chosen.Single().ID;

            // And: When posting changed values to /Ajax/Payee/Edit/
            var expected = GivenFakeItems<Payee>(90).Last();
            var formData = new Dictionary<string, string>()
            {
                { "ID", id.ToString() },
                { "Name", expected.Name },
                { "Category", expected.Category },
            };
            var response = await WhenGettingAndPostingForm("/Payees/Index/", d => $"/ajax/payee/edit/{id}", formData);
            response.EnsureSuccessStatusCode();

            // Then: The result is what we expect (ApiItemResult in JSON with the item returned to us)
            var apiresult = await JsonSerializer.DeserializeAsync<Payee>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            Assert.AreEqual(expected, apiresult);

            // And: The item was changed
            var actual = context.Payees.Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(expected, actual);
        }

        #endregion
    }
}
