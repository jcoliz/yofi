using Microsoft.Extensions.Configuration;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        [TestMethod]
        public async Task HomePage()
        {
            // When: Navigating to the root of the site
            await Page.GotoAsync("http://localhost:50419/");

            // Then: The home page loads
            var title = await Page.TitleAsync();
            Assert.AreEqual("Home - Development - YoFi", title);
        }

        [TestMethod]
        public async Task LoginPage()
        {
            // Given: Starting at the home page
            await HomePage();

            // When: Clicking on the login link
            await Page.ClickAsync("data-test-id=login");
            
            // Then: The loging page loads
            var title = await Page.TitleAsync();
            Assert.AreEqual("Login - Development - YoFi", title);
        }

        [TestMethod]
        public async Task DoLogin()
        {
            // Given: Starting at the Login Page
            await LoginPage();

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
        }

        [TestMethod]
        public async Task ClickTransactions()
        {
            // Given: Having logged in
            await DoLogin();

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
            // Given: Having logged in
            await DoLogin();

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
            // Given: Having logged in
            await DoLogin();

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
            // Given: Having logged in
            await DoLogin();

            // When: Clicking "Import" on the navbar
            await Page.ClickAsync("text=Import");

            // Then: We land at import page
            var title = await Page.TitleAsync();
            Assert.AreEqual("Import Transactions - Development - YoFi", title);
        }
    }
}
