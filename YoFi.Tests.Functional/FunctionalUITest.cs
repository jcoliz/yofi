using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Threading.Tasks;

// Currently shaves off all of 4 seconds to run them in parallel!
// Two or three workers seems to yield the optimum results.
[assembly: Parallelize(Workers = 3, Scope = ExecutionScope.ClassLevel)]

namespace YoFi.Tests.Functional
{
    public class FunctionalUITest: PageTest
    {

        public override BrowserNewContextOptions ContextOptions => _ContextOptions;

        private static BrowserNewContextOptions _ContextOptions { get; set; } = new BrowserNewContextOptions { AcceptDownloads = true };

        protected readonly string Site = "http://localhost:50419/";

        protected FunctionalUITest() { }

        protected async Task GivenLoggedIn()
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
                var config = new ConfigurationBuilder().AddUserSecrets(Assembly.GetAssembly(typeof(FunctionalUITest))).Build();
                var email = config["AdminUser:Email"];
                var password = config["AdminUser:Password"];

                // When: Filling out the login form with those credentials and pressing "sign in"
                await Page.FillAsync("id=floatingInput", email);
                await Page.FillAsync("id=floatingPassword", password);
                await Page.ClickAsync("data-test-id=signin");

                // Then: We land back at the home page
                await ThenIsOnPage("Home");

                // And: The navbar has our email
                var content = await Page.TextContentAsync("data-test-id=hello-user");
                Assert.IsTrue(content.Contains(email));

                // And: The login button is not visible
                var login = await Page.QuerySelectorAsync("data-test-id=login");
                Assert.IsNull(login);

                // Save storage state into a file for later use            
                var ConfigFileName = $"{TestContext.FullyQualifiedTestClassName}.loginstate.json";
                await Context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = ConfigFileName });

                // Set it as our new context options for later contexts
                _ContextOptions = new BrowserNewContextOptions { StorageStatePath = ConfigFileName, AcceptDownloads = true };
            }
        }
        protected async Task ThenIsOnPage(string expected)
        {
            var title = await Page.TitleAsync();
            Assert.AreEqual($"{expected} - Development - YoFi", title);
        }

        protected async Task ThenPageContainsItems(int from, int to)
        {
            Assert.AreEqual(from.ToString(), await Page.TextContentAsync("data-test-id=firstitem"));
            Assert.AreEqual(to.ToString(), await Page.TextContentAsync("data-test-id=lastitem"));
        }

        protected async Task ThenTotalItemsAreEqual(int howmany)
        {
            Assert.AreEqual(howmany.ToString(), await Page.TextContentAsync("data-test-id=totalitems"));
        }

        protected async Task ThenH2Is(string expected)
        {
            var content = await Page.TextContentAsync("h2");
            Assert.AreEqual(expected, content);
        }

    }
}
