using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoFi.Core.Reports;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration.Reports
{
    /// <summary>
    /// Test only the YoY report
    /// </summary>
    /// <remarks>
    /// Pulled out into a separate class because it uses a completely different dataset from
    /// the normal tests
    /// </remarks>
    [TestClass]
    public class ReportBuilderYoYTest: ReportBuilderTestBase
    {
        #region Fields

        protected static SampleDataStore data;

        #endregion

        #region Init/Cleanup

        [ClassInitialize]
        public static async Task InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);

            // Given: A large database of transactions, over many years

            // Load the partial data set in the usual way
            await SampleDataStore.LoadPartialAsync();
            data = SampleDataStore.Single;

            // Construct a 10-year dataset by spreading out the transactions over that timeframe
            context.Transactions.AddRange(data.Transactions.Select(x =>
            {
                var index = Convert.ToInt32(x.Payee);
                var adjust = index % 10;
                x.Timestamp = x.Timestamp.AddYears(-adjust);
                return x;
            }));
            context.SaveChanges();
        }

        [ClassCleanup]
        public static void FinalCleanup()
        {
            integrationcontext.Dispose();
        }

        #endregion

        #region Tests

        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataTestMethod]
        public async Task YoY(int level)
        {
            // Given: A large database of transactions, over many years

            // When: Building the 'yoy' report 
            await WhenGettingReport(new ReportParameters() { id = "yoy", level = level });

            // Then: Report has the correct total
            Assert.AreEqual(data.Transactions.Sum(x => x.Amount).ToString("C0", culture), total);

            // And: Report has the correct # columns: 10 years plus total
            Assert.AreEqual(11, cols.Count());

            // And: Report has the correct # rows
            var rowset = new int[] { 9, 21, 24, 26 };
            Assert.AreEqual(rowset[level - 1], rows.Count());

            // And: Report has the right levels
            // Note that we are using the report-row-x class
            var regex = new Regex("report-row-([0-9]+)");
            var levels = rows
                    .SelectMany(row => row.ClassList)
                    .Select(@class => regex.Match(@class))
                    .Where(match => match.Success)
                    .Select(match => match.Groups.Values.Last().Value)
                    .Select(value => int.Parse(value))
                    .Distinct();

            Assert.AreEqual(level, levels.Count());
        }
        #endregion

    }
}
