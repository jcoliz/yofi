using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YoFi.Core.Reports;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Reports
{
    [TestClass]
    public class ReportBuilderApiTest: IntegrationTest
    {
        #region Fields

        protected static SampleDataStore data;

        #endregion

        #region Init/Cleanup

        [ClassInitialize]
        public static async Task InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);

            // Given: A large database of transactions and budgettxs

            await SampleDataStore.LoadPartialAsync();
            data = SampleDataStore.Single;

            context.Transactions.AddRange(data.Transactions);
            context.BudgetTxs.AddRange(data.BudgetTxs);
            context.SaveChanges();
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            integrationcontext.Dispose();
        }

        [TestInitialize]
        public void SetUp()
        {
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:{integrationcontext.apiconfig.Key}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Remove ephemeral items
            context.BudgetTxs.RemoveRange(context.BudgetTxs.Where(x => x.Memo == "__TEST__"));
            context.SaveChanges();
        }

        #endregion

        #region Tests

        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        public async Task All(bool showmonths)
        {
            // Given: A large database of transactions
            // (Assembled on ClassInitialize)

            // When: Getting the report
            var parms = new ReportParameters() { id = "all", year = 2020, showmonths = showmonths };
            var response = await client.GetAsync($"/api/ReportV2/{parms.id}?year={parms.year}&showmonths={parms.showmonths}");
            response.EnsureSuccessStatusCode();
            var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            var rows = root.EnumerateArray();

            // And: Report has the correct total
            var TotalRow = rows.Where(x => x.GetProperty("IsTotal").GetBoolean()).Single();
            var expected = data.Transactions.Sum(x => x.Amount);
            var totalvalue = TotalRow.GetProperty("TOTAL").GetDecimal();
            Assert.AreEqual(expected, totalvalue);

            // And: Report has the correct # columns (One for each month plus total)
            var cols = TotalRow.EnumerateObject().Where(x => x.Name.StartsWith("ID:") || x.Name == "TOTAL");
            Assert.AreEqual(showmonths ? 13 : 1, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(21, rows.Count());
        }

        #endregion
    }

}
