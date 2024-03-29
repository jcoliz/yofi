﻿using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.AspNet.Tests.Functional
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

            await Page.WaitForSelectorAsync("[data-test-id=\"help-topic-title\"]", new PageWaitForSelectorOptions() { State = WaitForSelectorState.Visible });
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

            // Click the "..." icon on the first line to open the context menu
            await Page.ClickAsync("#actions-1");
            await Page.SaveScreenshotToAsync(TestContext, "Context Menu");

            // Click the "Edit" icon in the context menu
            await Page.ClickAsync("[data-test-id=edit]");

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
                // Click the "..." icon on the first line to open the context menu
                await Page.ClickAsync("#actions-1");
                await Page.SaveScreenshotToAsync(TestContext, "Context Menu");

                // Click the "Edit" icon in the context menu
                await Page.ClickAsync("data-test-id=edit-splits");
            });

            await NextPage.SaveScreenshotToAsync(TestContext);

            // Click the "..." icon on the first split line to open the context menu
            await NextPage.ClickAsync("#actions-1");
            await NextPage.SaveScreenshotToAsync(TestContext, "Context Menu");

        }

        /// <summary>
        /// Transactions import mechanism
        /// </summary>
        [TestMethod]
        public async Task _07_Transactions_Import()
        {
            // Navigate to https://www.try-yofi.com/Transactions/Import
            await NavigateToAsync("Import");

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
            // Navigate to https://www.try-yofi.com/Import
            await NavigateToAsync("Import");

            // Dismiss the help text, if appears
            await DismissHelpTest();
            await Page.SaveScreenshotToAsync(TestContext, "Ready");

            // Click the "Browse" button on the file control
            await Page.ClickAsync("[aria-label=\"Upload\"]");

            // Select a file from your computer to upload. 
            // For best results, use the sample file from GitHub 
            // https://github.com/jcoliz/yofi/blob/3c89902051fd34a3eb43ed2ea2dfa86f2b04d393/YoFi.Tests.Functional/SampleData/SampleData-2022-Upload-Tx.xlsx
            await Page.SetInputFilesAsync("[aria-label=\"Upload\"]", new[] { "SampleData/SampleData-2022-Upload-Tx.xlsx" });
            await Page.SaveScreenshotToAsync(TestContext, "Set Files");

            // Click the "Upload" button
            // (Note that this requires a validated account to accomplish)
            await Page.ClickAsync("text=\"Upload\"");

            await Page.SaveScreenshotToAsync(TestContext,"Final");

            // To clean up, click the "Delete" button
            await Page.ClickAsync("data-test-id=btn-delete");
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
            await Page.ClickInMenuAsync("[aria-label=\"Toggle page navigation\"]", "#dropdownMenuButtonLevel");

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
            await Page.ClickInMenuAsync("[aria-label=\"Toggle page navigation\"]", "#dropdownMenuButtonColumns");

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
            await Page.ClickInMenuAsync("[aria-label=\"Toggle page navigation\"]", "#dropdownMenuButtonMonth");

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
            await Page.ClickInMenuAsync("[aria-label=\"Toggle page navigation\"]", "#dropdownMenuButtonYear");

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

            // Click the "..." icon on the first line to open the context menu
            await Page.ClickAsync("#actions-1");
            await Page.SaveScreenshotToAsync(TestContext, "Context Menu");
        }

        /// <summary>
        /// Budget editor, bulk edit mechanism. Other views use a similar mechanism
        /// </summary>
        [TestMethod]
        public async Task _22_Budget_Edit_BulkEdit()
        {
            // Navigate to https://www.try-yofi.com/BudgetTxs
            await NavigateToAsync("BudgetTxs");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            // Click "Actions" in the page navbar
            await Page.ClickAsync("#dropdownMenuButtonAction");

            // Click "Bulk Edit" in the actions dropdown
            await Page.ClickAsync("text=Bulk Edit");

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

            // Click the "..." icon on the first line to open the context menu
            await Page.ClickAsync("#actions-1");
            await Page.SaveScreenshotToAsync(TestContext, "Context Menu");
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

        /// <summary>
        /// Central receipt uploader
        /// </summary>
        [TestMethod]
        public async Task _50_Receipts_Upload()
        {
            // Navigate to https://www.try-yofi.com/Receipts
            await NavigateToAsync("Receipts");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        /// <summary>
        /// List of uploaded receipts
        /// </summary>
        [TestMethod]
        public async Task _51_Receipts_Index()
        {
            // Navigate to https://www.try-yofi.com/Receipts
            await NavigateToAsync("Receipts");

            // Dismiss the help text, if appears
            await DismissHelpTest();

            // Upload several receipts with correct naming
            var receipts = new string[]
            {
                // Matches none
                $"Create Me $12.34 12-21 {testmarker}.png",
                // Matches exactly one at 200 (name and amount), but will also match 3 others at 100 (name only)
                $"Olive Garden $130.85 {testmarker}.png",
                // Matches exactly one
                $"Waste Management 12-27 {testmarker}.png",
                // Matches many
                $"Uptown Espresso ({testmarker}).png",
            };

            // Conjure up some bytes (the bytes don't really matter)
            byte[] bytes = Enumerable.Range(0, 255).Select(i => (byte)i).ToArray();

            // Make file payloads out of them
            var payloads = receipts.Select(x =>
                new FilePayload()
                {
                    Name = x,
                    MimeType = "image/png",
                    Buffer = bytes
                }
            );

            // Now upload them
            await Page.ClickAsync("[aria-label=Upload]");
            await Page.SetInputFilesAsync("[aria-label=Upload]", payloads);
            await Page.ClickAsync("data-test-id=btn-create-receipt");

            await Page.SaveScreenshotToAsync(TestContext);

            // Clean up by deleting each one
            for(int i=1; i<=4; i++)
            {
                await Page.ClickAsync("#actions-1");
                await Page.ClickAsync("button[value=Delete]");
            }
        }
    }
}