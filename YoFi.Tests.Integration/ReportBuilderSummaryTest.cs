using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Reports;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
{
    [TestClass]
    public class ReportBuilderSummaryTest: ReportBuilderTestBase
    {
        #region Init/Cleanup

        [ClassInitialize]
        public static async Task InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);

            // Given: A complete sample data set
            await SampleDataStore.LoadSingleAsync();
            context.Transactions.AddRange(SampleDataStore.Single.Transactions);
            context.SaveChanges();
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            integrationcontext.Dispose();
        }

        #endregion

        #region Helpers
        #endregion

        #region Tests

        [TestMethod]
        public async Task ReportsGet()
        {
            // Given: A complete sample data set
            // (Done in ClassInitialize)

            // When: Getting the "Reports" Page for the year of our sample data
            await WhenGettingReport(new ReportParameters() { id = "summary", year = 2021 });

            // Then: Reports have the correct total
            ThenReportHasTotal(report: "income", expected: 149000.08m);
            ThenReportHasTotal(report: "taxes", expected: -31872m);
            ThenReportHasTotal(report: "expenses", expected: -64619.77m);
            ThenReportHasTotal(report: "savings", expected: -32600.16m);
        }

        #endregion
    }
}
