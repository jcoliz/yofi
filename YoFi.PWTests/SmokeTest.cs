using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace YoFi.PWTests
{
    /// <summary>
    /// Run through top-level golden path to make sure primary pages load
    /// </summary>
    [TestClass]
    public class SmokeTest: PageTest
    {

        public override BrowserNewContextOptions ContextOptions => _ContextOptions;

        private static BrowserNewContextOptions _ContextOptions { get; set; }

        private readonly string Site = "http://localhost:50419/";

        [TestMethod]
        public async Task AAA_HomePage()
        {
            // Given: An empty context, where we are not logged in
            // (This is accomplished by ordering this test before the login test)

            // When: Navigating to the root of the site
            await Page.GotoAsync(Site);

            // Then: The home page loads
            var title = await Page.TitleAsync();
            Assert.AreEqual("Home - Development - YoFi", title);
        }

        [TestMethod]
        public async Task AAB_LoginPage()
        {
            // Given: Starting at the home page, not logged in
            await AAA_HomePage();

            // When: Clicking on the login link
            await Page.ClickAsync("data-test-id=login");
            
            // Then: The loging page loads
            var title = await Page.TitleAsync();
            Assert.AreEqual("Login - Development - YoFi", title);
        }

        [TestMethod]
        public async Task AAC_LoginAction()
        {
            // Given: Starting at the Login Page, not logged in
            await AAB_LoginPage();

            // And: User credentials as specified in user secrets
            var config = new ConfigurationBuilder().AddUserSecrets(Assembly.GetAssembly(typeof(SmokeTest))).Build();
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
            await Context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = "loginstate.json" });
            
            // Set it as our new context options for later contexts
            _ContextOptions = new BrowserNewContextOptions { StorageStatePath = "loginstate.json" };
        }

        private async Task GivenLoggedIn()
        {
            // Navigate to the root of the site
            await Page.GotoAsync(Site);

            // Are we already logged in?
            var login = await Page.QuerySelectorAsync("data-test-id=hello-user");

            // If we're not already logged in, well we need to do that then
            if (null == login)
            {
                Console.WriteLine("Logging in...");
                await AAC_LoginAction();
            }
        }

        [TestMethod]
        public async Task ClickTransactions()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Transactions" on the navbar
            await Page.ClickAsync("text=Transactions");

            // Then: We land at the transactions index page
            var title = await Page.TitleAsync();
            Assert.AreEqual("Transactions - Development - YoFi", title);

            // And: There are the expected number of transactions.
            // NOTE: This will change if the sample data pattern definitions change
            // This may be somewhat brittle. Consider changing this to a range
            var expected = "889";
            var content = await Page.TextContentAsync("data-test-id=totalitems");
            Assert.AreEqual(expected,content);
        }

        [TestMethod]
        public async Task ClickPayees()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Payees" on the navbar
            await Page.ClickAsync("text=Payees");

            // Then: We land at the payees index page
            var title = await Page.TitleAsync();
            Assert.AreEqual("Payees - Development - YoFi", title);

            // And: There are the expected number of items.
            // NOTE: This will change if the sample data pattern definitions change
            // This may be somewhat brittle. Consider changing this to a range
            var expected = "40";
            var content = await Page.TextContentAsync("data-test-id=totalitems");
            Assert.AreEqual(expected, content);
        }

        [TestMethod]
        public async Task ClickBudget()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Budget" on the navbar
            await Page.ClickAsync("text=Budget");

            // Then: We land at the budget index page
            var title = await Page.TitleAsync();
            Assert.AreEqual("Budget Line Items - Development - YoFi", title);

            // And: There are the expected number of items.
            // NOTE: This will change if the sample data pattern definitions change
            // This may be somewhat brittle. Consider changing this to a range
            var expected = "156";
            var content = await Page.TextContentAsync("data-test-id=totalitems");
            Assert.AreEqual(expected, content);
        }

        [TestMethod]
        public async Task ClickImport()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "Import" on the navbar
            await Page.ClickAsync("text=Import");

            // Then: We land at import page
            var title = await Page.TitleAsync();
            Assert.AreEqual("Import Transactions - Development - YoFi", title);
        }

        [TestMethod]
        public async Task ClickProfile()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking my email on the navbar
            await Page.ClickAsync("data-test-id=hello-user");

            // Then: We land at profile page
            var title = await Page.TitleAsync();
            Assert.AreEqual("Profile - Development - YoFi", title);
        }

        [TestMethod]
        public async Task ZZZ_LogOut()
        {
            // Given: We are already logged in and starting at the root of the site
            await GivenLoggedIn();

            // When: Clicking "logout" on the navbar
            await Page.ClickAsync("data-test-id=logout");

            // Then: We land at home page
            var title = await Page.TitleAsync();
            Assert.AreEqual("Home - Development - YoFi", title);

            // And: The login button is again visible
            var login = await Page.QuerySelectorAsync("data-test-id=login");
            Assert.IsNotNull(login);
            var text = await login.TextContentAsync();
            Assert.AreEqual("Log in", text);
        }
    }
}
