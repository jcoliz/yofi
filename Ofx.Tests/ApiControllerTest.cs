using Common.AspNetCore.Test;
using Common.Test.Mock;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfxWeb.Asp.Controllers;
using OfxWeb.Asp.Controllers.Helpers;
using OfxWeb.Asp.Data;
using OfxWeb.Asp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ofx.Tests
{
    [TestClass]
    public class ApiControllerTest
    {
        public ApiController controller { set; get; } = default(ApiController);

        public ApplicationDbContext context = null;

        public TestAzureStorage storage = null;

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);

            storage = new TestAzureStorage();

            controller = new ApiController(context,storage);

            // Need to inject the Auth header into the context.
            // https://stackoverflow.com/questions/41400030/mock-httpcontext-for-unit-testing-a-net-core-mvc-controller
            var http = new DefaultHttpContext();
            var userpass = "user:j+dF48FhiU+Dz83ZQYsoXw==";
            http.Request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(userpass)));
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = http;
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Didn't actually solve anything. Keep it around for possible future problem
            //DetachAllEntities();

            // https://stackoverflow.com/questions/33490696/how-can-i-reset-an-ef7-inmemory-provider-between-unit-tests
            context?.Database.EnsureDeleted();
            context = null;
            controller = default(ApiController);
        }

        async Task AddFiveTransactions()
        {            
            context.Transactions.Add(new Transaction() { Category = "BB", SubCategory = "AA", Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m });
            context.Transactions.Add(new Transaction() { Category = "AA", SubCategory = "AA", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m });
            context.Transactions.Add(new Transaction() { Category = "CC", SubCategory = "AA", Payee = "5", Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Amount = 300m });
            context.Transactions.Add(new Transaction() { Category = "BB", SubCategory = "AA", Payee = "1", Timestamp = new DateTime(DateTime.Now.Year, 01, 05), Amount = 400m });
            context.Transactions.Add(new Transaction() { Category = "BB", SubCategory = "BB", Payee = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m });
            
            await context.SaveChangesAsync();
        }
        async Task AddFiveTransactionsWithFourLevels(int year)
        {
            context.Transactions.Add(new Transaction() { SubCategory = "C:D", Category = "A:B", Payee = "3", Timestamp = new DateTime(year, 01, 03), Amount = 100m });
            context.Transactions.Add(new Transaction() { SubCategory = "C:D", Category = "A:B", Payee = "2", Timestamp = new DateTime(year, 01, 04), Amount = 100m });
            context.Transactions.Add(new Transaction() { SubCategory = "E:F", Category = "A:B", Payee = "5", Timestamp = new DateTime(year, 01, 01), Amount = 300m });
            context.Transactions.Add(new Transaction() { SubCategory = "G", Category = "E:F", Payee = "1", Timestamp = new DateTime(year, 01, 05), Amount = 400m });
            context.Transactions.Add(new Transaction() { SubCategory = "G:R", Category = "E:F", Payee = "4", Timestamp = new DateTime(year, 01, 03), Amount = 500m });

            await context.SaveChangesAsync();
        }

        async Task AddFivePayees()
        {
            context.Payees.Add(new Payee() { Category = "Y", SubCategory = "E", Name = "3" });
            context.Payees.Add(new Payee() { Category = "X", SubCategory = "E", Name = "2" });
            context.Payees.Add(new Payee() { Category = "Z", SubCategory = "E", Name = "5" });
            context.Payees.Add(new Payee() { Category = "X", SubCategory = "E", Name = "1" });
            context.Payees.Add(new Payee() { Category = "Y", SubCategory = "F", Name = "4" });

            await context.SaveChangesAsync();
        }

        async Task AddFiveBudgetTxs()
        {
            context.BudgetTxs.Add(new BudgetTx() { Timestamp = new System.DateTime(DateTime.Now.Year, 06, 01), Category = "BB:BB", Amount = 100m });
            context.BudgetTxs.Add(new BudgetTx() { Timestamp = new System.DateTime(DateTime.Now.Year, 06, 01), Category = "BB:AA", Amount = 200m });
            context.BudgetTxs.Add(new BudgetTx() { Timestamp = new System.DateTime(DateTime.Now.Year, 05, 01), Category = "CC:AA", Amount = 300m });
            context.BudgetTxs.Add(new BudgetTx() { Timestamp = new System.DateTime(DateTime.Now.Year, 05, 01), Category = "AA:AA", Amount = 400m });
            context.BudgetTxs.Add(new BudgetTx() { Timestamp = new System.DateTime(DateTime.Now.Year, 05, 01), Category = "AA:BB", Amount = 500m });

            await context.SaveChangesAsync();
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public void Get()
        {
            var json = controller.Get();
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsTrue(result.Ok);
        }
        [TestMethod]
        public async Task GetId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();

            var json = await controller.Get(expected.ID);
            var result = JsonSerializer.Deserialize<ApiTransactionResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(expected, result.Transaction);
        }
        [TestMethod]
        public async Task GetIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x=>x.ID);

            var json = await controller.Get(maxid + 1);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task HideId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();

            var json = await controller.Hide(expected.ID);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(true == expected.Hidden);
        }
        [TestMethod]
        public async Task HideIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var json = await controller.Hide(maxid + 1);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task ShowId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();
            expected.Hidden = true;
            await context.SaveChangesAsync();

            var json = await controller.Show(expected.ID);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(false == expected.Hidden);
        }
        [TestMethod]
        public async Task ShowIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var json = await controller.Show(maxid + 1);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task SelectId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();

            var json = await controller.Select(expected.ID);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(true == expected.Selected);
        }
        [TestMethod]
        public async Task SelectIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var json = await controller.Select(maxid + 1);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task DeselectId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();
            expected.Selected = true;
            await context.SaveChangesAsync();

            var json = await controller.Deselect(expected.ID);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(false == expected.Selected);
        }
        [TestMethod]
        public async Task DeselectIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var json = await controller.Deselect(maxid + 1);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task SelectPayeeId()
        {
            await AddFivePayees();
            var expected = await context.Payees.FirstAsync();

            var json = await controller.SelectPayee(expected.ID);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(true == expected.Selected);
        }
        [TestMethod]
        public async Task SelectPayeeIdFails()
        {
            await AddFivePayees();
            var maxid = await context.Payees.MaxAsync(x => x.ID);

            var json = await controller.SelectPayee(maxid + 1);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task DeselectPayeeId()
        {
            await AddFivePayees();
            var expected = await context.Payees.FirstAsync();
            expected.Selected = true;
            await context.SaveChangesAsync();

            var json = await controller.DeselectPayee(expected.ID);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(false == expected.Selected);
        }
        [TestMethod]
        public async Task DeselectPayeeIdFails()
        {
            await AddFivePayees();
            var maxid = await context.Payees.MaxAsync(x => x.ID);

            var json = await controller.DeselectPayee(maxid + 1);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }
        [TestMethod]
        public async Task AddPayee()
        {
            var expected = new Payee() { Category = "B", SubCategory = "A", Name = "3" };
            
            var json = await controller.AddPayee(expected);
            var result = JsonSerializer.Deserialize<ApiPayeeResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(expected, result.Payee);
        }
        [TestMethod]
        public async Task ApplyPayee()
        {
            await AddFivePayees();
            await AddFiveTransactions();

            // Pick an aribtrary transaction
            var tx = await context.Transactions.LastAsync();           

            var json = await controller.ApplyPayee(tx.ID);
            var result = JsonSerializer.Deserialize<ApiPayeeResult>(json);

            Assert.IsTrue(result.Ok);

            var expected = await context.Payees.Where(x => x.Name == tx.Payee).SingleAsync();

            Assert.AreEqual(expected, result.Payee);

            Assert.AreEqual(expected.Category, tx.Category);
            Assert.AreEqual(expected.SubCategory, tx.SubCategory);
        }
        [TestMethod]
        public async Task ApplyPayeeFailsNoTxId()
        {
            await AddFivePayees();
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var json = await controller.ApplyPayee(maxid + 1);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }

        [TestMethod]
        public async Task ApplyPayeeFailsNoPayee()
        {
            await AddFivePayees();
            await AddFiveTransactions();

            // Pick an aribtrary transaction
            var tx = await context.Transactions.LastAsync();

            // Now remove the matching payee
            var payee = await context.Payees.Where(x => x.Name == tx.Payee).SingleAsync();
            context.Payees.Remove(payee);
            await context.SaveChangesAsync();

            var json = await controller.ApplyPayee(tx.ID);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsFalse(result.Ok);
            Assert.IsNotNull(result.Exception);
        }

        [DataTestMethod]
        [DataRow("1234567 Bobby XN April 2021 5 wks")]
        [DataRow("1234567 Bobby MAR XN")]
        [DataRow("1234567 Jan XN ")]
        public async Task ApplyPayeeRegex_Pbi871(string name)
        {
            // Product Backlog Item 871: Match payee on regex, optionally

            context.Transactions.Add(new Transaction() { Payee = name, Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m });

            var expected = new Payee() { Category = "Y", SubCategory = "E", Name = "/1234567.*XN/" };
            context.Payees.Add(expected);

            await context.SaveChangesAsync();

            var tx = context.Transactions.First();

            var json = await controller.ApplyPayee(tx.ID);
            var result = JsonSerializer.Deserialize<ApiPayeeResult>(json);

            Assert.IsTrue(result.Ok);

            Assert.AreEqual(expected, result.Payee);

            Assert.AreEqual(expected.Category, tx.Category);
            Assert.AreEqual(expected.SubCategory, tx.SubCategory);
        }

        [TestMethod]
        public async Task Edit()
        {
            await AddFiveTransactions();
            var original = await context.Transactions.FirstAsync();

            // detach the original so we have an unmodified copy around
            context.Entry(original).State = EntityState.Detached;

            var newtx = new Transaction() { ID = original.ID, Payee = "I have edited you!", SubCategory = original.SubCategory, Timestamp = original.Timestamp, Amount = original.Amount };

            var json = await controller.Edit(original.ID, false, newtx);
            var result = JsonSerializer.Deserialize<ApiTransactionResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(newtx, result.Transaction);
            Assert.AreNotEqual(original, result.Transaction);

            var actual = await context.Transactions.Where(x => x.ID == original.ID).SingleAsync();
            Assert.AreEqual(newtx, actual);
            Assert.AreNotEqual(original, actual);
        }
        [TestMethod]
        public async Task EditDuplicate()
        {
            await AddFiveTransactions();
            var original = await context.Transactions.FirstAsync();

            // detach the original so we have an unmodified copy around
            context.Entry(original).State = EntityState.Detached;

            var newtx = new Transaction() { ID = original.ID, Payee = "I have edited you!", SubCategory = original.SubCategory, Timestamp = original.Timestamp, Amount = original.Amount };

            var json = await controller.Edit(original.ID, true, newtx);
            var result = JsonSerializer.Deserialize<ApiTransactionResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(newtx, result.Transaction);
            Assert.AreNotEqual(original, result.Transaction);

            var unmodified = await context.Transactions.Where(x => x.ID == original.ID).SingleAsync();
            Assert.AreEqual(original, unmodified);

            var modified = await context.Transactions.Where(x => x.Payee == newtx.Payee).SingleAsync();
            Assert.AreEqual(newtx, modified);
        }
        [TestMethod]
        public async Task EditPayee()
        {
            await AddFivePayees();
            var original = await context.Payees.FirstAsync();

            // detach the original so we have an unmodified copy around
            context.Entry(original).State = EntityState.Detached;

            var newitem = new Payee() { ID = original.ID, Name = "I have edited you!", SubCategory = original.SubCategory, Category = original.Category };

            var json = await controller.EditPayee(original.ID, false, newitem);
            var result = JsonSerializer.Deserialize<ApiPayeeResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(newitem, result.Payee);
            Assert.AreNotEqual(original, result.Payee);

            var actual = await context.Payees.Where(x => x.ID == original.ID).SingleAsync();
            Assert.AreEqual(newitem, actual);
            Assert.AreNotEqual(original, actual);
        }
        [TestMethod]
        public async Task EditPayeeDuplicate()
        {
            await AddFivePayees();
            var original = await context.Payees.FirstAsync();

            // detach the original so we have an unmodified copy around
            context.Entry(original).State = EntityState.Detached;

            var newitem = new Payee() { ID = original.ID, Name = "I have edited you!", SubCategory = original.SubCategory, Category = original.Category };

            var json = await controller.EditPayee(original.ID, true, newitem);
            var result = JsonSerializer.Deserialize<ApiPayeeResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(newitem, result.Payee);
            Assert.AreNotEqual(original, result.Payee);

            var unmodified = await context.Payees.Where(x => x.ID == original.ID).SingleAsync();
            Assert.AreEqual(original, unmodified);

            var modified = await context.Payees.Where(x => x.Name == newitem.Name).SingleAsync();
            Assert.AreEqual(newitem, modified);
        }
        [TestMethod]
        public async Task UpReceipt()
        {
            await AddFiveTransactions();
            var original = await context.Transactions.LastAsync();

            // Create a formfile with it
            var contenttype = "text/html";
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var stream = new MemoryStream(bytes);
            var file = new FormFile(stream, 0, 10, "Index", $"Index.html") { Headers = new HeaderDictionary(), ContentType = contenttype };
            
            var json = await controller.UpReceipt(original.ID,file);
            var result = JsonSerializer.Deserialize<ApiResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(contenttype, original.ReceiptUrl);

        }

        [TestMethod]
        public async Task UploadSplitsForTransaction()
        {
            // Don't add the splits here, we'll upload them
            var item = new Transaction() { Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m };

            context.Transactions.Add(item);
            context.SaveChanges();

            var splits = new List<Split>();
            splits.Add(new Split() { Amount = 25m, Category = "A", SubCategory = "B" });
            splits.Add(new Split() { Amount = 75m, Category = "C", SubCategory = "D" });

            // Make an HTML Form file containg an excel spreadsheet containing those splits
            var file = ControllerTestHelper<Split,SplitsController>.PrepareUpload(splits);

            // Upload that
            var json = await controller.UpSplits(item.ID, file);
            var result = JsonSerializer.Deserialize<ApiTransactionResult>(json);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(item.HasSplits);
            Assert.IsTrue(item.IsSplitsOK);
        }


        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task SplitsShownInReport(bool usesplits)
        {
            int year = DateTime.Now.Year;
            var expected_ab = 25m;
            var expected_cd = 75m;

            // Reason for running the SAME test with splits or transactions, is that the outcome
            // should be exactly the same.
            if (usesplits)
            {
                var splits = new List<Split>();
                splits.Add(new Split() { Amount = expected_ab, Category = "A:A", SubCategory = "B" });
                splits.Add(new Split() { Amount = expected_cd, Category = "C:C", SubCategory = "D" });

                var item = new Transaction() { Payee = "3", Timestamp = new DateTime(year, 01, 03), Amount = 100m, Splits = splits };

                context.Transactions.Add(item);
            }
            else
            {

                // This is how to create the list that this SHOULD look like
                var items = new List<Transaction>();
                items.Add(new Transaction() { Category = "A:A", SubCategory = "B", Payee = "3", Timestamp = new DateTime(year, 01, 03), Amount = expected_ab });
                items.Add(new Transaction() { Category = "C:C", SubCategory = "D", Payee = "2", Timestamp = new DateTime(year, 01, 04), Amount = expected_cd });
                context.Transactions.AddRange(items);
            }
            context.SaveChanges();

            var actionresult = await controller.Report("summary", year, null, null);
            var okresult = actionresult as OkObjectResult;
            var report = okresult.Value as ApiSummaryReportResult;

            Console.WriteLine(report);

            Assert.AreEqual(2, report.Lines.Count);

            var actual_AB = report.Lines.Where(x=>x.Keys == "A:A:B").Single().Amount;

            Assert.AreEqual(expected_ab, actual_AB);

            var actual_CD = report.Lines.Where(x => x.Keys == "C:C:D").Single().Amount;

            Assert.AreEqual(expected_cd, actual_CD);
        }

        [TestMethod]
        public async Task Key4ShowsInReport()
        {
            int year = DateTime.Now.Year;

            await AddFiveTransactionsWithFourLevels(year);
            await context.SaveChangesAsync();

            var actionresult = await controller.Report("summary", year, null, null);
            var okresult = actionresult as OkObjectResult;
            var report = okresult.Value as ApiSummaryReportResult;

            Console.WriteLine(report);

            // There are 4 unique cat/subcats
            Assert.AreEqual(4, report.Lines.Count);

            var efgr = report.Lines.Where(x => x.Keys.Contains("G:R")).Single();
            Assert.AreEqual(500m, efgr.Amount);
            Assert.AreEqual("E:F:G:R", efgr.Keys);

            var abcd = report.Lines.Where(x => x.Keys.Contains("C:D")).Single();
            Assert.AreEqual(200m, abcd.Amount);
            Assert.AreEqual("A:B:C:D", abcd.Keys);
        }

        [TestMethod]
        public async Task KeysShowsInReport()
        {
            int year = DateTime.Now.Year;

            await AddFiveTransactionsWithFourLevels(year);
            await context.SaveChangesAsync();

            var actionresult = await controller.Report("summary", year, null, null);
            var okresult = actionresult as OkObjectResult;
            var report = okresult.Value as ApiSummaryReportResult;

            Console.WriteLine(report);

            var efg = report.Lines.Where(x => x.Amount == 400m).Single();
            Assert.AreEqual("E:F:G", efg.Keys);

            var efgr = report.Lines.Where(x => x.Amount == 500m).Single();
            Assert.AreEqual("E:F:G:R", efgr.Keys);

            var abcd = report.Lines.Where(x => x.Amount == 200m).Single();
            Assert.AreEqual("A:B:C:D", abcd.Keys);
        }

        [TestMethod]
        public async Task BudgetTxsShowInReport()
        {
            // User Story 889: Add BudgetTxs to API report

            var year = DateTime.Now.Year;
            var items = new List<BudgetTx>();
            items.Add(new BudgetTx() { Timestamp = new System.DateTime(year, 06, 01), Category = "A", Amount = 100m });
            items.Add(new BudgetTx() { Timestamp = new System.DateTime(year, 06, 01), Category = "B:C", Amount = 200m });
            items.Add(new BudgetTx() { Timestamp = new System.DateTime(year, 05, 01), Category = "X:Y:Z", Amount = 300m });

            context.BudgetTxs.AddRange(items);
            await context.SaveChangesAsync();

            var actionresult = await controller.Report("summary", year, null, null);
            var okresult = actionresult as OkObjectResult;
            var report = okresult.Value as ApiSummaryReportResult;

            Console.WriteLine(report);

            foreach(var expected in items)
            {
                var actual = report.Lines.Where(x => x.Keys == expected.Category ).Single();
                Assert.AreEqual(expected.Amount, actual.Budget);
            }
        }

        [TestMethod]
        public async Task Key3and4ShowInReportFromCategory()
        {
            // User Story 874: Automatically split key3 out of category if enough separators

            var year = DateTime.Now.Year;

            var items = new List<Transaction>();
            items.Add(new Transaction() { Timestamp = new System.DateTime(year, 06, 01), Category = "A:B:C", Amount = 100m });
            items.Add(new Transaction() { Timestamp = new System.DateTime(year, 06, 01), Category = "A:B:C:D", Amount = 200m });

            context.Transactions.AddRange(items);

            await context.SaveChangesAsync();

            var actionresult = await controller.Report("summary", year, null, null);
            var okresult = actionresult as OkObjectResult;
            var report = okresult.Value as ApiSummaryReportResult;

            Console.WriteLine(report);

            foreach (var expected in items)
            {
                var actual = report.Lines.Where(x => x.Keys == expected.Category).Single();
                Assert.AreEqual(expected.Amount, actual.Amount);
            }
        }

        [TestMethod]
        public async Task ReportV2()
        {
            int year = DateTime.Now.Year;

            await AddFiveTransactions();
            await context.SaveChangesAsync();

            var actionresult = await controller.ReportV2( new ReportBuilder.Parameters() { id = "all" } );
            var okresult = actionresult as ContentResult;
            var report = okresult.Content;

            Console.WriteLine(report);

            var doc = JsonDocument.Parse(report);
            var root = doc.RootElement;

            var AAAA = root.EnumerateArray().Where(x => x.GetProperty("ID").GetString() == "AA:AA").Single();
            var BB = root.EnumerateArray().Where(x => x.GetProperty("ID").GetString() == "BB").Single();
            var CC = root.EnumerateArray().Where(x => x.GetProperty("ID").GetString() == "CC").Single();
            var Total = root.EnumerateArray().Where(x => x.GetProperty("IsTotal").GetBoolean()).Single();

            Assert.AreEqual(8, root.GetArrayLength());
            Assert.AreEqual(6, Total.EnumerateObject().Count());
            Assert.AreEqual(200m, AAAA.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(1000m, BB.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(300m, CC.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(1500m, Total.GetProperty("TOTAL").GetDecimal());
        }
        [TestMethod]
        public async Task ReportV2export()
        {
            int year = DateTime.Now.Year;

            await AddFiveBudgetTxs();
            await AddFiveTransactions();

            var actionresult = await controller.ReportV2(new ReportBuilder.Parameters() { id = "export" });
            var okresult = actionresult as ContentResult;
            var report = okresult.Content;

            Console.WriteLine(report);

            var doc = JsonDocument.Parse(report);
            var root = doc.RootElement;

            var AAAA = root.EnumerateArray().Where(x => x.GetProperty("ID").GetString() == "AA:AA:").Single();
            var AABB = root.EnumerateArray().Where(x => x.GetProperty("ID").GetString() == "AA:BB:").Single();
            var CCAA = root.EnumerateArray().Where(x => x.GetProperty("ID").GetString() == "CC:AA:").Single();
            var Total = root.EnumerateArray().Where(x => x.GetProperty("IsTotal").GetBoolean()).Single();

            Assert.AreEqual(6, root.GetArrayLength());
            Assert.AreEqual(6, Total.EnumerateObject().Count());
            Assert.AreEqual(200m, AAAA.GetProperty("ID:Actual").GetDecimal());
            Assert.AreEqual(0m, AABB.GetProperty("ID:Actual").GetDecimal());
            Assert.AreEqual(300m, CCAA.GetProperty("ID:Actual").GetDecimal());
            Assert.AreEqual(1500m, Total.GetProperty("ID:Actual").GetDecimal());
            Assert.AreEqual(400m, AAAA.GetProperty("ID:Budget").GetDecimal());
            Assert.AreEqual(500m, AABB.GetProperty("ID:Budget").GetDecimal());
            Assert.AreEqual(300m, CCAA.GetProperty("ID:Budget").GetDecimal());
            Assert.AreEqual(1500m, Total.GetProperty("ID:Budget").GetDecimal());
        }
    }
}
