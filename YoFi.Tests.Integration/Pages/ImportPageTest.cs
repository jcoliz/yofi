using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Pages
{
    [TestClass]
    public class ImportPageTest: IntegrationTest
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
            context.Set<Transaction>().RemoveRange(context.Set<Transaction>());
            context.SaveChanges();
        }

        #endregion

        #region Tests

        [TestMethod]
        public async Task UploadTransactionsXlsx()
        {
            // Given: A spreadsheet of items
            var items = GivenFakeItems<Transaction>(15).OrderBy(TestKeyOrder<Transaction>());
            var stream = GivenSpreadsheetOf(items);

            // When: Uploading it
            var document = await WhenUploadingSpreadsheet(stream, $"/Import/", $"/Import?handler=Upload");

            // Then: The uploaded items are returned
            ThenResultsAreEqualByTestKey(document, items);

            // And: The database now contains the items
            items.SequenceEqual(context.Set<Transaction>().OrderBy(TestKeyOrder<Transaction>()));
        }

        [TestMethod]
        public async Task UploadError()
        {
            // When: Calling upload with no files
            var response = await WhenUploadingEmpty($"/Import/", $"/Import?handler=Upload");

            // Then: OK
            response.EnsureSuccessStatusCode();

            // And: Database is empty
            Assert.IsFalse(context.Set<Transaction>().Any());
        }

        [TestMethod]
        public async Task Import()
        {
            // Given: A mix of transactions, some flagged as imported, some as not
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(10, 4, (x => { x.Imported = true; return x; }));

            // When: Loading the import page
            var document = await WhenGetAsync($"/Import/");

            // Then: The expected items are returned
            ThenResultsAreEqualByTestKey(document, chosen);
        }

        [TestMethod]
        public async Task ImportOk()
        {
            // Given: As set of items, some with imported & selected flags, some with not
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(10, 4, (x => { x.Imported = x.Selected = true; return x; }));

            // When: Approving the import
            var formData = new Dictionary<string, string>()
            {
                { "command", "ok" }
            };
            var response = await WhenGettingAndPostingForm($"/Import/", FormAction, formData);
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: All items remain, none have imported flag
            Assert.AreEqual(10, context.Set<Transaction>().Count());
            Assert.AreEqual(0, context.Set<Transaction>().Where(x => x.Imported == true).Count());
        }

        #endregion

    }
}
