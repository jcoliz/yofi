using Common.DotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Reports;
using YoFi.Tests.Helpers;

namespace YoFi.Tests.Core
{
    [TestClass]
    public class ReportBuilderTest
    {

        #region Fields

        protected static SampleDataStore data;
        private IReportEngine builder;
        private int year;

        #endregion

        #region Init/Cleanup

        [ClassInitialize]
        public static async Task InitialSetup(TestContext tcontext)
        {
            await SampleDataStore.LoadSingleAsync();
            data = SampleDataStore.Single;
        }

        [TestInitialize]
        public void SetUp()
        {
            var clock = new TestClock();
            builder = new ReportBuilder(data,clock);
            year = data.Transactions.First().Timestamp.Year;
        }

        #endregion


        #region Tests

        static readonly string[] empties = new[] { "trips" };

        [TestMethod]
        public void RunAll()
        {
            // Given: All the reports known to the system
            var tests = builder.Definitions;

            foreach (var def in tests)
                RunOne(def.id);
        }

        [DataRow("managed-budget")]
        [DataRow("budget")]
        [DataRow("trips")]
        [DataRow("savings")]
        [DataTestMethod]
        public void RunOne(string id)
        {
            // When: Running each w/ no parameters
            Console.WriteLine($"{id}...");
            var report = builder.Build(new ReportParameters() { id = id, year = year });

            // Then: There is data, except for reports we know will be empty
            if (empties.Contains(id))
            {
                Assert.IsFalse(report.RowLabels.Any(x=>!x.IsTotal));
                Assert.IsFalse(report.ColumnLabels.Any(x => x.IsTotal));
            }
            else
            {
                Assert.IsTrue(report.RowLabels.Any(x => !x.IsTotal));
                Assert.IsTrue(report.ColumnLabels.Any(x => x.IsTotal));
            }
            Console.WriteLine($"OK");
        }

        #endregion
    }
}
