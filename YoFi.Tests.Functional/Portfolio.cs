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
        [TestMethod]
        public async Task _01_Home()
        {
            await Page.GotoAsync(Properties.Url + "Home");
            await Page.SaveScreenshotToAsync(TestContext);
        }

        [TestMethod]
        public async Task _02_Transactions_Index()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Transactions");
            await DismissHelpTest();

            await Page.SaveScreenshotToAsync(TestContext);
        }
        
        [TestMethod]
        public async Task _03_Transactions_ActionsMenu()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Transactions");
            await DismissHelpTest();
            await Page.ClickAsync("#dropdownMenuButtonAction");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        [TestMethod]
        public async Task _04_Transactions_HelpTopic()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Transactions");
            await DismissHelpTest();
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Help Topic");
            await Page.WaitForSelectorAsync("[data-test-id=\"help-topic-title\"]", new Microsoft.Playwright.PageWaitForSelectorOptions() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
            await Task.Delay(500);

            await Page.SaveScreenshotToAsync(TestContext);
        }

        [TestMethod]
        public async Task _05_Transactions_EditModal()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Transactions");
            await DismissHelpTest();
            await Page.ClickAsync("[aria-label=\"Edit\"]");
            await Task.Delay(500);

            await Page.SaveScreenshotToAsync(TestContext);
        }

        [TestMethod]
        public async Task _06_Transactions_EditPage()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Transactions");
            await DismissHelpTest();
            await Page.SearchFor("a=256992");
            var NextPage = await Page.RunAndWaitForPopupAsync(async () =>
            {
                await Page.ClickAsync("data-test-id=edit-splits");
            });

            await NextPage.SaveScreenshotToAsync(TestContext);
        }

        [TestMethod]
        public async Task _07_Transactions_Import()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Transactions/Import");
            await DismissHelpTest();

            await Page.SaveScreenshotToAsync(TestContext);
        }

        [TestMethod]
        public async Task _08_Transactions_Uploaded()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Transactions/Import");
            await DismissHelpTest();

            await Page.ClickAsync("[aria-label=\"Upload\"]");
            await Page.SetInputFilesAsync("[aria-label=\"Upload\"]", new[] { "SampleData/Test-Generator-GenerateUploadSampleData.xlsx" });
            await Page.ClickAsync("text=Upload");

            await Page.SaveScreenshotToAsync(TestContext);

            await Page.ClickAsync("text=Delete");
        }

        [TestMethod]
        public async Task _10_Reports_Summary()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Reports");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        [TestMethod]
        public async Task _11_Reports_ChooseAReport()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Report/all-summary?level=1");
            await Page.ClickAsync("text=Choose a Report");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        [TestMethod]
        public async Task _12_Reports_Depth()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Report/all-summary?level=1");
            await Page.ClickAsync("#dropdownMenuButtonLevel");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        [TestMethod]
        public async Task _13_Reports_ShowMonths()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Report/all-summary?level=1");
            await Page.ClickAsync("#dropdownMenuButtonColumns");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        [TestMethod]
        public async Task _14_Reports_Month()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Report/all-summary?level=1");
            await Page.ClickAsync("#dropdownMenuButtonMonth");

            await Page.SaveScreenshotToAsync(TestContext);
        }

        [TestMethod]
        public async Task _15_Reports_Year()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Report/all-summary?level=1");
            await Page.ClickAsync("#dropdownMenuButtonYear");

            await Page.SaveScreenshotToAsync(TestContext);
        }
        [TestMethod]
        public async Task _16_Reports_SideChart()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Report/income");
            await Page.WaitForSelectorAsync("table.report");
            await Task.Delay(1000);

            await Page.SaveScreenshotToAsync(TestContext);
        }
        [TestMethod]
        public async Task _17_Reports_TopChart()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Report/expenses-detail?month=7");
            await Page.WaitForSelectorAsync("table.report");
            await Task.Delay(1000);

            await Page.SaveScreenshotToAsync(TestContext);
        }
        [TestMethod]
        public async Task _18_Reports_3Level()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Report/all?level=3");
            await Page.WaitForSelectorAsync("table.report");
            await Task.Delay(1000);

            await Page.SaveScreenshotToAsync(TestContext);
        }
        [TestMethod]
        public async Task _20_Budget_Summary()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Budget");
            await Page.WaitForSelectorAsync("table.report");
            await Task.Delay(1000);

            await Page.SaveScreenshotToAsync(TestContext);
        }
        [TestMethod]
        public async Task _21_Budget_Edit()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "BudgetTxs");
            await DismissHelpTest();

            await Page.SaveScreenshotToAsync(TestContext);
        }
        [TestMethod]
        public async Task _30_Payees()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Payees");
            await DismissHelpTest();

            await Page.SaveScreenshotToAsync(TestContext);
        }
        [TestMethod]
        public async Task _31_Payees_BulkEdit()
        {
            await GivenLoggedIn();
            await Page.GotoAsync(Properties.Url + "Payees");
            await DismissHelpTest();
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Bulk Edit");

            await Page.SaveScreenshotToAsync(TestContext);
        }

    }
}
