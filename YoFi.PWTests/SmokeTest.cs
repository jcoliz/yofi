using Microsoft.Extensions.Configuration;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading.Tasks;

namespace YoFi.PWTests
{
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
    }
}
