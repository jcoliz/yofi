using Common.AspNet.Test;
using Common.NET.Test;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Controllers.Reports;
using YoFi.AspNet.Data;
using YoFi.AspNet.Models;

namespace YoFi.Tests
{
    [TestClass]
    public class ApiControllerTest
    {
        public ApiController controller { set; get; } = default(ApiController);

        public ApplicationDbContext context = null;

        public TestAzureStorage storage = null;

        public ILoggerFactory logfact = LoggerFactory.Create(builder => 
        {
        builder
        .AddConsole((options) => { })
        .AddFilter((category, level) => 
                category == "Microsoft.EntityFrameworkCore.Query"
                && level == LogLevel.Debug);
        });

        [TestInitialize]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                //.UseLoggerFactory(logfact)
                .UseInMemoryDatabase(databaseName: "ApplicationDbContext")
                .Options;

            context = new ApplicationDbContext(options);

            storage = new TestAzureStorage();

            // NOTE: This is a unit test password only, not a real credential!!
            var password = "Password1234";

            // https://stackoverflow.com/questions/55497800/populate-iconfiguration-for-unit-tests
            var strings = new Dictionary<string, string>
            {
                { "Api:Key", password },
                { "Storage:BlobContainerName", "Testing" }
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(strings)
                .Build();

            controller = new ApiController(context,configuration,storage);

            // Need to inject the Auth header into the context.
            // https://stackoverflow.com/questions/41400030/mock-httpcontext-for-unit-testing-a-net-core-mvc-controller
            var http = new DefaultHttpContext();
            var userpass = $"user:{password}";
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
            context.Transactions.Add(new Transaction() { Category = "BB:AA", Payee = "3", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m });
            context.Transactions.Add(new Transaction() { Category = "AA:AA", Payee = "2", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m });
            context.Transactions.Add(new Transaction() { Category = "CC:AA", Payee = "5", Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Amount = 300m });
            context.Transactions.Add(new Transaction() { Category = "BB:AA", Payee = "1", Timestamp = new DateTime(DateTime.Now.Year, 01, 05), Amount = 400m });
            context.Transactions.Add(new Transaction() { Category = "BB:AA", Payee = "4", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m });
            
            await context.SaveChangesAsync();
        }

        async Task AddFivePayees()
        {
            context.Payees.Add(new Payee() { Category = "Y", Name = "3" });
            context.Payees.Add(new Payee() { Category = "X", Name = "2" });
            context.Payees.Add(new Payee() { Category = "Z", Name = "5" });
            context.Payees.Add(new Payee() { Category = "X", Name = "1" });
            context.Payees.Add(new Payee() { Category = "Y", Name = "4" });

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
            var result = controller.Get();

            Assert.IsTrue(result.Ok);
        }
        [TestMethod]
        public async Task GetId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();

