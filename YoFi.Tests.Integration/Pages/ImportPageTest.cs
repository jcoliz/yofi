using AngleSharp.Html.Parser;
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

        [TestMethod]
        public async Task ImportOkSelected()
        {
            // Given: As set of items, all of which have imported flags, some of which have selected flags
            (var _, var imported) = await GivenFakeDataInDatabase<Transaction>(10, 10, (x => { x.Imported = true; x.Selected = (x.Amount % 200) == 0; return x; }));
            var selected = imported.Where(x => x.Selected == true).ToList();

            // When: Approving the import
            var formData = new Dictionary<string, string>()
            {
                { "command", "ok" }
            };
            var response = await WhenGettingAndPostingForm($"/Import/", FormAction, formData);
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: Only selected items remain
            var actual = context.Set<Transaction>().AsNoTracking().OrderBy(TestKeyOrder<Transaction>());
            Assert.IsTrue(selected.SequenceEqual(actual));
        }

        [TestMethod]
        public async Task ImportCancel()
        {
            // Given: A mix of transactions, some flagged as imported, some as not
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(10, 4, (x => { x.Imported = true; return x; }));
            var expected = items.Except(chosen).ToList();

            // When: Cancelling the import
            var formData = new Dictionary<string, string>()
            {
                { "command", "cancel" }
            };
            var response = await WhenGettingAndPostingForm($"/Import/", FormAction, formData);
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);

            // Then: Only items without imported flag remain
            var actual = context.Set<Transaction>().AsNoTracking().OrderBy(TestKeyOrder<Transaction>());
            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [DataRow(null)]
        [DataRow("Bogus")]
        [DataTestMethod]
        public async Task ImportWrong(string command)
        {
            // Given: A mix of transactions, some flagged as imported, some as not
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(10, 4, (x => { x.Imported = true; return x; }));
            var expected = items.Except(chosen).ToList();

            // When: Sending the import an incorrect command
            var formData = new Dictionary<string, string>()
            {
                { "command", command }
            };
            var response = await WhenGettingAndPostingForm($"/Import/", FormAction, formData);

            // Then: Bad request
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            // Then: Bad request

            // Then: No change to db
            var actual = context.Set<Transaction>().AsNoTracking().OrderBy(TestKeyOrder<Transaction>());
            Assert.AreEqual(10, actual.Count());
            Assert.AreEqual(4, actual.Where(x => x.Imported == true).Count());
        }

        [TestMethod]
        public async Task ImportOk_NotSelected_Bug839()
        {
            //
            // Bug 839: Imported items are selected automatically :(
            //

            // When: Uploading items and approving the import
            await ImportOk();

            // Then: No items remain selected
            Assert.IsFalse(context.Set<Transaction>().Any(x => x.Selected == true));
        }

        [TestMethod]
        public async Task UploadTwins_Bug883()
        {
            //
            // Bug 883: Apparantly duplicate transactions in import are (incorrectly) coalesced to single transaction for input
            //

            // Given: Two transactions, which are the same
            var item = GivenFakeItems<Transaction>(1);
            var items = item.Concat(item);

            // And: A spreadsheet of those items
            var stream = GivenSpreadsheetOf(items);

            // When: Uploading it
            var document = await WhenUploadingSpreadsheet(stream, $"/Import/", $"/Import?handler=Upload");

            // Then: There are two items in db
            var actual = context.Set<Transaction>().AsNoTracking().OrderBy(TestKeyOrder<Transaction>());
            Assert.IsTrue(items.SequenceEqual(actual));
        }

        [TestMethod]
        public async Task UploadMatchPayees()
        {
            // Given : More than five payees, one of which matches the name of the transaction we care about
            (_, var payeeschosen) = await GivenFakeDataInDatabase<Payee>(15, 1);
            var payee = payeeschosen.Single();

            // Given: Five transactions, all of which have no category, and have "payee" matching name of chosen payee
            var items = GivenFakeItems<Transaction>(5, x => { x.Category = null; x.Payee = payee.Name; return x; });

            // And: A spreadsheet of those items
            var stream = GivenSpreadsheetOf(items);

            // When: Uploading it
            var document = await WhenUploadingSpreadsheet(stream, $"/Import/", $"/Import?handler=Upload");

            // Then: All the items in the DB have category matching the payee
            Assert.IsTrue(context.Set<Transaction>().All(x => x.Category == payee.Category));
        }

        [TestMethod]
        public async Task UploadMatchPayees_RegexFirst_Bug880()
        {
            //
            // Bug 880: Import applies substring matches (incorrectly) before regex matches
            //

            // Given: Two payee matching rules, with differing payees, one with a regex one without (ergo it's a substring match)
            // And: A transaction which could match either
            var regexpayee = new Payee() { Category = "Y", Name = "/DOG.*123/" };
            var substrpayee = new Payee() { Category = "X", Name = "BIGDOG" };
            var tx = new Transaction() { Payee = "BIGDOG SAYS 1234", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m };

            context.Payees.Add(regexpayee);
            context.Payees.Add(substrpayee);
            await context.SaveChangesAsync();

            // And: A spreadsheet of those items
            var stream = GivenSpreadsheetOf(new List<Transaction>() { tx });

            // When: Uploading it
            var document = await WhenUploadingSpreadsheet(stream, $"/Import/", $"/Import?handler=Upload");

            // Then: The transaction will be mapped to the payee which specifies a regex
            // (Because the regex is a more precise specification of what we want.)
            var actual = context.Transactions.AsNoTracking().Single();
            Assert.AreEqual(regexpayee.Category, actual.Category);
        }

        #endregion

    }
}
