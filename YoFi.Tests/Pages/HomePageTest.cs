using Microsoft.Extensions.Configuration;
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
            var page = new HomeModel(null);

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

            var section = new Mock<IConfigurationSection>();
            section.Setup(x => x.Value).Returns(brandexists ? "Brand" : null);
            section.Setup(x => x.GetChildren()).Returns(brandexists ? new List<IConfigurationSection>() { section.Object } : Enumerable.Empty<IConfigurationSection>());
            var config = new Mock<IConfiguration>();
            config.Setup(x => x.GetSection(It.IsAny<string>())).Returns(section.Object);

            var page = new HomeModel(config.Object);

            Assert.AreEqual(isdemo, page.isDemo);
        }
    }
}
