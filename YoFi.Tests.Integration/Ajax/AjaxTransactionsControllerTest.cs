using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Ajax
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
            // Given: There are 5 items in the database, one of which we care about, plus an additional item to be use as edit values
            var data = FakeObjects<Transaction>.Make(4).SaveTo(this).Add(1);
            var id = data.Group(0).Last().ID;
            var newvalues = data.Group(1).Single();

            // And: When posting changed values to /Ajax/Payee/Edit/
            var formData = new Dictionary<string, string>(FormDataFromObject(newvalues))
            {
                { "ID", id.ToString() },
            };
            var response = await WhenGettingAndPostingForm("/Transactions/Index/", d => $"/ajax/tx/edit/{id}", formData);
            response.EnsureSuccessStatusCode();

            // Then: The result is what we expect (ApiItemResult in JSON with the item returned to us)
            // Note that AjaxEdit ONLY allows changes to Memo,Payee,Category, so that's all we can verify
            var apiresult = await JsonSerializer.DeserializeAsync<Transaction>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            Assert.AreEqual(newvalues.Memo, apiresult.Memo);
            Assert.AreEqual(newvalues.Category, apiresult.Category);
            Assert.AreEqual(newvalues.Payee, apiresult.Payee);

            // And: The item was changed
            var actual = context.Set<Transaction>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(newvalues.Memo, actual.Memo);
            Assert.AreEqual(newvalues.Category, actual.Category);
            Assert.AreEqual(newvalues.Payee, actual.Payee);
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

        [TestMethod]
        public async Task ApplyPayeeLoanMatch()
        {
            // Given: A set of loan details
            var inmonth = 133;
            var interest = -359.32m;
            var principal = -1328.39m;
            var payment = -1687.71m;
            var year = 2000 + (inmonth - 1) / 12;
            var month = 1 + (inmonth - 1) % 12;
            var principalcategory = "Mortgage Principal";
            var interestcategory = "Mortgage Interest";
            var rule = $"{principalcategory} [Loan] {{ \"interest\": \"{interestcategory}\", \"amount\": 200000, \"rate\": 6, \"term\": 180, \"origination\": \"1/1/2000\" }} ";
            var payeename = "Mortgage Lender";

            // Given: A test transaction in the database which is a payment for that loan
            var transaction = new Transaction() { Payee = payeename, Amount = payment, Timestamp = new DateTime(year, month, 1) };
            context.Set<Transaction>().Add(transaction);

            // And: A payee matching rule for that loan
            var payee = new Payee() { Name = payeename, Category = rule };
            context.Set<Payee>().Add(payee);
            context.SaveChanges();

            // When: Applying the payee to the transaction's ID
            var response = await WhenGettingAndPostingForm($"/Transactions/Index/", d => $"/ajax/tx/applypayee/{transaction.ID}", new Dictionary<string, string>());

            // Then: The request succeeds
            response.EnsureSuccessStatusCode();

            // And: The returned text is "SPLIT"
            var apiresult = await JsonSerializer.DeserializeAsync<string>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            Assert.AreEqual("SPLIT", apiresult);

            // And: The item now has 2 splits which match the expected loan details
            var actual = context.Set<Transaction>().Include(x=>x.Splits).Where(x => x.ID == transaction.ID).AsNoTracking().Single();
            Assert.IsNull(actual.Category);
            Assert.AreEqual(2, actual.Splits.Count);
            Assert.AreEqual(interest, actual.Splits.Where(x => x.Category == interestcategory).Single().Amount);
            Assert.AreEqual(principal, actual.Splits.Where(x => x.Category == principalcategory).Single().Amount);
        }

        [DataTestMethod]
        [DataRow("1234567 Bobby XN April 2021 5 wks")]
        [DataRow("1234567 Bobby MAR XN")]
        [DataRow("1234567 Jan XN ")]
        public async Task ApplyPayeeRegex_Pbi871(string name)
        {
            // Product Backlog Item 871: Match payee on regex, optionally

            // Given: A payee with a regex for its name
            var expectedpayee = new Payee() { Category = "Y", Name = "/1234567.*XN/" };
            context.Set<Payee>().Add(expectedpayee);

            // And: A transaction which should match it
            var expected = new Transaction() { Payee = name, Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m };
            context.Set<Transaction>().Add(expected);
            context.SaveChanges();

            // When: Applying the payee to the transaction's ID
            var response = await WhenGettingAndPostingForm($"/Transactions/Index/", d => $"/ajax/tx/applypayee/{expected.ID}", new Dictionary<string, string>());

            // Then: The request succeeds
            response.EnsureSuccessStatusCode();

            // Then: The result is the applied category
            var apiresult = await JsonSerializer.DeserializeAsync<string>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            Assert.AreEqual(expectedpayee.Category, apiresult);

            // And: The chosen transaction has the chosen payee's category
            var actual = context.Set<Transaction>().Where(x => x.ID == expected.ID).AsNoTracking().Single();
            Assert.AreEqual(expectedpayee.Category, actual.Category);
        }

        [TestMethod]
        public async Task ApplyPayeeNotFound()
        {
            // Given: Many payees
            _ = await GivenFakeDataInDatabase<Payee>(15);

            // Given: Five transactions, one of which has no category, and has "payee" matching NONE of the payees in the DB
            (_, var txchosen) = await GivenFakeDataInDatabase<Transaction>(5, 1, x => { x.Category = null; x.Payee = "notfound"; return x; });
            var id = txchosen.Single().ID;

            // When: Applying the payee to the transaction's ID
            var response = await WhenGettingAndPostingForm($"/Transactions/Index/", d => $"/ajax/tx/applypayee/{id}", new Dictionary<string, string>());

            // Then: 404
            Assert.AreEqual(HttpStatusCode.NotFound,response.StatusCode);
        }

        [TestMethod]
        public async Task CategoryAutocomplete()
        {
            // Given: Many recent transactions, some with {word} in their category, some not
            var word = "WORD";
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(15, 5, (x => { x.Category += word; x.Timestamp = DateTime.Now; return x; }));

            // When: Calling CategoryAutocomplete with '{word}'
            var response = await client.GetAsync($"/ajax/tx/cat-ac?q={word}");
            response.EnsureSuccessStatusCode();

            // Then: All of the categories from given items which contain '{word}' are returned
            var apiresult = await JsonSerializer.DeserializeAsync<List<string>>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            Assert.IsTrue(apiresult.OrderBy(x=>x).SequenceEqual(chosen.Select(x=>x.Category).OrderBy(x=>x)));
        }

        [TestMethod]
        public async Task UpReceipt()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 1);
            var id = chosen.Single().ID;

            // And: An image file
            var length = 25;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());

            // When: Uploading it as a receipt for this ID uprcpt
            var filename = "img.png";
            var content = new MultipartFormDataContent
            {
                { new StreamContent(stream), "file", filename }
            };
            var response = await WhenUploading(content, $"/Transactions/Index/", $"/ajax/tx/uprcpt/{id}");

            // Then: The request is successful
            response.EnsureSuccessStatusCode();

            // And: The receipt was uploaded to storage
            Assert.AreEqual(1, integrationcontext.storage.BlobItems.Count);
            Assert.AreEqual(id.ToString(), integrationcontext.storage.BlobItems.Single().FileName);

            // And: The database was updated with a receipt url
            var actual = context.Set<Transaction>().Where(x => x.ID == id).AsNoTracking().Single();
            Assert.AreEqual(id.ToString(), actual.ReceiptUrl);
        }

        [TestMethod]
        public async Task UpReceiptAgainFails()
        {
            // Given: There are 5 items in the database, one of which we care about
            (var items, var chosen) = await GivenFakeDataInDatabase<Transaction>(5, 1);
            var id = chosen.Single().ID;

            // And: An image file
            var length = 25;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, length).ToArray());

            // And: It has already been uploaded
            var filename = "img.png";
            var content = new MultipartFormDataContent
            {
                { new StreamContent(stream), "file", filename }
            };
            var response = await WhenUploading(content, $"/Transactions/Index/", $"/ajax/tx/uprcpt/{id}");
            response.EnsureSuccessStatusCode();

            // When: Uploading it again
            response = await WhenUploading(content, $"/Transactions/Index/", $"/ajax/tx/uprcpt/{id}");

            // Then: Bad request
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
        #endregion
    }
}
