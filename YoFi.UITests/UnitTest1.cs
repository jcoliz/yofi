using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace YoFi.UITests
{
    [TestClass]
    public class UnitTest1
    {
        IWebDriver _driver;

        [TestInitialize]
        public void SetUp()
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless");
            _driver = new ChromeDriver(chromeOptions);
        }
        [TestMethod]
        public void HomePage()
        {
            // Needs to match launchSettings.json
            _driver.Navigate().GoToUrl("http://localhost:50419");

            Assert.AreEqual("Home - Development - YoFi", _driver.Title);
        }

        [TestMethod]
        public void NavigateToLogin()
        {
            _driver.Navigate().GoToUrl("http://localhost:50419");

            var login_link = _driver.FindElement(By.Id("a-login"));

            login_link.Click();

            Assert.AreEqual("Login - Development - YoFi", _driver.Title);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _driver.Quit();
        }
    }
}
