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
    public class ReportsUITest : PageTest
    {
        public override BrowserNewContextOptions ContextOptions => _ContextOptions;

        private static BrowserNewContextOptions _ContextOptions { get; set; }

        private readonly string Site = "http://localhost:50419/";

        private readonly string ConfigFileName = "reportsuitest-loginstate.json";

        private async Task GivenLoggedIn()
        {
            // Navigate to the root of the site
            await Page.GotoAsync(Site);

            // Are we already logged in?
            var hellouser = await Page.QuerySelectorAsync("data-test-id=hello-user");

            // If we're not already logged in, well we need to do that then
            if (null == hellouser)
            {
                Console.WriteLine("Logging in...");

                await Page.ClickAsync("data-test-id=login");

                // And: User credentials as specified in user secrets
                var config = new ConfigurationBuilder().AddUserSecrets(Assembly.GetAssembly(typeof(ReportsUITest))).Build();
                var email = config["AdminUser:Email"];
                var password = config["AdminUser:Password"];

                // When: Filling out the login form with those credentials and pressing "sign in"
                await Page.FillAsync("id=floatingInput", email);
                await Page.FillAsync("id=floatingPassword", password);
                await Page.ClickAsync("data-test-id=signin");

                // Then: We land back at the home page
                var title = await Page.TitleAsync();
                Assert.AreEqual("Home - Development - YoFi", title);

                // And: The navbar has our email
                var content = await Page.TextContentAsync("data-test-id=hello-user");
                Assert.IsTrue(content.Contains(email));

                // And: The login button is not visible
                var login = await Page.QuerySelectorAsync("data-test-id=login");
                Assert.IsNull(login);

                // Save storage state into a file for later use            
                await Context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = ConfigFileName });

                // Set it as our new context options for later contexts
                _ContextOptions = new BrowserNewContextOptions { StorageStatePath = ConfigFileName };
            }
        }

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

        public async Task ThenIsOnPage(string expected)
        {
            var title = await Page.TitleAsync();
            Assert.AreEqual($"{expected} - Development - YoFi", title);
        }

        public async Task ThenH2Is(string expected)
        {
            var content = await Page.TextContentAsync("h2");
            Assert.AreEqual(expected, content);
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
            var amountcols = await Page.QuerySelectorAllAsync("th.report-col-amount");
            Assert.AreEqual(1, amountcols.Count);
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
            var amountcols = await Page.QuerySelectorAllAsync("th.report-col-amount");
            Assert.AreEqual(7, amountcols.Count);
        }
    }
}
