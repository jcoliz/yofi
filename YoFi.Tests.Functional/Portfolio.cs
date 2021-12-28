using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    /// <summary>
    /// This test generates a portfolio of screen shots to be included in the visual refresh
    /// </summary>
    [TestClass]
    public class Portfolio: FunctionalUITest
    {
        private async Task NavigateToAsync(string fragment)
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + fragment);
        }

        /// <summary>
        /// Home page for new visitors
        /// </summary>
        [TestMethod]
        public async Task _01_Home()
        {
            // Navigate to https://www.try-yofi.com/Home
            await NavigateToAsync("Home");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Home page for new visitors, second carousel slide
        /// </summary>
        [TestMethod]
        public async Task _01A_Home_Carousel2()
        {
            // Navigate to https://www.try-yofi.com/Home
            await NavigateToAsync("Home");

            // Click the second carousel tab
            await Page.ClickAsync("[aria-label=\"Slide 2\"]");

            await Task.Delay(1000);
            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Home page for new visitors, third carousel slide
        /// </summary>
        [TestMethod]
        public async Task _01B_Home_Carousel3()
        {
            // Navigate to https://www.try-yofi.com/Home
            await NavigateToAsync("Home");

            // Click the third carousel tab
            await Page.ClickAsync("[aria-label=\"Slide 3\"]");

            await Task.Delay(1000);
            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Main page showing all transactions, one page at a time
        /// </summary>
        [TestMethod]
        public async Task _02_Transactions_Index()
        {
            // Navigate to https://www.try-yofi.com/Transactions
            await NavigateToAsync("Transactions");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            await Page.SaveScreenshotToAsync(TestContext);
        }
        
        /// <summary>
        /// Actions menu on Transactions Index. Other pages have a similar menu
        /// </summary>
        [TestMethod]
        public async Task _03_Transactions_ActionsMenu()
        {
            // Navigate to https://www.try-yofi.com/Transactions
            await NavigateToAsync("Transactions");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            // Click "Actions" in the page navbar
            await Page.ClickAsync("#dropdownMenuButtonAction");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Help Topic on Transactions Index. Other pages have a similar dialog
        /// </summary>
        [TestMethod]
        public async Task _04_Transactions_HelpTopic()
        {
            // Navigate to https://www.try-yofi.com/Transactions
            await NavigateToAsync("Transactions");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            // Click "Actions" in the page navbar
            await Page.ClickAsync("#dropdownMenuButtonAction");

            // Click "Help Topic" in the actions dropdown
            await Page.ClickAsync("text=Help Topic");

            await Page.WaitForSelectorAsync("[data-test-id=\"help-topic-title\"]", new Microsoft.Playwright.PageWaitForSelectorOptions() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
            await Task.Delay(500);
            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Modal Edit on Transactions Index. Other pages have a similar edit mechanism
        /// </summary>
        [TestMethod]
        public async Task _05_Transactions_EditModal()
        {
            // Navigate to https://www.try-yofi.com/Transactions
            await NavigateToAsync("Transactions");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            // Click the "Edit" icon on the first line
            await Page.ClickAsync("[aria-label=\"Edit\"]");

            await Task.Delay(500);
            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Edit Page from Transactions Index. Other pages have a similar edit mechanism
        /// </summary>
        [TestMethod]
        public async Task _06_Transactions_EditPage()
        {
            // Navigate to https://www.try-yofi.com/Transactions
            await NavigateToAsync("Transactions");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            // Search for "a=256992", which will find transactions which have splits
            await Page.SearchFor("a=256992");
            var NextPage = await Page.RunAndWaitForPopupAsync(async () =>
            {
                // Click the "Edit" icon on the first line
                await Page.ClickAsync("data-test-id=edit-splits");
            });

            await NextPage.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Transactions import mechanism
        /// </summary>
        [TestMethod]
        public async Task _07_Transactions_Import()
        {
            // Navigate to https://www.try-yofi.com/Transactions/Import
            await NavigateToAsync("Transactions/Import");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Transactions uploaded, reviewing for user approval
        /// </summary>
        [TestMethod]
        public async Task _08_Transactions_Uploaded()
        {
            // Navigate to https://www.try-yofi.com/Transactions/Import
            await NavigateToAsync("Transactions/Import");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            // Click the "Browse" button on the file control
            await Page.ClickAsync("[aria-label=\"Upload\"]");

            // Select a file from your computer to upload. 
            // For best results, use the sample file from GitHub 
            // https://github.com/jcoliz/yofi/blob/3c89902051fd34a3eb43ed2ea2dfa86f2b04d393/YoFi.Tests.Functional/SampleData/Test-Generator-GenerateUploadSampleData.xlsx
            await Page.SetInputFilesAsync("[aria-label=\"Upload\"]", new[] { "SampleData/Test-Generator-GenerateUploadSampleData.xlsx" });

            // Click the "Upload" button
            // (Note that this requires a validated account to accomplish)
            await Page.ClickAsync("text=Upload");

            await Page.SaveScreenshotToAsync(TestContext);

            // To clean up, click the "Delete" button
            await Page.ClickAsync("text=Delete");
        }

        /// <summary>
        /// Actions menu on Transactions Index. Other pages have a similar menu
        /// </summary>
        [TestMethod]
        public async Task _09_Transactions_BulkEdit()
        {
            // Navigate to https://www.try-yofi.com/Transactions
            await NavigateToAsync("Transactions");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            // Click "Actions" in the page navbar
            await Page.ClickAsync("#dropdownMenuButtonAction");

            // Click "Bulk Edit" in the actions dropdown
            await Page.ClickAsync("text=Bulk Edit");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Main page of reports. Shows user an overview of their income/spending/saving on a single screen
        /// </summary>
        [TestMethod]
        public async Task _10_Reports_Summary()
        {
            // Navigate to https://www.try-yofi.com/Reports
            await NavigateToAsync("Reports");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Reports menu: Choose a different report
        /// </summary>
        [TestMethod]
        public async Task _11_Reports_ChooseAReport()
        {
            // Navigate to https://www.try-yofi.com/Report/all-summary?level=1
            await NavigateToAsync("Report/all-summary?level=1");

            // Click "Choose a Report" in the page navbar
            await Page.ClickAsync("text=Choose a Report");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Reports menu: Set the depth of the current report
        /// </summary>
        [TestMethod]
        public async Task _12_Reports_Depth()
        {
            // Navigate to https://www.try-yofi.com/Report/all-summary?level=1
            await NavigateToAsync("Report/all-summary?level=1");

            // Click "Depth" in the page navbar
            await Page.ClickAsync("#dropdownMenuButtonLevel");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Reports menu: Choose whether current report shows month columns or just totals
        /// </summary>
        [TestMethod]
        public async Task _13_Reports_ShowMonths()
        {
            // Navigate to https://www.try-yofi.com/Report/all-summary?level=1
            await NavigateToAsync("Report/all-summary?level=1");

            // Click "Show Months" in the page navbar
            await Page.ClickAsync("#dropdownMenuButtonColumns");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Reports menu: Set which month of the selected year does the report cover
        /// </summary>
        [TestMethod]
        public async Task _14_Reports_Month()
        {
            // Navigate to https://www.try-yofi.com/Report/all-summary?level=1
            await NavigateToAsync("Report/all-summary?level=1");

            // Click "Month" in the page navbar
            await Page.ClickAsync("#dropdownMenuButtonMonth");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Reports menu: Set which year does the report cover
        /// </summary>
        [TestMethod]
        public async Task _15_Reports_Year()
        {
            // Navigate to https://www.try-yofi.com/Report/all-summary?level=1
            await NavigateToAsync("Report/all-summary?level=1");

            // Click "Year" in the page navbar
            await Page.ClickAsync("#dropdownMenuButtonYear");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Example of a report with a pie chart to the side
        /// </summary>
        [TestMethod]
        public async Task _16_Reports_SideChart()
        {
            // Navigate to https://www.try-yofi.com/Report/expenses
            await NavigateToAsync("Report/expenses");

            // Wait for the page to load fully in the background
            await Page.WaitForSelectorAsync("table.report");
            await Task.Delay(1000);

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Example of a report with line chart above
        /// </summary>
        [TestMethod]
        public async Task _17_Reports_TopChart()
        {
            // Navigate to https://www.try-yofi.com/Report/expenses-detail?month=7
            await NavigateToAsync("Report/expenses-detail?month=7");

            // Wait for the page to load fully in the background
            await Page.WaitForSelectorAsync("table.report");
            await Task.Delay(1000);

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Example of a report with three levels of depth
        /// </summary>
        [TestMethod]
        public async Task _18_Reports_3Level()
        {
            // Navigate to https://www.try-yofi.com/Report/all?level=3
            await NavigateToAsync("Report/all?level=3");

            // Wait for the page to load fully in the background
            await Page.WaitForSelectorAsync("table.report");
            await Task.Delay(1000);

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// The summary of users' progress against their budget sofar this year
        /// </summary>
        [TestMethod]
        public async Task _20_Budget_Summary()
        {
            // Navigate to https://www.try-yofi.com/Budget
            await NavigateToAsync("Budget");

            // Wait for the page to load fully in the background
            await Page.WaitForSelectorAsync("table.report");
            await Task.Delay(1000);

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Budget editor, where user can view, edit, or delete line items in their budget
        /// </summary>
        [TestMethod]
        public async Task _21_Budget_Edit()
        {
            // Navigate to https://www.try-yofi.com/BudgetTxs
            await NavigateToAsync("BudgetTxs");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Payee matching rules, where user can view, edit or delete the rules whereby categories are
        /// automatically assigned based on payee of a transaction
        /// </summary>
        [TestMethod]
        public async Task _30_Payees()
        {
            // Navigate to https://www.try-yofi.com/Payees
            await NavigateToAsync("Payees");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// Bulk edit mechanism. Other views use a similar mechanism
        /// </summary>
        [TestMethod]
        public async Task _31_Payees_BulkEdit()
        {
            // Navigate to https://www.try-yofi.com/Payees
            await NavigateToAsync("Payees");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            // Click "Actions" in the page navbar
            await Page.ClickAsync("#dropdownMenuButtonAction");

            // Click "Bulk Edit" in the actions dropdown
            await Page.ClickAsync("text=Bulk Edit");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// HTTP Error Page
        /// </summary>
        [TestMethod]
        public async Task _40_HttpError()
        {
            // Navigate to https://www.try-yofi.com/StatusCode?e=404
            await NavigateToAsync("StatusCode?e=404");

            await Page.SaveScreenshotToAsync(TestContext);
        }
    }
}