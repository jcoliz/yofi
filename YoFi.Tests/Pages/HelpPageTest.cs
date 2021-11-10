using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoFi.AspNet.Pages;

namespace YoFi.Tests.Pages
{
    [TestClass]
    public class PageModelTest
    {
        HelpModel pagemodel;

        [TestInitialize]
        public void SetUp()
        {   
            pagemodel = new HelpModel();
        }

        [TestMethod]
        public void Empty()
        {
            Assert.IsNotNull(pagemodel);
        }
    }
}
