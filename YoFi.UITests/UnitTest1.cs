using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Reflection;

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

        [TestMethod]
        public void Login()
        {
            // Get user secrets for login

            var config = new ConfigurationBuilder().AddUserSecrets(Assembly.GetAssembly(typeof(UnitTest1))).Build();

            var email = config["AdminUser:Email"];
            var password = config["AdminUser:Password"];

            // Navigate to login page from top

            _driver.Navigate().GoToUrl("http://localhost:50419");
            var login_link = _driver.FindElement(By.Id("a-login"));

            // Find the login fields
            
            // Type in the email and password

            // Click submit

            // Then: We should be logged in now

            login_link.Click();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _driver.Quit();
        }
    }
}
