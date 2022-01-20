using Common.DotNet;
using Common.DotNet.Test;
using Common.EFCore;
using Common.NET.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.AspNet.Controllers;
using YoFi.AspNet.Data;
using YoFi.Core.Models;
using YoFi.Core.Reports;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Database
{
    [TestClass]
    public class ApiControllerTest
    {
        public ApiController controller { set; get; } = default;

        public ApplicationDbContext context = null;

        public TestAzureStorage storage = null;

        private TestClock clock = null;

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
            controller = new ApiController();
            clock = new TestClock();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Didn't actually solve anything. Keep it around for possible future problem
            //DetachAllEntities();

            // https://stackoverflow.com/questions/33490696/how-can-i-reset-an-ef7-inmemory-provider-between-unit-tests
            context?.Database.EnsureDeleted();
            context = null;
            controller = default;
        }

        async Task AddFiveTransactions()
        {            
            context.Transactions.Add(new Transaction() { Category = "BB:AA", Payee = "3", Timestamp = clock.Now + TimeSpan.FromDays(2), Amount = 100m });
            context.Transactions.Add(new Transaction() { Category = "AA:AA", Payee = "2", Timestamp = clock.Now + TimeSpan.FromDays(3), Amount = 200m });
            context.Transactions.Add(new Transaction() { Category = "CC:AA", Payee = "5", Timestamp = clock.Now + TimeSpan.FromDays(0), Amount = 300m });
            context.Transactions.Add(new Transaction() { Category = "BB:AA", Payee = "1", Timestamp = clock.Now + TimeSpan.FromDays(4), Amount = 400m });
            context.Transactions.Add(new Transaction() { Category = "BB:AA", Payee = "4", Timestamp = clock.Now + TimeSpan.FromDays(2), Amount = 500m });
            
            await context.SaveChangesAsync();
        }

        async Task AddFiveBudgetTxs()
        {
            context.BudgetTxs.Add(new BudgetTx() { Timestamp = new System.DateTime(clock.Now.Year, 06, 01), Category = "BB:BB", Amount = 100m });
            context.BudgetTxs.Add(new BudgetTx() { Timestamp = new System.DateTime(clock.Now.Year, 06, 01), Category = "BB:AA", Amount = 200m });
            context.BudgetTxs.Add(new BudgetTx() { Timestamp = new System.DateTime(clock.Now.Year, 05, 01), Category = "CC:AA", Amount = 300m });
            context.BudgetTxs.Add(new BudgetTx() { Timestamp = new System.DateTime(clock.Now.Year, 05, 01), Category = "AA:AA", Amount = 400m });
            context.BudgetTxs.Add(new BudgetTx() { Timestamp = new System.DateTime(clock.Now.Year, 05, 01), Category = "AA:BB", Amount = 500m });

            await context.SaveChangesAsync();
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(controller);
        }

        [TestMethod]
        public async Task GetId()
        {
            await AddFiveTransactions();
            var expected = await context.Transactions.FirstAsync();

            var actionresult = await controller.Get(expected.ID, new TransactionRepository(context, new EFCoreAsyncQueryExecution()));

            var okresult = Assert.That.IsOfType<OkObjectResult>(actionresult);
            var actual = Assert.That.IsOfType<Transaction>(okresult.Value);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ReportV2()
        {
            int year = 2021;
            clock.Now = new DateTime(year, 1, 1);

            await AddFiveTransactions();
            await context.SaveChangesAsync();

            var actionresult = controller.ReportV2( new ReportParameters() { id = "all" }, new ReportBuilder(context,clock) );
            var okresult = Assert.That.IsOfType<ContentResult>(actionresult);
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
        public async Task ReportV2export()
        {
            int year = 2021;
            clock.Now = new DateTime(year, 1, 1);

            await AddFiveBudgetTxs();
            await AddFiveTransactions();

            var actionresult = controller.ReportV2(new ReportParameters() { id = "export" }, new ReportBuilder(context,clock));
            var okresult = Assert.That.IsOfType<ContentResult>(actionresult);
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
        public void ReportV2exportEmpty()
        {
            var actionresult = controller.ReportV2(new ReportParameters() { id = "export" }, new ReportBuilder(context,clock));
            var okresult = Assert.That.IsOfType<ContentResult>(actionresult);
            var report = okresult.Content;

            Console.WriteLine(report);

            var doc = JsonDocument.Parse(report);
            var root = doc.RootElement;

            Assert.AreEqual(0, root.GetArrayLength());
        }


        async Task<IEnumerable<Transaction>> WhenCallingGetTxWithQ(string q)
        {
            var actionresult = await controller.GetTransactions(new TransactionRepository(context, new EFCoreAsyncQueryExecution()), q: q);
            var jsonresult = Assert.That.IsOfType<OkObjectResult>(actionresult);
            var model = Assert.That.IsOfType<IEnumerable<Transaction>>(jsonresult.Value);

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

        [TestMethod]
        public async Task GetTxQMany()
        {
            // Given: A mix of MANY transactions, some with '{word}' in their category, memo, or payee and some without
            var word = "Word";
            var expected = 100;
            var items = Enumerable.Range(1, expected).Select(x => new Transaction() { Amount = x*100m, Payee = x.ToString(), Timestamp = clock.Now + TimeSpan.FromDays(x), Memo = word });
            var moreitems = Enumerable.Range(1, 20).Select(x => new Transaction() { Amount = x * 100m, Payee = x.ToString(), Timestamp = clock.Now + TimeSpan.FromDays(x) });
            context.Transactions.AddRange(items.Concat(moreitems));
            context.SaveChanges();

            // When: Calling GetTransactions q={word}
            var model = await WhenCallingGetTxWithQ(word);

            // Then: Only the transactions with '{word}' in their category, memo, or payee are returned
            Assert.AreEqual(expected, model.Count());
            Assert.IsTrue(model.All(tx => tx.Category?.Contains(word) == true || tx.Memo?.Contains(word) == true || tx.Payee?.Contains(word) == true));
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task GetTxQReceipt(bool with)
        {
            // Given: A mix of transactions, some with receipts, some without
            (var items, var moditems) = TransactionControllerTest.GivenItems(10, 3, x => x.ReceiptUrl = "Has receipt");
            context.AddRange(items);
            await context.SaveChangesAsync();

            // When: Calling GetTransactions q='r=1' (or r=0)
            var model = await WhenCallingGetTxWithQ($"R={(with ? '1' : '0')}");

            // Then: Only the transactions with (or without) receipts are returned
            if (with)
                Assert.AreEqual(moditems.Count(), model.Count());
            else
                Assert.AreEqual(items.Count() - moditems.Count(), model.Count());
        }

        [TestMethod]
        public async Task ClearTestTransactions()
        {
            // Given: A mix of transactions, some with __test__ marker, some without
            int numitems = 10;
            int nummoditems = 3;
            var items = TransactionControllerTest.TransactionItems.Take(numitems);
            foreach (var moditem in items.Take(nummoditems))
                moditem.Category += ApiController.TestMarker;

            context.AddRange(items);
            await context.SaveChangesAsync();

            // When: Calling ClearTestData with id="trx"
            var actionresult = await controller.ClearTestData("trx", context);

            // Then: Result is OK
            Assert.That.IsOfType<OkResult>(actionresult);

            // ANd: Only the transactions without __test__ remain
            Assert.AreEqual(numitems-nummoditems, context.Transactions.Count());
        }

        [TestMethod]
        public async Task ClearTestBudgetTx()
        {
            // Given: A mix of budget transactions, some with __test__ marker, some without
            await AddFiveBudgetTxs();
            int numitems = 5;
            int nummoditems = numitems / 2;
            foreach (var moditem in context.BudgetTxs.Take(nummoditems))
                moditem.Category += ApiController.TestMarker;

            await context.SaveChangesAsync();

            // When: Calling ClearTestData with id="budgettx"
            var actionresult = await controller.ClearTestData("budgettx", context);

            // Then: Result is OK
            Assert.That.IsOfType<OkResult>(actionresult);

            // ANd: Only the transactions without __test__ remain
            Assert.AreEqual(numitems - nummoditems, context.BudgetTxs.Count());
        }

        [TestMethod]
        public async Task ClearTestPayees()
        {
            // Given: A mix of budget transactions, some with __test__ marker, some without
            int numitems = 5;
            var items = TransactionControllerTest.PayeeItems.Take(numitems);
            int nummoditems = numitems / 2;
            foreach (var moditem in items.Take(nummoditems))
                moditem.Category += ApiController.TestMarker;

            context.AddRange(items);
            await context.SaveChangesAsync();

            // When: Calling ClearTestData with id="payee"
            var actionresult = await controller.ClearTestData("payee", context);

            // Then: Result is OK
            Assert.That.IsOfType<OkResult>(actionresult);

            // ANd: Only the transactions without __test__ remain
            Assert.AreEqual(numitems - nummoditems, context.Payees.Count());
        }


#if EFCORE_TESTS
        [TestMethod]
        public async Task EFGroupByBugBudgetTx()
        {
            await AddFiveBudgetTxs();

            // An expression tree may not contain a reference to a local function.
            // SO this code analysis rule is incorrectly applied in this case.
#pragma warning disable IDE0039 // Use local function
            Func<IReportable, bool> inscope_t = x => true;
#pragma warning restore IDE0039 // Use local function

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

            // An expression tree may not contain a reference to a local function.
            // SO this code analysis rule is incorrectly applied in this case.
#pragma warning disable IDE0039 // Use local function
            Func<IReportable, bool> inscope_t = x => true;
#pragma warning restore IDE0039 // Use local function

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
#endif
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
    }
}
