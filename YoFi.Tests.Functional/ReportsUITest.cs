using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YoFi.PWTests
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

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync("text=Reports");

            // Then: We land at the budget index page
            await ThenIsOnPage("Reports");

            // And: The summary is shown
            await ThenH2Is("Summary");
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

            // Then: The expected report is generated
            await ThenIsOnPage(expected);
            await ThenH2Is(expected);
        }

        [TestMethod]
        public async Task HideMonths()
        {
            // Given: We are logged in and on the All Transactions report
            await AllReport("All Transactions");

            // When: Selecting "hide months"
            await Page.ClickAsync("id=dropdownMenuButtonColumns");
            await Page.ClickAsync("text=Hide");

            // Then: There is only the one "amount" column, which is the total
            await ThenColumnCountIs(1);
        }
        [TestMethod]
        public async Task ShowMonthsThroughJune()
        {
            // Given: We are logged in and on the All Transactions report
            await AllReport("All Transactions");

            // When: Selecting "June"
            await Page.ClickAsync("id=dropdownMenuButtonMonth");
            await Page.ClickAsync("text=6 June");

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
