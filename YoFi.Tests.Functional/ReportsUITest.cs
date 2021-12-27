using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    /// <summary>
    /// Test the various permutation of Reports
    /// </summary>
    [TestClass]
    public class ReportsUITest : FunctionalUITest
    {
        [TestMethod]
        public async Task ClickReports()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Reports" on the navbar
            await WhenNavigatingToPage("Reports");

            // Then: We land at the budget index page
            await Page.ThenIsOnPageAsync("Reports");

            // And: The summary is shown
            await Page.ThenH2Is("Summary");
        }

        /// <summary>
        /// Reports cover lage
        /// </summary>
        /// <remarks>
        /// Feature 1114: Report Cover Page: One page summary view of the whole picture
        /// </remarks>
        [DataRow("income")]
        [DataRow("taxes")]
        [DataRow("expenses")]
        [DataRow("savings")]
        [DataTestMethod]
        public async Task Summary(string which)
        {
            // Given: We are logged in and on the reports page
            await ClickReports();

            // When: Checking the total of the {income} summary
            var total = await Page.QuerySelectorAsync($"data-test-id=report-{which} >> tr.report-row-total >> td.report-col-total");
            var totaltext = (await total.TextContentAsync()).Trim();

            // And: Clicking on the detailed report link for the {income} report
            await Page.ClickAsync($"data-test-id={which}-detail");

            // And: Waiting for the page to fully load
            await Page.WaitForSelectorAsync("table.report");

            // Then: The total on the detailed report is the same as in the summary
            var summarytotal = await Page.QuerySelectorAsync("tr.report-row-total >> td.report-col-total");
            var summarytotaltext = (await summarytotal.TextContentAsync()).Trim();

            Assert.AreEqual(summarytotaltext, totaltext);

        }

        /// <summary>
        /// Simply ensure that each report loads and thinks it loaded
        /// </summary>
        /// <param name="expected">Name of the report</param>
        [DataRow("Income")]
        [DataRow("Expenses")]
        [DataRow("Taxes")]
        [DataRow("Savings")]
        [DataRow("Income Detail")]
        [DataRow("Expenses Detail")]
        [DataRow("Taxes Detail")]
        [DataRow("Savings Detail")]
        [DataRow("Expenses Budget")]
        [DataRow("All Transactions")]
        [DataRow("Full Budget")]
        [DataRow("All vs. Budget")]
        [DataRow("Expenses vs. Budget")]
        [DataRow("Managed Budget")]
        [DataRow("Transaction Export")]
        [DataRow("Year over Year")]
        [DataTestMethod]
        public async Task AllReport(string expected)
        {
            // Given: We are logged in and on the reports page
            await ClickReports();

            // When: Selecting the "all" report from the dropdown
            await Page.ClickAsync("text=Choose a Report");
            await Page.ClickAsync($"text={expected}");

            // And: Waiting for the page to fully load
            await Page.WaitForSelectorAsync("table.report");

            // Then: The expected report is generated
            await Page.ThenIsOnPageAsync(expected);
            await Page.ThenH2Is(expected);
        }

        [TestMethod]
        public async Task HideMonths()
        {
            // Given: We are logged in and on the All Transactions report
            await AllReport("All Transactions");
            await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.DOMContentLoaded);

            // When: Selecting "hide months"
            await Page.ClickAsync("id=dropdownMenuButtonColumns");
            await Page.ClickAsync("text=Hide");

            // And: Waiting for the page to fully load
            await Page.WaitForSelectorAsync("table.report");

            // Then: There is only the one "amount" column, which is the total
            await ThenColumnCountIs(1);
        }
        [TestMethod]
        public async Task ShowMonthsThroughJune()
        {
            // Given: We are logged in and on the All Transactions report
            await AllReport("All Transactions");
            await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.DOMContentLoaded);

            // When: Selecting "June"
            await Page.ClickAsync("id=dropdownMenuButtonMonth");
            await Page.ClickAsync("text=6 June");

            // And: Waiting for the page to load
            await Page.WaitForSelectorAsync("table.report");

            // Then: There are 7 amount columns, one for each month plus total
            // NOTE: This relies on the fact that the sample data generator creates data for all months
            // in the year. If we ever change it to only doing until current month, then this test
            // with need some change.
            await ThenColumnCountIs(7);
        }

        private async Task ThenColumnCountIs(int howmany)
        {
            var amountcols = await Page.QuerySelectorAllAsync("th.report-col-amount");
            Assert.AreEqual(howmany, amountcols.Count);
        }
    }
}
