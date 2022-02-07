using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Reports;
using YoFi.Tests.Integration.Helpers;

namespace YoFi.Tests.Integration
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
        #region Init/Cleanup

        [ClassInitialize]
        public static void InitialSetup(TestContext tcontext)
        {
            integrationcontext = new IntegrationContext(tcontext.FullyQualifiedTestClassName);

            // Construct a 10-year dataset by spreading out the transactions over that timeframe

            // Transform into a 10-year timespan
            var txs = Given1000Transactions();
            context.Transactions.AddRange(txs.Select(x =>
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
            Assert.AreEqual(Transactions1000.Sum(x => x.Amount).ToString("C0", culture), total);

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
