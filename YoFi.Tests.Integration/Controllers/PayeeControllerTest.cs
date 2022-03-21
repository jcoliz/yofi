using jcoliz.FakeObjects;
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

namespace YoFi.Tests.Integration.Controllers
{
    [TestClass]
    public class PayeeControllerTest : ControllerTest<Payee>
    {
        protected override string urlroot => "/Payees";

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
            context.Set<Transaction>().RemoveRange(context.Set<Transaction>());
            context.SaveChanges();
        }

        #endregion

        #region Tests

        [TestMethod]
        public async Task EditModal()
        {
            // Given: There are 5 items in the database, one of which we care about
            var expected = FakeObjects<Payee>.Make(5).SaveTo(this).Last();
            var id = expected.ID;

            // When: Asking for the modal edit partial view
            var document = await WhenGetAsync($"{urlroot}/EditModal/{id}");

            // Then: That item is shown
            var testkey = TestKey<Payee>.Find().Name;
            var actual = document.QuerySelector($"input[name={testkey}]").GetAttribute("value").Trim();
            Assert.AreEqual(TestKey<Payee>.Order()(expected), actual);
        }

        [TestMethod]
        public async Task BulkEdit()
        {
            // Given: 10 items in the database, 7 of which are marked "selected"
            var data = FakeObjects<Payee>.Make(3).Add(7, x => x.Selected = true).SaveTo(this);
            var ids = data.Group(1).Select(x => x.ID);

            // When: Calling BulkEdit with a new category
            var category = "Edited Category";
            var formData = new Dictionary<string, string>()
            {
                { "Category", category },
            };
            var response = await WhenGettingAndPostingForm($"{urlroot}/Index/", d => $"{urlroot}/BulkEdit", formData);

            // Then: Redirected to index
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: All of the edited items have the new category
            var edited = context.Set<Payee>().Where(x => ids.Contains(x.ID)).AsNoTracking().ToList();
            Assert.IsTrue(edited.All(x=>x.Category == category));

            // And: None of the un-edited items have the new category
            var unedited = context.Set<Payee>().Where(x => !ids.Contains(x.ID)).AsNoTracking().OrderBy(TestKey<Payee>.Order()).ToList();
            Assert.IsTrue(data.Group(0).SequenceEqual(unedited));
        }

        [DataRow("Create/?txid")]
        [DataRow("CreateModal/?id")]
        [DataTestMethod]
        public async Task CreateFromTx(string endpoint)
        {
            // Given: There are 5 transactions in the database, one of which we care about
            var expected = FakeObjects<Transaction>.Make(5).SaveTo(this).Last();

            // When: Asking for the "create" page or modal given the chosen ID
            var document = await WhenGetAsync($"{urlroot}/{endpoint}={expected.ID}");

            // Then: The page is filled with the correct name and category
            var category = document.QuerySelector($"input[name=Category]").GetAttribute("value").Trim();
            Assert.AreEqual(expected.Category, category);
            var name = document.QuerySelector($"input[name=Name]").GetAttribute("value").Trim();
            Assert.AreEqual(expected.Payee, name);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task IndexShowSelected(bool isselected)
        {
            // Given: Many items in the database
            var items = FakeObjects<Payee>.Make(15).SaveTo(this);

            // When: Getting the index with or without the "selected" view
            var searchterm = isselected ? "?v=S" : string.Empty;
            var document = await WhenGetAsync($"{urlroot}/{searchterm}");

            // Then: The selection checkbox is available
            var selectionshown = ! (document.QuerySelector($"th[data-test-id=select]") is null);
            Assert.AreEqual(isselected, selectionshown);
        }

        [TestMethod]
        public async Task BulkDelete()
        {
            // Given: 10 items in the database, 7 of which are marked "selected"
            var data = FakeObjects<Payee>.Make(3).Add(7, x => x.Selected = true).SaveTo(this);

            // When: Calling BulkDelete
            var response = await WhenGettingAndPostingForm($"{urlroot}/Index/", d => $"{urlroot}/BulkDelete", new Dictionary<string, string>());

            // Then: Redirected to index
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            var redirect = response.Headers.GetValues("Location").Single();
            Assert.AreEqual($"{urlroot}", redirect);

            // And: Only the unselected items remain
            var actual = context.Set<Payee>().AsQueryable().OrderBy(TestKey<Payee>.Order()).ToList();
            Assert.IsTrue(actual.SequenceEqual(data.Group(0)));
        }

        #endregion
    }
}
