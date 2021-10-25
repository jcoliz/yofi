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
            await Page.GotoAsync("http://localhost:50419/");

            var title = await Page.TitleAsync();

            Assert.AreEqual("Home - Development - YoFi", title);
        }

        [TestMethod]
        public async Task LoginPage()
        {
            await HomePage();

            await Page.ClickAsync("text=Log in");

            var title = await Page.TitleAsync();

            Assert.AreEqual("Login - Development - YoFi", title);
        }

        [TestMethod]
        public async Task DoLogin()
        {
            // Get user secrets for login

            var config = new ConfigurationBuilder().AddUserSecrets(Assembly.GetAssembly(typeof(SmokeTest))).Build();

            var email = config["AdminUser:Email"];
            var password = config["AdminUser:Password"];

            await LoginPage();

            // Fill [placeholder="name@example.com"]
            await Page.FillAsync("[placeholder=\"name@example.com\"]", email);

            // Fill [placeholder="Password"]
            await Page.FillAsync("[placeholder=\"Password\"]", password);

            // Click button:has-text("Sign in")
            await Page.ClickAsync("button:has-text(\"Sign in\")");

            var title = await Page.TitleAsync();
            Assert.AreEqual("Home - Development - YoFi", title);
        }
    }
}
