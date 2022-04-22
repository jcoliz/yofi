using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace YoFi.AspNet.Tests.Functional
{
    [TestClass]
    public class HelpUITest: FunctionalUITest
    {
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
            var topics = await Page.Locator("data-test-id=help-topic-title").CountAsync();
            
            // Flexible to not break this test if we add topics
            Assert.IsTrue(topics >= 3);
        }

        /// <summary>
        /// User Story 1174: [User can] View page-specific help when desired
        /// </summary>
        [DataRow("Budget/Edit Budget")]
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
            await Page.SaveScreenshotToAsync(TestContext, $"{page} help");
            var text = await element.TextContentAsync();
            Assert.IsTrue(text.Contains(page[..3]));

            // When: Clicking OK
            await Page.ClickAsync("[data-test-id=\"btn-help-close\"]");
            await Page.SaveScreenshotToAsync(TestContext,$"{page} ok");

            // Then: The help topic is dismissed
            element = await Page.WaitForSelectorAsync("[data-test-id=\"help-topic-title\"]", new Microsoft.Playwright.PageWaitForSelectorOptions() { State = Microsoft.Playwright.WaitForSelectorState.Hidden });
            Assert.IsNull(element);

            // And: The dialog is completely removed
            var backdrop = await Page.WaitForSelectorAsync(".modal-backdrop", new Microsoft.Playwright.PageWaitForSelectorOptions() { State = Microsoft.Playwright.WaitForSelectorState.Hidden });
            Assert.IsNull(backdrop);

            await Page.SaveScreenshotToAsync(TestContext, $"{page} done");
        }

        /// <summary>
        /// User Story 1180: [User Can] Quickly navigate from a page's help topic to more details on that topic
        /// </summary>
        /// <param name="page">Which page to test on </param>
        [DataRow("Budget/Edit Budget")]
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

            // When: Clicking "More..."
            var NextPage = await Page.RunAndWaitForPopupAsync(async () =>
            {
                await Page.ClickAsync("[data-test-id=\"btn-help-more\"]");
            });

            // Then: Is on Help page
            await NextPage.ThenIsOnPageAsync("Help");
            await NextPage.SaveScreenshotToAsync(TestContext, page);

            // And: There are lots of topics
            var topics = await NextPage.Locator("data-test-id=help-topic-title").CountAsync();

            // Flexible to not break this test if we add topics
            Assert.IsTrue(topics >= 3);

            // And: This topic is highlighted
            var id = await NextPage.Locator("[data-test-id=highlight]").GetAttributeAsync("id");
            Assert.AreEqual(page.ToLower()[..3], id[..3]);
        }

        /// <summary>
        /// User Story 1196: [User Can] Receive necessary context when clicking "Try the demo" on the Home page Given: On the Home page
        /// </summary>
        /// <returns></returns>
        
        // NOTE: We don't run in demo mode anymore so we're not able to test this.
#if false
        public async Task DemoHelp()
        {
            /*
            Given: On the Home page
            And: Not on a branded site (e.g. demo or eval)
            When: Clicking "Try the demo"
            Then: User is given a help prompt on the next page explaining this is a demo
            */

            // Given: Is a demo site
            if (TestContext.Properties.Contains("demo"))
                if (bool.TryParse(TestContext.Properties["demo"] as string, out var demo))
                    if (!demo)
                        return;
               
            // Given: We are already logged in and starting at the root of the site
            // (Logged in needed so we can actually get to transactions page)
            await GivenLoggedIn();

            // And: Starting at the home page
            var site = TestContext.Properties["webAppUrl"] as string;
            await Page.GotoAsync(site + "Home");
            await Page.SaveScreenshotToAsync(TestContext);

            // When: Clicking on "Try the demo"
            await Page.ClickAsync("text=Try the demo");

            // Then: User is given a help prompt on the next page explaining this is a demo
            var element = await Page.WaitForSelectorAsync("[data-test-id=\"help-topic-title\"]");
            await Page.SaveScreenshotToAsync(TestContext);
            var text = await element.TextContentAsync();
            Assert.IsTrue(text.ToLowerInvariant().Contains("demo"));

            // When: Clicking on "Go"
            await Page.ClickAsync("div[role=\"document\"] >> text=Go");

            // Then: User is taken to Transactions page
            await Page.ThenIsOnPageAsync("Transactions");
        }
#endif        
    }
}
