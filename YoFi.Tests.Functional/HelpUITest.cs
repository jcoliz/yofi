using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YoFi.Tests.Functional
{
    [TestClass]
    public class HelpUITest: FunctionalUITest
    {
        private async Task WhenNavigatingToPage(string page)
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync($"text={page}");
        }

        /// <summary>
        /// User Story 1175: [User Can] View all help topics on a single page
        /// </summary>
        [TestMethod]
        public async Task SinglePage()
        {
            /*
            When: Clicking "Help" on the nav bar
            Then: A single page is displayed with all help topics in sequence
            */

            // When: On Help
            await WhenNavigatingToPage("Help");

            // Then: Is on Help page
            await Page.ThenIsOnPageAsync("Help");

            // And: There are lots of topics
            var topics = await Page.QuerySelectorAllAsync("data-test-id=help-topic-title");
            
            // Flexible to not break this test if we add topics
            Assert.IsTrue(topics.Count >= 3);
        }

        /// <summary>
        /// User Story 1174: [User can] View page-specific help when desired
        /// </summary>
        [DataRow("Budget")]
        [DataRow("Payees")]
        [DataRow("Import")]
        [DataRow("Transactions")]
        [DataTestMethod]
        public async Task HelpTopic(string page)
        {
            /*
                Given: On each page in {Transactions, Import, Budget, Payees}
                When: Clicking Actions > Help
                Then: The help topic specific to this page is displayed
                When: Clicking OK
                Then: The help topic is dismissed
             */

            // Given: On {page}
            await WhenNavigatingToPage(page);

            // When: Clicking Actions > Help
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Help Topic");

            // Then: The help topic specific to this page is displayed
            var element = await Page.WaitForSelectorAsync("[data-test-id=\"help-topic-title\"]");
            await Page.SaveScreenshotToAsync(TestContext);
            var text = await element.TextContentAsync();
            Assert.IsTrue(text.Contains(page[..3]));

            // When: Clicking OK
            await Page.ClickAsync("[data-test-id=\"btn-help-close\"]");
            await Page.SaveScreenshotToAsync(TestContext);

            // Then: The help topic is dismissed
            element = await Page.WaitForSelectorAsync("[data-test-id=\"help-topic-title\"]", new Microsoft.Playwright.PageWaitForSelectorOptions() { State = Microsoft.Playwright.WaitForSelectorState.Hidden });
            Assert.IsNull(element);
        }

        /// <summary>
        /// User Story 1180: [User Can] Quickly navigate from a page's help topic to more details on that topic
        /// </summary>
        /// <param name="page">Which page to test on </param>
        [DataRow("Budget")]
        [DataRow("Payees")]
        [DataRow("Import")]
        [DataRow("Transactions")]
        [DataTestMethod]
        public async Task MoreButton(string page)
        {
            /*
            Given: On each page in {Transactions, Import, Budget, Payees}
            And: Clicking Actions > Help
            When: Clicking "More..."
            Then: All help topics are displayed on a single page, in a new window
            And: The view is scrolled to the expanded help topic where we just clicked from 
            */

            // Given: On {page}
            await WhenNavigatingToPage(page);

            // And: Clicking Actions > Help
            await Page.ClickAsync("#dropdownMenuButtonAction");
            await Page.ClickAsync("text=Help Topic");
            await Page.WaitForSelectorAsync("[data-test-id=\"help-topic-title\"]");
            await Page.SaveScreenshotToAsync(TestContext);

            // When: Clicking "More..."
            var NextPage = await Page.RunAndWaitForPopupAsync(async () =>
            {
                await Page.ClickAsync("[data-test-id=\"btn-help-more\"]");
            });

            // Then: Is on Help page
            await NextPage.ThenIsOnPageAsync("Help");

            // And: There are lots of topics
            var topics = await NextPage.QuerySelectorAllAsync("data-test-id=help-topic-title");

            // Flexible to not break this test if we add topics
            Assert.IsTrue(topics.Count >= 3);

            // And: This topic is highlighted
            var element = await NextPage.QuerySelectorAsync("[data-test-id=\"highlight\"]");

            Assert.AreEqual(page.ToLower()[..3], (await element.GetAttributeAsync("id"))[..3]);
        }
    }
}