            var result = await controller.Get(expected.ID);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(expected, result.Item);
        }
        [TestMethod]
        public async Task GetIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x=>x.ID);

            var result = await controller.Get(maxid + 1);

            Assert.IsFalse(result.Ok);
            Assert.IsFalse(string.IsNullOrEmpty(result.Error));
        }
        [TestMethod]
        public async Task HideId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();

            var result = await controller.Hide(expected.ID,true);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(true == expected.Hidden);
        }
        [TestMethod]
        public async Task HideIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var result = await controller.Hide(maxid + 1,true);

            Assert.IsFalse(result.Ok);
            Assert.IsFalse(string.IsNullOrEmpty(result.Error));
        }
        [TestMethod]
        public async Task ShowId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();
            expected.Hidden = true;
            await context.SaveChangesAsync();

            var result = await controller.Hide(expected.ID,false);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(false == expected.Hidden);
        }
        [TestMethod]
        public async Task ShowIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var result = await controller.Hide(maxid + 1,false);

            Assert.IsFalse(result.Ok);
            Assert.IsFalse(string.IsNullOrEmpty(result.Error));
        }
        [TestMethod]
        public async Task SelectId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();

            var result = await controller.Select(expected.ID,true);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(true == expected.Selected);
        }
        [TestMethod]
        public async Task SelectIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var result = await controller.Select(maxid + 1,true);

            Assert.IsFalse(result.Ok);
            Assert.IsFalse(string.IsNullOrEmpty(result.Error));
        }
        [TestMethod]
        public async Task DeselectId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();
            expected.Selected = true;
            await context.SaveChangesAsync();

            var result = await controller.Select(expected.ID,false);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(false == expected.Selected);
        }
        [TestMethod]
        public async Task DeselectIdFails()
        {
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var result = await controller.Select(maxid + 1,false);

            Assert.IsFalse(result.Ok);
            Assert.IsFalse(string.IsNullOrEmpty(result.Error));
        }
        [TestMethod]
        public async Task SelectPayeeId()
        {
            await AddFivePayees();
            var expected = await context.Payees.FirstAsync();

            var result = await controller.SelectPayee(expected.ID,true);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(true == expected.Selected);
        }
        [TestMethod]
        public async Task SelectPayeeIdFails()
        {
            await AddFivePayees();
            var maxid = await context.Payees.MaxAsync(x => x.ID);

            var result = await controller.SelectPayee(maxid + 1,true);

            Assert.IsFalse(result.Ok);
            Assert.IsFalse(string.IsNullOrEmpty(result.Error));
        }
        [TestMethod]
        public async Task DeselectPayeeId()
        {
            await AddFivePayees();
            var expected = await context.Payees.FirstAsync();
            expected.Selected = true;
            await context.SaveChangesAsync();

            var result = await controller.SelectPayee(expected.ID,false);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(false == expected.Selected);
        }
        [TestMethod]
        public async Task DeselectPayeeIdFails()
        {
            await AddFivePayees();
            var maxid = await context.Payees.MaxAsync(x => x.ID);

            var result = await controller.SelectPayee(maxid + 1,false);

            Assert.IsFalse(result.Ok);
            Assert.IsFalse(string.IsNullOrEmpty(result.Error));
        }
        [TestMethod]
        public async Task AddPayee()
        {
            var expected = new Payee() { Category = "B", Name = "3" };

            var result = await controller.AddPayee(expected);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(expected, result.Item);
        }
        [TestMethod]
        public async Task ApplyPayee()
        {
            await AddFivePayees();
            await AddFiveTransactions();

            // Pick an aribtrary transaction
            var tx = await context.Transactions.LastAsync();

            var result = await controller.ApplyPayee(tx.ID);

            Assert.IsTrue(result.Ok);

            var expected = await context.Payees.Where(x => x.Name == tx.Payee).SingleAsync();

            Assert.AreEqual(expected, result.Item);

            Assert.AreEqual(expected.Category, tx.Category);
        }
        [TestMethod]
        public async Task ApplyPayeeFailsNoTxId()
        {
            await AddFivePayees();
            await AddFiveTransactions();
            var maxid = await context.Transactions.MaxAsync(x => x.ID);

            var result = await controller.ApplyPayee(maxid + 1);

            Assert.IsFalse(result.Ok);
            Assert.IsFalse(string.IsNullOrEmpty(result.Error));
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

            var result = await controller.ApplyPayee(tx.ID);

            Assert.IsFalse(result.Ok);
            Assert.IsFalse(string.IsNullOrEmpty(result.Error));
        }

        [DataTestMethod]
        [DataRow("1234567 Bobby XN April 2021 5 wks")]
        [DataRow("1234567 Bobby MAR XN")]
        [DataRow("1234567 Jan XN ")]
        public async Task ApplyPayeeRegex_Pbi871(string name)
        {
            // Product Backlog Item 871: Match payee on regex, optionally

            context.Transactions.Add(new Transaction() { Payee = name, Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m });

            var expected = new Payee() { Category = "Y", Name = "/1234567.*XN/" };
            context.Payees.Add(expected);

            await context.SaveChangesAsync();

            var tx = context.Transactions.First();

            var result = await controller.ApplyPayee(tx.ID);

            Assert.IsTrue(result.Ok);

            Assert.AreEqual(expected, result.Item);

            Assert.AreEqual(expected.Category, tx.Category);
        }

        [TestMethod]
        public async Task Edit()
        {
            await AddFiveTransactions();
            var original = await context.Transactions.FirstAsync();

            // detach the original so we have an unmodified copy around
            context.Entry(original).State = EntityState.Detached;

            var newtx = new Transaction() { ID = original.ID, Payee = "I have edited you!", Timestamp = original.Timestamp, Amount = original.Amount };

            var result = await controller.Edit(original.ID, false, newtx);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(newtx, result.Item);
            Assert.AreNotEqual(original, result.Item);

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

            var newtx = new Transaction() { ID = original.ID, Payee = "I have edited you!", Timestamp = original.Timestamp, Amount = original.Amount };

            var result = await controller.Edit(original.ID, true, newtx);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(newtx, result.Item);
            Assert.AreNotEqual(original, result.Item);

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

            var newitem = new Payee() { ID = original.ID, Name = "I have edited you!", Category = original.Category };

            var result = await controller.EditPayee(original.ID, false, newitem);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(newitem, result.Item);
            Assert.AreNotEqual(original, result.Item);

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

            var newitem = new Payee() { ID = original.ID, Name = "I have edited you!", Category = original.Category };

            var result = await controller.EditPayee(original.ID, true, newitem);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(newitem, result.Item);
            Assert.AreNotEqual(original, result.Item);

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
            var count = 10;
            var stream = new MemoryStream(Enumerable.Repeat<byte>(0x60, count).ToArray());
            var file = new FormFile(stream, 0, count, "Index", $"Index.html") { Headers = new HeaderDictionary(), ContentType = contenttype };
            
            var result = await controller.UpReceipt(original.ID,file);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(original.ID.ToString(), original.ReceiptUrl);

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

            // Make an HTML Form file containg a spreadsheet containing those splits
            var file = ControllerTestHelper<Split,SplitsController>.PrepareUpload(splits);

            // Upload that
            var result = await controller.UpSplits(item.ID, file);

            Assert.IsTrue(result.Ok);
            Assert.IsTrue(item.HasSplits);
            Assert.IsTrue(item.IsSplitsOK);
        }


