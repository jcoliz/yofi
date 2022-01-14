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
            var page = new HomeModel(new DemoConfig());

            // When: Calling Get
            page.OnGet();

            // Then: Nothing goes wrong
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void Demo(bool isdemo)
        {
            var page = new HomeModel(new DemoConfig() { IsEnabled = isdemo });

            Assert.AreEqual(isdemo, page.isDemo);
        }
    }
}
