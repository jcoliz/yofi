using Common.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YoFi.AspNet.Pages;

namespace YoFi.Tests.Pages
{
    [TestClass]
    public class HomePageTest
    {
        [TestMethod]
        public void OnGet()
        {
            // Given: A blank model page

            var config = new Mock<IOptions<BrandConfig>>();
            config.Setup(x => x.Value).Returns(new BrandConfig());

            var page = new HomeModel(config.Object);

            // When: Calling Get
            page.OnGet();

            // Then: Nothing goes wrong
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void Demo(bool isdemo)
        {
            var brandexists = !isdemo;

            var brand = new BrandConfig();
            if (!isdemo)
                brand.Name = "Is not demo!";

            var config = new Mock<IOptions<BrandConfig>>();
            config.Setup(x => x.Value).Returns(brand);

            var page = new HomeModel(config.Object);

            Assert.AreEqual(isdemo, page.isDemo);
        }
    }
}
