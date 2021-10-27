using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    /// <summary>
    /// Test the Transactions page
    /// </summary>
    [TestClass]
    public class TransactionsUITest : FunctionalUITest
    {
        [TestMethod]
        public async Task ClickTransactions()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Transactions" on the navbar
            await Page.ClickAsync("text=Transactions");

            // Then: We land at the transactions index page
            await ThenIsOnPage("Transactions");
        }

        [TestMethod]
        public async Task IndexQAny12()
        {
            // Given: We are logged in and on the transactions page
            await ClickTransactions();

            // When: Searching for "Farquat"
            await Page.FillAsync("data-test-id=q", "Farquat");
            await Page.ClickAsync("data-test-id=btn-search");

            // Then: Exactly 12 transactions are found, because we know this about our source data
            await ThenTotalItemsAreEqual(12);
        }

        [TestMethod]
        public async Task IndexQAnyDate()
        {
            // Given: We are logged in and on the transactions page
            await ClickTransactions();

            // When: Searching for "Farquat"
            await Page.FillAsync("data-test-id=q", "1230");
            await Page.ClickAsync("data-test-id=btn-search");

            // Then: Exactly 3 transactions are found, because we know this about our source data
            await ThenTotalItemsAreEqual(3);
        }

        [TestMethod]
        public async Task IndexClear()
        {
            // Given: We are logged in and on the transactions page, with an active search
            await IndexQAny12();

            // When: Pressing clear
            await Page.ClickAsync("data-test-id=btn-clear");

            // Then: Back to all the items
            await ThenTotalItemsAreEqual(889);
        }

        /* Not sure how to assert this
        [TestMethod]
        public async Task HelpPopup()
        {
            // Given: We are logged in and on the transactions page
            await ClickTransactions();

            // When: Hitting the help button
            await Page.ClickAsync("data-test-id=btn-help");

            // Then: The help text is findable
            var title = await Page.QuerySelectorAsync("id=searchHelpModal");
            Assert.IsNotNull(title);
            Assert.IsTrue(await title.IsEnabledAsync());
        }

        [TestMethod]
        public async Task HelpPopupClose()
        {
            // Given: We have the help popup active
            await HelpPopup();

            // When: Hitting the close button
            await Page.ClickAsync("text=Close");

            // Then: The help text is not findable
            var title = await Page.QuerySelectorAsync("data-test-id=help-title");
            Assert.IsFalse(await title.IsEnabledAsync());
        }
        */
    }
}
