using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace YoFi.PWTests
{
    /// <summary>
    /// Test the Budget page
    /// </summary>
    [TestClass]
    public class BudgetUITest : FunctionalUITest
    {
        [TestMethod]
        public async Task ClickBudget()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync("text=Budget");

            // Then: We land at the budget index page
            await ThenIsOnPage("Budget Line Items");

            // And: This page covers items 1-25
            await ThenPageContainsItems(from: 1, to: 25);
        }

        [TestMethod]
        public async Task Page2()
        {
            // Given: We are logged in and on the budget page
            await ClickBudget();

            // When: Clicking on the next page on the pagination control
            await Page.ClickAsync("data-test-id=nextpage");

            // Then: We are still on the budget index page
            await ThenIsOnPage("Budget Line Items");

            // And: This page covers items 26-50
            await ThenPageContainsItems(from: 26, to: 50);
        }

        [TestMethod]
        public async Task IndexQ25()
        {
            // Given: We are logged in and on the budget page
            await ClickBudget();

            // When: Searching for "Farquat"
            await Page.FillAsync("data-test-id=q", "Healthcare");
            await Page.ClickAsync("data-test-id=btn-search");

            // Then: Exactly 25 transactions are found, because we know this about our source data
            await ThenTotalItemsAreEqual(25);
        }

    }
}