#if false
        // TODO: Wrtire for V3 reports
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
#endif

        [TestMethod]
        public async Task ReportV2()
        {
            int year = DateTime.Now.Year;

            await AddFiveTransactions();
            await context.SaveChangesAsync();

            var actionresult = controller.ReportV2( new ReportBuilder.Parameters() { id = "all" } );
            if (actionresult is ObjectResult or)
                throw or.Value as Exception;

            var okresult = actionresult as ContentResult;
            var report = okresult.Content;

            Console.WriteLine(report);

            var doc = JsonDocument.Parse(report);
            var root = doc.RootElement;

            var AAAA = root.EnumerateArray().Where(x => x.GetProperty("ID").GetString() == "AA:AA").Single();
            var BB = root.EnumerateArray().Where(x => x.GetProperty("ID").GetString() == "BB").Single();
            var CC = root.EnumerateArray().Where(x => x.GetProperty("ID").GetString() == "CC").Single();
            var Total = root.EnumerateArray().Where(x => x.GetProperty("IsTotal").GetBoolean()).Single();

            Assert.AreEqual(7, root.GetArrayLength());
            Assert.AreEqual(6, Total.EnumerateObject().Count());
            Assert.AreEqual(200m, AAAA.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(1000m, BB.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(300m, CC.GetProperty("TOTAL").GetDecimal());
            Assert.AreEqual(1500m, Total.GetProperty("TOTAL").GetDecimal());
        }
        [TestMethod]
        public async Task EFGroupByBugBudgetTx()
        {
            await AddFiveBudgetTxs();

            Func<IReportable, bool> inscope_t = x => true;

            var budgettxs = context.BudgetTxs.Where(inscope_t);

            var budgetgroups = budgettxs.GroupBy(x => x.Category);

            foreach (var group in budgetgroups)
            {
                Console.WriteLine(group.Key);
                Console.WriteLine(group.Count());

                foreach(var item in group.AsEnumerable())
                {
                    Console.WriteLine(item.Category);
                }
            }

        }
        [TestMethod]
        public async Task EFGroupByBugTransactionSplits()
        {
            await AddFiveTransactions();

            Func<IReportable, bool> inscope_t = x => true;

            var txs = context.Transactions.Where(inscope_t);

            var groups = txs.GroupBy(x => x.Category);

            foreach (var group in groups)
            {
                Console.WriteLine(group.Key);
                Console.WriteLine(group.Count());

                foreach (var item in group.AsEnumerable())
                {
                    Console.WriteLine(item.Category);
                }
            }

        }
        [TestMethod]
        public async Task EFGroupByBugTransactionV3()
        {
            await AddFiveTransactions();

            var groups = context.Transactions.GroupBy(x => x.Category).Select(g => new { key = g.Key, sum = g.Sum(y => y.Amount) });

            foreach (var group in groups)
            {
                Console.WriteLine(group.key);
                Console.WriteLine(group.sum);
            }

        }
        [TestMethod]
        public async Task EFMultiGroupByBugTransactionV3()
        {
            // I think this will be the new lingua-franca of V3 reports.
            // Server will group by category or category+month.
            // Client will split into levels and filter as needed.

            context.Transactions.Add(new Transaction() { Category = "AA", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 100m });
            context.Transactions.Add(new Transaction() { Category = "AA", Timestamp = new DateTime(DateTime.Now.Year, 01, 04), Amount = 200m });
            context.Transactions.Add(new Transaction() { Category = "AA", Timestamp = new DateTime(DateTime.Now.Year, 02, 03), Amount = 100m });
            context.Transactions.Add(new Transaction() { Category = "AA", Timestamp = new DateTime(DateTime.Now.Year, 02, 04), Amount = 200m });
            context.Transactions.Add(new Transaction() { Category = "CC", Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Amount = 300m });
            context.Transactions.Add(new Transaction() { Category = "CC", Timestamp = new DateTime(DateTime.Now.Year, 01, 01), Amount = 300m });
            context.Transactions.Add(new Transaction() { Category = "CC", Timestamp = new DateTime(DateTime.Now.Year, 03, 01), Amount = 300m });
            context.Transactions.Add(new Transaction() { Category = "CC", Timestamp = new DateTime(DateTime.Now.Year, 03, 01), Amount = 300m });
            context.Transactions.Add(new Transaction() { Category = "BB", Timestamp = new DateTime(DateTime.Now.Year, 01, 05), Amount = 400m });
            context.Transactions.Add(new Transaction() { Category = "BB", Timestamp = new DateTime(DateTime.Now.Year, 01, 03), Amount = 500m });
            context.Transactions.Add(new Transaction() { Category = "BB", Timestamp = new DateTime(DateTime.Now.Year, 04, 05), Amount = 400m });
            context.Transactions.Add(new Transaction() { Category = "BB", Timestamp = new DateTime(DateTime.Now.Year, 04, 03), Amount = 500m });

            await context.SaveChangesAsync();

            var source1 = context.Transactions.Where(x=>x.Category == "AA").AsQueryable<IReportable>();
            var source2 = context.Transactions.Where(x => x.Category == "BB").AsQueryable<IReportable>();
            var source = source1.Concat(source2);
            var groups = source.GroupBy(x => new { cat = x.Category, month = x.Timestamp.Month }).Select(g => new { key = g.Key, sum = g.Sum(y => y.Amount) });

            foreach (var group in groups)
            {
                Console.WriteLine($"{group.key.cat} {group.key.month} {group.sum,6:C0}");
            }
        }

        [TestMethod]
        public async Task ReportV2export()
        {
            int year = DateTime.Now.Year;

            await AddFiveBudgetTxs();
            await AddFiveTransactions();

            var actionresult = controller.ReportV2(new ReportBuilder.Parameters() { id = "export" });
            if (actionresult is ObjectResult or)
                throw or.Value as Exception;

            var okresult = actionresult as ContentResult;
            var report = okresult.Content;

            Console.WriteLine(report);

            var doc = JsonDocument.Parse(report);
            var root = doc.RootElement;

            var AAAA = root.EnumerateArray().Where(x => x.GetProperty("ID").GetString() == "AA:AA").Single();
            var AABB = root.EnumerateArray().Where(x => x.GetProperty("ID").GetString() == "AA:BB").Single();
            var CCAA = root.EnumerateArray().Where(x => x.GetProperty("ID").GetString() == "CC:AA").Single();

            Assert.AreEqual(5, root.GetArrayLength());
            Assert.AreEqual(200m, AAAA.GetProperty("ID:Actual").GetDecimal());
            Assert.AreEqual(0m, AABB.GetProperty("ID:Actual").GetDecimal());
            Assert.AreEqual(300m, CCAA.GetProperty("ID:Actual").GetDecimal());
            Assert.AreEqual(400m, AAAA.GetProperty("ID:Budget").GetDecimal());
            Assert.AreEqual(500m, AABB.GetProperty("ID:Budget").GetDecimal());
            Assert.AreEqual(300m, CCAA.GetProperty("ID:Budget").GetDecimal());
        }

        [TestMethod]
        public async Task CategoryAutocomplete()
        {
            // Given: A set of five transactions, some with {word} in their category, some not
            await AddFiveTransactions();

            // When: Calling CategoryAutocomplete with '{word}'
            var word = "BB";
            var result = controller.CategoryAutocomplete(word);

            // Then: All of the categories from given items which contain '{word}' are returned
            var expected = await context.Transactions.Select(x=>x.Category).Distinct().Where(c => c.Contains(word)).ToListAsync();
            CollectionAssert.AreEqual(expected, result);
        }

        async Task<IEnumerable<Transaction>> WhenCallingGetTxWithQ(string q)
        {
            var result = await controller.GetTransactions(q: q);
            var jsonresult = result as JsonResult;
            var model = jsonresult.Value as IEnumerable<Transaction>;

            return model;
        }

        // Note that I have stolen these tests directly from TransactionControllerTest.

        [TestMethod]
        public async Task GetTxQAny()
        {
            // Given: A mix of transactions, some with '{word}' in their category, memo, or payee and some without
            var items = TransactionControllerTest.TransactionItems.Take(19);
            context.Transactions.AddRange(items);
            context.SaveChanges();

            // When: Calling GetTransactions q={word}
            var word = "CAF";
            var model = await WhenCallingGetTxWithQ(word);

            // Then: Only the transactions with '{word}' in their category, memo, or payee are returned
            Assert.AreEqual(6, model.Count());
            Assert.IsTrue(model.All(tx => tx.Category?.Contains(word) == true || tx.Memo?.Contains(word) == true || tx.Payee?.Contains(word) == true));
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task GetTxQReceipt(bool with)
        {
            // Given: A mix of transactions, some with receipts, some without
            IEnumerable<Transaction> items, moditems;
            TransactionControllerTest.GivenItemsWithAndWithoutReceipt(context, out items, out moditems);

            // When: Calling GetTransactions q='r=1' (or r=0)
            var model = await WhenCallingGetTxWithQ($"R={(with ? '1' : '0')}");

            // Then: Only the transactions with (or without) receipts are returned
            if (with)
                Assert.AreEqual(moditems.Count(), model.Count());
            else
                Assert.AreEqual(items.Count() - moditems.Count(), model.Count());
        }

    }
}
