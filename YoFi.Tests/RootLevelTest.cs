using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoFi.AspNet.Root;

namespace YoFi.Tests
{
    [TestClass]
    public class RootLevelTest
    {
        [TestMethod]
        public void StartupTest()
        {
            // We are just looking for successful execution, here.

            var webHost = Microsoft.AspNetCore.WebHost.CreateDefaultBuilder().UseStartup<Startup>().Build();
            Assert.IsNotNull(webHost);
        }

        [TestMethod]
        public void MainTest()
        {
            Program.Main(new string[] { "--norun" });
        }
    }
}
