using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    }
}
