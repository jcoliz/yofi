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
        private JsonElement.ArrayEnumerator rows;
        private JsonElement totalrow;
        private IEnumerable<JsonProperty> cols;

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

        #region Helpers

        protected async Task WhenGettingReport(ReportParameters parameters)
        {
            // Given: A URL which encodes these report parameters

            var start = $"/api/ReportV2/{parameters.id}?year={parameters.year}";
            var builder = new StringBuilder(start);

            if (parameters.showmonths.HasValue)
                builder.Append($"&showmonths={parameters.showmonths}");
            if (parameters.month.HasValue)
                builder.Append($"&month={parameters.month}");
            if (parameters.level.HasValue)
                builder.Append($"&level={parameters.level}");

            var url = builder.ToString();

            // When: Getting the report
            var response = await client.GetAsync(url);

            // Then: Response is OK
            response.EnsureSuccessStatusCode();

            // And: Parse for further checking
            var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            rows = root.EnumerateArray();
            totalrow = rows.Where(x => x.GetProperty("IsTotal").GetBoolean()).Single();
            cols = totalrow.EnumerateObject().Where(x => x.Name.StartsWith("ID:") || x.Name == "TOTAL");
        }

        private void ThenReportHasTotal(decimal expected)
        {
            var totalvalue = totalrow.GetProperty("TOTAL").GetDecimal();
            Assert.AreEqual(expected, totalvalue);
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
            await WhenGettingReport(new ReportParameters() { id = "all", year = 2020, showmonths = showmonths });

            // And: Report has the correct total
            ThenReportHasTotal(data.Transactions.Sum(x => x.Amount));

            // And: Report has the correct # columns (One for each month plus total)
            Assert.AreEqual(showmonths ? 13 : 1, cols.Count());

            // And: Report has the correct # rows
            Assert.AreEqual(21, rows.Count());
        }

        #endregion
    }

}
